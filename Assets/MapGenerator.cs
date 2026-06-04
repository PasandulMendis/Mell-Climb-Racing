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
    [Tooltip("How deep the terrain extends downwards. A smaller depth (e.g. 15 or 20) saves mobile rendering performance.")]
    public float terrainDepth = 15f;
    
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

    private Transform playerCar;
    private float nextSpawnX = 0f;
    private List<GameObject> activeChunks = new List<GameObject>();
    private float oldestChunkStartX = 0f;

    // Deterministic Spacing Variables
    private float nextCoinX = 10f; 
    private float nextFuelX = 50f;  
    private int coinsLeftInGroup = 5; 

    void Start()
    {
        Random.InitState(seed);
        nextSpawnX = 0f;
        oldestChunkStartX = 0f;

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
            float localX = percent * chunkWidth; // Will be exactly 0.0 at start, and exactly 20.0 at end!
            float globalX = startX + localX;
            
            float currentAmplitude = baseAmplitude + (globalX * difficultyScaling);
            currentAmplitude = Mathf.Min(currentAmplitude, maxAmplitude); 

            float noise1 = Mathf.PerlinNoise((globalX + seed) * noiseFrequency, 0f) * currentAmplitude;
            float noise2 = Mathf.PerlinNoise((globalX + seed) * secondaryNoiseFrequency, 0f) * secondaryAmplitude;
            float finalY = noise1 + noise2;

            Vector3 localPoint = new Vector3(localX, finalY, 0f);
            spline.InsertPointAt(pointIndex, localPoint);
            
            // Add point to our bulletproof physics line
            physicsPoints.Add(new Vector2(localX, finalY));
            
            // Use Linear tangent mode
            spline.SetTangentMode(pointIndex, ShapeTangentMode.Linear);
            spline.SetCorner(pointIndex, false); // Disable corners on the top edge points

            // Only try to spawn pickups if we are NOT on the exact last connecting seam
            if (i < numSurfacePoints)
            {
                SpawnPickups(new Vector3(globalX, finalY, 0f), newChunk.transform);
            }

            pointIndex++;
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