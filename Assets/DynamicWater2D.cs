using UnityEngine;

// Forces Unity to automatically add the components needed to draw our dynamic water shape!
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider2D))]
public class DynamicWater2D : MonoBehaviour
{
    [Header("Mesh Generation")]
    [Tooltip("How many 'springs' make up the surface. More nodes = smoother waves but heavier on CPU. 70 is great!")]
    public int nodeCount = 70;
    public Color surfaceColor = new Color(0.2f, 0.7f, 1f, 0.8f);
    public Color depthColor = new Color(0.05f, 0.3f, 0.7f, 0.9f);

    [Header("Wave Physics (Springs)")]
    public float springConstant = 0.02f;
    public float damping = 0.04f;
    public float spread = 0.05f;
    [Tooltip("How much force the car transfers into the waves when splashing")]
    public float collisionForceMultiplier = 0.05f;

    [Header("Fluid Dynamics")]
    public float buoyancyStrength = 15f;
    public float fluidViscosity = 1.5f;
    [Tooltip("How much the water ripples continuously while driving through it")]
    public float continuousWakeMultiplier = 0.0015f;

    [Header("Effects")]
    public GameObject splashPrefab;
    public AudioClip splashSound;

    // Physics Arrays
    private float[] xPositions;
    private float[] yPositions;
    private float[] velocities;
    private float[] accelerations;

    // Mesh Arrays
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

    // Called dynamically by MapGenerator
    // UPDATED: Now receives a separate 'physicsWidth' to stop the car from floating while on the dirt edge!
    public void GenerateWater(float visualWidth, float visualHeight, float physicsWidth)
    {
        waterWidth = visualWidth;
        waterHeight = visualHeight;

        // Setup Audio
        if (splashSound != null && audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }

        // Setup Trigger Collider
        col2D = GetComponent<BoxCollider2D>();
        col2D.isTrigger = true;
        // The trigger uses physicsWidth so it doesn't bleed into the dirt, but keeps the full visual height
        col2D.size = new Vector2(physicsWidth, waterHeight);
        col2D.offset = Vector2.zero; // Assuming pivot is in the center

        // Setup Mesh Component
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        
        if (mr.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) mr.sharedMaterial = new Material(shader);
        }

        mesh = new Mesh();
        mesh.name = "DynamicWaterMesh";
        mf.mesh = mesh;

        // Initialize Arrays
        xPositions = new float[nodeCount];
        yPositions = new float[nodeCount];
        velocities = new float[nodeCount];
        accelerations = new float[nodeCount];

        vertices = new Vector3[nodeCount * 2];
        triangles = new int[(nodeCount - 1) * 6];
        colors = new Color[nodeCount * 2];
        uvs = new Vector2[nodeCount * 2];

        float leftEdge = -waterWidth / 2f;
        float topEdge = waterHeight / 2f;
        float bottomEdge = -waterHeight / 2f;

        // Build the physical nodes
        for (int i = 0; i < nodeCount; i++)
        {
            xPositions[i] = leftEdge + (waterWidth * ((float)i / (nodeCount - 1)));
            yPositions[i] = topEdge;
            velocities[i] = 0f;
            accelerations[i] = 0f;

            // Surface Vertex
            vertices[i] = new Vector3(xPositions[i], yPositions[i], 0f);
            uvs[i] = new Vector2((float)i / (nodeCount - 1), 1f);
            colors[i] = surfaceColor;

            // Bottom Floor Vertex
            vertices[i + nodeCount] = new Vector3(xPositions[i], bottomEdge, 0f);
            uvs[i + nodeCount] = new Vector2((float)i / (nodeCount - 1), 0f);
            colors[i + nodeCount] = depthColor;
        }

        // Connect the vertices into triangles so Unity can render it
        int t = 0;
        for (int i = 0; i < nodeCount - 1; i++)
        {
            triangles[t++] = i;
            triangles[t++] = i + nodeCount;
            triangles[t++] = i + 1;

            triangles[t++] = i + 1;
            triangles[t++] = i + nodeCount;
            triangles[t++] = i + nodeCount + 1;
        }

        UpdateMesh();
        
        // Track all bubble particle systems
        bubbleSystems = GetComponentsInChildren<ParticleSystem>();
    }

    void FixedUpdate()
    {
        if (xPositions == null || xPositions.Length == 0) return;

        float baseHeight = waterHeight / 2f;

        // 1. Hooke's Law (Spring simulation)
        for (int i = 0; i < nodeCount; i++)
        {
            float displacement = yPositions[i] - baseHeight;
            accelerations[i] = -springConstant * displacement - damping * velocities[i];
            velocities[i] += accelerations[i];
            yPositions[i] += velocities[i];
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
            for (int i = 0; i < nodeCount; i++)
            {
                if (i > 0) yPositions[i - 1] += leftDeltas[i];
                if (i < nodeCount - 1) yPositions[i + 1] += rightDeltas[i];
            }
        }

        // 3. Apply physics visually
        for (int i = 0; i < nodeCount; i++)
        {
            vertices[i].y = yPositions[i];
        }
        UpdateMesh();
        
        // Pop bubbles exactly as they hit the dynamically waving water surface!
        if (bubbleSystems != null)
        {
            foreach (ParticleSystem ps in bubbleSystems)
            {
                if (ps == null || !ps.isPlaying) continue;

                ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.main.maxParticles];
                int aliveCount = ps.GetParticles(particles);

                for (int i = 0; i < aliveCount; i++)
                {
                    Vector3 worldPos = ps.transform.TransformPoint(particles[i].position);
                    float localX = worldPos.x - transform.position.x;

                    float surfaceY = waterHeight / 2f;
                    if (xPositions != null && xPositions.Length > 1)
                    {
                        float halfWidth = waterWidth / 2f;
                        float clampedX = Mathf.Clamp(localX, -halfWidth, halfWidth);
                        for (int k = 0; k < nodeCount - 1; k++)
                        {
                            if (clampedX >= xPositions[k] && clampedX <= xPositions[k + 1])
                            {
                                float t = (clampedX - xPositions[k]) / (xPositions[k + 1] - xPositions[k]);
                                surfaceY = Mathf.Lerp(yPositions[k], yPositions[k + 1], t);
                                break;
                            }
                        }
                    }

                    float worldSurfaceY = transform.position.y + surfaceY;
                    if (worldPos.y >= worldSurfaceY)
                    {
                        particles[i].remainingLifetime = -1f; 
                    }
                }
                ps.SetParticles(particles, aliveCount);
            }
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
        
        int nearestNode = 0;
        float shortestDist = float.MaxValue;
        
        for (int i = 0; i < nodeCount; i++)
        {
            float dist = Mathf.Abs(xPositions[i] - localX);
            if (dist < shortestDist)
            {
                shortestDist = dist;
                nearestNode = i;
            }
        }
        
        float impact = Mathf.Clamp(velocity, -200f, 200f);
        velocities[nearestNode] += impact * collisionForceMultiplier;

        if (splashPrefab != null)
        {
            Vector3 splashPos = new Vector3(worldXPosition, transform.position.y + yPositions[nearestNode], 0);
            Instantiate(splashPrefab, splashPos, Quaternion.identity);
        }

        if (audioSource != null && splashSound != null)
        {
            audioSource.pitch = Random.Range(0.8f, 1.2f);
            audioSource.PlayOneShot(splashSound, 0.5f);
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        Rigidbody2D rb = col.attachedRigidbody;
        if (rb != null && rb.linearVelocity.y < -1f)
        {
            Splash(col.bounds.center.x, rb.linearVelocity.y * rb.mass);
        }
    }

    void OnTriggerStay2D(Collider2D col)
    {
        Rigidbody2D rb = col.attachedRigidbody;
        if (rb == null) return;

        float localX = col.bounds.center.x - transform.position.x;
        
        float surfaceY = waterHeight / 2f; 
        int nearestNode = 0;

        if (xPositions != null)
        {
            for (int i = 0; i < nodeCount - 1; i++)
            {
                if (localX >= xPositions[i] && localX <= xPositions[i+1])
                {
                    float t = (localX - xPositions[i]) / (xPositions[i+1] - xPositions[i]);
                    surfaceY = Mathf.Lerp(yPositions[i], yPositions[i+1], t);
                    
                    nearestNode = (t < 0.5f) ? i : i + 1;
                    break;
                }
            }
        }

        float worldSurfaceY = transform.position.y + surfaceY;
        float depth = worldSurfaceY - col.bounds.center.y;

        if (depth > 0)
        {
            float upwardForce = depth * buoyancyStrength * rb.mass;
            rb.AddForce(Vector2.up * upwardForce, ForceMode2D.Force);

            rb.linearVelocity *= (1f - (fluidViscosity * Time.fixedDeltaTime));
            rb.angularVelocity *= (1f - (fluidViscosity * Time.fixedDeltaTime));

            float speed = rb.linearVelocity.magnitude;
            
            if (speed > 0.5f && xPositions != null)
            {
                float wakeForce = speed * rb.mass * continuousWakeMultiplier;
                
                if (rb.linearVelocity.y > 0.1f) velocities[nearestNode] += wakeForce; 
                else velocities[nearestNode] -= wakeForce;
            }
        }
    }
}