using UnityEngine;
using UnityEngine.U2D; 
using System.Collections.Generic;

public class VillageMap : MapGenerator
{
    [Header("--- VILLAGE SPECIFIC FEATURES ---")]
    
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
    [Tooltip("Adjust to sink the hay bales perfectly into the grass")]
    public float bouncyYOffset = 0.5f;

    [Header("Jelly Obstacles (Troll Trees)")]
    public bool generateJellyObstacles = true;
    public GameObject[] jellyObstaclePrefabs;
    public float minJellyDistance = 100f; 
    public float maxJellyDistance = 250f;
    [Range(0f, 1f)] public float jellySolidChance = 0.5f;
    [Tooltip("Adjust to sink the trees perfectly into the grass")]
    public float jellyYOffset = 0f;

    [Header("NEW: Windmills")]
    public bool generateWindmills = true;
    public GameObject windmillPrefab;
    public float minWindmillDistance = 300f;
    public float maxWindmillDistance = 600f;
    [Tooltip("Move the windmill up or down (Y-axis) to sit perfectly on the dirt.")]
    public float windmillYOffset = 5f;
    [Tooltip("Push the windmill into the background (+Z) so the car drives in front of the base.")]
    public float windmillZOffset = 2f;

    [Header("NEW: Runaway Livestock")]
    public bool generateLivestock = true;
    public GameObject livestockPrefab;
    public float minLivestockDistance = 150f;
    public float maxLivestockDistance = 350f;

    [Header("NEW: Scarecrows (Interactive)")]
    public bool generateScarecrows = true;
    public GameObject scarecrowPrefab;
    public float minScarecrowDistance = 200f;
    public float maxScarecrowDistance = 400f;
    [Tooltip("Move the scarecrow up or down to sit perfectly on the dirt.")]
    public float scarecrowYOffset = 0f;

    [Header("NEW: Hot Air Balloons (Sky Objects)")]
    public bool generateBalloons = true;
    public GameObject balloonPrefab;
    public float minBalloonDistance = 400f;
    public float maxBalloonDistance = 800f;
    [Tooltip("How high in the sky they spawn")]
    public float minBalloonHeight = 25f;
    public float maxBalloonHeight = 40f;

    // Internal trackers
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

    protected override void InitializeMap()
    {
        runRNG = new System.Random(seed);

        // Pre-calculate first spawn points using seed
        float rawNextBridge = 100f + (Mathf.PerlinNoise(seed, seed) * 50f);
        nextBridgeX = Mathf.Round(rawNextBridge / pointSpacing) * pointSpacing;
        float rawGap = Mathf.Lerp(minBridgeGapWidth, maxBridgeGapWidth, Mathf.PerlinNoise(nextBridgeX, seed));
        currentBridgeGapWidth = Mathf.Round(rawGap / pointSpacing) * pointSpacing;

        float rawNextWater = 250f + (Mathf.PerlinNoise(seed + 1, seed) * 100f);
        nextWaterX = Mathf.Round(rawNextWater / pointSpacing) * pointSpacing;
        float rawWaterWidth = Mathf.Lerp(minWaterWidth, maxWaterWidth, Mathf.PerlinNoise(nextWaterX, seed));
        currentWaterWidth = Mathf.Round(rawWaterWidth / pointSpacing) * pointSpacing;

        // FIXED: Now using your dynamic Inspector Minimums instead of hardcoded numbers!
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
                
                float paddedVisualWidth = currentWaterWidth + 15f;
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

        bool isBalloonPoint = generateBalloons && Mathf.Abs(globalX - nextBalloonX) < 0.01f;
        if (isBalloonPoint)
        {
            if (balloonPrefab != null)
            {
                // Spawn high in the sky
                float skyHeight = originalY + Mathf.Lerp(minBalloonHeight, maxBalloonHeight, Mathf.PerlinNoise(globalX, seed));
                Instantiate(balloonPrefab, new Vector3(globalX, skyHeight, 0f), Quaternion.identity, chunkParent);
            }
            nextBalloonX = Mathf.Round((nextBalloonX + Mathf.Lerp(minBalloonDistance, maxBalloonDistance, Mathf.PerlinNoise(globalX * 0.9f, seed))) / pointSpacing) * pointSpacing;
            // Notice we do NOT return true here, because balloons are in the sky! Ground hazards can still spawn below them!
        }

        bool isScarecrowPoint = generateScarecrows && Mathf.Abs(globalX - nextScarecrowX) < 0.01f;
        if (isScarecrowPoint)
        {
            if (scarecrowPrefab != null) Instantiate(scarecrowPrefab, new Vector3(globalX, originalY + scarecrowYOffset, 0f), Quaternion.identity, chunkParent);
            nextScarecrowX = Mathf.Round((nextScarecrowX + Mathf.Lerp(minScarecrowDistance, maxScarecrowDistance, Mathf.PerlinNoise(globalX * 0.95f, seed))) / pointSpacing) * pointSpacing;
            return true;
        }

        // NEW: Windmills!
        bool isWindmillPoint = generateWindmills && Mathf.Abs(globalX - nextWindmillX) < 0.01f;
        if (isWindmillPoint)
        {
            if (windmillPrefab != null)
            {
                // Spawn it slightly in the background using your custom Z offset, and UP using the Y offset!
                Instantiate(windmillPrefab, new Vector3(globalX, originalY + windmillYOffset, windmillZOffset), Quaternion.identity, chunkParent);
            }
            nextWindmillX = Mathf.Round((nextWindmillX + Mathf.Lerp(minWindmillDistance, maxWindmillDistance, Mathf.PerlinNoise(globalX * 0.7f, seed))) / pointSpacing) * pointSpacing;
            return true;
        }

        // NEW: Runaway Livestock!
        bool isLivestockPoint = generateLivestock && Mathf.Abs(globalX - nextLivestockX) < 0.01f;
        if (isLivestockPoint)
        {
            if (livestockPrefab != null) Instantiate(livestockPrefab, new Vector3(globalX, originalY + 1.5f, 0f), Quaternion.identity, chunkParent);
            nextLivestockX = Mathf.Round((nextLivestockX + Mathf.Lerp(minLivestockDistance, maxLivestockDistance, Mathf.PerlinNoise(globalX * 0.8f, seed))) / pointSpacing) * pointSpacing;
            return true;
        }

        bool isBouncePoint = generateBouncyHazards && Mathf.Abs(globalX - nextBounceX) < 0.01f;
        if (isBouncePoint)
        {
            if (bouncyHazardPrefab != null) Instantiate(bouncyHazardPrefab, new Vector3(globalX, originalY + bouncyYOffset, 0f), Quaternion.identity, chunkParent);
            nextBounceX = Mathf.Round((nextBounceX + Mathf.Lerp(minBounceDistance, maxBounceDistance, Mathf.PerlinNoise(globalX * 0.4f, seed))) / pointSpacing) * pointSpacing;
            return true;
        }

        bool isScatterPoint = generateScatterObstacles && Mathf.Abs(globalX - nextScatterX) < 0.01f;
        if (isScatterPoint)
        {
            if (scatterPrefabs != null && scatterPrefabs.Length > 0)
            {
                int pIdx = Mathf.Clamp(Mathf.FloorToInt(Mathf.PerlinNoise(globalX, seed) * scatterPrefabs.Length), 0, scatterPrefabs.Length - 1);
                Instantiate(scatterPrefabs[pIdx], new Vector3(globalX, originalY + 1f, 0f), Quaternion.identity, chunkParent);
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
                fluidScript.GenerateWater(visualWidth, visualDepth, physicsWidth);
            }
            else fluid.transform.localScale = new Vector3(visualWidth, visualDepth, 1f);
        }
    }
}