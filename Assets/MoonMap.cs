using UnityEngine;
using UnityEngine.U2D; 
using System.Collections.Generic;

public class MoonMap : MapGenerator
{
    public enum MoonTimeOfDay { Day, Night }

    [Header("--- MOON BASE SETTINGS ---")]
    
    [Header("Moon Physics (Low Gravity)")]
    [Tooltip("Standard Unity gravity is -9.81. The real moon is -1.62, but -4.0 feels much better for 2D driving games!")]
    public float moonGravityY = -4.0f;
    private Vector2 originalGravity;

    [Header("Time of Day Settings")]
    public bool randomizeTime = true;
    public MoonTimeOfDay specificTimeOfDay = MoonTimeOfDay.Day;

    [Header("Sky Backgrounds")]
    public SpriteRenderer skyBackgroundRenderer; 
    public Sprite dayEarthSprite;
    public Sprite nightEarthSprite;
    
    [Header("Color Grading")]
    public Color dayPropTint = Color.white;
    public Color nightPropTint = new Color(0.3f, 0.3f, 0.5f);

    [Header("--- MOON HAZARDS ---")]
    
    [Header("Floating Moon Rocks")]
    public bool generateFloatingRocks = true;
    public GameObject[] floatingRockPrefabs;
    
    [Tooltip("How far apart the swarms are from each other")]
    public float minRockFlockDistance = 150f;
    public float maxRockFlockDistance = 300f;
    
    [Tooltip("How many rocks spawn in a single flock")]
    public int minRocksPerFlock = 10;
    public int maxRocksPerFlock = 20;
    
    [Tooltip("How wide the flock is spread out (X axis)")]
    public float flockSpreadWidth = 40f;
    
    [Tooltip("How high off the ground the rocks spawn (Y axis)")]
    public float minRockHeight = 2f;
    public float maxRockHeight = 15f;

    [Header("Alien UFOs")]
    public bool generateUFOs = true;
    public GameObject ufoPrefab;
    public float minUFODistance = 500f;
    public float maxUFODistance = 1000f;

    private Color propTint = Color.white;
    private bool hasTintedCar = false;
    private MoonTimeOfDay activeTimeOfDay;
    
    private float nextRockFlockX;
    private float nextUFOX;
    private System.Random runRNG;

    void LateUpdate()
    {
        if (!hasTintedCar && playerCar != null)
        {
            hasTintedCar = true;
            TintObject(playerCar.gameObject);

            if (activeTimeOfDay == MoonTimeOfDay.Night)
            {
                CarController cc = playerCar.GetComponent<CarController>();
                if (cc != null) cc.ToggleHeadlights(true);
            }
        }

        if (skyBackgroundRenderer != null && Camera.main != null)
        {
            Vector3 camPos = Camera.main.transform.position;
            skyBackgroundRenderer.transform.position = new Vector3(camPos.x, camPos.y, skyBackgroundRenderer.transform.position.z);
        }
    }

    void SetupTimeOfDay()
    {
        MoonTimeOfDay runTime = specificTimeOfDay;
        if (randomizeTime)
        {
            System.Random trueTimeRNG = new System.Random(System.Environment.TickCount);
            runTime = (MoonTimeOfDay)trueTimeRNG.Next(0, 2); 
        }

        activeTimeOfDay = runTime;

        if (runTime == MoonTimeOfDay.Day)
        {
            propTint = dayPropTint;
            if (skyBackgroundRenderer != null && dayEarthSprite != null) 
                skyBackgroundRenderer.sprite = dayEarthSprite;
            
            if (Camera.main != null) Camera.main.backgroundColor = Color.black; 
        }
        else
        {
            propTint = nightPropTint;
            if (skyBackgroundRenderer != null && nightEarthSprite != null) 
                skyBackgroundRenderer.sprite = nightEarthSprite;
                
            if (Camera.main != null) Camera.main.backgroundColor = new Color(0.05f, 0.05f, 0.05f); 
        }
    }

    void TintObject(GameObject obj)
    {
        if (propTint == Color.white) return;

        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in renderers)
        {
            sr.color *= propTint;
        }
    }

    protected override void OnEnvironmentPropSpawned(GameObject prop)
    {
        TintObject(prop);
    }

    protected override void OnChunkGenerated(GameObject chunk)
    {
        SpriteShapeRenderer ssr = chunk.GetComponent<SpriteShapeRenderer>();
        if (ssr != null)
        {
            Color groundTint = Color.Lerp(Color.white, propTint, 0.5f);
            ssr.color = groundTint;
        }
    }

    protected override void InitializeMap()
    {
        runRNG = new System.Random(seed);
        SetupTimeOfDay();

        originalGravity = Physics2D.gravity;
        Physics2D.gravity = new Vector2(0f, moonGravityY);

        // Initialize the first Moon Rock flock spawn point
        float rawNextFlock = minRockFlockDistance + (Mathf.PerlinNoise(seed + 1, seed) * 50f);
        nextRockFlockX = Mathf.Round(rawNextFlock / pointSpacing) * pointSpacing;

        // Initialize the first UFO trigger point
        float rawNextUFO = minUFODistance + (Mathf.PerlinNoise(seed + 2, seed) * 100f);
        nextUFOX = Mathf.Round(rawNextUFO / pointSpacing) * pointSpacing;
    }

    protected override bool ProcessTerrainSpline(float globalX, float localX, float originalY, ref float finalY, Spline spline, List<Vector2> physicsPoints, ref int pointIndex, int i, int numSurfacePoints, Transform chunkParent, out bool isHazardZone)
    {
        isHazardZone = false;
        return false;
    }

    protected override bool ProcessHazards(float globalX, float originalY, int i, int numSurfacePoints, Transform chunkParent)
    {
        if (i >= numSurfacePoints) return false;

        // --- FLOATING MOON ROCK FLOCKS ---
        if (generateFloatingRocks && Mathf.Abs(globalX - nextRockFlockX) < 0.01f)
        {
            if (floatingRockPrefabs != null && floatingRockPrefabs.Length > 0)
            {
                int flockSize = Random.Range(minRocksPerFlock, maxRocksPerFlock + 1);

                for (int k = 0; k < flockSize; k++)
                {
                    // Randomize the exact position of each rock within the designated "Zone"
                    float offsetX = Random.Range(0f, flockSpreadWidth);
                    float heightOffset = Random.Range(minRockHeight, maxRockHeight);
                    
                    float spawnX = globalX + offsetX;
                    // Get the estimated ground height at that offset so they spawn properly in valleys!
                    float spawnY = GetRawTerrainY(spawnX) + heightOffset;

                    // Pick a random rock prefab and a random rotation!
                    int prefabIndex = Random.Range(0, floatingRockPrefabs.Length);
                    float randomRotation = Random.Range(0f, 360f);

                    GameObject rock = Instantiate(floatingRockPrefabs[prefabIndex], new Vector3(spawnX, spawnY, 0f), Quaternion.Euler(0f, 0f, randomRotation), chunkParent);
                    
                    // Apply day/night tinting to the floating space rocks
                    TintObject(rock);
                }
            }
            
            // Calculate where the NEXT flock will spawn
            nextRockFlockX = Mathf.Round((nextRockFlockX + Mathf.Lerp(minRockFlockDistance, maxRockFlockDistance, Mathf.PerlinNoise(globalX * 0.5f, seed))) / pointSpacing) * pointSpacing;
        }

        // --- ALIEN UFOS (Spawns Behind or Front of the Car) ---
        if (generateUFOs && Mathf.Abs(globalX - nextUFOX) < 0.01f)
        {
            if (ufoPrefab != null && playerCar != null)
            {
                // Use TickCount to guarantee true randomness apart from the map seed
                System.Random trueRNG = new System.Random(System.Environment.TickCount + (int)globalX);
                
                // 50% chance to spawn 40m in front (+40), 50% chance to spawn 40m behind (-40)
                float spawnOffset = (trueRNG.Next(0, 2) == 0) ? 40f : -40f;
                
                float spawnX = playerCar.position.x + spawnOffset;
                float spawnY = playerCar.position.y + 20f;
                
                // NOTICE: We pass 'null' for the parent so it does NOT get destroyed when the chunk unloads!
                GameObject ufo = Instantiate(ufoPrefab, new Vector3(spawnX, spawnY, 0f), Quaternion.identity, null);
                
                // Apply the map's day/night tint to the UFO body so it matches the lighting
                TintObject(ufo);
            }
            
            nextUFOX = Mathf.Round((nextUFOX + Mathf.Lerp(minUFODistance, maxUFODistance, Mathf.PerlinNoise(globalX * 0.7f, seed))) / pointSpacing) * pointSpacing;
        }

        return false; 
    }

    void OnDestroy()
    {
        Physics2D.gravity = originalGravity;
    }
}