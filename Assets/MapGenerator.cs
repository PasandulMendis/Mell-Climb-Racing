using UnityEngine;
using UnityEngine.U2D; 
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public int seed = 8888; 
    public int chunksToSpawnAhead = 5; 
    public float chunkWidth = 20f;     
    public float destroyDistanceBehind = 50f; 
    public float terrainDepth = 80f;
    
    [Header("Math & Difficulty (Perlin Noise)")]
    public float pointSpacing = 2f; 
    public float baseAmplitude = 5f; 
    public float maxAmplitude = 25f; 
    public float difficultyScaling = 0.02f; 
    public float noiseFrequency = 0.05f; 
    public float secondaryNoiseFrequency = 0.2f; 
    public float secondaryAmplitude = 1f;

    [Header("Prefabs")]
    public GameObject blankChunkPrefab; 
    public GameObject coinPrefab;
    public GameObject fuelPrefab;

    [Header("Destructible Terrain & Bridges")]
    public bool generateBridges = false; 
    public GameObject ropeBridgePrefab;
    public float minBridgeGapWidth = 15f;
    public float maxBridgeGapWidth = 35f;
    public float gorgeDepth = 30f;
    public float expectedPlankWidth = 1.5f;
    public float bridgeYOffset = -0.1f;

    [Header("Fluid Hazards (Water & Mud)")]
    public bool generateWaterPools = false;
    public GameObject waterPoolPrefab;
    public GameObject mudPoolPrefab; 
    [Range(0f, 1f)] public float mudProbability = 0.3f;
    public float minWaterWidth = 15f;
    public float maxWaterWidth = 40f;
    public float waterDepth = 8f;
    public float waterLevelOffset = -2f;
    public int waterSortingOrder = 3;

    [Header("Scatter Obstacles (Logs, Boulders)")]
    public bool generateScatterObstacles = false;
    public GameObject[] scatterPrefabs;
    public float minScatterDistance = 100f;
    public float maxScatterDistance = 250f;

    private Transform playerCar;
    private float nextSpawnX = 0f;
    private List<GameObject> activeChunks = new List<GameObject>();
    private float oldestChunkStartX = 0f;

    private float nextCoinX = 10f; 
    private float nextFuelX = 50f;  
    private int coinsLeftInGroup = 5; 
    
    private float nextBridgeX = 100f;
    private float currentBridgeGapWidth;
    
    private float nextWaterX = 250f;
    private float currentWaterWidth;
    private bool isBuildingWater = false;
    
    private float nextScatterX = 150f;

    void Start()
    {
        Random.InitState(seed);
        nextSpawnX = 0f;
        oldestChunkStartX = 0f;

        float rawNextBridge = 100f + (Mathf.PerlinNoise(seed, seed) * 50f);
        nextBridgeX = Mathf.Round(rawNextBridge / pointSpacing) * pointSpacing;
        float rawGap = Mathf.Lerp(minBridgeGapWidth, maxBridgeGapWidth, Mathf.PerlinNoise(nextBridgeX, seed));
        currentBridgeGapWidth = Mathf.Round(rawGap / pointSpacing) * pointSpacing;

        float rawNextWater = 250f + (Mathf.PerlinNoise(seed + 1, seed) * 100f);
        nextWaterX = Mathf.Round(rawNextWater / pointSpacing) * pointSpacing;
        float rawWaterWidth = Mathf.Lerp(minWaterWidth, maxWaterWidth, Mathf.PerlinNoise(nextWaterX, seed));
        currentWaterWidth = Mathf.Round(rawWaterWidth / pointSpacing) * pointSpacing;

        float rawNextScatter = 150f + (Mathf.PerlinNoise(seed + 2, seed) * 100f);
        nextScatterX = Mathf.Round(rawNextScatter / pointSpacing) * pointSpacing;

        for (int i = 0; i < chunksToSpawnAhead; i++)
        {
            SpawnChunk();
        }
    }

    void Update()
    {
        if (playerCar == null)
        {
            if (GameManager.instance != null && GameManager.instance.playerCar != null)
                playerCar = GameManager.instance.playerCar;
            return;
        }

        while (nextSpawnX - playerCar.position.x < (chunksToSpawnAhead * chunkWidth)) SpawnChunk();

        while (activeChunks.Count > 0)
        {
            float oldestChunkEndX = oldestChunkStartX + chunkWidth;
            if (playerCar.position.x - oldestChunkEndX > destroyDistanceBehind)
            {
                GameObject oldestChunk = activeChunks[0];
                activeChunks.RemoveAt(0);
                Destroy(oldestChunk); 
                oldestChunkStartX += chunkWidth;
            }
            else break; 
        }
    }

    float GetRawTerrainY(float globalX)
    {
        float currentAmplitude = baseAmplitude + (globalX * difficultyScaling);
        currentAmplitude = Mathf.Min(currentAmplitude, maxAmplitude); 

        float noise1 = Mathf.PerlinNoise((globalX + seed) * noiseFrequency, 0f) * currentAmplitude;
        float noise2 = Mathf.PerlinNoise((globalX + seed) * secondaryNoiseFrequency, 0f) * secondaryAmplitude;
        return noise1 + noise2;
    }

    void SpawnChunk()
    {
        float startX = nextSpawnX;
        GameObject newChunk = Instantiate(blankChunkPrefab, new Vector3(startX, 0f, 0f), Quaternion.identity);
        newChunk.transform.parent = this.transform; 
        
        SpriteShapeController ssc = newChunk.GetComponent<SpriteShapeController>();
        Spline spline = ssc.spline;
        spline.Clear();

        EdgeCollider2D edgeCol = newChunk.GetComponent<EdgeCollider2D>();
        if (edgeCol == null) edgeCol = newChunk.AddComponent<EdgeCollider2D>();
        List<Vector2> physicsPoints = new List<Vector2>();

        spline.InsertPointAt(0, new Vector3(0f, -terrainDepth, 0f));
        spline.SetTangentMode(0, ShapeTangentMode.Linear);
        spline.SetCorner(0, false); 

        int numSurfacePoints = Mathf.CeilToInt(chunkWidth / pointSpacing);
        int pointIndex = 1;

        for (int i = 0; i <= numSurfacePoints; i++)
        {
            float percent = (float)i / numSurfacePoints;
            float localX = percent * chunkWidth; 
            float globalX = startX + localX;
            
            float originalY = GetRawTerrainY(globalX);
            float finalY = originalY;

            // --- FLUID POOL LOGIC ---
            bool inWaterZone = generateWaterPools && globalX >= nextWaterX && globalX <= nextWaterX + currentWaterWidth;
            
            if (inWaterZone)
            {
                if (!isBuildingWater)
                {
                    isBuildingWater = true;
                    float startY = GetRawTerrainY(nextWaterX);
                    float endY = GetRawTerrainY(nextWaterX + currentWaterWidth);
                    float midY = (startY + endY) / 2f; 
                    
                    // --- GAP FIXES ---
                    // 1. Visually bleed the width out 15 units to hide vertical side gaps in the dirt walls
                    float paddedVisualWidth = currentWaterWidth + 15f;
                    
                    // 2. Add massive depth padding so it completely buries into the floor
                    float paddedVisualDepth = (waterDepth * 2f) + 20f; 
                    
                    // 3. Anchor the top surface of the mesh flawlessly so depth changes push downwards, not upwards!
                    float surfaceY = midY + waterLevelOffset;
                    float centerOffsetY = surfaceY - (paddedVisualDepth / 2f);
                    
                    Vector2 centerPos = new Vector2(nextWaterX + currentWaterWidth / 2f, centerOffsetY);
                    
                    bool isMud = mudPoolPrefab != null && (Mathf.PerlinNoise(nextWaterX * 0.1f, seed) < mudProbability);
                    
                    // Pass the padded sizes for visuals, and the currentWaterWidth for physics!
                    SpawnFluid(centerPos, paddedVisualWidth, paddedVisualDepth, currentWaterWidth, newChunk.transform, isMud);
                }
                
                float dipPercent = (globalX - nextWaterX) / currentWaterWidth;
                float dipAmount = Mathf.Sin(dipPercent * Mathf.PI) * waterDepth;
                finalY -= dipAmount;
            }
            else if (globalX > nextWaterX + currentWaterWidth && isBuildingWater)
            {
                isBuildingWater = false;
                float rawNextWater = nextWaterX + 250f + (Mathf.PerlinNoise(globalX * 0.2f, seed) * 150f);
                nextWaterX = Mathf.Round(rawNextWater / pointSpacing) * pointSpacing;
                float rawWaterWidth = Mathf.Lerp(minWaterWidth, maxWaterWidth, Mathf.PerlinNoise(nextWaterX, seed));
                currentWaterWidth = Mathf.Round(rawWaterWidth / pointSpacing) * pointSpacing;
            }

            // --- ROPE BRIDGE LOGIC ---
            bool isStartAnchor = generateBridges && Mathf.Abs(globalX - nextBridgeX) < 0.01f;
            bool isEndAnchor = generateBridges && Mathf.Abs(globalX - (nextBridgeX + currentBridgeGapWidth)) < 0.01f;
            bool insideGorge = generateBridges && globalX > nextBridgeX + 0.01f && globalX < (nextBridgeX + currentBridgeGapWidth) - 0.01f;

            if (isStartAnchor)
            {
                Vector3 anchorPoint = new Vector3(localX, originalY, 0f);
                spline.InsertPointAt(pointIndex, anchorPoint);
                physicsPoints.Add(new Vector2(localX, originalY));
                spline.SetTangentMode(pointIndex, ShapeTangentMode.Linear);
                spline.SetCorner(pointIndex, false);
                pointIndex++;

                if (i < numSurfacePoints)
                {
                    float endY = GetRawTerrainY(nextBridgeX + currentBridgeGapWidth);
                    Vector2 startPos = new Vector2(globalX, originalY);
                    Vector2 endPos = new Vector2(nextBridgeX + currentBridgeGapWidth, endY);
                    SpawnBridge(startPos, endPos, newChunk.transform);
                }

                Vector3 dropPoint = new Vector3(localX + 0.01f, originalY - gorgeDepth, 0f);
                spline.InsertPointAt(pointIndex, dropPoint);
                physicsPoints.Add(new Vector2(localX + 0.01f, originalY - gorgeDepth));
                spline.SetTangentMode(pointIndex, ShapeTangentMode.Linear);
                spline.SetCorner(pointIndex, false);
                pointIndex++;
            }
            else if (isEndAnchor)
            {
                Vector3 risePoint = new Vector3(localX - 0.01f, originalY - gorgeDepth, 0f);
                spline.InsertPointAt(pointIndex, risePoint);
                physicsPoints.Add(new Vector2(localX - 0.01f, originalY - gorgeDepth));
                spline.SetTangentMode(pointIndex, ShapeTangentMode.Linear);
                spline.SetCorner(pointIndex, false);
                pointIndex++;

                Vector3 anchorPoint = new Vector3(localX, originalY, 0f);
                spline.InsertPointAt(pointIndex, anchorPoint);
                physicsPoints.Add(new Vector2(localX, originalY));
                spline.SetTangentMode(pointIndex, ShapeTangentMode.Linear);
                spline.SetCorner(pointIndex, false);
                pointIndex++;

                if (i < numSurfacePoints)
                {
                    float rawNextBridge = nextBridgeX + 150f + (Mathf.PerlinNoise(globalX * 0.1f, seed) * 150f);
                    nextBridgeX = Mathf.Round(rawNextBridge / pointSpacing) * pointSpacing;
                    float rawGap = Mathf.Lerp(minBridgeGapWidth, maxBridgeGapWidth, Mathf.PerlinNoise(nextBridgeX, seed));
                    currentBridgeGapWidth = Mathf.Round(rawGap / pointSpacing) * pointSpacing;
                }
            }
            else
            {
                if (insideGorge) finalY -= gorgeDepth;

                Vector3 localPoint = new Vector3(localX, finalY, 0f);
                spline.InsertPointAt(pointIndex, localPoint);
                physicsPoints.Add(new Vector2(localX, finalY));
                spline.SetTangentMode(pointIndex, ShapeTangentMode.Linear);
                spline.SetCorner(pointIndex, false);
                pointIndex++;
            }

            // --- SCATTER OBSTACLE LOGIC ---
            bool isScatterPoint = generateScatterObstacles && Mathf.Abs(globalX - nextScatterX) < 0.01f;
            if (isScatterPoint)
            {
                if (!inWaterZone && !insideGorge && !isStartAnchor && !isEndAnchor && i < numSurfacePoints)
                {
                    if (scatterPrefabs != null && scatterPrefabs.Length > 0)
                    {
                        int prefabIndex = Mathf.FloorToInt(Mathf.PerlinNoise(globalX, seed) * scatterPrefabs.Length);
                        prefabIndex = Mathf.Clamp(prefabIndex, 0, scatterPrefabs.Length - 1);
                        Vector3 spawnPos = new Vector3(globalX, originalY + 1f, 0f);
                        Instantiate(scatterPrefabs[prefabIndex], spawnPos, Quaternion.identity, newChunk.transform);
                    }
                }
                float rawNextScatter = nextScatterX + Mathf.Lerp(minScatterDistance, maxScatterDistance, Mathf.PerlinNoise(globalX * 0.3f, seed));
                nextScatterX = Mathf.Round(rawNextScatter / pointSpacing) * pointSpacing;
            }

            // --- PICKUPS ---
            if (i < numSurfacePoints && !isStartAnchor && !isEndAnchor)
            {
                SpawnPickups(new Vector3(globalX, originalY, 0f), newChunk.transform);
            }
        }

        spline.InsertPointAt(pointIndex, new Vector3(chunkWidth, -terrainDepth, 0f));
        spline.SetTangentMode(pointIndex, ShapeTangentMode.Linear);
        spline.SetCorner(pointIndex, false); 

        ssc.RefreshSpriteShape();
        
        if (spline.GetPointCount() > 0)
        {
            Bounds localBounds = new Bounds(spline.GetPosition(0), Vector3.zero);
            for (int k = 1; k < spline.GetPointCount(); k++)
            {
                localBounds.Encapsulate(spline.GetPosition(k));
            }
            if (ssc.spriteShapeRenderer != null) ssc.spriteShapeRenderer.SetLocalAABB(localBounds);
        }
        
        edgeCol.SetPoints(physicsPoints);
        activeChunks.Add(newChunk);
        nextSpawnX += chunkWidth;
    }

    void SpawnBridge(Vector2 startPos, Vector2 endPos, Transform chunkParent)
    {
        if (ropeBridgePrefab != null)
        {
            GameObject bridge = Instantiate(ropeBridgePrefab, startPos, Quaternion.identity, chunkParent);
            RopeBridge bridgeScript = bridge.GetComponent<RopeBridge>();
            if (bridgeScript != null)
            {
                float distance = Vector2.Distance(startPos, endPos);
                bridgeScript.plankCount = Mathf.Max(2, Mathf.RoundToInt(distance / expectedPlankWidth));

                startPos.y += bridgeYOffset; 
                endPos.y += bridgeYOffset;
                
                bridgeScript.BuildBridge(startPos, endPos);
            }
        }
    }

    // UPDATED to accept physicsWidth so buoyancy triggers don't bleed onto the dirt slopes
    void SpawnFluid(Vector2 centerPos, float visualWidth, float visualDepth, float physicsWidth, Transform chunkParent, bool isMud)
    {
        GameObject prefabToUse = isMud ? mudPoolPrefab : waterPoolPrefab;

        if (prefabToUse != null)
        {
            Vector3 spawnPos = new Vector3(centerPos.x, centerPos.y, 0f);
            GameObject fluid = Instantiate(prefabToUse, spawnPos, Quaternion.identity, chunkParent);
            
            MeshRenderer mr = fluid.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = waterSortingOrder;
            }

            DynamicWater2D fluidScript = fluid.GetComponent<DynamicWater2D>();
            if (fluidScript != null)
            {
                fluid.transform.localScale = Vector3.one; 
                // Pass the true physics width along with the padded visuals
                fluidScript.GenerateWater(visualWidth, visualDepth, physicsWidth);
            }
            else
            {
                fluid.transform.localScale = new Vector3(visualWidth, visualDepth, 1f);
            }
        }
    }

    void SpawnPickups(Vector3 groundPosition, Transform chunkParent)
    {
        if (groundPosition.x >= nextFuelX)
        {
            if (fuelPrefab != null) Instantiate(fuelPrefab, groundPosition + new Vector3(0, 1.5f, 0), Quaternion.identity, chunkParent);
            nextFuelX += 80f + (Mathf.PerlinNoise(groundPosition.x * 0.1f, seed) * 40f); 
        }
        else if (groundPosition.x >= nextCoinX)
        {
            if (coinPrefab != null) Instantiate(coinPrefab, groundPosition + new Vector3(0, 1.0f, 0), Quaternion.identity, chunkParent);
            coinsLeftInGroup--;
            if (coinsLeftInGroup > 0) nextCoinX += pointSpacing; 
            else
            {
                nextCoinX += 15f + (Mathf.PerlinNoise(groundPosition.x * 0.2f, seed) * 20f);
                coinsLeftInGroup = Mathf.FloorToInt(Mathf.PerlinNoise(groundPosition.x * 0.3f, seed) * 4) + 3;
            }
        }
    }
}