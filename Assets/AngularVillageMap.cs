using UnityEngine;
using UnityEngine.U2D;
using System.Collections.Generic;

public class AngularVillageMap : MapGenerator
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

    [Header("--- ANGULAR TERRAIN SETTINGS (NEW) ---")]
    [Tooltip("How wide each geometric segment is. Lower values allow secondary noise to create more steps/kinks.")]
    public float angularSegmentLength = 10f;
    [Tooltip("The vertical grid step. Higher values create flatter plateaus and deeper valleys.")]
    public float heightStep = 3f;
    [Tooltip("Percentage (0 to 0.4) of each segment's edges that are perfectly flat. Prevents 'V' valleys and creates plateaus.")]
    [Range(0f, 0.4f)]
    public float flatRatio = 0.25f;

    [Header("--- ANGULAR TERRAIN DIVERSITY ---")]
    [Tooltip("Adds massive height variations to break up repeating hill heights.")]
    public float macroHeightVariation = 15f;
    [Tooltip("Frequency for the macro height variation. Lower is wider hills.")]
    public float macroNoiseFrequency = 0.015f;
    [Tooltip("If true, the flat ratio changes per segment, preventing repeating long flat patterns.")]
    public bool dynamicFlats = true;

    [Header("--- VILLAGE SPECIFIC FEATURES ---")]
    
    [Header("Static Time of Day Settings")]
    public bool enableStaticTime = true;
    [Tooltip("If true, picks a random time. If false, uses the specific time below.")]
    public bool randomizeTime = true;
    public TimeOfDay specificTimeOfDay = TimeOfDay.Noon;

    [Header("Color Grading / Intensity")]
    [Tooltip("Drag your unique white Sky background sprite here!")]
    public SpriteRenderer skyBackgroundRenderer; 
    
    public TimeColors dawnColors = new TimeColors(new Color(0.9f, 0.6f, 0.5f), new Color(0.8f, 0.7f, 0.7f));
    public TimeColors morningColors = new TimeColors(new Color(0.6f, 0.8f, 1.0f), Color.white);
    public TimeColors noonColors = new TimeColors(new Color(0.4f, 0.7f, 1.0f), Color.white);
    public TimeColors duskColors = new TimeColors(new Color(0.7f, 0.4f, 0.6f), new Color(0.7f, 0.5f, 0.6f));
    public TimeColors nightColors = new TimeColors(new Color(0.1f, 0.1f, 0.2f), new Color(0.3f, 0.3f, 0.5f));

    private Color propTint = Color.white;
    private bool hasTintedCar = false;
    private TimeOfDay activeTimeOfDay;

    [Header("Fluid Hazards (Water & Mud)")]
    public bool generateWaterPools = true;
    public GameObject waterPoolPrefab;
    public GameObject mudPoolPrefab; 
    [Range(0f, 1f)] public float mudProbability = 0.3f;
    public float minWaterWidth = 15f;
    public float maxWaterWidth = 40f;
    public float waterDepth = 8f;
    public float waterMeshDepth = 15f; 
    public float waterLevelOffset = -2f;
    public int waterSortingOrder = 3;

    [Header("Water Terrain Placement Rules")]
    public bool requireFlatOrValleyTerrain = true;
    public float maxWaterEdgeHeightDiff = 1.0f;
    public float maxWaterTerrainSlope = 0.15f;
    public float maxPeakBulgeTolerance = 0.05f;

    [Header("Scatter Obstacles (Hay Bales & Fences)")]
    public bool generateScatterObstacles = true;
    public GameObject[] scatterPrefabs;
    public float minScatterDistance = 100f;
    public float maxScatterDistance = 250f;

    [Header("Bouncy Hazards (Tires)")]
    public bool generateBouncyHazards = true;
    public GameObject bouncyHazardPrefab;
    public float minBounceDistance = 200f;
    public float maxBounceDistance = 400f;
    public float bouncyYOffset = 0.5f;

    [Header("Windmills")]
    public bool generateWindmills = true;
    public GameObject windmillPrefab;
    public float minWindmillDistance = 300f;
    public float maxWindmillDistance = 600f;
    public float windmillYOffset = 5f;
    public float windmillZOffset = 2f;

    [Header("Runaway Livestock")]
    public bool generateLivestock = true;
    public GameObject livestockPrefab;
    public float minLivestockDistance = 150f;
    public float maxLivestockDistance = 350f;

    [Header("Scarecrows")]
    public bool generateScarecrows = true;
    public GameObject scarecrowPrefab;
    public float minScarecrowDistance = 200f;
    public float maxScarecrowDistance = 400f;
    public float scarecrowYOffset = 0f;

    [Header("Hot Air Balloons")]
    public bool generateBalloons = true;
    public GameObject balloonPrefab;
    public float minBalloonDistance = 400f;
    public float maxBalloonDistance = 800f;
    public float minBalloonHeight = 25f;
    public float maxBalloonHeight = 40f;

    private float nextWaterX;
    private float currentWaterWidth;
    private bool isBuildingWater = false;
    private float nextScatterX;
    private float nextBounceX; 
    private float nextWindmillX;
    private float nextLivestockX;
    private float nextScarecrowX;
    private float nextBalloonX;
    private System.Random runRNG;

    void LateUpdate()
    {
        if (enableStaticTime && !hasTintedCar && playerCar != null)
        {
            hasTintedCar = true;
            TintObject(playerCar.gameObject);

            if (activeTimeOfDay == TimeOfDay.Night)
            {
                // Requires a CarController script with ToggleHeadlights method
                // CarController cc = playerCar.GetComponent<CarController>();
                // if (cc != null) cc.ToggleHeadlights(true);
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
        TimeOfDay runTime = specificTimeOfDay;
        if (randomizeTime)
        {
            System.Random trueTimeRNG = new System.Random(System.Environment.TickCount);
            runTime = (TimeOfDay)trueTimeRNG.Next(0, 5); 
        }

        activeTimeOfDay = runTime;
        Color skyColor = Color.white;

        switch (runTime)
        {
            case TimeOfDay.Dawn:
                skyColor = dawnColors.skyColor;
                propTint = dawnColors.propTint;
                break;
            case TimeOfDay.Morning:
                skyColor = morningColors.skyColor;
                propTint = morningColors.propTint;
                break;
            case TimeOfDay.Noon:
                skyColor = noonColors.skyColor;
                propTint = noonColors.propTint;
                break;
            case TimeOfDay.Dusk:
                skyColor = duskColors.skyColor;
                propTint = duskColors.propTint;
                break;
            case TimeOfDay.Night:
                skyColor = nightColors.skyColor;
                propTint = nightColors.propTint;
                break;
        }

        if (Camera.main != null)
        {
            Camera.main.backgroundColor = skyColor;
        }

        if (skyBackgroundRenderer != null)
        {
            skyBackgroundRenderer.color = skyColor;
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

    
    // NEW: Intercept pickup spawns (Coins, Fuel) to ensure they snap to the angular slopes, not the base Perlin curve.
    protected override void SpawnPickups(Vector3 groundPosition, Transform chunkParent)
    {
        float correctAngularY = GetAngularTerrainY(groundPosition.x);
        Vector3 alignedPosition = new Vector3(groundPosition.x, correctAngularY, groundPosition.z);
        base.SpawnPickups(alignedPosition, chunkParent);
    }

    // UPDATED: Adjust props to follow the angular slopes
    protected override void OnEnvironmentPropSpawned(GameObject prop)
    {
        TintObject(prop);
        
        float globalX = prop.transform.position.x;
        float correctY = GetAngularTerrainY(globalX);
        
        // Adjust physical height to angular terrain
        Vector3 pos = prop.transform.position;
        float originalBaseY = base.GetRawTerrainY(globalX);
        float yOffset = pos.y - originalBaseY; 
        prop.transform.position = new Vector3(pos.x, correctY + yOffset, pos.z);

        // Adjust rotation to match the straight angular slope
        float yAhead = GetAngularTerrainY(globalX + 0.1f);
        float yBehind = GetAngularTerrainY(globalX - 0.1f);
        float terrainAngle = Mathf.Atan2((yAhead - yBehind), 0.2f) * Mathf.Rad2Deg;
        
        prop.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, terrainAngle, propTerrainAlignment));
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

        if (enableStaticTime) SetupTimeOfDay();

        float rawNextWater = 250f + (Mathf.PerlinNoise(seed + 1, seed) * 100f);
        nextWaterX = Mathf.Round(rawNextWater / pointSpacing) * pointSpacing;
        float rawWaterWidth = Mathf.Lerp(minWaterWidth, maxWaterWidth, Mathf.PerlinNoise(nextWaterX, seed));
        currentWaterWidth = Mathf.Round(rawWaterWidth / pointSpacing) * pointSpacing;

        float rawNextScatter = minScatterDistance + (Mathf.PerlinNoise(seed + 2, seed) * 50f);
        nextScatterX = Mathf.Round(rawNextScatter / pointSpacing) * pointSpacing;

        float rawNextBounce = minBounceDistance + (Mathf.PerlinNoise(seed + 3, seed) * 50f);
        nextBounceX = Mathf.Round(rawNextBounce / pointSpacing) * pointSpacing;

        float rawNextWindmill = minWindmillDistance + (Mathf.PerlinNoise(seed + 5, seed) * 50f);
        nextWindmillX = Mathf.Round(rawNextWindmill / pointSpacing) * pointSpacing;

        float rawNextLivestock = minLivestockDistance + (Mathf.PerlinNoise(seed + 6, seed) * 50f);
        nextLivestockX = Mathf.Round(rawNextLivestock / pointSpacing) * pointSpacing;

        float rawNextScarecrow = minScarecrowDistance + (Mathf.PerlinNoise(seed + 7, seed) * 50f);
        nextScarecrowX = Mathf.Round(rawNextScarecrow / pointSpacing) * pointSpacing;

        float rawNextBalloon = minBalloonDistance + (Mathf.PerlinNoise(seed + 8, seed) * 50f);
        nextBalloonX = Mathf.Round(rawNextBalloon / pointSpacing) * pointSpacing;
    }

    
    /// <summary>
    /// NEW CORE LOGIC: Converts smooth Perlin noise into box-fold/angular terrain.
    /// It samples the noise at large intervals, quantizes the heights to form flat plateaus,
    /// and linearly interpolates the points in between to form straight slopes.
    /// </summary>
    protected float GetAngularTerrainY(float globalX)
    {
        // 1. Find the start and end of the current geometric segment
        float segmentStart = Mathf.Floor(globalX / angularSegmentLength) * angularSegmentLength;
        float segmentEnd = segmentStart + angularSegmentLength;
        float t = (globalX - segmentStart) / angularSegmentLength;

        // 2. Sample the underlying smooth Perlin terrain at those two boundaries (Includes Secondary Noise)
        float rawStart = base.GetRawTerrainY(segmentStart);
        float rawEnd = base.GetRawTerrainY(segmentEnd);

        // --- NEW: MACRO DIVERSITY ---
        // Add a slow-moving, large amplitude noise to break the "same height" repeating hills pattern
        float macroStart = (Mathf.PerlinNoise(segmentStart * macroNoiseFrequency, seed + 100) * 2f - 1f) * macroHeightVariation;
        float macroEnd = (Mathf.PerlinNoise(segmentEnd * macroNoiseFrequency, seed + 100) * 2f - 1f) * macroHeightVariation;
        rawStart += macroStart;
        rawEnd += macroEnd;

        // 3. Quantize the heights. 
        float heightStart = Mathf.Round(rawStart / heightStep) * heightStep;
        float heightEnd = Mathf.Round(rawEnd / heightStep) * heightStep;

        // --- NEW: DYNAMIC FLATS ---
        // Vary the flat ratio per segment to break the "very long flat surfaces" pattern.
        float currentFlatRatio = flatRatio;
        if (dynamicFlats)
        {
            float flatNoise = Mathf.PerlinNoise(segmentStart * 0.1f, seed + 200);
            currentFlatRatio = Mathf.Clamp(flatRatio * (flatNoise * 1.5f), 0.02f, 0.45f);
        }

        // 4. Flat-Slope-Flat Logic: Prevents "V" shapes and naturally allows secondary noise steps/kinks.
        if (t <= currentFlatRatio)
        {
            // Flat area at the start of the segment
            return heightStart;
        }
        else if (t >= 1f - currentFlatRatio)
        {
            // Flat area at the end of the segment
            return heightEnd;
        }
        else
        {
            // Straight angular slope connecting the two flats in the middle
            float slopeT = (t - currentFlatRatio) / (1f - 2f * currentFlatRatio);
            return Mathf.Lerp(heightStart, heightEnd, slopeT);
        }
    }

    // UPDATED: Now uses GetAngularTerrainY instead of GetRawTerrainY to find valid flat zones.
    private bool IsTerrainSuitableForWater(float waterX, float width)
    {
        if (!requireFlatOrValleyTerrain) return true;

        float startY = GetAngularTerrainY(waterX);
        float endY = GetAngularTerrainY(waterX + width);

        float edgeDiff = Mathf.Abs(startY - endY);
        if (edgeDiff > maxWaterEdgeHeightDiff) return false;

        float globalSlope = edgeDiff / width;
        if (globalSlope > maxWaterTerrainSlope) return false;

        float bankBaseY = Mathf.Min(startY, endY);
        float surfaceY = bankBaseY + waterLevelOffset;

        float edgePadding = Mathf.Clamp(width * 0.2f, 4f, 15f);
        float leftVisualX = waterX - (edgePadding * 0.5f);
        float rightVisualX = waterX + width + (edgePadding * 0.5f);

        int paddingSamples = 3;
        for (int p = 1; p <= paddingSamples; p++)
        {
            float t = (float)p / paddingSamples;
            float checkLeftX = Mathf.Lerp(waterX, leftVisualX, t);
            float checkRightX = Mathf.Lerp(waterX + width, rightVisualX, t);

            if (GetAngularTerrainY(checkLeftX) < surfaceY || GetAngularTerrainY(checkRightX) < surfaceY)
            {
                return false; 
            }
        }

        float maxBankY = Mathf.Max(startY, endY);
        int samples = 10; 
        float step = width / samples;

        for (int k = 0; k <= samples; k++)
        {
            float currX = waterX + k * step;
            float currY = GetAngularTerrainY(currX);

            if (currY > maxBankY + maxPeakBulgeTolerance) return false; 
            
            if (k > 0)
            {
                float prevY = GetAngularTerrainY(waterX + (k - 1) * step);
                float localSlope = Mathf.Abs(currY - prevY) / step;
                if (localSlope > maxWaterTerrainSlope) return false;
            }
        }

        return true; 
    }

    protected override bool ProcessTerrainSpline(float globalX, float localX, float originalY, ref float finalY, Spline spline, List<Vector2> physicsPoints, ref int pointIndex, int i, int numSurfacePoints, Transform chunkParent, out bool isHazardZone)
    {
        isHazardZone = false;

        // --- NEW: INJECT ANGULAR TERRAIN LOGIC HERE ---
        // Overwrite the smooth 'originalY' generated by the base class with our new angular terrain.
        float angularY = GetAngularTerrainY(globalX);
        finalY = angularY;

        // --- WATER/MUD LOGIC WITH FLAT/VALLEY TERRAIN FILTERING ---
        if (generateWaterPools && globalX >= nextWaterX && !isBuildingWater)
        {
            if (!IsTerrainSuitableForWater(nextWaterX, currentWaterWidth))
            {
                nextWaterX = Mathf.Round((nextWaterX + pointSpacing) / pointSpacing) * pointSpacing;
            }
        }

        bool inWaterZone = generateWaterPools && globalX >= nextWaterX && globalX <= nextWaterX + currentWaterWidth;
        
        if (inWaterZone)
        {
            isHazardZone = true;
            if (!isBuildingWater)
            {
                isBuildingWater = true;
                // Use Angular Y bounds
                float startY = GetAngularTerrainY(nextWaterX);
                float endY = GetAngularTerrainY(nextWaterX + currentWaterWidth);
                
                float bankBaseY = Mathf.Min(startY, endY);
                
                float edgePadding = Mathf.Clamp(currentWaterWidth * 0.2f, 4f, 15f);
                float paddedVisualWidth = currentWaterWidth + edgePadding;
                float paddedVisualDepth = waterMeshDepth; 
                
                float surfaceY = bankBaseY + waterLevelOffset;
                float centerOffsetY = surfaceY - (paddedVisualDepth / 2f);
                
                Vector2 centerPos = new Vector2(nextWaterX + currentWaterWidth / 2f, centerOffsetY);
                bool isMud = mudPoolPrefab != null && (Mathf.PerlinNoise(nextWaterX * 0.1f, seed) < mudProbability);
                SpawnFluid(centerPos, paddedVisualWidth, paddedVisualDepth, currentWaterWidth, chunkParent, isMud);
            }
            
            float dipPercent = (globalX - nextWaterX) / currentWaterWidth;
            float smoothFactor = 0.5f * (1f - Mathf.Cos(dipPercent * 2f * Mathf.PI));
            float dipAmount = smoothFactor * waterDepth;
            finalY -= dipAmount; 
            return false; 
        }
        else if (globalX > nextWaterX + currentWaterWidth && isBuildingWater)
        {
            isBuildingWater = false;
            float rawNextWater = nextWaterX + 250f + (Mathf.PerlinNoise(globalX * 0.2f, seed) * 150f);
            nextWaterX = Mathf.Round(rawNextWater / pointSpacing) * pointSpacing;
            float rawWaterWidth = Mathf.Lerp(minWaterWidth, maxWaterWidth, Mathf.PerlinNoise(nextWaterX, seed));
            currentWaterWidth = Mathf.Round(rawWaterWidth / pointSpacing) * pointSpacing;
        }

        return false;
    }

    protected override bool ProcessHazards(float globalX, float originalY, int i, int numSurfacePoints, Transform chunkParent)
    {
        if (i >= numSurfacePoints) return false;

        // NEW: Get the correct angular Y instead of using the raw originalY passed from base MapGenerator
        float angularY = GetAngularTerrainY(globalX);

        bool isBalloonPoint = generateBalloons && Mathf.Abs(globalX - nextBalloonX) < 0.01f;
        if (isBalloonPoint)
        {
            if (balloonPrefab != null)
            {
                float skyHeight = angularY + Mathf.Lerp(minBalloonHeight, maxBalloonHeight, Mathf.PerlinNoise(globalX, seed));
                GameObject balloon = Instantiate(balloonPrefab, new Vector3(globalX, skyHeight, 0f), Quaternion.identity, chunkParent);
                TintObject(balloon);
            }
            nextBalloonX = Mathf.Round((nextBalloonX + Mathf.Lerp(minBalloonDistance, maxBalloonDistance, Mathf.PerlinNoise(globalX * 0.9f, seed))) / pointSpacing) * pointSpacing;
        }

        bool isScarecrowPoint = generateScarecrows && Mathf.Abs(globalX - nextScarecrowX) < 0.01f;
        if (isScarecrowPoint)
        {
            if (scarecrowPrefab != null) 
            {
                GameObject scarecrow = Instantiate(scarecrowPrefab, new Vector3(globalX, angularY + scarecrowYOffset, 0f), Quaternion.identity, chunkParent);
                TintObject(scarecrow);
            }
            nextScarecrowX = Mathf.Round((nextScarecrowX + Mathf.Lerp(minScarecrowDistance, maxScarecrowDistance, Mathf.PerlinNoise(globalX * 0.95f, seed))) / pointSpacing) * pointSpacing;
            return true;
        }

        bool isWindmillPoint = generateWindmills && Mathf.Abs(globalX - nextWindmillX) < 0.01f;
        if (isWindmillPoint)
        {
            if (windmillPrefab != null)
            {
                GameObject windmill = Instantiate(windmillPrefab, new Vector3(globalX, angularY + windmillYOffset, windmillZOffset), Quaternion.identity, chunkParent);
                TintObject(windmill);
            }
            nextWindmillX = Mathf.Round((nextWindmillX + Mathf.Lerp(minWindmillDistance, maxWindmillDistance, Mathf.PerlinNoise(globalX * 0.7f, seed))) / pointSpacing) * pointSpacing;
            return true;
        }

        bool isLivestockPoint = generateLivestock && Mathf.Abs(globalX - nextLivestockX) < 0.01f;
        if (isLivestockPoint)
        {
            if (livestockPrefab != null) 
            {
                GameObject livestock = Instantiate(livestockPrefab, new Vector3(globalX, angularY + 1.5f, 0f), Quaternion.identity, chunkParent);
                TintObject(livestock);
            }
            nextLivestockX = Mathf.Round((nextLivestockX + Mathf.Lerp(minLivestockDistance, maxLivestockDistance, Mathf.PerlinNoise(globalX * 0.8f, seed))) / pointSpacing) * pointSpacing;
            return true;
        }

        bool isBouncePoint = generateBouncyHazards && Mathf.Abs(globalX - nextBounceX) < 0.01f;
        if (isBouncePoint)
        {
            if (bouncyHazardPrefab != null) 
            {
                GameObject bouncy = Instantiate(bouncyHazardPrefab, new Vector3(globalX, angularY + bouncyYOffset, 0f), Quaternion.identity, chunkParent);
                TintObject(bouncy);
            }
            nextBounceX = Mathf.Round((nextBounceX + Mathf.Lerp(minBounceDistance, maxBounceDistance, Mathf.PerlinNoise(globalX * 0.4f, seed))) / pointSpacing) * pointSpacing;
            return true;
        }

        bool isScatterPoint = generateScatterObstacles && Mathf.Abs(globalX - nextScatterX) < 0.01f;
        if (isScatterPoint)
        {
            if (scatterPrefabs != null && scatterPrefabs.Length > 0)
            {
                int pIdx = Mathf.Clamp(Mathf.FloorToInt(Mathf.PerlinNoise(globalX, seed) * scatterPrefabs.Length), 0, scatterPrefabs.Length - 1);
                GameObject scatter = Instantiate(scatterPrefabs[pIdx], new Vector3(globalX, angularY + 1f, 0f), Quaternion.identity, chunkParent);
                TintObject(scatter);
            }
            nextScatterX = Mathf.Round((nextScatterX + Mathf.Lerp(minScatterDistance, maxScatterDistance, Mathf.PerlinNoise(globalX * 0.3f, seed))) / pointSpacing) * pointSpacing;
            return true;
        }

        return false; 
    }

    void SpawnFluid(Vector2 centerPos, float visualWidth, float visualDepth, float physicsWidth, Transform chunkParent, bool isMud)
    {
        GameObject prefabToUse = isMud ? mudPoolPrefab : waterPoolPrefab;
        if (prefabToUse != null)
        {
            Vector3 spawnPos = new Vector3(centerPos.x, centerPos.y, 0f);
            GameObject fluid = Instantiate(prefabToUse, spawnPos, Quaternion.identity, chunkParent);
            MeshRenderer mr = fluid.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = waterSortingOrder;

            // Requires DynamicWater2D Script setup
            // DynamicWater2D fluidScript = fluid.GetComponent<DynamicWater2D>();
            // if (fluidScript != null)
            // {
            //     fluid.transform.localScale = Vector3.one; 
            //     
            //     if (propTint != Color.white)
            //     {
            //         fluidScript.surfaceColor *= propTint;
            //         fluidScript.depthColor *= propTint;
            //     }
            //
            //     fluidScript.GenerateWater(visualWidth, visualDepth, physicsWidth);
            // }
            // else 
            
            fluid.transform.localScale = new Vector3(visualWidth, visualDepth, 1f);
            TintObject(fluid);
        }
    }
}