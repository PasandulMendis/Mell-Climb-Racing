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

    [Header("Fluid Hazards (Water/Mud)")]
    public bool generateWaterPools = false;
    public GameObject waterPoolPrefab;
    public float minWaterWidth = 15f;
    public float maxWaterWidth = 40f;
    [Tooltip("How deep the U-shaped bowl dips into the earth")]
    public float waterDepth = 8f;
    [Tooltip("Adjusts where the water sits vertically. Positive moves the water line UP.")]
    public float waterLevelOffset = -2f;
    [Tooltip("Set this higher than your car (e.g., 3 if car is 2) but lower than your terrain (e.g., 4) to make the car look submerged!")]
    public int waterSortingOrder = 3;

    private Transform playerCar;
    private float nextSpawnX = 0f;
    private List<GameObject> activeChunks = new List<GameObject>();
    private float oldestChunkStartX = 0f;

    // Spacing Variables
    private float nextCoinX = 10f; 
    private float nextFuelX = 50f;  
    private int coinsLeftInGroup = 5; 
    
    private float nextBridgeX = 100f;
    private float currentBridgeGapWidth;
    
    private float nextWaterX = 250f;
    private float currentWaterWidth;
    private bool isBuildingWater = false;

    void Start()
    {
        Random.InitState(seed);
        nextSpawnX = 0f;
        oldestChunkStartX = 0f;

        // Setup Bridge Seed
        float rawNextBridge = 100f + (Mathf.PerlinNoise(seed, seed) * 50f);
        nextBridgeX = Mathf.Round(rawNextBridge / pointSpacing) * pointSpacing;
        float rawGap = Mathf.Lerp(minBridgeGapWidth, maxBridgeGapWidth, Mathf.PerlinNoise(nextBridgeX, seed));
        currentBridgeGapWidth = Mathf.Round(rawGap / pointSpacing) * pointSpacing;

        // Setup Water Seed (Offset from bridges to reduce overlap)
        float rawNextWater = 250f + (Mathf.PerlinNoise(seed + 1, seed) * 100f);
        nextWaterX = Mathf.Round(rawNextWater / pointSpacing) * pointSpacing;
        float rawWaterWidth = Mathf.Lerp(minWaterWidth, maxWaterWidth, Mathf.PerlinNoise(nextWaterX, seed));
        currentWaterWidth = Mathf.Round(rawWaterWidth / pointSpacing) * pointSpacing;

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

            // --- WATER POOL LOGIC (Smooth U-Shape Dip) ---
            bool inWaterZone = generateWaterPools && globalX >= nextWaterX && globalX <= nextWaterX + currentWaterWidth;
            
            if (inWaterZone)
            {
                if (!isBuildingWater)
                {
                    isBuildingWater = true;
                    // Spawn the visual water and the fluid physics trigger
                    float startY = GetRawTerrainY(nextWaterX);
                    float endY = GetRawTerrainY(nextWaterX + currentWaterWidth);
                    float midY = (startY + endY) / 2f; 
                    
                    Vector2 centerPos = new Vector2(nextWaterX + currentWaterWidth/2f, midY + waterLevelOffset);
                    SpawnWater(centerPos, currentWaterWidth, waterDepth, newChunk.transform);
                }
                
                // MATH MAGIC: A perfect Sine wave creates a smooth, drivable U-Shaped bowl in the dirt!
                float dipPercent = (globalX - nextWaterX) / currentWaterWidth;
                float dipAmount = Mathf.Sin(dipPercent * Mathf.PI) * waterDepth;
                finalY -= dipAmount;
            }
            else if (globalX > nextWaterX + currentWaterWidth && isBuildingWater)
            {
                isBuildingWater = false;
                
                // Calculate next water pool
                float rawNextWater = nextWaterX + 250f + (Mathf.PerlinNoise(globalX * 0.2f, seed) * 150f);
                nextWaterX = Mathf.Round(rawNextWater / pointSpacing) * pointSpacing;
                
                float rawWaterWidth = Mathf.Lerp(minWaterWidth, maxWaterWidth, Mathf.PerlinNoise(nextWaterX, seed));
                currentWaterWidth = Mathf.Round(rawWaterWidth / pointSpacing) * pointSpacing;
            }

            // --- ROPE BRIDGE LOGIC (90-Degree Gorge) ---
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

    void SpawnWater(Vector2 centerPos, float width, float depth, Transform chunkParent)
    {
        if (waterPoolPrefab != null)
        {
            // FIX: Spawn exactly on Z = 0f alongside the car so orthographic clipping works smoothly
            Vector3 spawnPos = new Vector3(centerPos.x, centerPos.y, 0f);
            GameObject water = Instantiate(waterPoolPrefab, spawnPos, Quaternion.identity, chunkParent);
            
            // FIX: Force the mesh renderer to sort based on inspector settings to allow car submersion!
            MeshRenderer mr = water.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = waterSortingOrder;
            }

            DynamicWater2D waterScript = water.GetComponent<DynamicWater2D>();
            if (waterScript != null)
            {
                water.transform.localScale = Vector3.one; 
                waterScript.GenerateWater(width, depth);
            }
            else
            {
                water.transform.localScale = new Vector3(width, depth, 1f);
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