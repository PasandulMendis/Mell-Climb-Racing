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
    [Tooltip("Increased to 80 to completely hide the bottom of the world during deep gorges!")]
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
    [Tooltip("Minimum random width of the bridge gap")]
    public float minBridgeGapWidth = 15f;
    [Tooltip("Maximum random width of the bridge gap")]
    public float maxBridgeGapWidth = 35f;
    [Tooltip("How deep the terrain drops into a gorge under the bridge")]
    public float gorgeDepth = 30f;
    
    [Tooltip("Approximate width of a single plank. Used to calculate how many planks are needed for random gaps.")]
    public float expectedPlankWidth = 1.5f;
    [Tooltip("Adjust this to make the bridge perfectly flush with the dirt. (Use a negative number to sink the bridge slightly)")]
    public float bridgeYOffset = -0.1f;

    private Transform playerCar;
    private float nextSpawnX = 0f;
    private List<GameObject> activeChunks = new List<GameObject>();
    private float oldestChunkStartX = 0f;

    // Deterministic Spacing Variables
    private float nextCoinX = 10f; 
    private float nextFuelX = 50f;  
    private int coinsLeftInGroup = 5; 
    
    // Bridge Spacing Variables
    private float nextBridgeX = 100f;
    private float currentBridgeGapWidth;

    void Start()
    {
        Random.InitState(seed);
        nextSpawnX = 0f;
        oldestChunkStartX = 0f;

        // Deterministically calculate the first bridge based on map seed!
        float rawNextBridge = 100f + (Mathf.PerlinNoise(seed, seed) * 50f);
        // Snap strictly to the pointSpacing grid to prevent alignment bugs!
        nextBridgeX = Mathf.Round(rawNextBridge / pointSpacing) * pointSpacing;
        
        float rawGap = Mathf.Lerp(minBridgeGapWidth, maxBridgeGapWidth, Mathf.PerlinNoise(nextBridgeX, seed));
        currentBridgeGapWidth = Mathf.Round(rawGap / pointSpacing) * pointSpacing;

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

        // Keep generating chunks as long as the end of the track is too close
        while (nextSpawnX - playerCar.position.x < (chunksToSpawnAhead * chunkWidth))
        {
            SpawnChunk();
        }

        // Clean up chunks safely behind us
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
            else
            {
                break; 
            }
        }
    }

    // --- NEW HELPER: Extract raw Y calculation so we can predict cliffs! ---
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

        // 1. MANUALLY GRAB THE PHYSICS COLLIDER
        EdgeCollider2D edgeCol = newChunk.GetComponent<EdgeCollider2D>();
        if (edgeCol == null) edgeCol = newChunk.AddComponent<EdgeCollider2D>();
        List<Vector2> physicsPoints = new List<Vector2>();

        // Bottom Left Corner (Now using adjustable terrainDepth)
        spline.InsertPointAt(0, new Vector3(0f, -terrainDepth, 0f));
        spline.SetTangentMode(0, ShapeTangentMode.Linear);
        spline.SetCorner(0, false); // FORCE DISABLING CORNERS TO PREVENT WHITE BOX GLITCH

        // 2. FLAWLESS PERCENTAGE MATH (Guarantees exactly hitting the Chunk Width)
        int numSurfacePoints = Mathf.CeilToInt(chunkWidth / pointSpacing);
        int pointIndex = 1;

        for (int i = 0; i <= numSurfacePoints; i++)
        {
            float percent = (float)i / numSurfacePoints;
            float localX = percent * chunkWidth; 
            float globalX = startX + localX;
            
            float originalY = GetRawTerrainY(globalX);
            float finalY = originalY;

            // Track exactly where we are relative to the current bridge gorge
            bool isStartAnchor = generateBridges && Mathf.Abs(globalX - nextBridgeX) < 0.01f;
            bool isEndAnchor = generateBridges && Mathf.Abs(globalX - (nextBridgeX + currentBridgeGapWidth)) < 0.01f;
            bool insideGorge = generateBridges && globalX > nextBridgeX + 0.01f && globalX < (nextBridgeX + currentBridgeGapWidth) - 0.01f;

            if (isStartAnchor)
            {
                // 1. Anchor Point (High Ground)
                Vector3 anchorPoint = new Vector3(localX, originalY, 0f);
                spline.InsertPointAt(pointIndex, anchorPoint);
                physicsPoints.Add(new Vector2(localX, originalY));
                spline.SetTangentMode(pointIndex, ShapeTangentMode.Linear);
                spline.SetCorner(pointIndex, false);
                pointIndex++;

                // 2. Spawn Bridge visually (Only once per physical anchor to avoid duplicates on chunk seams)
                if (i < numSurfacePoints)
                {
                    float endY = GetRawTerrainY(nextBridgeX + currentBridgeGapWidth);
                    Vector2 startPos = new Vector2(globalX, originalY);
                    Vector2 endPos = new Vector2(nextBridgeX + currentBridgeGapWidth, endY);
                    SpawnBridge(startPos, endPos, newChunk.transform);
                }

                // 3. FIX: Drop Point (Low) - This instantly forces a 90 degree cliff edge!
                Vector3 dropPoint = new Vector3(localX + 0.01f, originalY - gorgeDepth, 0f);
                spline.InsertPointAt(pointIndex, dropPoint);
                physicsPoints.Add(new Vector2(localX + 0.01f, originalY - gorgeDepth));
                spline.SetTangentMode(pointIndex, ShapeTangentMode.Linear);
                spline.SetCorner(pointIndex, false);
                pointIndex++;
            }
            else if (isEndAnchor)
            {
                // 1. FIX: Rise Point (Low) - This instantly forces the other 90 degree cliff edge!
                Vector3 risePoint = new Vector3(localX - 0.01f, originalY - gorgeDepth, 0f);
                spline.InsertPointAt(pointIndex, risePoint);
                physicsPoints.Add(new Vector2(localX - 0.01f, originalY - gorgeDepth));
                spline.SetTangentMode(pointIndex, ShapeTangentMode.Linear);
                spline.SetCorner(pointIndex, false);
                pointIndex++;

                // 2. Anchor Point (High Ground)
                Vector3 anchorPoint = new Vector3(localX, originalY, 0f);
                spline.InsertPointAt(pointIndex, anchorPoint);
                physicsPoints.Add(new Vector2(localX, originalY));
                spline.SetTangentMode(pointIndex, ShapeTangentMode.Linear);
                spline.SetCorner(pointIndex, false);
                pointIndex++;

                // 3. Setup Next Bridge mathematically
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
                // Standard terrain (Or keeping the terrain flat down inside the gorge)
                if (insideGorge) finalY -= gorgeDepth;

                Vector3 localPoint = new Vector3(localX, finalY, 0f);
                spline.InsertPointAt(pointIndex, localPoint);
                physicsPoints.Add(new Vector2(localX, finalY));
                spline.SetTangentMode(pointIndex, ShapeTangentMode.Linear);
                spline.SetCorner(pointIndex, false);
                pointIndex++;
            }

            // Only try to spawn pickups if we are NOT on the exact last connecting seam
            // AND not exactly on the bridge anchors (to avoid weird floating physics)
            if (i < numSurfacePoints && !isStartAnchor && !isEndAnchor)
            {
                // Notice we pass "originalY", so if a coin spawns in a gorge, it floats safely UP on the bridge!
                SpawnPickups(new Vector3(globalX, originalY, 0f), newChunk.transform);
            }
        }

        // Bottom Right Corner (Now using adjustable terrainDepth)
        spline.InsertPointAt(pointIndex, new Vector3(chunkWidth, -terrainDepth, 0f));
        spline.SetTangentMode(pointIndex, ShapeTangentMode.Linear);
        spline.SetCorner(pointIndex, false); // Disable corners on the bottom right point

        // Refresh structural assets first
        ssc.RefreshSpriteShape();
        
        // --- CRITICAL BOUNDING BOX FIX ---
        if (spline.GetPointCount() > 0)
        {
            Bounds localBounds = new Bounds(spline.GetPosition(0), Vector3.zero);
            for (int k = 1; k < spline.GetPointCount(); k++)
            {
                localBounds.Encapsulate(spline.GetPosition(k));
            }
            
            // Push these updated, accurate bounds directly to the SpriteShapeRenderer!
            if (ssc.spriteShapeRenderer != null)
            {
                ssc.spriteShapeRenderer.SetLocalAABB(localBounds);
            }
        }
        
        // 3. APPLY THE BULLETPROOF PHYSICS LINE
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
                // FIX 1: Dynamically calculate the exact number of planks needed based on gap distance
                float distance = Vector2.Distance(startPos, endPos);
                bridgeScript.plankCount = Mathf.Max(2, Mathf.RoundToInt(distance / expectedPlankWidth));

                // FIX 2: Apply the customizable offset so the bridge sits flush with the road
                startPos.y += bridgeYOffset; 
                endPos.y += bridgeYOffset;
                
                bridgeScript.BuildBridge(startPos, endPos);
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