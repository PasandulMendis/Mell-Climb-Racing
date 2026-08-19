using UnityEngine;
using UnityEngine.U2D; 
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("Base Generation Settings")]
    public int seed = 8888; 
    public int chunksToSpawnAhead = 5; 
    // NEW: How many chunks to generate behind the starting position (0) at launch
    public int chunksToSpawnBehindAtStart = 3; 
    public float chunkWidth = 20f;
    public float destroyDistanceBehind = 50f; 
    public float terrainDepth = 80f;
    public GameObject blankChunkPrefab; 

    [Header("Player Limits")]
    // NEW: Maximum distance player can reverse from 0 before hitting a wall
    public float maxReverseDistance = 50f; 
    [Tooltip("The name of the layer created in Unity for the invisible wall (e.g., 'InvisibleWall'). Must exist in Edit -> Project Settings -> Tags and Layers.")]
    public string invisibleWallLayerName = "InvisibleWall";
    private GameObject invisibleWall;
    private float maxProgressedX = 0f;

    [Header("Math & Difficulty (Perlin Noise)")]
    public float pointSpacing = 2f; 
    public float baseAmplitude = 5f; 
    public float maxAmplitude = 25f; 
    public float difficultyScaling = 0.02f; 
    public float noiseFrequency = 0.05f; 
    public float secondaryNoiseFrequency = 0.2f; 
    public float secondaryAmplitude = 1f;

    [Header("Pickups (Coins & Fuel)")]
    public GameObject coin5Prefab; 
    public GameObject coin10Prefab; 
    public GameObject coin25Prefab; 
    [Tooltip("Distance between coins in a group. Keeps them spread out even if Point Spacing is low!")]
    public float coinSpacing = 2f; 
    
    [Header("Coin Progression Settings")]
    public float silverCoinDistance = 1000f;
    public float goldCoinDistance = 2000f;
    public int tier1GroupSize = 5;
    public int tier2GroupSize = 8;
    public int tier3GroupSize = 12;

    public GameObject fuelPrefab;

    [Header("Premium Currency (Gems)")]
    public GameObject gemPrefab;
    public float minGemDistance = 300f;
    public float maxGemDistance = 600f;
    public float gemHeightOffset = 1.0f; 

    [Header("Environment Props (Visuals)")]
    public bool generateProps = true;
    public GameObject[] propPrefabs;
    public float propCheckInterval = 5f;
    [Range(0f, 1f)] public float propSpawnProbability = 0.5f;
    public float propYOffset = 0f;
    [Range(0f, 1f)] public float propTerrainAlignment = 0.5f;

    // --- PROTECTED VARIABLES (Children can see these!) ---
    protected Transform playerCar;
    protected float nextSpawnX = 0f;
    protected List<GameObject> activeChunks = new List<GameObject>();
    protected float oldestChunkStartX = 0f;

    protected float nextCoinX = 10f; 
    protected float nextFuelX = 50f;
    protected float nextGemX = 300f;
    protected int coinsLeftInGroup = 5; 
    protected float nextPropX = 5f; 

    void Start()
    {
        Random.InitState(seed);
        
        // MODIFIED: Start generating chunks from behind the zero point to prevent falling if reversing immediately
        float startGenerationX = -(chunksToSpawnBehindAtStart * chunkWidth);
        nextSpawnX = startGenerationX;
        oldestChunkStartX = startGenerationX;

        float rawNextGem = minGemDistance + (Mathf.PerlinNoise(seed + 9, seed) * (maxGemDistance - minGemDistance));
        nextGemX = Mathf.Round(rawNextGem / pointSpacing) * pointSpacing;

        // Don't spawn props or coins behind the starting line if we don't want them there.
        // We leave these at positive values assuming gameplay goes forward.
        nextPropX = pointSpacing;

        // Give the child map a chance to setup its own variables!
        InitializeMap();

        // Spawn initial chunks (behind + ahead)
        int totalInitialChunks = chunksToSpawnBehindAtStart + chunksToSpawnAhead;
        for (int i = 0; i < totalInitialChunks; i++)
        {
            SpawnChunk();
        }

        // Create the invisible wall to restrict reversing
        CreateInvisibleWall();
    }

    private void CreateInvisibleWall()
    {
        invisibleWall = new GameObject("InvisibleReverseWall");
        invisibleWall.transform.parent = this.transform;
        
        // Assign the layer so it only collides with the player (based on Physics2D matrix settings)
        int wallLayer = LayerMask.NameToLayer(invisibleWallLayerName);
        if (wallLayer != -1)
        {
            invisibleWall.layer = wallLayer;
        }
        else
        {
            Debug.LogWarning($"[MapGenerator] Layer '{invisibleWallLayerName}' does not exist! Please create it in Unity's Tags and Layers and update the collision matrix.");
        }

        // Position it initially at max reverse distance from start
        float wallX = -maxReverseDistance;
        invisibleWall.transform.position = new Vector3(wallX, 0f, 0f);

        BoxCollider2D boxCol = invisibleWall.AddComponent<BoxCollider2D>();
        // Make it very tall so the player can't jump over it, and wide enough to stop them
        boxCol.size = new Vector2(2f, 200f); 

        // Set a physics material to make it frictionless so car doesn't stick to it
        PhysicsMaterial2D slipMat = new PhysicsMaterial2D("Slip");
        slipMat.friction = 0f;
        slipMat.bounciness = 0f;
        boxCol.sharedMaterial = slipMat;
    }

    void Update()
    {
        if (playerCar == null)
        {
            if (GameManager.instance != null && GameManager.instance.playerCar != null)
                playerCar = GameManager.instance.playerCar;
            return;
        }

        // Track the maximum distance the player has traveled forward
        if (playerCar.position.x > maxProgressedX)
        {
            maxProgressedX = playerCar.position.x;
        }

        // Update the invisible wall's position to trail behind the max progress
        if (invisibleWall != null)
        {
            float targetWallX = maxProgressedX - maxReverseDistance;
            
            // Adjust the Y position so the wall roughly follows the terrain height, 
            // preventing the car from slipping under it if the terrain drops sharply.
            float terrainY = GetRawTerrainY(targetWallX);
            invisibleWall.transform.position = new Vector3(targetWallX, terrainY, 0f);
        }

        // Generate chunks ahead
        while (nextSpawnX - playerCar.position.x < (chunksToSpawnAhead * chunkWidth)) SpawnChunk();

        // Destroy chunks behind
        while (activeChunks.Count > 0)
        {
            float oldestChunkEndX = oldestChunkStartX + chunkWidth;
            
            // We can destroy a chunk if it is entirely behind the moving invisible wall
            float currentWallX = invisibleWall != null ? invisibleWall.transform.position.x : -maxReverseDistance;

            if (oldestChunkEndX < currentWallX - 10f) // Add a small 10f buffer to be safe
            {
                GameObject oldestChunk = activeChunks[0];
                activeChunks.RemoveAt(0);
                Destroy(oldestChunk); 
                oldestChunkStartX += chunkWidth;
            }
            else break; 
        }
    }

    protected float GetRawTerrainY(float globalX)
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
            bool isHazardZone = false;

            // 1. VIRTUAL HOOK: Let child maps deform the terrain
            bool handledSpline = ProcessTerrainSpline(globalX, localX, originalY, ref finalY, spline, physicsPoints, ref pointIndex, i, numSurfacePoints, newChunk.transform, out isHazardZone);

            // Standard dirt point
            if (!handledSpline)
            {
                Vector3 localPoint = new Vector3(localX, finalY, 0f);
                spline.InsertPointAt(pointIndex, localPoint);
                physicsPoints.Add(new Vector2(localX, finalY));
                spline.SetTangentMode(pointIndex, ShapeTangentMode.Linear);
                spline.SetCorner(pointIndex, false);
                pointIndex++;
            }

            // 2. VIRTUAL HOOK: Let child maps spawn obstacles
            bool spawnedObstacle = false;
            // Don't spawn obstacles behind the starting line (0)
            if (!isHazardZone && globalX >= 0) 
            {
                spawnedObstacle = ProcessHazards(globalX, originalY, i, numSurfacePoints, newChunk.transform);
            }

            // 3. BASE PICKUPS (Coins, Fuel, Gems)
            // Don't spawn pickups behind the starting line (0)
            if (i < numSurfacePoints && !isHazardZone && !spawnedObstacle && globalX >= 0)
            {
                SpawnPickups(new Vector3(globalX, originalY, 0f), newChunk.transform);
            }
            
            // 4. BASE ENVIRONMENT PROPS 
            if (generateProps && globalX >= nextPropX && i < numSurfacePoints)
            {
                if (!isHazardZone)
                {
                    SpawnEnvironmentProps(new Vector3(globalX, originalY, 0f), newChunk.transform);
                }
                else
                {
                    nextPropX += propCheckInterval;
                }
            }
        }

        spline.InsertPointAt(pointIndex, new Vector3(chunkWidth, -terrainDepth, 0f));
        spline.SetTangentMode(pointIndex, ShapeTangentMode.Linear);
        spline.SetCorner(pointIndex, false); 

        ssc.RefreshSpriteShape();
        
        if (spline.GetPointCount() > 0)
        {
            Bounds localBounds = new Bounds(spline.GetPosition(0), Vector3.zero);
            for (int k = 1; k < spline.GetPointCount(); k++) localBounds.Encapsulate(spline.GetPosition(k));
            if (ssc.spriteShapeRenderer != null) ssc.spriteShapeRenderer.SetLocalAABB(localBounds);
        }
        
        edgeCol.SetPoints(physicsPoints);
        OnChunkGenerated(newChunk);
        activeChunks.Add(newChunk);
        nextSpawnX += chunkWidth;
    }

    protected virtual void SpawnPickups(Vector3 groundPosition, Transform chunkParent)
    {
        if (groundPosition.x >= nextFuelX)
        {
            if (fuelPrefab != null) Instantiate(fuelPrefab, groundPosition + new Vector3(0, 1.5f, 0), Quaternion.identity, chunkParent);
            nextFuelX += 80f + (Mathf.PerlinNoise(groundPosition.x * 0.1f, seed) * 40f); 
        }
        else if (gemPrefab != null && groundPosition.x >= nextGemX)
        {
            Instantiate(gemPrefab, groundPosition + new Vector3(0, gemHeightOffset, 0), Quaternion.identity, chunkParent);
            
            float noiseVal = Mathf.PerlinNoise(groundPosition.x * 0.37f, seed + 12f);
            float addedDistance = Mathf.Lerp(minGemDistance, maxGemDistance, noiseVal);
            nextGemX += addedDistance; 
        }
        else if (groundPosition.x >= nextCoinX)
        {
            GameObject coinToSpawn = coin5Prefab;
            if (groundPosition.x >= goldCoinDistance) coinToSpawn = coin25Prefab;
            else if (groundPosition.x >= silverCoinDistance) coinToSpawn = coin10Prefab;

            if (coinToSpawn != null) Instantiate(coinToSpawn, groundPosition + new Vector3(0, 1.0f, 0), Quaternion.identity, chunkParent);
            
            coinsLeftInGroup--;
            if (coinsLeftInGroup > 0) 
            {
                nextCoinX += coinSpacing; 
            }
            else
            {
                nextCoinX += 15f + (Mathf.PerlinNoise(groundPosition.x * 0.2f, seed) * 20f);
                
                int currentBaseSize = tier1GroupSize;
                if (groundPosition.x >= goldCoinDistance) currentBaseSize = tier3GroupSize;
                else if (groundPosition.x >= silverCoinDistance) currentBaseSize = tier2GroupSize;
                
                coinsLeftInGroup = currentBaseSize + Mathf.FloorToInt(Mathf.PerlinNoise(groundPosition.x * 0.3f, seed) * 3);
            }
        }
    }

    protected virtual void SpawnEnvironmentProps(Vector3 groundPosition, Transform chunkParent)
    {
        if (propPrefabs != null && propPrefabs.Length > 0)
        {
            float randomVal = Mathf.PerlinNoise(groundPosition.x * 0.4f, seed + 5.5f);
            if (randomVal < propSpawnProbability)
            {
                int prefabIndex = Mathf.FloorToInt(Mathf.PerlinNoise(groundPosition.x * 12.34f, seed + 6.6f) * propPrefabs.Length);
                prefabIndex = Mathf.Clamp(prefabIndex, 0, propPrefabs.Length - 1);
                
                float zOffset = (Mathf.PerlinNoise(groundPosition.x * 8.1f, seed + 7.7f) > 0.5f) ? Random.Range(3f, 8f) : Random.Range(-2f, -4f);
                Vector3 spawnPos = new Vector3(groundPosition.x, groundPosition.y + propYOffset, zOffset);
                GameObject prop = Instantiate(propPrefabs[prefabIndex], spawnPos, Quaternion.identity, chunkParent);

                SpriteRenderer sr = prop.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) prop.transform.position += new Vector3(0f, spawnPos.y - sr.bounds.min.y, 0f);
                
                float yAhead = GetRawTerrainY(groundPosition.x + 0.1f);
                float yBehind = GetRawTerrainY(groundPosition.x - 0.1f);
                float terrainAngle = Mathf.Atan2((yAhead - yBehind), 0.2f) * Mathf.Rad2Deg;
                
                prop.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, terrainAngle, propTerrainAlignment));
                if (Mathf.PerlinNoise(groundPosition.x * 9.9f, seed + 8.8f) > 0.5f) prop.transform.localScale = new Vector3(-prop.transform.localScale.x, prop.transform.localScale.y, prop.transform.localScale.z);

                OnEnvironmentPropSpawned(prop);
            }
        }
        nextPropX += propCheckInterval;
    }

    // =====================================================================
    // VIRTUAL HOOKS (Child scripts will override these to add unique magic!)
    // =====================================================================
    protected virtual void InitializeMap() { }
    protected virtual bool ProcessTerrainSpline(float globalX, float localX, float originalY, ref float finalY, Spline spline, List<Vector2> physicsPoints, ref int pointIndex, int i, int numSurfacePoints, Transform chunkParent, out bool isHazardZone) { isHazardZone = false; return false; }
    protected virtual bool ProcessHazards(float globalX, float originalY, int i, int numSurfacePoints, Transform chunkParent) { return false; }
    protected virtual void OnEnvironmentPropSpawned(GameObject prop) { }
    protected virtual void OnChunkGenerated(GameObject chunk) { }

}