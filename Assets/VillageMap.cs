using UnityEngine;
using UnityEngine.U2D; 
using System.Collections.Generic;

public class VillageMap : MapGenerator
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
    private TimeOfDay activeTimeOfDay; // Tracks active time of day

    [Header("Destructible Bridges")]
    public bool generateBridges = true; 
    public GameObject ropeBridgePrefab;
    public float minBridgeGapWidth = 15f;
    public float maxBridgeGapWidth = 35f;
    public float gorgeDepth = 30f;
    public float expectedPlankWidth = 1.5f;
    public float bridgeYOffset = -0.1f;

    [Header("Fluid Hazards (Water & Mud)")]
    public bool generateWaterPools = true;
    public GameObject waterPoolPrefab;
    public GameObject mudPoolPrefab; 
    [Range(0f, 1f)] public float mudProbability = 0.3f;
    public float minWaterWidth = 15f;
    public float maxWaterWidth = 40f;
    [Tooltip("The physical depth of the water affecting the terrain dip.")]
    public float waterDepth = 8f;
    [Tooltip("The visual depth of the water mesh generated downwards.")]
    public float waterMeshDepth = 15f; 
    public float waterLevelOffset = -2f;
    public int waterSortingOrder = 3;

    [Header("Water Terrain Placement Rules")]
    [Tooltip("If true, water pools will only spawn on flat ground or inside valleys between hills.")]
    public bool requireFlatOrValleyTerrain = true;
    [Tooltip("Maximum allowed height difference between start bank and end bank of water pool.")]
    public float maxWaterEdgeHeightDiff = 1.0f;
    [Tooltip("Maximum allowed internal slope across the terrain segment before water is rejected.")]
    public float maxWaterTerrainSlope = 0.15f;
    [Tooltip("Tolerated bulge height above bank height. Keeps water off top of hills/peaks.")]
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

    [Header("Jelly Obstacles (Troll Trees)")]
    public bool generateJellyObstacles = true;
    public GameObject[] jellyObstaclePrefabs;
    public float minJellyDistance = 100f; 
    public float maxJellyDistance = 250f;
    [Range(0f, 1f)] public float jellySolidChance = 0.5f;
    public float jellyYOffset = 0f;

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

    private float nextBridgeX;
    private float currentBridgeGapWidth;
    private float nextWaterX;
    private float currentWaterWidth;
    private bool isBuildingWater = false;
    private float nextScatterX;
    private float nextBounceX; 
    private float nextJellyX; 
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

        Debug.Log("Village Map started at: " + runTime.ToString());
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

        float rawNextBridge = 100f + (Mathf.PerlinNoise(seed, seed) * 50f);
        nextBridgeX = Mathf.Round(rawNextBridge / pointSpacing) * pointSpacing;
        float rawGap = Mathf.Lerp(minBridgeGapWidth, maxBridgeGapWidth, Mathf.PerlinNoise(nextBridgeX, seed));
        currentBridgeGapWidth = Mathf.Round(rawGap / pointSpacing) * pointSpacing;

        float rawNextWater = 250f + (Mathf.PerlinNoise(seed + 1, seed) * 100f);
        nextWaterX = Mathf.Round(rawNextWater / pointSpacing) * pointSpacing;
        float rawWaterWidth = Mathf.Lerp(minWaterWidth, maxWaterWidth, Mathf.PerlinNoise(nextWaterX, seed));
        currentWaterWidth = Mathf.Round(rawWaterWidth / pointSpacing) * pointSpacing;

        // Ensure water and bridge initial coordinates do not overlap
        float hazardBuffer = 25f;
        if (generateBridges && generateWaterPools && Mathf.Abs(nextWaterX - nextBridgeX) < (currentWaterWidth + currentBridgeGapWidth + hazardBuffer))
        {
            nextWaterX = Mathf.Round((nextBridgeX + currentBridgeGapWidth + hazardBuffer) / pointSpacing) * pointSpacing;
        }

        float rawNextScatter = minScatterDistance + (Mathf.PerlinNoise(seed + 2, seed) * 50f);
        nextScatterX = Mathf.Round(rawNextScatter / pointSpacing) * pointSpacing;

        float rawNextBounce = minBounceDistance + (Mathf.PerlinNoise(seed + 3, seed) * 50f);
        nextBounceX = Mathf.Round(rawNextBounce / pointSpacing) * pointSpacing;
        
        float rawNextJelly = minJellyDistance + (Mathf.PerlinNoise(seed + 4, seed) * 50f);
        nextJellyX = Mathf.Round(rawNextJelly / pointSpacing) * pointSpacing;

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
    /// Checks whether the raw terrain profile from waterX to waterX + width is flat or forms a natural valley,
    /// avoiding steep hills, sloped grades, and hill peaks.
    /// </summary>
    private bool IsTerrainSuitableForWater(float waterX, float width)
    {
        if (!requireFlatOrValleyTerrain) return true;

        float startY = GetRawTerrainY(waterX);
        float endY = GetRawTerrainY(waterX + width);

        // 1. Bank Level Check: Start & End banks should be close in height
        float edgeDiff = Mathf.Abs(startY - endY);
        if (edgeDiff > maxWaterEdgeHeightDiff)
        {
            return false; // Terrain is on a steep global gradient
        }

        // Global Gradient Check: Ensure ground across entire pool width isn't a continuous incline/decline
        float globalSlope = edgeDiff / width;
        if (globalSlope > maxWaterTerrainSlope)
        {
            return false; // Overall hillside gradient is too steep for standing water
        }

        // Calculate water surface elevation
        float bankBaseY = Mathf.Min(startY, endY);
        float surfaceY = bankBaseY + waterLevelOffset;

        // Calculate visual padding bounds
        float edgePadding = Mathf.Clamp(width * 0.2f, 4f, 15f);
        float leftVisualX = waterX - (edgePadding * 0.5f);
        float rightVisualX = waterX + width + (edgePadding * 0.5f);

        // 2. Visual Padding Edge Check:
        // Ensure terrain outside both banks doesn't drop below the water surface level.
        // If the ground past either bank dips below surfaceY, the flat top water mesh will poke out of the slope.
        int paddingSamples = 3;
        for (int p = 1; p <= paddingSamples; p++)
        {
            float t = (float)p / paddingSamples;
            float checkLeftX = Mathf.Lerp(waterX, leftVisualX, t);
            float checkRightX = Mathf.Lerp(waterX + width, rightVisualX, t);

            if (GetRawTerrainY(checkLeftX) < surfaceY || GetRawTerrainY(checkRightX) < surfaceY)
            {
                return false; // Ground dips below water level in the visual padding region!
            }
        }

        // 3. Peak / Convex Hill Detection:
        // Real water rests in valleys (concave) or flat ground.
        // If ANY interior point is higher than the HIGHER of the two banks, it's a hill peak/mound.
        float maxBankY = Mathf.Max(startY, endY);

        int samples = 10; // Fine-grained sampling
        float step = width / samples;

        for (int k = 0; k <= samples; k++)
        {
            float currX = waterX + k * step;
            float currY = GetRawTerrainY(currX);

            // REJECT PEAKS: Interior terrain rises above highest bank elevation
            if (currY > maxBankY + maxPeakBulgeTolerance)
            {
                return false; 
            }

            // Slope check against adjacent sample
            if (k > 0)
            {
                float prevY = GetRawTerrainY(waterX + (k - 1) * step);
                float localSlope = Mathf.Abs(currY - prevY) / step;
                if (localSlope > maxWaterTerrainSlope)
                {
                    return false; // Terrain grade is too steep inside this segment
                }
            }
        }

        return true; // Suitable flat ground or natural valley!
    }

    protected override bool ProcessTerrainSpline(float globalX, float localX, float originalY, ref float finalY, Spline spline, List<Vector2> physicsPoints, ref int pointIndex, int i, int numSurfacePoints, Transform chunkParent, out bool isHazardZone)
    {
        isHazardZone = false;

        float hazardBuffer = 25f;

        // --- WATER/MUD LOGIC WITH FLAT/VALLEY TERRAIN FILTERING & BRIDGE EXCLUSION ---
        if (generateWaterPools && globalX >= nextWaterX && !isBuildingWater)
        {
            // Push water forward if it conflicts with an active or upcoming bridge zone
            if (generateBridges && (nextWaterX + currentWaterWidth >= nextBridgeX - hazardBuffer) && (nextWaterX <= nextBridgeX + currentBridgeGapWidth + hazardBuffer))
            {
                nextWaterX = Mathf.Round((nextBridgeX + currentBridgeGapWidth + hazardBuffer) / pointSpacing) * pointSpacing;
            }

            // If terrain is on a hill/slope/peak, push water location forward until suitable flat/valley terrain is found
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
                float startY = GetRawTerrainY(nextWaterX);
                float endY = GetRawTerrainY(nextWaterX + currentWaterWidth);
                
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
            
            // Raised-Cosine curve: blend slope smoothly into and out of the water bed (derivative at edges = 0)
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

            if (generateBridges && (nextWaterX + currentWaterWidth >= nextBridgeX - hazardBuffer) && (nextWaterX <= nextBridgeX + currentBridgeGapWidth + hazardBuffer))
            {
                nextWaterX = Mathf.Round((nextBridgeX + currentBridgeGapWidth + hazardBuffer) / pointSpacing) * pointSpacing;
            }
        }

        // --- ROPE BRIDGE LOGIC WITH WATER EXCLUSION ---
        if (generateBridges && generateWaterPools && Mathf.Abs(globalX - nextBridgeX) < 0.01f)
        {
            // Push bridge start location forward if it lands inside or too close to a water pool
            if (nextBridgeX >= nextWaterX - hazardBuffer && nextBridgeX <= nextWaterX + currentWaterWidth + hazardBuffer)
            {
                nextBridgeX = Mathf.Round((nextWaterX + currentWaterWidth + hazardBuffer) / pointSpacing) * pointSpacing;
            }
        }

        bool isStartAnchor = generateBridges && Mathf.Abs(globalX - nextBridgeX) < 0.01f;
        bool isEndAnchor = generateBridges && Mathf.Abs(globalX - (nextBridgeX + currentBridgeGapWidth)) < 0.01f;
        bool insideGorge = generateBridges && globalX > nextBridgeX + 0.01f && globalX < (nextBridgeX + currentBridgeGapWidth) - 0.01f;

        if (isStartAnchor || isEndAnchor || insideGorge) isHazardZone = true;

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
                SpawnBridge(new Vector2(globalX, originalY), new Vector2(nextBridgeX + currentBridgeGapWidth, endY), chunkParent);
            }

            Vector3 dropPoint = new Vector3(localX + 0.01f, originalY - gorgeDepth, 0f);
            spline.InsertPointAt(pointIndex, dropPoint);
            physicsPoints.Add(new Vector2(localX + 0.01f, originalY - gorgeDepth));
            spline.SetTangentMode(pointIndex, ShapeTangentMode.Linear);
            spline.SetCorner(pointIndex, false);
            pointIndex++;
            return true; 
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

                if (generateWaterPools && (nextBridgeX >= nextWaterX - hazardBuffer) && (nextBridgeX <= nextWaterX + currentWaterWidth + hazardBuffer))
                {
                    nextBridgeX = Mathf.Round((nextWaterX + currentWaterWidth + hazardBuffer) / pointSpacing) * pointSpacing;
                }
            }
            return true;
        }
        else if (insideGorge)
        {
            finalY -= gorgeDepth;
            return false; 
        }

        return false;
    }

    protected override bool ProcessHazards(float globalX, float originalY, int i, int numSurfacePoints, Transform chunkParent)
    {
        if (i >= numSurfacePoints) return false;

        bool isBalloonPoint = generateBalloons && Mathf.Abs(globalX - nextBalloonX) < 0.01f;
        if (isBalloonPoint)
        {
            if (balloonPrefab != null)
            {
                float skyHeight = originalY + Mathf.Lerp(minBalloonHeight, maxBalloonHeight, Mathf.PerlinNoise(globalX, seed));
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
                GameObject scarecrow = Instantiate(scarecrowPrefab, new Vector3(globalX, originalY + scarecrowYOffset, 0f), Quaternion.identity, chunkParent);
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
                GameObject windmill = Instantiate(windmillPrefab, new Vector3(globalX, originalY + windmillYOffset, windmillZOffset), Quaternion.identity, chunkParent);
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
                GameObject livestock = Instantiate(livestockPrefab, new Vector3(globalX, originalY + 1.5f, 0f), Quaternion.identity, chunkParent);
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
                GameObject bouncy = Instantiate(bouncyHazardPrefab, new Vector3(globalX, originalY + bouncyYOffset, 0f), Quaternion.identity, chunkParent);
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
                GameObject scatter = Instantiate(scatterPrefabs[pIdx], new Vector3(globalX, originalY + 1f, 0f), Quaternion.identity, chunkParent);
                TintObject(scatter);
            }
            nextScatterX = Mathf.Round((nextScatterX + Mathf.Lerp(minScatterDistance, maxScatterDistance, Mathf.PerlinNoise(globalX * 0.3f, seed))) / pointSpacing) * pointSpacing;
            return true;
        }
        
        bool isJellyPoint = generateJellyObstacles && Mathf.Abs(globalX - nextJellyX) < 0.01f;
        if (isJellyPoint)
        {
            if (jellyObstaclePrefabs != null && jellyObstaclePrefabs.Length > 0)
            {
                int pIdx = Mathf.Clamp(Mathf.FloorToInt(Mathf.PerlinNoise(globalX * 3.3f, seed) * jellyObstaclePrefabs.Length), 0, jellyObstaclePrefabs.Length - 1);
                Vector3 spawnPos = new Vector3(globalX, originalY + jellyYOffset, 0f); 
                GameObject jelly = Instantiate(jellyObstaclePrefabs[pIdx], spawnPos, Quaternion.identity, chunkParent);

                SpriteRenderer sr = jelly.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) jelly.transform.position += new Vector3(0f, spawnPos.y - sr.bounds.min.y, 0f);

                float yAhead = GetRawTerrainY(globalX + 0.1f);
                float yBehind = GetRawTerrainY(globalX - 0.1f);
                float terrainAngle = Mathf.Atan2((yAhead - yBehind), 0.2f) * Mathf.Rad2Deg;
                jelly.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, terrainAngle, propTerrainAlignment));
                
                if (runRNG.NextDouble() > jellySolidChance)
                {
                    JellyObstacle script = jelly.GetComponent<JellyObstacle>();
                    if (script != null) Destroy(script);
                    Collider2D col = jelly.GetComponent<Collider2D>();
                    if (col != null) Destroy(col);
                    jelly.transform.position += new Vector3(0f, 0f, 2f);
                }

                TintObject(jelly);
            }
            nextJellyX = Mathf.Round((nextJellyX + Mathf.Lerp(minJellyDistance, maxJellyDistance, Mathf.PerlinNoise(globalX * 0.6f, seed))) / pointSpacing) * pointSpacing;
            return true;
        }

        return false; 
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

            TintObject(bridge);
        }
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

            DynamicWater2D fluidScript = fluid.GetComponent<DynamicWater2D>();
            if (fluidScript != null)
            {
                fluid.transform.localScale = Vector3.one; 
                
                if (propTint != Color.white)
                {
                    fluidScript.surfaceColor *= propTint;
                    fluidScript.depthColor *= propTint;
                }

                fluidScript.GenerateWater(visualWidth, visualDepth, physicsWidth);
            }
            else fluid.transform.localScale = new Vector3(visualWidth, visualDepth, 1f);
            TintObject(fluid);
        }
    }
}