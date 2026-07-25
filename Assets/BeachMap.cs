using UnityEngine;
using UnityEngine.U2D; 
using System.Collections.Generic;

public class BeachMap : MapGenerator
{
        public enum TimeOfDay { Dawn, Morning, Noon, Dusk, Night }

    [System.Serializable]
    public class TimeColors
    {
        [Tooltip("Changes the Sky background color")]
        public Color skyColor;
        [Tooltip("Multiplies the sprite color to match the lighting")]
        public Color propTint;

        public TimeColors(Color sky, Color prop)
        {
            skyColor = sky;
            propTint = prop;
        }
    }

    [Header("--- TIME OF DAY SETTINGS ---")]
    public bool enableStaticTime = true;
    [Tooltip("If true, picks a random time. If false, uses the specific time below.")]
    public bool randomizeTime = true;
    public TimeOfDay specificTimeOfDay = TimeOfDay.Noon;

    [Tooltip("Drag your unique white Sky background sprite here!")]
    public SpriteRenderer skyBackgroundRenderer; 
    
    public TimeColors dawnColors = new TimeColors(new Color(0.9f, 0.6f, 0.5f), new Color(0.8f, 0.7f, 0.7f));
    public TimeColors morningColors = new TimeColors(new Color(0.6f, 0.8f, 1.0f), Color.white);
    public TimeColors noonColors = new TimeColors(new Color(0.4f, 0.7f, 1.0f), Color.white);
    public TimeColors duskColors = new TimeColors(new Color(0.7f, 0.4f, 0.6f), new Color(0.7f, 0.5f, 0.6f));
    public TimeColors nightColors = new TimeColors(new Color(0.1f, 0.1f, 0.2f), new Color(0.3f, 0.3f, 0.5f));

    private Color propTint = Color.white;
    private bool hasTintedCar = false;


    [Header("--- BEACH SPECIFIC FEATURES ---")]
    
    [Header("Fluid Hazards (Water & Sand Dunes)")]
    public bool generateFluids = true;
    public GameObject waterPoolPrefab;
    public GameObject sandDunePrefab; 
    [Tooltip("50% chance for Water, 50% for Sand Dunes")]
    [Range(0f, 1f)] public float sandDuneChance = 0.5f;
    public float minWaterWidth = 15f;
    public float maxWaterWidth = 40f;
    public float waterDepth = 8f;
    public float waterLevelOffset = -2f;
    public int waterSortingOrder = 3;
    
    [Header("Buoys (Water Only)")]
    public bool generateBuoys = true;
    public GameObject buoyPrefab;

    [Header("Bouncy Hazards (Umbrellas/Tires)")]
    public bool generateBouncyHazards = true;
    public GameObject bouncyHazardPrefab;
    public float minBounceDistance = 200f;
    public float maxBounceDistance = 400f;
    public float bouncyYOffset = 0.5f;

    [Header("Giant Beach Balls")]
    public bool generateBeachBalls = true;
    public GameObject beachBallPrefab;
    public float minBallDistance = 200f;
    public float maxBallDistance = 450f;
    [Tooltip("How big the beach balls can get late-game!")]
    public float maxBallScale = 4f;

    [Header("Seagull Flocks (Sky Trolls)")]
    public bool generateSeagulls = true;
    public GameObject seagullPrefab;
    public float minGullDistance = 300f;
    public float maxGullDistance = 700f;
    [Tooltip("Minimum height above the ground the flock spawns")]
    public float minGullHeight = 25f;
    [Tooltip("Maximum height above the ground the flock spawns")]
    public float maxGullHeight = 40f;

    [Header("Coconut Piles")]
    public bool generateCoconuts = true;
    public GameObject coconutPrefab;
    public float minCoconutDistance = 200f;
    public float maxCoconutDistance = 500f;
    
    // Internal trackers
    private float nextFluidX;
    private float currentFluidWidth;
    private bool isBuildingFluid = false;
    
    private float nextBounceX; 
    private float nextBallX;
    private float nextGullX;
    private float nextCoconutX;
    
    private System.Random runRNG;

        void LateUpdate()
    {
        if (enableStaticTime && !hasTintedCar && playerCar != null)
        {
            hasTintedCar = true;
            TintObject(playerCar.gameObject);
        }

        if (skyBackgroundRenderer != null && Camera.main != null)
        {
            Vector3 camPos = Camera.main.transform.position;
            skyBackgroundRenderer.transform.position = new Vector3(camPos.x, camPos.y, skyBackgroundRenderer.transform.position.z);
        }
    }

    void SetupTimeOfDay()
    {
        TimeOfDay runTime = specificTimeOfDay;
        if (randomizeTime)
        {
            System.Random trueTimeRNG = new System.Random(System.Environment.TickCount);
            runTime = (TimeOfDay)trueTimeRNG.Next(0, 5); 
        }

        Color skyColor = Color.white;
        switch (runTime)
        {
            case TimeOfDay.Dawn: skyColor = dawnColors.skyColor; propTint = dawnColors.propTint; break;
            case TimeOfDay.Morning: skyColor = morningColors.skyColor; propTint = morningColors.propTint; break;
            case TimeOfDay.Noon: skyColor = noonColors.skyColor; propTint = noonColors.propTint; break;
            case TimeOfDay.Dusk: skyColor = duskColors.skyColor; propTint = duskColors.propTint; break;
            case TimeOfDay.Night: skyColor = nightColors.skyColor; propTint = nightColors.propTint; break;
        }

        if (Camera.main != null) Camera.main.backgroundColor = skyColor;
        if (skyBackgroundRenderer != null) skyBackgroundRenderer.color = skyColor;
        
        Debug.Log("Beach Map started at: " + runTime.ToString());
    }

    void TintObject(GameObject obj)
    {
        if (propTint == Color.white) return; 
        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in renderers) sr.color *= propTint;
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
            Color groundTint = Color.Lerp(Color.white, propTint, 0.75f);
            ssr.color = groundTint;
        }
    }



    protected override void InitializeMap()
    {
        runRNG = new System.Random(seed);

                if (enableStaticTime)
        {
            SetupTimeOfDay();
        }

        // First Fluid (Water/Sand)
        float rawNextFluid = 250f + (Mathf.PerlinNoise(seed + 1, seed) * 100f);
        nextFluidX = Mathf.Round(rawNextFluid / pointSpacing) * pointSpacing;
        currentFluidWidth = Mathf.Round(Mathf.Lerp(minWaterWidth, maxWaterWidth, 0.5f) / pointSpacing) * pointSpacing;

        nextBounceX = Mathf.Round((minBounceDistance + (Mathf.PerlinNoise(seed + 2, seed) * 50f)) / pointSpacing) * pointSpacing;
        nextBallX = Mathf.Round((minBallDistance + (Mathf.PerlinNoise(seed + 3, seed) * 50f)) / pointSpacing) * pointSpacing;
        nextGullX = Mathf.Round((minGullDistance + (Mathf.PerlinNoise(seed + 4, seed) * 50f)) / pointSpacing) * pointSpacing;
        nextCoconutX = Mathf.Round((minCoconutDistance + (Mathf.PerlinNoise(seed + 5, seed) * 50f)) / pointSpacing) * pointSpacing;
    }

    protected override bool ProcessTerrainSpline(float globalX, float localX, float originalY, ref float finalY, Spline spline, List<Vector2> physicsPoints, ref int pointIndex, int i, int numSurfacePoints, Transform chunkParent, out bool isHazardZone)
    {
        isHazardZone = false;

        // --- FLUID LOGIC (Water or Sand Dunes) ---
        bool inFluidZone = generateFluids && globalX >= nextFluidX && globalX <= nextFluidX + currentFluidWidth;
        if (inFluidZone)
        {
            isHazardZone = true;
            if (!isBuildingFluid)
            {
                isBuildingFluid = true;
                float startY = GetRawTerrainY(nextFluidX);
                float endY = GetRawTerrainY(nextFluidX + currentFluidWidth);
                float midY = (startY + endY) / 2f; 
                
                // FIX: Dynamic padding! Tiny puddles get tiny overlaps so they don't bleed out of the ground
                float edgePadding = Mathf.Clamp(currentFluidWidth * 0.2f, 4f, 15f);
                float paddedVisualWidth = currentFluidWidth + edgePadding;
                float paddedVisualDepth = (waterDepth * 2f) + 20f;
                
                float surfaceY = midY + waterLevelOffset;
                float centerOffsetY = surfaceY - (paddedVisualDepth / 2f);
                Vector2 centerPos = new Vector2(nextFluidX + currentFluidWidth / 2f, centerOffsetY);
                
                // 50% chance for Sand Dune, 50% chance for Water!
                bool isSandDune = (Mathf.PerlinNoise(nextFluidX * 0.1f, seed) < sandDuneChance);
                
                GameObject prefabToUse = isSandDune ? sandDunePrefab : waterPoolPrefab;
                if (prefabToUse != null)
                {
                    GameObject fluid = Instantiate(prefabToUse, new Vector3(centerPos.x, centerPos.y, 0f), Quaternion.identity, chunkParent);
                    
                    MeshRenderer mr = fluid.GetComponent<MeshRenderer>();
                    if (mr != null) mr.sortingOrder = waterSortingOrder;

                    DynamicWater2D fluidScript = fluid.GetComponent<DynamicWater2D>();
                    if (fluidScript != null)
                    {
                        if (propTint != Color.white)
                        {
                            fluidScript.surfaceColor *= propTint;
                            fluidScript.depthColor *= propTint;
                        }
                        fluidScript.GenerateWater(paddedVisualWidth, paddedVisualDepth, currentFluidWidth);
                    }
                    else fluid.transform.localScale = new Vector3(paddedVisualWidth, paddedVisualDepth, 1f);

                    TintObject(fluid);

                    // Spawn a Buoy ONLY if it's water!
                    if (!isSandDune && generateBuoys && buoyPrefab != null)
                    {
                        GameObject buoy = Instantiate(buoyPrefab, new Vector3(centerPos.x, surfaceY + 1f, 0f), Quaternion.identity, chunkParent);
                        TintObject(buoy);

                    }
                }
            }
            
            float dipPercent = (globalX - nextFluidX) / currentFluidWidth;
            float dipAmount = Mathf.Sin(dipPercent * Mathf.PI) * waterDepth;
            finalY -= dipAmount; 
            return false; 
        }
        else if (globalX > nextFluidX + currentFluidWidth && isBuildingFluid)
        {
            isBuildingFluid = false;
            nextFluidX = Mathf.Round((nextFluidX + 250f + (Mathf.PerlinNoise(globalX * 0.2f, seed) * 150f)) / pointSpacing) * pointSpacing;
            currentFluidWidth = Mathf.Round(Mathf.Lerp(minWaterWidth, maxWaterWidth, Mathf.PerlinNoise(nextFluidX, seed)) / pointSpacing) * pointSpacing;
        }

        return false;
    }

    protected override bool ProcessHazards(float globalX, float originalY, int i, int numSurfacePoints, Transform chunkParent)
    {
        if (i >= numSurfacePoints) return false;

        // 1. SEAGULL FLOCKS (Sky)
        if (generateSeagulls && Mathf.Abs(globalX - nextGullX) < 0.01f)
        {
            if (seagullPrefab != null)
            {
                // Start with 1 seagull at 0m, 2 at 1000m, 3 at 2000m, capping at 10!
                int flockSize = Mathf.Clamp(1 + Mathf.FloorToInt(globalX / 1000f), 1, 10);
                float skyHeight = originalY + Mathf.Lerp(minGullHeight, maxGullHeight, Mathf.PerlinNoise(globalX, seed));
                
                for (int g = 0; g < flockSize; g++)
                {
                    Vector3 skyPos = new Vector3(globalX + Random.Range(-5f, 15f), skyHeight + (g * 1.5f), 0f);
                    GameObject seagull = Instantiate(seagullPrefab, skyPos, Quaternion.identity, chunkParent);
                    TintObject(seagull);

                }
            }
            nextGullX = Mathf.Round((nextGullX + Mathf.Lerp(minGullDistance, maxGullDistance, Mathf.PerlinNoise(globalX * 0.8f, seed))) / pointSpacing) * pointSpacing;
        }

        // 2. GIANT BEACH BALLS
        if (generateBeachBalls && Mathf.Abs(globalX - nextBallX) < 0.01f)
        {
            if (beachBallPrefab != null)
            {
                float ballScale = Random.Range(1f, maxBallScale);
                GameObject ball = Instantiate(beachBallPrefab, new Vector3(globalX, originalY + (ballScale * 0.5f) + 1f, 0f), Quaternion.identity, chunkParent);
                ball.transform.localScale = Vector3.one * ballScale;
                TintObject(ball);

            }
            nextBallX = Mathf.Round((nextBallX + Mathf.Lerp(minBallDistance, maxBallDistance, Mathf.PerlinNoise(globalX * 0.5f, seed))) / pointSpacing) * pointSpacing;
            return true;
        }

        // 3. COCONUT PILES
        if (generateCoconuts && Mathf.Abs(globalX - nextCoconutX) < 0.01f)
        {
            if (coconutPrefab != null)
            {
                int rows = 3;
                float spacing = 1.0f;
                
                for (int r = 0; r < rows; r++)
                {
                    int coconutsInRow = rows - r;
                    for (int c = 0; c < coconutsInRow; c++)
                    {
                        float startX = globalX - ((coconutsInRow - 1) * spacing * 0.5f);
                        float posX = startX + (c * spacing);
                        float posY = originalY + 1.5f + (r * spacing * 0.85f); 
                        GameObject coconut = Instantiate(coconutPrefab, new Vector3(posX, posY, 0f), Quaternion.identity, chunkParent);
                        TintObject(coconut);

                    }
                }
            }
            nextCoconutX = Mathf.Round((nextCoconutX + Mathf.Lerp(minCoconutDistance, maxCoconutDistance, Mathf.PerlinNoise(globalX * 0.75f, seed))) / pointSpacing) * pointSpacing;
            return true;
        }

        // 4. BOUNCY UMBRELLAS
        if (generateBouncyHazards && Mathf.Abs(globalX - nextBounceX) < 0.01f)
        {
            if (bouncyHazardPrefab != null)
            {
                GameObject umbrella = Instantiate(bouncyHazardPrefab, new Vector3(globalX, originalY + bouncyYOffset, 0f), Quaternion.identity, chunkParent);
                TintObject(umbrella);
            }
            nextBounceX = Mathf.Round((nextBounceX + Mathf.Lerp(minBounceDistance, maxBounceDistance, Mathf.PerlinNoise(globalX * 0.4f, seed))) / pointSpacing) * pointSpacing;
            return true;
        }

        return false; 
    }
}