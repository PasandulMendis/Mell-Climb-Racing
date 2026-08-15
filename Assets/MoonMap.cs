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

    [Header("Black Holes (Portals)")]
    public bool generateBlackHoles = true;
    public GameObject blackHolePrefab;
    public float minBlackHoleDistance = 600f;
    public float maxBlackHoleDistance = 1200f;
    [Tooltip("How far apart Portal A and Portal B spawn from each other")]
    public float portalLinkDistance = 50f;
    [Tooltip("Minimum height above the ground the portals can spawn")]
    public float minPortalHeight = 0f;
    public float maxPortalHeight = 15f;

    [Header("Moon Geysers (Hover Vents)")]
    public bool generateGeysers = true;
    public GameObject geyserPrefab;
    public float minGeyserDistance = 250f;
    public float maxGeyserDistance = 500f;
    [Tooltip("Negative values sink it into the ground, positive lift it up.")]
    public float geyserYOffset = -0.2f;
    [Range(0f, 1f)] 
    [Tooltip("1.0 = Perfectly flat against the slope. 0.0 = Pointing straight up to space.")]
    public float geyserTerrainAlignment = 1.0f;

    [Header("Falling Meteorites")]
    public bool generateMeteorites = true;
    public GameObject meteoritePrefab;
    public float minMeteoriteDistance = 600f;
    public float maxMeteoriteDistance = 1500f;
    [Tooltip("How high in the sky the meteorite spawns and waits for the player")]
    public float meteoriteAltitude = 35f;

    [Header("--- HAZARD ZONE (MOON BASE) ---")]
    public bool generateHazardZones = true;
    [Tooltip("The flag planted at the very beginning of the ruined base")]
    public GameObject warningFlagPrefab;
    
    [Tooltip("Vertical height offset for the warning flag. Increase this value if the flag buries into the road!")]
    public float warningFlagYOffset = 0.5f;

    [Tooltip("Attach your FloatingMoonRock.cs script to these prefabs too!")]
    public GameObject[] spaceDebrisPrefabs;
    
    public float minZoneDistance = 1000f;
    public float maxZoneDistance = 2000f;
    public float minZoneLength = 400f;
    public float maxZoneLength = 800f;
    
    [Tooltip("How far apart the dense debris flocks spawn inside the zone")]
    public float debrisFlockDistance = 60f; 
    public int minDebrisPerFlock = 4;
    public int maxDebrisPerFlock = 8;

    private Color propTint = Color.white;
    private bool hasTintedCar = false;
    private MoonTimeOfDay activeTimeOfDay;
    
    private float nextRockFlockX;
    private float nextUFOX;
    private float nextBlackHoleX;
    private float nextGeyserX;
    private float nextMeteoriteX;
    private System.Random runRNG;

    // --- Hazard Zone Trackers ---
    private float nextHazardZoneX;
    private float currentZoneLength;
    private float nextDebrisX;
    private bool inHazardZoneFlag = false;

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

        // Initialize the first Black Hole point
        float rawNextBlackHole = minBlackHoleDistance + (Mathf.PerlinNoise(seed + 3, seed) * 100f);
        nextBlackHoleX = Mathf.Round(rawNextBlackHole / pointSpacing) * pointSpacing;
        
        // Initialize the first Geyser point
        float rawNextGeyser = minGeyserDistance + (Mathf.PerlinNoise(seed + 4, seed) * 100f);
        nextGeyserX = Mathf.Round(rawNextGeyser / pointSpacing) * pointSpacing;
       
        // Initialize the first Meteorite point
        float rawNextMeteorite = minMeteoriteDistance + (Mathf.PerlinNoise(seed + 7, seed) * 200f);
        nextMeteoriteX = Mathf.Round(rawNextMeteorite / pointSpacing) * pointSpacing;

        // Initialize the first Hazard Zone
        float rawNextZone = minZoneDistance + (Mathf.PerlinNoise(seed + 5, seed) * 200f);
        nextHazardZoneX = Mathf.Round(rawNextZone / pointSpacing) * pointSpacing;
        currentZoneLength = Mathf.Round(Mathf.Lerp(minZoneLength, maxZoneLength, Mathf.PerlinNoise(seed + 6, seed)) / pointSpacing) * pointSpacing;
        nextDebrisX = nextHazardZoneX + 20f;
    }

    protected override bool ProcessTerrainSpline(float globalX, float localX, float originalY, ref float finalY, Spline spline, List<Vector2> physicsPoints, ref int pointIndex, int i, int numSurfacePoints, Transform chunkParent, out bool isHazardZone)
    {
        isHazardZone = false;

        bool inZone = generateHazardZones && globalX >= nextHazardZoneX && globalX <= nextHazardZoneX + currentZoneLength;

        // 1. PROPS ONLY SPAWN IN THE HAZARD ZONE!
        generateProps = inZone;

        // 2. ZONE ENTER / EXIT LOGIC
        if (inZone && !inHazardZoneFlag)
        {
            inHazardZoneFlag = true;
            
            // Fast-forward the prop spawner so it doesn't dump 20 "missed" props at once
            nextPropX = globalX;
            
            // Plant the warning flag at the start of the zone with dedicated Y offset!
            if (warningFlagPrefab != null)
            {
                Vector3 flagPos = new Vector3(globalX, originalY + warningFlagYOffset, 0f);
                GameObject flag = Instantiate(warningFlagPrefab, flagPos, Quaternion.identity, chunkParent);
                TintObject(flag);
            }
        }
        else if (globalX > nextHazardZoneX + currentZoneLength && inHazardZoneFlag)
        {
            inHazardZoneFlag = false;
            
            // Calculate next zone
            nextHazardZoneX = Mathf.Round((globalX + Mathf.Lerp(minZoneDistance, maxZoneDistance, Mathf.PerlinNoise(globalX * 0.3f, seed))) / pointSpacing) * pointSpacing;
            currentZoneLength = Mathf.Round(Mathf.Lerp(minZoneLength, maxZoneLength, Mathf.PerlinNoise(nextHazardZoneX, seed)) / pointSpacing) * pointSpacing;
            nextDebrisX = nextHazardZoneX + 20f;
        }

        return false;
    }

    protected override bool ProcessHazards(float globalX, float originalY, int i, int numSurfacePoints, Transform chunkParent)
    {
        if (i >= numSurfacePoints) return false;

        bool inZone = generateHazardZones && globalX >= nextHazardZoneX && globalX <= nextHazardZoneX + currentZoneLength;

        // ==========================================
        // HAZARD ZONE LOGIC (Debris Only)
        // ==========================================
        if (inZone)
        {
            // Spawn heavy flocks of floating space debris
            if (Mathf.Abs(globalX - nextDebrisX) < 0.01f)
            {
                if (spaceDebrisPrefabs != null && spaceDebrisPrefabs.Length > 0)
                {
                    int flockSize = Random.Range(minDebrisPerFlock, maxDebrisPerFlock + 1);

                    for (int k = 0; k < flockSize; k++)
                    {
                        float offsetX = Random.Range(0f, debrisFlockDistance);
                        float heightOffset = Random.Range(minRockHeight, maxRockHeight);
                        float spawnX = globalX + offsetX;
                        float spawnY = GetRawTerrainY(spawnX) + heightOffset;

                        int prefabIndex = Random.Range(0, spaceDebrisPrefabs.Length);
                        float randomRotation = Random.Range(0f, 360f);

                        // CHANGED: Parent set to null instead of chunkParent so they don't vanish instantly when chunk is deleted
                        GameObject debris = Instantiate(spaceDebrisPrefabs[prefabIndex], new Vector3(spawnX, spawnY, 0f), Quaternion.Euler(0f, 0f, randomRotation), null);
                        TintObject(debris);
                        
                        // FIX: If the space debris prefabs are missing the cleanup script in the Unity Editor, 
                        // forcefully attach our distance destroyer script here so they don't live in memory forever!
                        if (debris.GetComponent<FloatingMoonRock>() == null && debris.GetComponent<DebrisDistanceDestroyer>() == null)
                        {
                            debris.AddComponent<DebrisDistanceDestroyer>();
                        }
                    }
                }
                
                nextDebrisX = Mathf.Round((nextDebrisX + debrisFlockDistance) / pointSpacing) * pointSpacing;
            }
            
            // Return false so Coins and Fuel still spawn on the ground below the debris!
            return false; 
        }

        // ==========================================
        // NORMAL MOON LOGIC (No Debris, Props off)
        // ==========================================

        // --- FLOATING MOON ROCK FLOCKS ---
        if (generateFloatingRocks && Mathf.Abs(globalX - nextRockFlockX) < 0.01f)
        {
            if (floatingRockPrefabs != null && floatingRockPrefabs.Length > 0)
            {
                int flockSize = Random.Range(minRocksPerFlock, maxRocksPerFlock + 1);

                for (int k = 0; k < flockSize; k++)
                {
                    float offsetX = Random.Range(0f, flockSpreadWidth);
                    float heightOffset = Random.Range(minRockHeight, maxRockHeight);

                    float spawnX = globalX + offsetX;
                    float spawnY = GetRawTerrainY(spawnX) + heightOffset;

                    int prefabIndex = Random.Range(0, floatingRockPrefabs.Length);
                    float randomRotation = Random.Range(0f, 360f);

                    // CHANGED: Parent set to null instead of chunkParent so they don't vanish instantly when chunk is deleted
                    GameObject rock = Instantiate(floatingRockPrefabs[prefabIndex], new Vector3(spawnX, spawnY, 0f), Quaternion.Euler(0f, 0f, randomRotation), null);
                    TintObject(rock);
                }
            }

            nextRockFlockX = Mathf.Round((nextRockFlockX + Mathf.Lerp(minRockFlockDistance, maxRockFlockDistance, Mathf.PerlinNoise(globalX * 0.4f, seed))) / pointSpacing) * pointSpacing;
        }

        // --- ALIEN UFOS ---
        if (generateUFOs && Mathf.Abs(globalX - nextUFOX) < 0.01f)
        {
            if (ufoPrefab != null && playerCar != null)
            {
                System.Random trueRNG = new System.Random(System.Environment.TickCount + (int)globalX);
                float spawnOffset = (trueRNG.Next(0, 2) == 0) ? 40f : -40f;

                float spawnX = playerCar.position.x + spawnOffset;
                float spawnY = playerCar.position.y + 20f;

                GameObject ufo = Instantiate(ufoPrefab, new Vector3(spawnX, spawnY, 0f), Quaternion.identity, null);
                TintObject(ufo);
            }

            nextUFOX = Mathf.Round((nextUFOX + Mathf.Lerp(minUFODistance, maxUFODistance, Mathf.PerlinNoise(globalX * 0.7f, seed))) / pointSpacing) * pointSpacing;
        }

        // --- BLACK HOLES (PORTALS) ---
        if (generateBlackHoles && Mathf.Abs(globalX - nextBlackHoleX) < 0.01f)
        {
            if (blackHolePrefab != null)
            {
                System.Random trueRNG = new System.Random(System.Environment.TickCount + (int)globalX);

                float heightA = originalY + Mathf.Lerp(minPortalHeight, maxPortalHeight, (float)trueRNG.NextDouble());
                // Portals are fine attached to chunks because they don't move with the car
                GameObject portalA = Instantiate(blackHolePrefab, new Vector3(globalX, heightA, 0f), Quaternion.identity, chunkParent);

                float destX = globalX + portalLinkDistance;
                float destGroundY = GetRawTerrainY(destX);
                float heightB = destGroundY + Mathf.Lerp(minPortalHeight, maxPortalHeight, (float)trueRNG.NextDouble());

                GameObject portalB = Instantiate(blackHolePrefab, new Vector3(destX, heightB, 0f), Quaternion.identity, chunkParent);

                BlackHolePortal scriptA = portalA.GetComponent<BlackHolePortal>();
                BlackHolePortal scriptB = portalB.GetComponent<BlackHolePortal>();

                if (scriptA != null && scriptB != null)
                {
                    scriptA.linkedPortal = scriptB;
                    scriptB.linkedPortal = scriptA;
                }

                TintObject(portalA);
                TintObject(portalB);
            }

            System.Random nextRng = new System.Random(System.Environment.TickCount);
            nextBlackHoleX = Mathf.Round((nextBlackHoleX + Mathf.Lerp(minBlackHoleDistance, maxBlackHoleDistance, (float)nextRng.NextDouble())) / pointSpacing) * pointSpacing;

            return true;
        }

        // --- GEYSERS ---
        if (generateGeysers && Mathf.Abs(globalX - nextGeyserX) < 0.01f)
        {
            if (geyserPrefab != null)
            {
                Vector3 spawnPos = new Vector3(globalX, originalY + geyserYOffset, 0f);
                // Geysers are fine attached to chunks because they don't move
                GameObject geyser = Instantiate(geyserPrefab, spawnPos, Quaternion.identity, chunkParent);

                float yAhead = GetRawTerrainY(globalX + 0.1f);
                float yBehind = GetRawTerrainY(globalX - 0.1f);

                float terrainAngle = Mathf.Atan2((yAhead - yBehind), 0.2f) * Mathf.Rad2Deg;
                geyser.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, terrainAngle, geyserTerrainAlignment));

                TintObject(geyser);
            }
            nextGeyserX = Mathf.Round((nextGeyserX + Mathf.Lerp(minGeyserDistance, maxGeyserDistance, Mathf.PerlinNoise(globalX * 0.9f, seed))) / pointSpacing) * pointSpacing;
            return true;
        }

        // --- METEORITES ---
        if (generateMeteorites && Mathf.Abs(globalX - nextMeteoriteX) < 0.01f)
        {
            if (meteoritePrefab != null)
            {
                GameObject meteor = Instantiate(meteoritePrefab, new Vector3(globalX, originalY + meteoriteAltitude, 0f), Quaternion.identity, null);
                TintObject(meteor);
            }
            nextMeteoriteX = Mathf.Round((nextMeteoriteX + Mathf.Lerp(minMeteoriteDistance, maxMeteoriteDistance, Mathf.PerlinNoise(globalX * 0.77f, seed))) / pointSpacing) * pointSpacing;
        }

        return false;
    }

    void OnDestroy()
    {
        Physics2D.gravity = originalGravity;
    }
}