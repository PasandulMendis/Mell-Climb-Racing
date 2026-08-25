using System.Collections.Generic;
using UnityEngine;

// AAA 2D Water System with Continuous Wheel Splashes & Realistic Wave Inertia
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class DynamicWater2D : MonoBehaviour
{
    [Header("AAA Mesh Generation")]
    [Tooltip("High node count required for fluid curvature. 150-200 is recommended for AAA look.")]
    public int nodeCount = 150;
    
    [Header("Styling & Colors")]
    public Color foamColor = new Color(1f, 1f, 1f, 0.9f);
    public Color surfaceColor = new Color(0.1f, 0.6f, 0.9f, 0.8f);
    public Color depthColor = new Color(0.02f, 0.15f, 0.5f, 0.95f);
    
    [Header("Procedural Kinetic Foam")]
    public float baseFoamThickness = 0.05f;
    public float maxFoamThickness = 0.4f;
    public float foamGenerationMultiplier = 0.02f;
    public float foamDecayRate = 2.0f;

    [Header("Reactive Spring Physics")]
    [Tooltip("Stiffness of water restoring force.")]
    public float springConstant = 0.025f;
    [Tooltip("Reduced damping allowing waves to oscillate naturally after object leaves.")]
    public float damping = 0.015f; 
    [Tooltip("How fast waves propagate across neighboring nodes.")]
    public float spread = 0.05f;
    public float collisionForceMultiplier = 0.08f;

    [Header("Complex Ocean Waves (Gerstner-esque)")]
    public bool useComplexAmbient = true;
    public float wave1Speed = 1.5f; public float wave1Freq = 2f; public float wave1Amp = 0.06f;
    public float wave2Speed = -2.1f; public float wave2Freq = 5f; public float wave2Amp = 0.03f;
    public float wave3Speed = 3.5f; public float wave3Freq = 1.5f; public float wave3Amp = 0.04f;

    [Header("Advanced Fluid Dynamics")]
    public float buoyancyStrength = 18f;
    public float hydrodynamicDrag = 2.5f;
    public float rotationalFriction = 3f;

    [Header("Continuous Wheel Movement Splashes")]
    public bool enableContinuousSplash = true;
    public float minSpeedForSplash = 1.0f;
    public float continuousSplashInterval = 0.08f;
    public float continuousSplashScale = 0.5f;
    [Range(0f, 0.45f)]
    public float wheelInsetRatio = 0.12f;

    [Header("Effects")]
    public GameObject splashPrefab;
    public AudioClip splashSound;

    // Node Physics
    private float[] xPositions;
    private float[] yPositions;
    private float[] velocities;
    private float[] accelerations;
    private float[] dynamicFoamThickness;

    // Mesh Data
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Color[] colors;
    private Vector2[] uvs;

    private float waterWidth;
    private float waterHeight;
    private BoxCollider2D col2D;
    private AudioSource audioSource;
    private ParticleSystem[] bubbleSystems;

    private ParticleSystem.Particle[] cachedParticleBuffer = new ParticleSystem.Particle[256];

    // Cooldown trackers for front/rear wheel splashes per object
    private Dictionary<Collider2D, float> lastSplashTimeRear = new Dictionary<Collider2D, float>();
    private Dictionary<Collider2D, float> lastSplashTimeFront = new Dictionary<Collider2D, float>();

    public void GenerateWater(float visualWidth, float visualHeight, float physicsWidth)
    {
        waterWidth = visualWidth;
        waterHeight = visualHeight;

        if (splashSound != null && audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }

        col2D = GetComponent<BoxCollider2D>();
        col2D.isTrigger = true;
        col2D.size = new Vector2(physicsWidth, waterHeight);
        col2D.offset = Vector2.zero; 

        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr.sharedMaterial == null) mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

        mesh = new Mesh();
        mesh.name = "AAADynamicWaterMesh";
        mf.mesh = mesh;

        // Initialize Physics Arrays
        xPositions = new float[nodeCount];
        yPositions = new float[nodeCount];
        velocities = new float[nodeCount];
        accelerations = new float[nodeCount];
        dynamicFoamThickness = new float[nodeCount];

        vertices = new Vector3[nodeCount * 3];
        colors = new Color[nodeCount * 3];
        uvs = new Vector2[nodeCount * 3];
        triangles = new int[(nodeCount - 1) * 12]; 

        float leftEdge = -waterWidth / 2f;
        float topEdge = waterHeight / 2f;
        float bottomEdge = -waterHeight / 2f;

        for (int i = 0; i < nodeCount; i++)
        {
            xPositions[i] = leftEdge + (waterWidth * ((float)i / (nodeCount - 1)));
            yPositions[i] = topEdge;
            velocities[i] = 0f;
            accelerations[i] = 0f;
            dynamicFoamThickness[i] = baseFoamThickness;

            // Tier 0: Foam Top
            vertices[i] = new Vector3(xPositions[i], yPositions[i] + baseFoamThickness, 0f);
            colors[i] = foamColor;
            uvs[i] = new Vector2((float)i / (nodeCount - 1), 1f);

            // Tier 1: Water Surface
            vertices[i + nodeCount] = new Vector3(xPositions[i], yPositions[i], 0f);
            colors[i + nodeCount] = surfaceColor;
            uvs[i + nodeCount] = new Vector2((float)i / (nodeCount - 1), 0.8f);

            // Tier 2: Deep Water
            vertices[i + nodeCount * 2] = new Vector3(xPositions[i], bottomEdge, 0f);
            colors[i + nodeCount * 2] = depthColor;
            uvs[i + nodeCount * 2] = new Vector2((float)i / (nodeCount - 1), 0f);
        }

        int t = 0;
        for (int i = 0; i < nodeCount - 1; i++)
        {
            // Foam Quad
            triangles[t++] = i;
            triangles[t++] = i + nodeCount;
            triangles[t++] = i + 1;
            triangles[t++] = i + 1;
            triangles[t++] = i + nodeCount;
            triangles[t++] = i + nodeCount + 1;

            // Deep Water Quad
            triangles[t++] = i + nodeCount;
            triangles[t++] = i + nodeCount * 2;
            triangles[t++] = i + nodeCount + 1;
            triangles[t++] = i + nodeCount + 1;
            triangles[t++] = i + nodeCount * 2;
            triangles[t++] = i + nodeCount * 2 + 1;
        }

        UpdateMesh();
        bubbleSystems = GetComponentsInChildren<ParticleSystem>();
    }

    void FixedUpdate()
    {
        if (xPositions == null || xPositions.Length == 0) return;

        float baseHeight = waterHeight / 2f;
        float time = Time.time;

        // 1. Hooke's Law & Ambient Waves
        for (int i = 0; i < nodeCount; i++)
        {
            if (i == 0 || i == nodeCount - 1)
            {
                yPositions[i] = baseHeight;
                continue;
            }

            float targetHeight = baseHeight;
            if (useComplexAmbient)
            {
                targetHeight += Mathf.Sin(xPositions[i] * wave1Freq + time * wave1Speed) * wave1Amp;
                targetHeight += Mathf.Sin(xPositions[i] * wave2Freq + time * wave2Speed) * wave2Amp;
                targetHeight += Mathf.Cos(xPositions[i] * wave3Freq + time * wave3Speed) * wave3Amp;
            }

            float displacement = yPositions[i] - targetHeight;
            accelerations[i] = -springConstant * displacement - damping * velocities[i];
            velocities[i] += accelerations[i];
            yPositions[i] += velocities[i];

            float velocityMagnitude = Mathf.Abs(velocities[i]);
            if (velocityMagnitude > 0.05f)
            {
                dynamicFoamThickness[i] += velocityMagnitude * foamGenerationMultiplier;
                dynamicFoamThickness[i] = Mathf.Min(dynamicFoamThickness[i], maxFoamThickness);
            }
            dynamicFoamThickness[i] = Mathf.Lerp(dynamicFoamThickness[i], baseFoamThickness, Time.fixedDeltaTime * foamDecayRate);
        }

        // 2. Smooth Wave Propagation Loop
        float[] leftDeltas = new float[nodeCount];
        float[] rightDeltas = new float[nodeCount];

        for (int j = 0; j < 4; j++) // Reduced iterations to preserve momentum long enough to ripple
        {
            for (int i = 0; i < nodeCount; i++)
            {
                if (i > 0)
                {
                    leftDeltas[i] = spread * (yPositions[i] - yPositions[i - 1]);
                    velocities[i - 1] += leftDeltas[i];
                }
                if (i < nodeCount - 1)
                {
                    rightDeltas[i] = spread * (yPositions[i] - yPositions[i + 1]);
                    velocities[i + 1] += rightDeltas[i];
                }
            }
            for (int i = 1; i < nodeCount - 1; i++)
            {
                yPositions[i - 1] += leftDeltas[i];
                yPositions[i + 1] += rightDeltas[i];
            }
        }

        for (int i = 0; i < nodeCount; i++)
        {
            vertices[i].y = yPositions[i] + dynamicFoamThickness[i];
            vertices[i + nodeCount].y = yPositions[i];
        }
        UpdateMesh();
        
        HandleBubblePop();
    }

    void HandleBubblePop()
    {
        if (bubbleSystems == null) return;
        
        foreach (ParticleSystem ps in bubbleSystems)
        {
            if (ps == null || !ps.isPlaying) continue;

            int maxParticles = ps.main.maxParticles;
            if (cachedParticleBuffer.Length < maxParticles)
            {
                cachedParticleBuffer = new ParticleSystem.Particle[maxParticles];
            }

            int aliveCount = ps.GetParticles(cachedParticleBuffer);

            for (int i = 0; i < aliveCount; i++)
            {
                Vector3 worldPos = ps.transform.TransformPoint(cachedParticleBuffer[i].position);
                float surfaceY = GetWaterHeightAtPosition(worldPos.x);

                if (worldPos.y >= surfaceY) 
                {
                    cachedParticleBuffer[i].remainingLifetime = -1f; 
                }
            }
            ps.SetParticles(cachedParticleBuffer, aliveCount);
        }
    }

    void UpdateMesh()
    {
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.RecalculateNormals();
    }

    public void Splash(float worldXPosition, float velocity)
    {
        if (xPositions == null) return;
        
        float localX = worldXPosition - transform.position.x;
        int nearestNode = GetNearestNodeIndex(localX);
        
        float impact = Mathf.Clamp(velocity, -250f, 250f);
        velocities[nearestNode] += impact * collisionForceMultiplier;
        
        dynamicFoamThickness[nearestNode] = maxFoamThickness;
        if (nearestNode > 0) dynamicFoamThickness[nearestNode - 1] += maxFoamThickness * 0.5f;
        if (nearestNode < nodeCount - 1) dynamicFoamThickness[nearestNode + 1] += maxFoamThickness * 0.5f;

        if (splashPrefab != null)
        {
            Vector3 splashPos = new Vector3(worldXPosition, transform.position.y + yPositions[nearestNode], 0);
            SpawnSplashEffect(splashPos, 1.0f);
        }

        if (audioSource != null && splashSound != null)
        {
            audioSource.pitch = Random.Range(0.85f, 1.15f);
            audioSource.PlayOneShot(splashSound, Mathf.Abs(velocity) * 0.01f);
        }
    }

    public void TriggerWheelSplash(float worldX, float surfaceY, Vector2 velocity, float scale = 1.0f)
    {
        if (xPositions == null) return;

        float localX = worldX - transform.position.x;
        int nearestNode = GetNearestNodeIndex(localX);

        float splashForce = velocity.magnitude * 0.02f;
        velocities[nearestNode] += (velocity.y < 0 ? -splashForce : splashForce) * collisionForceMultiplier;
        dynamicFoamThickness[nearestNode] = Mathf.Min(maxFoamThickness, dynamicFoamThickness[nearestNode] + 0.12f * scale);

        if (splashPrefab != null)
        {
            Vector3 spawnPos = new Vector3(worldX, surfaceY, 0f);
            SpawnSplashEffect(spawnPos, scale);
        }

        if (audioSource != null && splashSound != null && !audioSource.isPlaying)
        {
            audioSource.pitch = Random.Range(1.1f, 1.4f);
            audioSource.PlayOneShot(splashSound, Mathf.Clamp01(velocity.magnitude * 0.005f));
        }
    }

    private void SpawnSplashEffect(Vector3 position, float scale)
    {
        GameObject splashInstance = Instantiate(splashPrefab, position, Quaternion.identity);
        splashInstance.transform.localScale *= scale;

        ParticleSystem ps = splashInstance.GetComponent<ParticleSystem>();
        float destroyTime = ps != null ? ps.main.duration + ps.main.startLifetime.constantMax : 2.0f;
        Destroy(splashInstance, destroyTime);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        Rigidbody2D rb = col.attachedRigidbody;
        if (rb != null && Mathf.Abs(rb.linearVelocity.y) > 1.5f)
        {
            Splash(col.bounds.center.x, rb.linearVelocity.y * rb.mass);
        }
    }

    void OnTriggerStay2D(Collider2D col)
    {
        Rigidbody2D rb = col.attachedRigidbody;
        if (rb == null) return;

        float boundsMinX = col.bounds.min.x;
        float boundsMaxX = col.bounds.max.x;
        float bottomWorldY = col.bounds.min.y;

        float rearWheelX = Mathf.Lerp(boundsMinX, boundsMaxX, wheelInsetRatio);
        float frontWheelX = Mathf.Lerp(boundsMaxX, boundsMinX, wheelInsetRatio);

        float leftSurfaceY = GetWaterHeightAtPosition(rearWheelX);
        float rightSurfaceY = GetWaterHeightAtPosition(frontWheelX);

        float leftDepth = leftSurfaceY - bottomWorldY;
        float rightDepth = rightSurfaceY - bottomWorldY;

        ApplyBuoyancyAtPoint(rb, new Vector2(rearWheelX, bottomWorldY), leftDepth);
        ApplyBuoyancyAtPoint(rb, new Vector2(frontWheelX, bottomWorldY), rightDepth);

        Vector2 currentVelocity = rb.linearVelocity;
        float speed = currentVelocity.magnitude;

        if (leftDepth > 0 || rightDepth > 0) 
        {
            Vector2 dragForce = -currentVelocity.normalized * (currentVelocity.sqrMagnitude * hydrodynamicDrag);
            rb.AddForce(dragForce * Time.fixedDeltaTime, ForceMode2D.Force);
            rb.angularVelocity = Mathf.Lerp(rb.angularVelocity, 0, rotationalFriction * Time.fixedDeltaTime);
            
            // Generate continuous surface disturbances based on horizontal & vertical velocity
            if (speed > 0.5f)
            {
                int node = GetNearestNodeIndex(col.bounds.center.x - transform.position.x);
                float displacementImpulse = (currentVelocity.x * 0.008f + currentVelocity.y * 0.01f) * rb.mass;
                velocities[node] += displacementImpulse * Time.fixedDeltaTime;
            }
        }

        if (enableContinuousSplash && speed >= minSpeedForSplash)
        {
            float currentTime = Time.time;

            if (leftDepth > 0)
            {
                if (!lastSplashTimeRear.TryGetValue(col, out float lastTime) || (currentTime - lastTime) >= continuousSplashInterval)
                {
                    TriggerWheelSplash(rearWheelX, leftSurfaceY, currentVelocity, continuousSplashScale);
                    lastSplashTimeRear[col] = currentTime;
                }
            }

            if (rightDepth > 0)
            {
                if (!lastSplashTimeFront.TryGetValue(col, out float lastTime) || (currentTime - lastTime) >= continuousSplashInterval)
                {
                    TriggerWheelSplash(frontWheelX, rightSurfaceY, currentVelocity, continuousSplashScale);
                    lastSplashTimeFront[col] = currentTime;
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        Rigidbody2D rb = col.attachedRigidbody;
        if (rb != null)
        {
            // Exit impulse: Transfer remaining vehicle momentum into the water surface upon leaving
            float exitX = col.bounds.center.x;
            float exitVelocityY = rb.linearVelocity.y;
            float exitVelocityX = rb.linearVelocity.x;

            int exitNode = GetNearestNodeIndex(exitX - transform.position.x);
            float impulseForce = (Mathf.Abs(exitVelocityX) * 0.02f + exitVelocityY * 0.05f) * rb.mass;

            // Push the exit node and adjacent nodes to keep wave ripples propagating outward
            velocities[exitNode] += impulseForce * collisionForceMultiplier;
            if (exitNode > 0) velocities[exitNode - 1] += impulseForce * 0.5f * collisionForceMultiplier;
            if (exitNode < nodeCount - 1) velocities[exitNode + 1] += impulseForce * 0.5f * collisionForceMultiplier;

            dynamicFoamThickness[exitNode] = Mathf.Min(maxFoamThickness, dynamicFoamThickness[exitNode] + 0.15f);
        }

        lastSplashTimeRear.Remove(col);
        lastSplashTimeFront.Remove(col);
    }

    void ApplyBuoyancyAtPoint(Rigidbody2D rb, Vector2 worldPoint, float depth)
    {
        if (depth > 0)
        {
            float upwardForce = depth * buoyancyStrength * rb.mass;
            rb.AddForceAtPosition(Vector2.up * upwardForce, worldPoint, ForceMode2D.Force);
        }
    }

    float GetWaterHeightAtPosition(float worldX)
    {
        if (xPositions == null || xPositions.Length == 0) return transform.position.y + (waterHeight / 2f);
        
        float localX = worldX - transform.position.x;
        float halfWidth = waterWidth / 2f;
        localX = Mathf.Clamp(localX, -halfWidth, halfWidth);

        for (int i = 0; i < nodeCount - 1; i++)
        {
            if (localX >= xPositions[i] && localX <= xPositions[i + 1])
            {
                float t = (localX - xPositions[i]) / (xPositions[i + 1] - xPositions[i]);
                float localY = Mathf.Lerp(yPositions[i], yPositions[i + 1], t);
                return transform.position.y + localY;
            }
        }
        return transform.position.y + (waterHeight / 2f);
    }

    int GetNearestNodeIndex(float localX)
    {
        if (xPositions == null) return 0;
        int nearest = 1;
        float shortest = float.MaxValue;
        
        for (int i = 1; i < nodeCount - 1; i++)
        {
            float dist = Mathf.Abs(xPositions[i] - localX);
            if (dist < shortest)
            {
                shortest = dist;
                nearest = i;
            }
        }
        return nearest;
    }
}