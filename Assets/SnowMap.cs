using UnityEngine;
using UnityEngine.U2D; 
using System.Collections.Generic;

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
    public float minLakeDistance = 300f; // <-- NEW
    [Tooltip("Maximum distance between lakes")]
    public float maxLakeDistance = 700f; // <-- NEW
    public float minLakeWidth = 15f;
    public float maxLakeWidth = 40f;
    public float lakeDepth = 8f;
    public float waterLevelOffset = -2f;
    public int waterSortingOrder = 3;

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

    [Header("4. Snowmen Obstacles")]
    public bool generateSnowmen = true;
    public GameObject snowmanPrefab;
    [Tooltip("Minimum distance between snowmen")]
    public float minSnowmanDistance = 150f;
    [Tooltip("Maximum distance between snowmen")]
    public float maxSnowmanDistance = 400f;
    public float snowmanYOffset = 0.5f;

   [Header("3. Giant Snowballs")]
    public bool generateGiantSnowballs = true;
    public GameObject giantSnowballPrefab;
    [Tooltip("Minimum distance between giant snowballs")]
    public float minSnowballDistance = 800f;
    [Tooltip("Maximum distance between giant snowballs")]
    public float maxSnowballDistance = 1500f;
    [Tooltip("Spawns the snowball slightly in the air so it drops naturally onto the slope")]
    public float snowballYOffset = 3f;

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
    private int ceilingPtIndex = 0;
    
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

        // FIX: Prevent the cave ceiling from visually disappearing/flickering!
        // Unity culls SpriteShapeRenderers that are dynamically built because their bounds stay at 0.
        // We force a massive bounding box on the active ceiling so it always renders on camera.
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

        // 2. FIX: Tint SpriteShapes (Frozen Lake surface, Ice Pyramids, etc)
        SpriteShapeRenderer[] shapeRenderers = obj.GetComponentsInChildren<SpriteShapeRenderer>();
        foreach (SpriteShapeRenderer ssr in shapeRenderers) 
        {
            ssr.color *= propTint;
        }

        // 3. FIX: Tint Frozen Lakes directly! 
        // We modify the iceColor variable before Start() runs and builds the mesh.
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

        // Init Lakes
        float rawNextLake = minLakeDistance + (Mathf.PerlinNoise(seed + 1, seed) * (maxLakeDistance - minLakeDistance));
        nextLakeX = Mathf.Round(rawNextLake / pointSpacing) * pointSpacing;
        currentLakeWidth = Mathf.Round(Mathf.Lerp(minLakeWidth, maxLakeWidth, 0.5f) / pointSpacing) * pointSpacing;

        // Init Tunnels 
        float rawNextTunnel = minTunnelDistance + (Mathf.PerlinNoise(seed + 2, seed) * (maxTunnelDistance - minTunnelDistance));
        nextTunnelX = Mathf.Round(rawNextTunnel / pointSpacing) * pointSpacing;
        currentTunnelLength = Mathf.Round(Random.Range(minTunnelLength, maxTunnelLength) / pointSpacing) * pointSpacing;

        // --- NEW: Init Snowballs ---
        float rawNextSnowball = minSnowballDistance + (Mathf.PerlinNoise(seed + 3, seed) * (maxSnowballDistance - minSnowballDistance));
        nextSnowballX = Mathf.Round(rawNextSnowball / pointSpacing) * pointSpacing;

        // --- NEW: Init Snowmen ---
        float rawNextSnowman = minSnowmanDistance + (Mathf.PerlinNoise(seed + 4, seed) * (maxSnowmanDistance - minSnowmanDistance));
        nextSnowmanX = Mathf.Round(rawNextSnowman / pointSpacing) * pointSpacing;

        // --- NEW: Init Ice Pyramids ---
        float rawNextPyramid = minIcePyramidDistance + (Mathf.PerlinNoise(seed + 5, seed) * (maxIcePyramidDistance - minIcePyramidDistance));
        nextIcePyramidX = Mathf.Round(rawNextPyramid / pointSpacing) * pointSpacing;
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
            // Disable background trees and normal obstacles in the flat zone & tunnel
            isHazardZone = true; 

            // 1. Capture the exact natural heights of the terrain 100m before AND after the tunnel
            if (!inFlatZone)
            {
                inFlatZone = true;
                tunnelStartY = GetRawTerrainY(flatZoneStartX);
                tunnelEndY = GetRawTerrainY(flatZoneEndX);
            }

            // SmoothStep for a base curve instead of a strict linear ramp to prevent sharp V-shapes
            float progress = Mathf.InverseLerp(flatZoneStartX, flatZoneEndX, globalX);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            float baseTunnelY = Mathf.Lerp(tunnelStartY, tunnelEndY, smoothProgress);

            // 3. Smoothly blend the natural Perlin noise into our smooth base.
            float blendIn = Mathf.InverseLerp(flatZoneStartX, flatZoneStartX + 100f, globalX);
            float blendOut = Mathf.InverseLerp(flatZoneEndX, flatZoneEndX - 100f, globalX);
            float smoothT = Mathf.SmoothStep(0f, 1f, Mathf.Min(blendIn, blendOut));

            // 4. Add tiny bumps ONLY inside the tunnel, fading them in/out so they don't cause sharp cliff steps!
            float smallHills = 0f;
            bool isActuallyInsideTunnel = globalX >= nextTunnelX && globalX <= tunnelEndX;
            
            if (isActuallyInsideTunnel) 
            {
                float hillFadeIn = Mathf.InverseLerp(nextTunnelX, nextTunnelX + 20f, globalX);
                float hillFadeOut = Mathf.InverseLerp(tunnelEndX, tunnelEndX - 20f, globalX);
                float hillMultiplier = Mathf.Min(hillFadeIn, hillFadeOut);
                
                smallHills = Mathf.Sin((globalX - nextTunnelX) * 0.15f) * 1.5f * hillMultiplier; 
            }

            // Apply the final perfectly smooth math
            finalY = Mathf.Lerp(originalY, baseTunnelY + smallHills, smoothT);

            // MANUALLY SPAWN PICKUPS INSIDE TUNNEL
            // Because we set isHazardZone = true (to block props), the base MapGenerator skips pickups here.
            // We manually call SpawnPickups so coins/gems spawn normally riding along the new finalY smooth floor!
            if (i < numSurfacePoints)
            {
                SpawnPickups(new Vector3(globalX, finalY, 0f), chunkParent);

                // NEW: Manually check and spawn Ice Pyramids on the tunnel floor!
                TrySpawnIcePyramid(globalX, finalY);
            }

            // ==========================================
            // CEILING & ICICLE LOGIC
            // ==========================================
            if (isActuallyInsideTunnel && ceilingChunkPrefab != null)
            {
                // Start a new ceiling chunk
                if (!isBuildingCeiling)
                {
                    isBuildingCeiling = true;
                    StartCeiling(chunkParent, localX);
                }

                // Calculate the erratic gap between floor and ceiling
                float gap = Mathf.Lerp(minTunnelHeight, maxTunnelHeight, Mathf.PerlinNoise(globalX * 0.08f, seed));
                float ceilingY = finalY + gap;

                AddCeilingPoint(localX, ceilingY);

                // Try spawning an icicle on this ceiling point!
                if (iciclePrefab != null && Random.value < icicleSpawnChance)
                {
                    // FIXED: Added icicleYOffset to prevent the icicle from spawning inside the solid ceiling collider!
                    GameObject icicle = Instantiate(iciclePrefab, new Vector3(globalX, ceilingY + icicleYOffset, 0f), Quaternion.identity, chunkParent);
                    TintObject(icicle);
                }

                // Cap off the ceiling if the chunk ends OR the tunnel ends!
                if (i == numSurfacePoints || globalX >= tunnelEndX - 0.01f)
                {
                    FinishCeiling(localX);
                    isBuildingCeiling = false;
                }
            }

            return false; // Let MapGenerator draw the floor point!
        }
        else if (globalX > flatZoneEndX && inFlatZone)
        {
            inFlatZone = false;
            nextTunnelX = Mathf.Round((globalX + Mathf.Lerp(minTunnelDistance, maxTunnelDistance, Mathf.PerlinNoise(globalX, seed))) / pointSpacing) * pointSpacing;
            currentTunnelLength = Mathf.Round(Random.Range(minTunnelLength, maxTunnelLength) / pointSpacing) * pointSpacing;
        }

        // ==========================================
        // FLUID LOGIC (Lakes)
        // ==========================================
        bool inLakeZone = generateLakes && globalX >= nextLakeX && globalX <= nextLakeX + currentLakeWidth;
        
        // Prevent lakes from spawning inside tunnels
        if (inLakeZone && (globalX >= nextTunnelX - 200f && globalX <= nextTunnelX + currentTunnelLength + 200f))
        {
            nextLakeX = nextTunnelX + currentTunnelLength + 300f;
            inLakeZone = false;
            isBuildingLake = false;
        }

        if (inLakeZone)
        {
            isHazardZone = true;
            if (!isBuildingLake)
            {
                isBuildingLake = true;
                float startY = GetRawTerrainY(nextLakeX);
                float endY = GetRawTerrainY(nextLakeX + currentLakeWidth);
                float midY = (startY + endY) / 2f; 
                
                float paddedVisualWidth = currentLakeWidth + 15f;
                float paddedVisualDepth = (lakeDepth * 2f) + 20f; 
                
                float surfaceY = midY + waterLevelOffset;
                float centerOffsetY = surfaceY - (paddedVisualDepth / 2f);
                
                Vector2 centerPos = new Vector2(nextLakeX + currentLakeWidth / 2f, centerOffsetY);
                
                // 1. Spawn the liquid water pool
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
                            // FIXED: Added a stronger lerp for the liquid water underneath the ice!
                            Color fluidFinalTint = Color.LerpUnclamped(Color.white, propTint, 1.2f);
                            fluidScript.surfaceColor *= fluidFinalTint;
                            fluidScript.depthColor *= fluidFinalTint;

                        }
                        fluidScript.GenerateWater(paddedVisualWidth, paddedVisualDepth, currentLakeWidth);
                    }
                    else fluid.transform.localScale = new Vector3(paddedVisualWidth, paddedVisualDepth, 1f);

                    TintObject(fluid);
                }

                // 2. Spawn the solid ice sheet on top of the water!
                if (frozenLakePrefab != null)
                {
                    Vector3 icePos = new Vector3(nextLakeX + (currentLakeWidth / 2f), surfaceY, 0f);
                    GameObject iceSheet = Instantiate(frozenLakePrefab, icePos, Quaternion.identity, chunkParent);
                    iceSheet.transform.localScale = new Vector3(currentLakeWidth, 1f, 1f);
                    TintObject(iceSheet);

                    // Link the water to the ice, and turn the water off!
                    FrozenLake iceScript = iceSheet.GetComponent<FrozenLake>();
                    if (iceScript != null && fluid != null)
                    {
                        iceScript.hiddenWaterPool = fluid; 
                        fluid.SetActive(false); 
                    }
                }
            }
            
            float dipPercent = (globalX - nextLakeX) / currentLakeWidth;
            float dipAmount = Mathf.Sin(dipPercent * Mathf.PI) * lakeDepth;
            finalY -= dipAmount; 
            return false; 
        }
        else if (globalX > nextLakeX + currentLakeWidth && isBuildingLake)
        {
            isBuildingLake = false;
            
            // FIXED: Now uses your Min/Max variables instead of hardcoded numbers!
            float nextDist = Mathf.Lerp(minLakeDistance, maxLakeDistance, Mathf.PerlinNoise(globalX * 0.2f, seed + 10));
            nextLakeX = Mathf.Round((globalX + nextDist) / pointSpacing) * pointSpacing;
            
            currentLakeWidth = Mathf.Round(Mathf.Lerp(minLakeWidth, maxLakeWidth, Mathf.PerlinNoise(nextLakeX, seed)) / pointSpacing) * pointSpacing;
        }

        return false;
    }

    protected override bool ProcessHazards(float globalX, float originalY, int i, int numSurfacePoints, Transform chunkParent)
    {
        if (i >= numSurfacePoints) return false;

        // 3. Check for Ice Pyramids first
        if (TrySpawnIcePyramid(globalX, originalY)) return true;

        // 1. Check for Giant Snowballs First (They take priority if both land on the same spot)
        if (generateGiantSnowballs && giantSnowballPrefab != null && globalX >= nextSnowballX)
        {
            Vector3 spawnPos = new Vector3(globalX, originalY + snowballYOffset, 0f);
            GameObject snowball = Instantiate(giantSnowballPrefab, spawnPos, Quaternion.identity, chunkParent);
            TintObject(snowball);

            float nextDist = Mathf.Lerp(minSnowballDistance, maxSnowballDistance, Mathf.PerlinNoise(globalX * 0.1f, seed + 4));
            nextSnowballX = Mathf.Round((globalX + nextDist) / pointSpacing) * pointSpacing;

            return true; // Stop here so we don't spawn a snowman and a snowball stacked together!
        }

        // 2. Check for Snowmen
        if (generateSnowmen && snowmanPrefab != null && globalX >= nextSnowmanX)
        {
            // Spawn the snowman flush with the terrain
            Vector3 spawnPos = new Vector3(globalX, originalY + snowmanYOffset, 0f);
            
            // Optional: Tilt the snowman to match the hill slope
            float yAhead = GetRawTerrainY(globalX + 0.1f);
            float yBehind = GetRawTerrainY(globalX - 0.1f);
            float terrainAngle = Mathf.Atan2((yAhead - yBehind), 0.2f) * Mathf.Rad2Deg;
            
            GameObject snowman = Instantiate(snowmanPrefab, spawnPos, Quaternion.Euler(0f, 0f, terrainAngle), chunkParent);
            TintObject(snowman);

            float nextDist = Mathf.Lerp(minSnowmanDistance, maxSnowmanDistance, Mathf.PerlinNoise(globalX * 0.2f, seed + 5));
            nextSnowmanX = Mathf.Round((globalX + nextDist) / pointSpacing) * pointSpacing;

            return true; // Prevents coins from spawning inside the snowman
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
                
                // FIXED: Removed 'chunkParent' so it is no longer tied to the deleting terrain!
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
            // Call the spawn method (passing null for the chunkParent since we unparented them!)
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
        
        SpriteShapeController ssc = currentCeilingObj.GetComponent<SpriteShapeController>();
        currentCeilingSpline = ssc.spline;
        currentCeilingSpline.Clear();
        
        ceilingPhysicsPoints = new List<Vector2>();

        currentCeilingSpline.InsertPointAt(0, new Vector3(localX, 100f, 0f));
        currentCeilingSpline.SetTangentMode(0, ShapeTangentMode.Linear);
        ceilingPtIndex = 1;
    }

    void AddCeilingPoint(float localX, float ceilingY)
    {
        currentCeilingSpline.InsertPointAt(ceilingPtIndex, new Vector3(localX, ceilingY, 0f));
        currentCeilingSpline.SetTangentMode(ceilingPtIndex, ShapeTangentMode.Continuous);
        ceilingPhysicsPoints.Add(new Vector2(localX, ceilingY));
        ceilingPtIndex++;
    }

    void FinishCeiling(float localX)
    {
        currentCeilingSpline.InsertPointAt(ceilingPtIndex, new Vector3(localX, 100f, 0f));
        currentCeilingSpline.SetTangentMode(ceilingPtIndex, ShapeTangentMode.Linear);

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

        EdgeCollider2D col = currentCeilingObj.GetComponent<EdgeCollider2D>();
        if (col == null) col = currentCeilingObj.AddComponent<EdgeCollider2D>();
        col.SetPoints(ceilingPhysicsPoints);
    }
}