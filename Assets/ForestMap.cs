using UnityEngine;
using UnityEngine.U2D; 
using System.Collections.Generic;

public class ForestMap : MapGenerator
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

    [Header("--- FOREST SPECIFIC FEATURES ---")]
    
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
    public float waterDepth = 8f;
    public float waterLevelOffset = -2f;
    public int waterSortingOrder = 3;

    [Header("Bouncy Hazards (e.g. Giant Mushrooms)")]
    public bool generateBouncyHazards = true;
    public GameObject bouncyHazardPrefab;
    public float minBounceDistance = 200f;
    public float maxBounceDistance = 400f;
    [Tooltip("Adjust to sink the bouncy objects perfectly into the grass")]
    public float bouncyYOffset = 0.5f;
    [Tooltip("If true, rotates the bouncy hazard to match the angle of the terrain")]
    public bool alignBouncyToTerrain = true;


    [Header("NEW: Bear Traps (Tire Poppers)")]
    public bool generateBearTraps = true;
    public GameObject bearTrapPrefab;
    public float minTrapDistance = 150f;
    public float maxTrapDistance = 350f;
    [Tooltip("Adjust to bury the trap slightly into the dirt so it's harder to see!")]
    public float trapYOffset = -0.2f;

    [Header("NEW: Speed Shrooms (Boosters)")]
    public bool generateSpeedShrooms = true;
    public GameObject speedShroomPrefab;
    public float minShroomDistance = 300f;
    public float maxShroomDistance = 600f;
    [Tooltip("Adjust if the mushroom needs to sit higher or lower on the ground")]
    public float shroomYOffset = 0f;

    [Header("NEW: Giant Seesaws")]
    public bool generateSeesaws = true;
    public GameObject seesawPrefab;
    public float minSeesawDistance = 350f;
    public float maxSeesawDistance = 750f;
    [Tooltip("Adjust to bury the rock base slightly into the dirt")]
    public float seesawYOffset = -0.5f;

    [Header("NEW: Bee Hives (Fuel Drain)")]
    public bool generateBeeHives = true;
    public GameObject beeHivePrefab;
    public float minHiveDistance = 450f;
    public float maxHiveDistance = 850f;
    [Tooltip("Adjust to make the hive sit in the grass or float slightly above the road!")]
    public float hiveYOffset = 0.2f;


    private float nextBridgeX;
    private float currentBridgeGapWidth;
    private float nextWaterX;
    private float currentWaterWidth;
    private bool isBuildingWater = false;
    private float nextBounceX; 
    private float nextTrapX; 
    private float nextShroomX; 
    private float nextSeesawX; 
    private float nextHiveX; // Tracker for the Bee Hives
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

        Debug.Log("Forest Map started at: " + runTime.ToString());
    }

    // --- NEW: Changed to 'public' so BeeHives can access it when spawning bees mid-game! ---
    public void TintObject(GameObject obj)
    {
        if (propTint == Color.white) return;

        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in renderers)
        {
            sr.color *= propTint;
        }

        // --- NEW: Also tint Sprite Shapes! (This fixes the Mud/Fluid hazards if they use Sprite Shapes) ---
        SpriteShapeRenderer[] shapeRenderers = obj.GetComponentsInChildren<SpriteShapeRenderer>();
        foreach (SpriteShapeRenderer ssr in shapeRenderers)
        {
            ssr.color *= propTint;
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

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer != -1)
        {
            chunk.layer = groundLayer;
            foreach (Transform child in chunk.transform)
            {
                child.gameObject.layer = groundLayer;
            }
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

        float rawNextBounce = minBounceDistance + (Mathf.PerlinNoise(seed + 3, seed) * 50f);
        nextBounceX = Mathf.Round(rawNextBounce / pointSpacing) * pointSpacing;

        float rawNextTrap = minTrapDistance + (Mathf.PerlinNoise(seed + 4, seed) * 50f);
        nextTrapX = Mathf.Round(rawNextTrap / pointSpacing) * pointSpacing;

        float rawNextShroom = minShroomDistance + (Mathf.PerlinNoise(seed + 5, seed) * 100f);
        nextShroomX = Mathf.Round(rawNextShroom / pointSpacing) * pointSpacing;

        float rawNextSeesaw = minSeesawDistance + (Mathf.PerlinNoise(seed + 6, seed) * 150f);
        nextSeesawX = Mathf.Round(rawNextSeesaw / pointSpacing) * pointSpacing;

        // Initialize the first Bee Hive position
        float rawNextHive = minHiveDistance + (Mathf.PerlinNoise(seed + 7, seed) * 100f);
        nextHiveX = Mathf.Round(rawNextHive / pointSpacing) * pointSpacing;
    }

    protected override bool ProcessTerrainSpline(float globalX, float localX, float originalY, ref float finalY, Spline spline, List<Vector2> physicsPoints, ref int pointIndex, int i, int numSurfacePoints, Transform chunkParent, out bool isHazardZone)
    {
        isHazardZone = false;

        // --- WATER/MUD LOGIC ---
        bool inWaterZone = generateWaterPools && globalX >= nextWaterX && globalX <= nextWaterX + currentWaterWidth;
        if (inWaterZone)
        {
            isHazardZone = true;
            if (!isBuildingWater)
            {
                isBuildingWater = true;
                float startY = GetRawTerrainY(nextWaterX);
                float endY = GetRawTerrainY(nextWaterX + currentWaterWidth);
                float midY = (startY + endY) / 2f; 
                
                float edgePadding = Mathf.Clamp(currentWaterWidth * 0.2f, 4f, 15f);
                float paddedVisualWidth = currentWaterWidth + edgePadding;
                float paddedVisualDepth = (waterDepth * 2f) + 20f; 
                
                float surfaceY = midY + waterLevelOffset;
                float centerOffsetY = surfaceY - (paddedVisualDepth / 2f);
                
                Vector2 centerPos = new Vector2(nextWaterX + currentWaterWidth / 2f, centerOffsetY);
                bool isMud = mudPoolPrefab != null && (Mathf.PerlinNoise(nextWaterX * 0.1f, seed) < mudProbability);
                SpawnFluid(centerPos, paddedVisualWidth, paddedVisualDepth, currentWaterWidth, chunkParent, isMud);
            }
            
            float dipPercent = (globalX - nextWaterX) / currentWaterWidth;
            float dipAmount = Mathf.Sin(dipPercent * Mathf.PI) * waterDepth;
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

        // --- ROPE BRIDGE LOGIC ---
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

        bool isTrapPoint = generateBearTraps && Mathf.Abs(globalX - nextTrapX) < 0.01f;
        if (isTrapPoint)
        {
            if (bearTrapPrefab != null) 
            {
                GameObject trap = Instantiate(bearTrapPrefab, new Vector3(globalX, originalY + trapYOffset, 0f), Quaternion.identity, chunkParent);
                TintObject(trap);
            }
            nextTrapX = Mathf.Round((nextTrapX + Mathf.Lerp(minTrapDistance, maxTrapDistance, Mathf.PerlinNoise(globalX * 0.75f, seed))) / pointSpacing) * pointSpacing;
            return true;
        }

        bool isBouncePoint = generateBouncyHazards && Mathf.Abs(globalX - nextBounceX) < 0.01f;
        if (isBouncePoint)
        {
            if (bouncyHazardPrefab != null) 
            {
                GameObject bouncy = Instantiate(bouncyHazardPrefab, new Vector3(globalX, originalY + bouncyYOffset, 0f), Quaternion.identity, chunkParent);
                
                if (alignBouncyToTerrain)
                {
                    float deltaX = 0.5f;
                    float leftY = GetRawTerrainY(globalX - deltaX);
                    float rightY = GetRawTerrainY(globalX + deltaX);
                    
                    float angle = Mathf.Atan2(rightY - leftY, deltaX * 2f) * Mathf.Rad2Deg;
                    bouncy.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }
                
                TintObject(bouncy);
            }
            nextBounceX = Mathf.Round((nextBounceX + Mathf.Lerp(minBounceDistance, maxBounceDistance, Mathf.PerlinNoise(globalX * 0.4f, seed))) / pointSpacing) * pointSpacing;
            return true;
        }

        bool isShroomPoint = generateSpeedShrooms && Mathf.Abs(globalX - nextShroomX) < 0.01f;
        if (isShroomPoint)
        {
            if (speedShroomPrefab != null)
            {
                GameObject shroom = Instantiate(speedShroomPrefab, new Vector3(globalX, originalY + shroomYOffset, 0f), Quaternion.identity, chunkParent);
                
                SpeedShroom shroomScript = shroom.GetComponent<SpeedShroom>();
                Color originalGlowColor = Color.white;

                if (shroomScript != null)
                {
                    shroomScript.isActiveAtNight = (activeTimeOfDay == TimeOfDay.Night);
                    if (shroomScript.glowRenderer != null) originalGlowColor = shroomScript.glowRenderer.color;
                }

                // Always tint the shroom so the base sprite gets dark!
                TintObject(shroom);

                // If it is night time, instantly restore ONLY the glow sprite back to its full brightness!
                if (activeTimeOfDay == TimeOfDay.Night && shroomScript != null && shroomScript.glowRenderer != null)
                {
                    shroomScript.glowRenderer.color = originalGlowColor;
                }
            }
            
            nextShroomX = Mathf.Round((nextShroomX + Mathf.Lerp(minShroomDistance, maxShroomDistance, Mathf.PerlinNoise(globalX * 0.85f, seed))) / pointSpacing) * pointSpacing;
            return true;
        }

        bool isSeesawPoint = generateSeesaws && Mathf.Abs(globalX - nextSeesawX) < 0.01f;
        if (isSeesawPoint)
        {
            if (seesawPrefab != null)
            {
                GameObject seesaw = Instantiate(seesawPrefab, new Vector3(globalX, originalY + seesawYOffset, 0f), Quaternion.identity, chunkParent);
                TintObject(seesaw);
            }
            nextSeesawX = Mathf.Round((nextSeesawX + Mathf.Lerp(minSeesawDistance, maxSeesawDistance, Mathf.PerlinNoise(globalX * 0.95f, seed))) / pointSpacing) * pointSpacing;
            return true;
        }

        // NEW: Check and place Bee Hives
        bool isHivePoint = generateBeeHives && Mathf.Abs(globalX - nextHiveX) < 0.01f;
        if (isHivePoint)
        {
            if (beeHivePrefab != null)
            {
                GameObject hive = Instantiate(beeHivePrefab, new Vector3(globalX, originalY + hiveYOffset, 0f), Quaternion.identity, chunkParent);
                TintObject(hive);
            }
            // Calculate when the next hive should spawn
            nextHiveX = Mathf.Round((nextHiveX + Mathf.Lerp(minHiveDistance, maxHiveDistance, Mathf.PerlinNoise(globalX * 0.55f, seed))) / pointSpacing) * pointSpacing;
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