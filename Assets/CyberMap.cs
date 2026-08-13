using UnityEngine;
using UnityEngine.U2D; 
using System.Collections.Generic;

public class CyberMap : MapGenerator
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

    [Header("--- CYBER CITY FEATURES ---")]
    
    [Header("Smooth Terrain Math")]
    [Tooltip("How long each 'hill' segment is. Smaller values make closer, more frequent hills.")]
    public float segmentLength = 30f;
    [Tooltip("The height difference modifier for the hills. Higher = steeper hills!")]
    public float heightStepSize = 15f;
    [Tooltip("Maximum height limit for the terrain.")]
    public int maxHeightLevels = 4;
    [Tooltip("How smooth the transition between heights should be. 1 is standard SmoothStep.")]
    [Range(0.5f, 2f)]
    public float smoothnessMultiplier = 1f;

    [Header("Radiation Pools (Deadly Sludge)")]
    public bool generateRadiation = true;
    public GameObject radiationPoolPrefab;
    public float minRadiationDistance = 300f;
    public float maxRadiationDistance = 600f;
    public float radiationWidth = 20f;
    public float radiationDepth = 8f;
    public float radiationLevelOffset = -2f;
    public int waterSortingOrder = 3;

    [Header("Static Time of Day Settings")]
    public bool enableStaticTime = true;
    public bool randomizeTime = true;
    public TimeOfDay specificTimeOfDay = TimeOfDay.Night; // Default to Night for Cyber vibe!

    [Header("Color Grading (Synthwave Defaults)")]
    public SpriteRenderer skyBackgroundRenderer; 
    
    // Defaulted to Cyberpunk/Synthwave color palettes!
    public TimeColors dawnColors = new TimeColors(new Color(0.9f, 0.2f, 0.5f), new Color(0.8f, 0.4f, 0.7f)); // Neon Pink
    public TimeColors morningColors = new TimeColors(new Color(0.2f, 0.8f, 1.0f), Color.white); // Cyan
    public TimeColors noonColors = new TimeColors(new Color(0.4f, 0.0f, 0.8f), new Color(0.7f, 0.7f, 1.0f)); // Deep Purple
    public TimeColors duskColors = new TimeColors(new Color(1.0f, 0.4f, 0.0f), new Color(1.0f, 0.5f, 0.5f)); // Retro Orange
    public TimeColors nightColors = new TimeColors(new Color(0.05f, 0.05f, 0.15f), new Color(0.3f, 0.8f, 1.0f)); // Pitch Black & Neon Blue

    private Color propTint = Color.white;
    private bool hasTintedCar = false;
    private TimeOfDay activeTimeOfDay;
    private System.Random runRNG;
    
    // Radiation Trackers
    private float nextRadiationX;
    private bool isBuildingRadiation = false;

    void LateUpdate()
    {
        if (enableStaticTime && !hasTintedCar && playerCar != null)
        {
            hasTintedCar = true;
            TintObject(playerCar.gameObject);

            if (activeTimeOfDay == TimeOfDay.Night || activeTimeOfDay == TimeOfDay.Dusk)
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

        if (Camera.main != null) Camera.main.backgroundColor = skyColor;
        if (skyBackgroundRenderer != null) skyBackgroundRenderer.color = skyColor;
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
            Color groundTint = Color.Lerp(Color.white, propTint, 0.5f);
            ssr.color = groundTint;
        }
    }

    protected override void InitializeMap()
    {
        runRNG = new System.Random(seed);
        if (enableStaticTime) SetupTimeOfDay();

        float rawNextRad = 200f + (Mathf.PerlinNoise(seed + 1, seed) * 100f);
        nextRadiationX = Mathf.Round(rawNextRad / pointSpacing) * pointSpacing;
    }

    protected override bool ProcessTerrainSpline(float globalX, float localX, float originalY, ref float finalY, Spline spline, List<Vector2> physicsPoints, ref int pointIndex, int i, int numSurfacePoints, Transform chunkParent, out bool isHazardZone)
    {
        // --- SMOOTH TERRAIN MATH ---
        // Instead of hard steps, we use smooth interpolation (Cosine interpolation) between height points
        
        // Determine which segment we are in
        int currentIndex = Mathf.FloorToInt(globalX / segmentLength);
        
        // Calculate the progress through the current segment (0.0 to 1.0)
        float t = (globalX - (currentIndex * segmentLength)) / segmentLength;
        
        // Get the target heights for the start and end of this segment
        float startY = GetSegmentHeight(currentIndex);
        float endY = GetSegmentHeight(currentIndex + 1);
        
        // Apply smooth interpolation (Cosine Easing)
        // This replaces the blocky 'flat-ramp-flat' logic with a smooth curve
        float smoothT = (1f - Mathf.Cos(t * Mathf.PI)) * 0.5f;
        
        // Interpolate the final Y position smoothly
        finalY = Mathf.Lerp(startY, endY, smoothT);

        // Safe spawn zone blend (gradually introduce the generated terrain height)
        if (globalX < 50f)
        {
            float spawnBlend = 1f - (globalX / 50f);
            // Assuming originalY is the flat base height at start
            finalY = Mathf.Lerp(finalY, originalY, spawnBlend);
        }

        // --- RADIATION POOL LOGIC ---
        bool inRadZone = generateRadiation && globalX >= nextRadiationX && globalX <= nextRadiationX + radiationWidth;
        if (inRadZone)
        {
            isHazardZone = true; // Tell the base spawner NOT to spawn coins in the sludge!
            if (!isBuildingRadiation)
            {
                isBuildingRadiation = true;
                
                float edgePadding = Mathf.Clamp(radiationWidth * 0.2f, 4f, 15f);
                float paddedVisualWidth = radiationWidth + edgePadding;
                float paddedVisualDepth = (radiationDepth * 2f) + 20f;
                
                float surfaceY = finalY + radiationLevelOffset;
                float centerOffsetY = surfaceY - (paddedVisualDepth / 2f);
                
                Vector2 centerPos = new Vector2(nextRadiationX + radiationWidth / 2f, centerOffsetY);
                
                SpawnFluid(centerPos, paddedVisualWidth, paddedVisualDepth, radiationWidth, chunkParent);
            }

            // Keep the sharp drop for the pool itself, or you can smooth this too if desired.
            // Using a sharper step here makes sense for a "pit".
            float dipPercent = (globalX - nextRadiationX) / radiationWidth;
            float dipAmount = 0f;

            if (dipPercent < 0.15f) dipAmount = Mathf.SmoothStep(0, radiationDepth, dipPercent / 0.15f); // Smooth entry to pool
            else if (dipPercent > 0.85f) dipAmount = Mathf.SmoothStep(radiationDepth, 0, (dipPercent - 0.85f) / 0.15f); // Smooth exit
            else dipAmount = radiationDepth;

            finalY -= dipAmount; 
            return false; 
        }
        else if (globalX > nextRadiationX + radiationWidth && isBuildingRadiation)
        {
            isBuildingRadiation = false;
            nextRadiationX = Mathf.Round((nextRadiationX + Mathf.Lerp(minRadiationDistance, maxRadiationDistance, Mathf.PerlinNoise(globalX * 0.2f, seed))) / pointSpacing) * pointSpacing;
        }

        // --- BYPASS BASE GENERATOR ---
        isHazardZone = true; 

        // Manually trigger our perfectly aligned linear Pickups and Props!
        bool spawnedObstacle = ProcessHazards(globalX, finalY, i, numSurfacePoints, chunkParent);
        if (i < numSurfacePoints && !spawnedObstacle)
        {
            base.SpawnPickups(new Vector3(globalX, finalY, 0f), chunkParent);
            
            if (generateProps && globalX >= nextPropX)
            {
                SpawnCyberProps(new Vector3(globalX, finalY, 0f), chunkParent);
            }
        }

        return false; 
    }

    private float GetSegmentHeight(int segmentIndex)
    {
        // Use perlin noise to determine a target height for this segment point
        float noise = Mathf.PerlinNoise(segmentIndex * noiseFrequency, seed + 100);
        
        // Scale the noise value to our defined height levels
        float targetHeight = noise * maxHeightLevels * heightStepSize;
        
        return targetHeight;
    }

    void SpawnFluid(Vector2 centerPos, float visualWidth, float visualDepth, float physicsWidth, Transform chunkParent)
    {
        if (radiationPoolPrefab != null)
        {
            Vector3 spawnPos = new Vector3(centerPos.x, centerPos.y, 0f);
            GameObject fluid = Instantiate(radiationPoolPrefab, spawnPos, Quaternion.identity, chunkParent);
            MeshRenderer mr = fluid.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = waterSortingOrder;

            DynamicWater2D fluidScript = fluid.GetComponent<DynamicWater2D>();
            if (fluidScript != null)
            {
                fluid.transform.localScale = Vector3.one; 
                
                // Keep the custom neon colors bright!
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

    void SpawnCyberProps(Vector3 groundPosition, Transform chunkParent)
    {
        if (propPrefabs != null && propPrefabs.Length > 0)
        {
            float randomVal = Mathf.PerlinNoise(groundPosition.x * 0.4f, seed + 5.5f);
            if (randomVal < propSpawnProbability)
            {
                int prefabIndex = Mathf.FloorToInt(Mathf.PerlinNoise(groundPosition.x * 12.34f, seed + 6.6f) * propPrefabs.Length);
                prefabIndex = Mathf.Clamp(prefabIndex, 0, propPrefabs.Length - 1);
                
                Vector3 spawnPos = new Vector3(groundPosition.x, groundPosition.y + propYOffset, 0f);
                GameObject prop = Instantiate(propPrefabs[prefabIndex], spawnPos, Quaternion.identity, chunkParent);

                SpriteRenderer sr = prop.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) prop.transform.position += new Vector3(0f, spawnPos.y - sr.bounds.min.y, 0f);
                
                // FORCE UPRIGHT: Cyber props stand tall!
                prop.transform.localRotation = Quaternion.identity;
                
                if (Mathf.PerlinNoise(groundPosition.x * 9.9f, seed + 8.8f) > 0.5f) 
                {
                    prop.transform.localScale = new Vector3(-prop.transform.localScale.x, prop.transform.localScale.y, prop.transform.localScale.z);
                }
                
                OnEnvironmentPropSpawned(prop);
            }
        }
        nextPropX += propCheckInterval; 
    }

    protected override bool ProcessHazards(float globalX, float originalY, int i, int numSurfacePoints, Transform chunkParent)
    {
        return false; 
    }
}