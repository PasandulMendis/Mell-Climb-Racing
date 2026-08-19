using UnityEngine;

// AAA 2D Water System
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
    [Tooltip("Foam expands when water is violently disturbed and shrinks when calm.")]
    public float baseFoamThickness = 0.05f;
    public float maxFoamThickness = 0.4f;
    public float foamGenerationMultiplier = 0.02f;
    public float foamDecayRate = 2.0f;

    [Header("Reactive Spring Physics")]
    public float springConstant = 0.025f;
    public float damping = 0.035f;
    public float spread = 0.06f;
    public float collisionForceMultiplier = 0.08f;

    [Header("Complex Ocean Waves (Gerstner-esque)")]
    public bool useComplexAmbient = true;
    public float wave1Speed = 1.5f; public float wave1Freq = 2f; public float wave1Amp = 0.06f;
    public float wave2Speed = -2.1f; public float wave2Freq = 5f; public float wave2Amp = 0.03f;
    public float wave3Speed = 3.5f; public float wave3Freq = 1.5f; public float wave3Amp = 0.04f;

    [Header("Advanced Fluid Dynamics")]
    [Tooltip("Buoyancy force multiplier")]
    public float buoyancyStrength = 18f;
    [Tooltip("Applies realistic velocity-squared drag")]
    public float hydrodynamicDrag = 2.5f;
    [Tooltip("Rotational stability - higher values make floating objects fight to stay upright")]
    public float rotationalFriction = 3f;

    [Header("Effects")]
    public GameObject splashPrefab;
    public AudioClip splashSound;

    // Node Physics
    private float[] xPositions;
    private float[] yPositions;
    private float[] velocities;
    private float[] accelerations;
    private float[] dynamicFoamThickness; // Tracks kinetic foam per node

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

        // 3 Tiers of Vertices: Foam Top, Water Top, Water Bottom
        vertices = new Vector3[nodeCount * 3];
        colors = new Color[nodeCount * 3];
        uvs = new Vector2[nodeCount * 3];
        
        // 2 Rows of Quads (Foam Row + Water Row) -> 2 * 6 points per column
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

            // Tier 1: Water Surface (Bottom of Foam)
            vertices[i + nodeCount] = new Vector3(xPositions[i], yPositions[i], 0f);
            colors[i + nodeCount] = surfaceColor;
            uvs[i + nodeCount] = new Vector2((float)i / (nodeCount - 1), 0.8f);

            // Tier 2: Deep Water (Floor)
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

        // 1. Hooke's Law & Complex Ambient Waves
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
                // Multi-frequency wave generation (simulates chaotic ocean surface)
                targetHeight += Mathf.Sin(xPositions[i] * wave1Freq + time * wave1Speed) * wave1Amp;
                targetHeight += Mathf.Sin(xPositions[i] * wave2Freq + time * wave2Speed) * wave2Amp;
                targetHeight += Mathf.Cos(xPositions[i] * wave3Freq + time * wave3Speed) * wave3Amp;
            }

            float displacement = yPositions[i] - targetHeight;
            accelerations[i] = -springConstant * displacement - damping * velocities[i];
            velocities[i] += accelerations[i];
            yPositions[i] += velocities[i];
            
            // Kinetic Foam Logic: Fast moving nodes generate thicker foam
            float velocityMagnitude = Mathf.Abs(velocities[i]);
            if (velocityMagnitude > 0.05f)
            {
                dynamicFoamThickness[i] += velocityMagnitude * foamGenerationMultiplier;
                dynamicFoamThickness[i] = Mathf.Min(dynamicFoamThickness[i], maxFoamThickness);
            }
            // Decay foam slowly back to baseline
            dynamicFoamThickness[i] = Mathf.Lerp(dynamicFoamThickness[i], baseFoamThickness, Time.fixedDeltaTime * foamDecayRate);
        }

        // 2. Wave Propagation
        float[] leftDeltas = new float[nodeCount];
        float[] rightDeltas = new float[nodeCount];

        for (int j = 0; j < 8; j++) 
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
            for (int i = 1; i < nodeCount - 1; i++) // Skip edges
            {
                yPositions[i - 1] += leftDeltas[i];
                yPositions[i + 1] += rightDeltas[i];
            }
        }

        for (int i = 0; i < nodeCount; i++)
        {
            vertices[i].y = yPositions[i] + dynamicFoamThickness[i]; // Tier 0 (Foam Top)
            vertices[i + nodeCount].y = yPositions[i]; // Tier 1 (Water Top)
            // Tier 2 (Floor) remains static
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

            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.main.maxParticles];
            int aliveCount = ps.GetParticles(particles);

            for (int i = 0; i < aliveCount; i++)
            {
                Vector3 worldPos = ps.transform.TransformPoint(particles[i].position);
                float surfaceY = GetWaterHeightAtPosition(worldPos.x);

                if (worldPos.y >= surfaceY) particles[i].remainingLifetime = -1f; 
            }
            ps.SetParticles(particles, aliveCount);
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
        
        // Instant foam burst at impact site
        dynamicFoamThickness[nearestNode] = maxFoamThickness;
        if (nearestNode > 0) dynamicFoamThickness[nearestNode - 1] += maxFoamThickness * 0.5f;
        if (nearestNode < nodeCount - 1) dynamicFoamThickness[nearestNode + 1] += maxFoamThickness * 0.5f;

        if (splashPrefab != null)
        {
            Vector3 splashPos = new Vector3(worldXPosition, transform.position.y + yPositions[nearestNode], 0);
            Instantiate(splashPrefab, splashPos, Quaternion.identity);
        }

        if (audioSource != null && splashSound != null)
        {
            audioSource.pitch = Random.Range(0.85f, 1.15f);
            audioSource.PlayOneShot(splashSound, Mathf.Abs(velocity) * 0.01f); // Velocity scales volume
        }
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

        // MULTI-POINT BUOYANCY: Sample left and right sides of the object separately
        // This causes realistic pitch and rotation (e.g. boats rocking, boxes tilting)
        float leftWorldX = col.bounds.min.x;
        float rightWorldX = col.bounds.max.x;
        float bottomWorldY = col.bounds.min.y;

        float leftSurfaceY = GetWaterHeightAtPosition(leftWorldX);
        float rightSurfaceY = GetWaterHeightAtPosition(rightWorldX);

        float leftDepth = leftSurfaceY - bottomWorldY;
        float rightDepth = rightSurfaceY - bottomWorldY;

        ApplyBuoyancyAtPoint(rb, new Vector2(leftWorldX, bottomWorldY), leftDepth);
        ApplyBuoyancyAtPoint(rb, new Vector2(rightWorldX, bottomWorldY), rightDepth);

        // Apply Hydrodynamic Drag (Velocity Squared formula for realistic resistance)
        if (leftDepth > 0 || rightDepth > 0) 
        {
            Vector2 currentVelocity = rb.linearVelocity;
            Vector2 dragForce = -currentVelocity.normalized * (currentVelocity.sqrMagnitude * hydrodynamicDrag);
            rb.AddForce(dragForce * Time.fixedDeltaTime, ForceMode2D.Force);
            
            // Rotational damping to stop infinite spinning in water
            rb.angularVelocity = Mathf.Lerp(rb.angularVelocity, 0, rotationalFriction * Time.fixedDeltaTime);
            
            // Create continuous waves while moving through water
            if (currentVelocity.magnitude > 0.5f)
            {
                int node = GetNearestNodeIndex(col.bounds.center.x - transform.position.x);
                velocities[node] += (currentVelocity.y * 0.005f) * rb.mass;
            }
        }
    }

    void ApplyBuoyancyAtPoint(Rigidbody2D rb, Vector2 worldPoint, float depth)
    {
        if (depth > 0)
        {
            // Archimedes Principle: Upward force is proportional to volume displaced (approximated by depth here)
            float upwardForce = depth * buoyancyStrength * rb.mass;
            rb.AddForceAtPosition(Vector2.up * upwardForce, worldPoint, ForceMode2D.Force);
        }
    }

    float GetWaterHeightAtPosition(float worldX)
    {
        if (xPositions == null || xPositions.Length == 0) return transform.position.y + (waterHeight / 2f);
        
        float localX = worldX - transform.position.x;
        
        // Clamp to prevent out of bounds
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
        int nearest = 1; // Default to 1 to avoid edge locking issues
        float shortest = float.MaxValue;
        
        for (int i = 1; i < nodeCount - 1; i++) // Ignore edges
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