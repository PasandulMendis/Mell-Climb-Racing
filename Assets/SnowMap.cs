using UnityEngine;
using UnityEngine.U2D; 
using System.Collections.Generic;
using System.Linq; 

public class SnowMap : MapGenerator
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

    [Header("--- SNOW SPECIFIC FEATURES ---")]
    
    [Header("1. Frozen Lakes (Fluid Hazards)")]
    public bool generateLakes = true;
    public GameObject waterPoolPrefab;
    public GameObject frozenLakePrefab;
    [Tooltip("Minimum distance between lakes")]
    public float minLakeDistance = 300f; 
    [Tooltip("Maximum distance between lakes")]
    public float maxLakeDistance = 700f; 
    public float minLakeWidth = 15f;
    public float maxLakeWidth = 40f;
    public float lakeDepth = 8f;
    [Tooltip("The visual depth of the water mesh generated downwards. Keep small to save performance.")]
    public float waterMeshDepth = 15f;

    public float waterLevelOffset = -2f;
    public int waterSortingOrder = 3;

    [Header("Fluid Terrain Placement Rules")]
    [Tooltip("If true, fluid pools (lakes) will only spawn on flat ground or inside valleys between hills.")]
    public bool requireFlatOrValleyTerrain = true;
    [Tooltip("Maximum allowed height difference between start bank and end bank of fluid pool.")]
    public float maxFluidEdgeHeightDiff = 1.0f;
    [Tooltip("Maximum allowed internal slope across the terrain segment before fluid is rejected.")]
    public float maxFluidTerrainSlope = 0.15f;
    [Tooltip("Tolerated bulge height above bank height. Keeps lakes off top of hills/peaks.")]
    public float maxPeakBulgeTolerance = 0.05f;

    [Header("2. Snow Tunnels (Major Feature)")]
    public bool generateTunnels = true;
    [Tooltip("Drag a Blank Chunk Prefab here to act as the ceiling texture!")]
    public GameObject ceilingChunkPrefab; 
    public GameObject iciclePrefab;
    
    [Tooltip("Minimum distance between tunnels")]
    public float minTunnelDistance = 1000f;
    [Tooltip("Maximum distance between tunnels")]
    public float maxTunnelDistance = 2500f;

    public float minTunnelLength = 400f;
    public float maxTunnelLength = 600f;
    
    [Tooltip("The gap between the floor and the ceiling")]
    public float minTunnelHeight = 15f;
    public float maxTunnelHeight = 25f;
    
    [Range(0f, 1f)]
    [Tooltip("Chance to spawn an icicle on a ceiling point")]
    public float icicleSpawnChance = 0.15f;

    [Tooltip("Offsets the icicle downwards so it doesn't get stuck in the ceiling collider")]
    public float icicleYOffset = -1.5f;

    [Header("Custom Meme Integration")]
    [Tooltip("Played when the car enters the 100m flat zone before a tunnel")]
    public string tunnelStartMemeCategory = "Tunnel_Start";

    [Header("3. Giant Snowballs")]
    public bool generateGiantSnowballs = true;
    public GameObject giantSnowballPrefab;
    [Tooltip("Minimum distance between giant snowballs")]
    public float minSnowballDistance = 800f;
    [Tooltip("Maximum distance between giant snowballs")]
    public float maxSnowballDistance = 1500f;
    [Tooltip("Spawns the snowball slightly in the air so it drops naturally onto the slope")]
    public float snowballYOffset = 3f;

    [Header("4. Snowmen Obstacles")]
    public bool generateSnowmen = true;
    public GameObject snowmanPrefab;
    [Tooltip("Minimum distance between snowmen")]
    public float minSnowmanDistance = 150f;
    [Tooltip("Maximum distance between snowmen")]
    public float maxSnowmanDistance = 400f;
    public float snowmanYOffset = 0.5f;

    [Header("5. Ice Block Pyramids")]
    public bool generateIcePyramids = true;
    public GameObject iceBlockPrefab;
    [Tooltip("Minimum distance between pyramids")]
    public float minIcePyramidDistance = 400f;
    [Tooltip("Maximum distance between pyramids")]
    public float maxIcePyramidDistance = 900f;
    [Tooltip("How many blocks wide the bottom row is")]
    public int pyramidBaseWidth = 3;
    [Tooltip("Width of a single block so they stack perfectly side-by-side")]
    public float blockWidth = 1f;
    [Tooltip("Height of a single block so they stack perfectly on top of each other")]
    public float blockHeight = 1f;

    private float nextIcePyramidX;

    // Feature Trackers
    private float nextSnowballX;
    private float nextSnowmanX;

    // Fluid Trackers
    private float nextLakeX;
    private float currentLakeWidth;
    private bool isBuildingLake = false;

    // Tunnel Trackers
    private float nextTunnelX;
    private float currentTunnelLength;
    private bool inFlatZone = false;
    
    private float tunnelStartY = 0f;
    private float tunnelEndY = 0f;
    
    // Ceiling Construction
    private bool isBuildingCeiling = false;
    private GameObject currentCeilingObj;
    private Spline currentCeilingSpline;
    private List<Vector2> ceilingPhysicsPoints;
    
    private List<Vector2> tempCeilingPoints; 
    
    private System.Random runRNG;
    private bool isNightTime = false; 

    void LateUpdate()
    {
        if (enableStaticTime && !hasTintedCar && playerCar != null)
        {
            hasTintedCar = true;
            TintObject(playerCar.gameObject);
        }
        
        if (playerCar != null)
        {
            CarController carScript = playerCar.GetComponent<CarController>();
            if (carScript != null)
            {
                carScript.ToggleHeadlights(isNightTime);
            }
        }

        if (skyBackgroundRenderer != null && Camera.main != null)
        {
            Vector3 camPos = Camera.main.transform.position;
            skyBackgroundRenderer.transform.position = new Vector3(camPos.x, camPos.y, skyBackgroundRenderer.transform.position.z);
        }

        if (currentCeilingObj != null)
        {
            SpriteShapeRenderer ceilingSSR = currentCeilingObj.GetComponent<SpriteShapeRenderer>();
            if (ceilingSSR != null)
            {
                ceilingSSR.SetLocalAABB(new Bounds(Vector3.zero, new Vector3(100000f, 10000f, 10000f)));
            }
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
                
        isNightTime = (runTime == TimeOfDay.Night);

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
        
        Debug.Log("Snow Map started at: " + runTime.ToString());
    }

    void TintObject(GameObject obj)
    {
        if (propTint == Color.white) return; 
        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in renderers) sr.color *= propTint;

        SpriteShapeRenderer[] shapeRenderers = obj.GetComponentsInChildren<SpriteShapeRenderer>();
        foreach (SpriteShapeRenderer ssr in shapeRenderers) 
        {
            ssr.color *= propTint;
        }

        FrozenLake[] lakes = obj.GetComponentsInChildren<FrozenLake>();
        foreach (FrozenLake lake in lakes)
        {
            lake.iceColor *= propTint;
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
            Color groundTint = Color.Lerp(Color.white, propTint, 0.9f);
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

        // Init Lakes
        float rawNextLake = minLakeDistance + (Mathf.PerlinNoise(seed + 1, seed) * (maxLakeDistance - minLakeDistance));
        nextLakeX = Mathf.Round(rawNextLake / pointSpacing) * pointSpacing;
        currentLakeWidth = Mathf.Round(Mathf.Lerp(minLakeWidth, maxLakeWidth, 0.5f) / pointSpacing) * pointSpacing;

        // Init Tunnels 
        float rawNextTunnel = minTunnelDistance + (Mathf.PerlinNoise(seed + 2, seed) * (maxTunnelDistance - minTunnelDistance));
        nextTunnelX = Mathf.Round(rawNextTunnel / pointSpacing) * pointSpacing;
        currentTunnelLength = Mathf.Round(Random.Range(minTunnelLength, maxTunnelLength) / pointSpacing) * pointSpacing;

        // Init Snowballs 
        float rawNextSnowball = minSnowballDistance + (Mathf.PerlinNoise(seed + 3, seed) * (maxSnowballDistance - minSnowballDistance));
        nextSnowballX = Mathf.Round(rawNextSnowball / pointSpacing) * pointSpacing;

        // Init Snowmen 
        float rawNextSnowman = minSnowmanDistance + (Mathf.PerlinNoise(seed + 4, seed) * (maxSnowmanDistance - minSnowmanDistance));
        nextSnowmanX = Mathf.Round(rawNextSnowman / pointSpacing) * pointSpacing;

        // Init Ice Pyramids 
        float rawNextPyramid = minIcePyramidDistance + (Mathf.PerlinNoise(seed + 5, seed) * (maxIcePyramidDistance - minIcePyramidDistance));
        nextIcePyramidX = Mathf.Round(rawNextPyramid / pointSpacing) * pointSpacing;
    }

    /// <summary>
    /// Checks whether the raw terrain profile from fluidX to fluidX + width is flat or forms a natural valley,
    /// avoiding steep hills, sloped grades, hill peaks, and floating visual mesh edges.
    /// </summary>
    private bool IsTerrainSuitableForFluid(float fluidX, float width)
    {
        if (!requireFlatOrValleyTerrain) return true;

        float startY = GetRawTerrainY(fluidX);
        float endY = GetRawTerrainY(fluidX + width);

        // 1. Bank Level Check: Start & End banks should be close in height
        float edgeDiff = Mathf.Abs(startY - endY);
        if (edgeDiff > maxFluidEdgeHeightDiff)
        {
            return false; // Terrain is on a steep global gradient
        }

        // Global Gradient Check: Ensure ground across entire pool width isn't a continuous incline/decline
        float globalSlope = edgeDiff / width;
        if (globalSlope > maxFluidTerrainSlope)
        {
            return false; // Overall hillside gradient is too steep for standing fluid
        }

        // Calculate fluid surface elevation
        float bankBaseY = Mathf.Min(startY, endY);
        float surfaceY = bankBaseY + waterLevelOffset;

        // Calculate visual padding bounds
        float edgePadding = Mathf.Clamp(width * 0.2f, 4f, 15f);
        float leftVisualX = fluidX - (edgePadding * 0.5f);
        float rightVisualX = fluidX + width + (edgePadding * 0.5f);

        // 2. Visual Padding Edge Check:
        // Ensure terrain outside both banks doesn't drop below the fluid surface level.
        // If the ground past either bank dips below surfaceY, the flat top fluid mesh will poke out of the slope.
        int paddingSamples = 3;
        for (int p = 1; p <= paddingSamples; p++)
        {
            float t = (float)p / paddingSamples;
            float checkLeftX = Mathf.Lerp(fluidX, leftVisualX, t);
            float checkRightX = Mathf.Lerp(fluidX + width, rightVisualX, t);

            if (GetRawTerrainY(checkLeftX) < surfaceY || GetRawTerrainY(checkRightX) < surfaceY)
            {
                return false; // Ground dips below fluid level in the visual padding region!
            }
        }

        // 3. Peak / Convex Hill Detection:
        // Real lakes rest in valleys (concave) or flat ground.
        // If ANY interior point is higher than the HIGHER of the two banks, it's a hill peak/mound.
        float maxBankY = Mathf.Max(startY, endY);

        int samples = 10; // Fine-grained sampling
        float step = width / samples;

        for (int k = 0; k <= samples; k++)
        {
            float currX = fluidX + k * step;
            float currY = GetRawTerrainY(currX);

            // REJECT PEAKS: Interior terrain rises above highest bank elevation
            if (currY > maxBankY + maxPeakBulgeTolerance)
            {
                return false; 
            }

            // Slope check against adjacent sample
            if (k > 0)
            {
                float prevY = GetRawTerrainY(fluidX + (k - 1) * step);
                float localSlope = Mathf.Abs(currY - prevY) / step;
                if (localSlope > maxFluidTerrainSlope)
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

        // ==========================================
        // TUNNEL & FLAT ZONE LOGIC
        // ==========================================
        float flatZoneStartX = nextTunnelX - 100f;
        float tunnelEndX = nextTunnelX + currentTunnelLength;
        float flatZoneEndX = tunnelEndX + 100f; 
        
        bool isInTunnelZone = generateTunnels && globalX >= flatZoneStartX && globalX <= flatZoneEndX;

        if (isInTunnelZone)
        {
            isHazardZone = true; 

            if (!inFlatZone)
            {
                inFlatZone = true;
                tunnelStartY = GetRawTerrainY(flatZoneStartX);
                tunnelEndY = GetRawTerrainY(flatZoneEndX);
            }

            float progress = Mathf.InverseLerp(flatZoneStartX, flatZoneEndX, globalX);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            float baseTunnelY = Mathf.Lerp(tunnelStartY, tunnelEndY, smoothProgress);

            float blendIn = Mathf.InverseLerp(flatZoneStartX, flatZoneStartX + 100f, globalX);
            float blendOut = Mathf.InverseLerp(flatZoneEndX, flatZoneEndX - 100f, globalX);
            float smoothT = Mathf.SmoothStep(0f, 1f, Mathf.Min(blendIn, blendOut));

            float smallHills = 0f;
            bool isActuallyInsideTunnel = globalX >= nextTunnelX && globalX <= tunnelEndX;

            // --- NEW: SPAWN INVISIBLE MEME TRIPWIRE ---
            if (!string.IsNullOrEmpty(tunnelStartMemeCategory))
            {
                GameObject triggerObj = new GameObject("TunnelMemeTrigger");
                // Place it exactly at the start of the 100m flat zone, reaching high up
                triggerObj.transform.position = new Vector3(globalX, tunnelStartY + 20f, 0f);
                triggerObj.transform.SetParent(chunkParent);

                BoxCollider2D triggerCol = triggerObj.AddComponent<BoxCollider2D>();
                triggerCol.isTrigger = true;
                triggerCol.size = new Vector2(10f, 100f); // 10m wide, 100m tall to easily catch the car

                TunnelMemeTrigger triggerScript = triggerObj.AddComponent<TunnelMemeTrigger>();
                triggerScript.memeCategory = tunnelStartMemeCategory;
            }

            
            if (isActuallyInsideTunnel) 
            {
                float hillFadeIn = Mathf.InverseLerp(nextTunnelX, nextTunnelX + 20f, globalX);
                float hillFadeOut = Mathf.InverseLerp(tunnelEndX, tunnelEndX - 20f, globalX);
                float hillMultiplier = Mathf.Min(hillFadeIn, hillFadeOut);
                
                smallHills = Mathf.Sin((globalX - nextTunnelX) * 0.15f) * 1.5f * hillMultiplier; 
            }

            finalY = Mathf.Lerp(originalY, baseTunnelY + smallHills, smoothT);

            if (i < numSurfacePoints)
            {
                SpawnPickups(new Vector3(globalX, finalY, 0f), chunkParent);
                TrySpawnIcePyramid(globalX, finalY);
            }

            // ==========================================
            // CEILING & ICICLE LOGIC
            // ==========================================
            if (isActuallyInsideTunnel && ceilingChunkPrefab != null)
            {
                if (!isBuildingCeiling)
                {
                    isBuildingCeiling = true;
                    StartCeiling(chunkParent, localX);
                }

                float gap = Mathf.Lerp(minTunnelHeight, maxTunnelHeight, Mathf.PerlinNoise(globalX * 0.08f, seed));
                float ceilingY = finalY + gap;

                AddCeilingPoint(localX, ceilingY);

                if (iciclePrefab != null && Random.value < icicleSpawnChance)
                {
                    GameObject icicle = Instantiate(iciclePrefab, new Vector3(globalX, ceilingY + icicleYOffset, 0f), Quaternion.identity, chunkParent);
                    TintObject(icicle);
                }

                if (i == numSurfacePoints || globalX >= tunnelEndX - 0.01f)
                {
                    FinishCeiling();
                    isBuildingCeiling = false;
                }
            }

            return false; 
        }
        else if (globalX > flatZoneEndX && inFlatZone)
        {
            inFlatZone = false;
            nextTunnelX = Mathf.Round((globalX + Mathf.Lerp(minTunnelDistance, maxTunnelDistance, Mathf.PerlinNoise(globalX, seed))) / pointSpacing) * pointSpacing;
            currentTunnelLength = Mathf.Round(Random.Range(minTunnelLength, maxTunnelLength) / pointSpacing) * pointSpacing;
        }

        // ==========================================
        // FLUID LOGIC (Frozen Lakes)
        // ==========================================
        if (generateLakes && globalX >= nextLakeX && !isBuildingLake)
        {
            // 1. Tunnel Zone Avoidance: Don't spawn a lake too close to or inside a tunnel.
            bool overlapsTunnel = (nextLakeX + currentLakeWidth >= nextTunnelX - 200f && nextLakeX <= nextTunnelX + currentTunnelLength + 200f);
            if (overlapsTunnel)
            {
                // Push completely past the tunnel zone
                nextLakeX = Mathf.Round((nextTunnelX + currentTunnelLength + 300f) / pointSpacing) * pointSpacing;
            }
            // 2. Terrain Suitability Check: If terrain is on a hill/slope, push fluid location forward point by point
            else if (!IsTerrainSuitableForFluid(nextLakeX, currentLakeWidth))
            {
                nextLakeX = Mathf.Round((nextLakeX + pointSpacing) / pointSpacing) * pointSpacing;
            }
        }

        bool inLakeZone = generateLakes && globalX >= nextLakeX && globalX <= nextLakeX + currentLakeWidth;

        if (inLakeZone)
        {
            isHazardZone = true;
            if (!isBuildingLake)
            {
                isBuildingLake = true;
                float startY = GetRawTerrainY(nextLakeX);
                float endY = GetRawTerrainY(nextLakeX + currentLakeWidth);
                
                // Use minimum bank Y to anchor top surface correctly below both banks
                float bankBaseY = Mathf.Min(startY, endY); 
                
                float edgePadding = Mathf.Clamp(currentLakeWidth * 0.2f, 4f, 15f);
                float paddedVisualWidth = currentLakeWidth + edgePadding;
                float paddedVisualDepth = waterMeshDepth; 
 
                float surfaceY = bankBaseY + waterLevelOffset;
                float centerOffsetY = surfaceY - (paddedVisualDepth / 2f);
                
                Vector2 centerPos = new Vector2(nextLakeX + currentLakeWidth / 2f, centerOffsetY);
                
                GameObject fluid = null; 
                if (waterPoolPrefab != null)
                {
                    fluid = Instantiate(waterPoolPrefab, new Vector3(centerPos.x, centerPos.y, 0f), Quaternion.identity, chunkParent);
                    MeshRenderer mr = fluid.GetComponent<MeshRenderer>();
                    if (mr != null) mr.sortingOrder = waterSortingOrder;
                    
                    DynamicWater2D fluidScript = fluid.GetComponent<DynamicWater2D>();
                    if (fluidScript != null)
                    {
                        if (propTint != Color.white)
                        {
                            Color fluidFinalTint = Color.LerpUnclamped(Color.white, propTint, 1.2f);
                            fluidScript.surfaceColor *= fluidFinalTint;
                            fluidScript.depthColor *= fluidFinalTint;
                        }
                        fluidScript.GenerateWater(paddedVisualWidth, paddedVisualDepth, currentLakeWidth);
                    }
                    else fluid.transform.localScale = new Vector3(paddedVisualWidth, paddedVisualDepth, 1f);

                    TintObject(fluid);
                }

                if (frozenLakePrefab != null)
                {
                    Vector3 icePos = new Vector3(nextLakeX + (currentLakeWidth / 2f), surfaceY, 0f);
                    GameObject iceSheet = Instantiate(frozenLakePrefab, icePos, Quaternion.identity, chunkParent);
                    iceSheet.transform.localScale = new Vector3(currentLakeWidth, 1f, 1f);
                    TintObject(iceSheet);

                    FrozenLake iceScript = iceSheet.GetComponent<FrozenLake>();
                    if (iceScript != null && fluid != null)
                    {
                        iceScript.hiddenWaterPool = fluid; 
                        fluid.SetActive(false); 
                    }
                }
            }
            
            // Raised-Cosine curve: blend slope smoothly into and out of the lake bed (derivative at edges = 0)
            float dipPercent = (globalX - nextLakeX) / currentLakeWidth;
            float smoothFactor = 0.5f * (1f - Mathf.Cos(dipPercent * 2f * Mathf.PI));
            float dipAmount = smoothFactor * lakeDepth;
            finalY -= dipAmount; 
            return false; 
        }
        else if (globalX > nextLakeX + currentLakeWidth && isBuildingLake)
        {
            isBuildingLake = false;
            
            float nextDist = Mathf.Lerp(minLakeDistance, maxLakeDistance, Mathf.PerlinNoise(globalX * 0.2f, seed + 10));
            nextLakeX = Mathf.Round((globalX + nextDist) / pointSpacing) * pointSpacing;
            
            currentLakeWidth = Mathf.Round(Mathf.Lerp(minLakeWidth, maxLakeWidth, Mathf.PerlinNoise(nextLakeX, seed)) / pointSpacing) * pointSpacing;
        }

        return false;
    }

    protected override bool ProcessHazards(float globalX, float originalY, int i, int numSurfacePoints, Transform chunkParent)
    {
        if (i >= numSurfacePoints) return false;

        if (TrySpawnIcePyramid(globalX, originalY)) return true;

        if (generateGiantSnowballs && giantSnowballPrefab != null && globalX >= nextSnowballX)
        {
            Vector3 spawnPos = new Vector3(globalX, originalY + snowballYOffset, 0f);
            GameObject snowball = Instantiate(giantSnowballPrefab, spawnPos, Quaternion.identity, chunkParent);
            TintObject(snowball);

            float nextDist = Mathf.Lerp(minSnowballDistance, maxSnowballDistance, Mathf.PerlinNoise(globalX * 0.1f, seed + 4));
            nextSnowballX = Mathf.Round((globalX + nextDist) / pointSpacing) * pointSpacing;

            return true; 
        }

        if (generateSnowmen && snowmanPrefab != null && globalX >= nextSnowmanX)
        {
            Vector3 spawnPos = new Vector3(globalX, originalY + snowmanYOffset, 0f);
            
            float yAhead = GetRawTerrainY(globalX + 0.1f);
            float yBehind = GetRawTerrainY(globalX - 0.1f);
            float terrainAngle = Mathf.Atan2((yAhead - yBehind), 0.2f) * Mathf.Rad2Deg;
            
            GameObject snowman = Instantiate(snowmanPrefab, spawnPos, Quaternion.Euler(0f, 0f, terrainAngle), chunkParent);
            TintObject(snowman);

            float nextDist = Mathf.Lerp(minSnowmanDistance, maxSnowmanDistance, Mathf.PerlinNoise(globalX * 0.2f, seed + 5));
            nextSnowmanX = Mathf.Round((globalX + nextDist) / pointSpacing) * pointSpacing;

            return true; 
        }

        return false; 
    }

    // ==========================================
    // ICE PYRAMID BUILDER
    // ==========================================

    private void SpawnIcePyramid(float startX, float startY, Transform chunkParent)
    {
        float currentY = startY + (blockHeight / 2f); 

        for (int row = 0; row < pyramidBaseWidth; row++)
        {
            int blocksInRow = pyramidBaseWidth - row;
            float startXOffset = -((blocksInRow - 1) * blockWidth) / 2f;

            for (int col = 0; col < blocksInRow; col++)
            {
                float spawnX = startX + startXOffset + (col * blockWidth);
                Vector3 spawnPos = new Vector3(spawnX, currentY, 0f);
                
                GameObject iceBlock = Instantiate(iceBlockPrefab, spawnPos, Quaternion.identity);
                TintObject(iceBlock);
            }
            
            currentY += blockHeight; 
        }
    }

    private bool TrySpawnIcePyramid(float globalX, float groundY)
    {
        if (generateIcePyramids && iceBlockPrefab != null && globalX >= nextIcePyramidX)
        {
            SpawnIcePyramid(globalX, groundY, null);

            float nextDist = Mathf.Lerp(minIcePyramidDistance, maxIcePyramidDistance, Mathf.PerlinNoise(globalX * 0.3f, seed + 6));
            nextIcePyramidX = Mathf.Round((globalX + nextDist) / pointSpacing) * pointSpacing;

            return true; 
        }
        return false;
    }

    // ==========================================
    // CEILING BUILDER HELPERS
    // ==========================================
    void StartCeiling(Transform chunkParent, float localX)
    {
        if (ceilingChunkPrefab == null) return;

        currentCeilingObj = Instantiate(ceilingChunkPrefab, chunkParent.position, Quaternion.identity, chunkParent);
        
        // SpriteShape profile only has snow assigned to the "UP" normal (top half).
        // By drawing left-to-right but flipping the object, the "UP" snow texture hangs downwards!
        currentCeilingObj.transform.localScale = new Vector3(1f, -1f, 1f);
        
        SpriteShapeController ssc = currentCeilingObj.GetComponent<SpriteShapeController>();
        currentCeilingSpline = ssc.spline;
        
        tempCeilingPoints = new List<Vector2>();
        ceilingPhysicsPoints = new List<Vector2>();
    }

    void AddCeilingPoint(float localX, float ceilingY)
    {
        // Because the object is flipped Y, we must invert the local Y coordinates
        float invertedY = -ceilingY;
        
        tempCeilingPoints.Add(new Vector2(localX, invertedY));
        ceilingPhysicsPoints.Add(new Vector2(localX, invertedY));
    }

    void FinishCeiling()
    {
        if (tempCeilingPoints == null || tempCeilingPoints.Count == 0) return;

        currentCeilingSpline.Clear();

        // Draw Left to Right normally so it uses the top arc of your SpriteShape Profile
        float leftmostX = tempCeilingPoints[0].x;
        float rightmostX = tempCeilingPoints[tempCeilingPoints.Count - 1].x;
        
        float ceilingTop = -100f; 

        // 1. Start the spline top-left
        currentCeilingSpline.InsertPointAt(0, new Vector3(leftmostX, ceilingTop, 0f));
        currentCeilingSpline.SetTangentMode(0, ShapeTangentMode.Linear);
        
        // 2. Draw the actual surface points (Left to Right)
        int splineIndex = 1;
        foreach (Vector2 pt in tempCeilingPoints)
        {
            currentCeilingSpline.InsertPointAt(splineIndex, new Vector3(pt.x, pt.y, 0f));
            currentCeilingSpline.SetTangentMode(splineIndex, ShapeTangentMode.Continuous);
            splineIndex++;
        }

        // 3. Close the shape top-right
        currentCeilingSpline.InsertPointAt(splineIndex, new Vector3(rightmostX, ceilingTop, 0f));
        currentCeilingSpline.SetTangentMode(splineIndex, ShapeTangentMode.Linear);

        SpriteShapeController ssc = currentCeilingObj.GetComponent<SpriteShapeController>();
        ssc.RefreshSpriteShape();

        if (currentCeilingSpline.GetPointCount() > 0)
        {
            Bounds localBounds = new Bounds(currentCeilingSpline.GetPosition(0), Vector3.zero);
            for (int k = 1; k < currentCeilingSpline.GetPointCount(); k++) localBounds.Encapsulate(currentCeilingSpline.GetPosition(k));
            if (ssc.spriteShapeRenderer != null)
            {
                Color groundTint = Color.Lerp(Color.white, propTint, 0.75f);
                ssc.spriteShapeRenderer.color = groundTint;
            }
        }

        // Apply EdgeCollider
        EdgeCollider2D col = currentCeilingObj.GetComponent<EdgeCollider2D>();
        if (col == null) col = currentCeilingObj.AddComponent<EdgeCollider2D>();
        col.SetPoints(ceilingPhysicsPoints);
        
        tempCeilingPoints.Clear();
    }
}

// --- NEW: INVISIBLE TRIPWIRE CLASS ---
// This listens for the car to drive through it, plays the meme, and then disables itself!
public class TunnelMemeTrigger : MonoBehaviour
{
    public string memeCategory;
    private bool hasTriggered = false;

    void OnTriggerEnter2D(Collider2D col)
    {
        if (hasTriggered) return;
        
        // Ensure it's the car that hit the tripwire
        if (col.GetComponentInParent<CarController>() != null)
        {
            hasTriggered = true;
            if (MemeManager.instance != null && !string.IsNullOrEmpty(memeCategory))
            {
                MemeManager.instance.TriggerReaction(memeCategory);
            }
            // --- BUG FIX: Completely delete the tripwire so it can NEVER fire twice! ---
            Destroy(gameObject);
        }
    }
}
