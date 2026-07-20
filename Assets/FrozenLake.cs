using UnityEngine;

// NEW: Added MeshFilter and MeshRenderer so it can draw the ice block!
[RequireComponent(typeof(BoxCollider2D), typeof(MeshFilter), typeof(MeshRenderer))]
public class FrozenLake : MonoBehaviour
{
    [Header("Ice Visuals")]
    public Color iceColor = new Color(0.6f, 0.9f, 1f, 0.85f);
    [Tooltip("How thick the ice layer is visually and physically")]
    public float iceThickness = 1.5f;

    [Header("Ice Physics")]
    [Tooltip("How slippery the lake surface is (0 = perfectly slippery)")]
    [Range(0f, 1f)] public float iceFriction = 0.02f;

    [Header("Shatter Mechanics")]
    [Tooltip("How hard the car must hit the ice vertically to break it")]
    public float shatterThreshold = 10f;
    [Tooltip("Will heavy vehicles break it just by driving on it? (Threshold for Car's Rigidbody Mass)")]
    public float massShatterThreshold = 4f;

    [Header("Effects")]
    public ParticleSystem shatterParticles;
    public AudioClip shatterSound;
    public AudioClip creakSound;

    private BoxCollider2D col;
    private AudioSource audioSource;
    private bool isShattered = false;
    private float nextCreakTime = 0f;

    // Hidden in inspector because SnowMap assigns this automatically via code
    [HideInInspector] public GameObject hiddenWaterPool;

    void Start()
    {
        // 1. GENERATE VISUALS: Build the ice mesh so it isn't invisible!
        GenerateIceMesh();

        // 2. CONFIGURE PHYSICS: Force the collider to match the exact size of the mesh
        col = GetComponent<BoxCollider2D>();
        col.isTrigger = false; // It must be solid to hold the car
        col.size = new Vector2(1f, iceThickness); 
        // Offset it downwards so the exact top flat edge is at Y=0 (matching the water surface)
        col.offset = new Vector2(0f, -iceThickness / 2f);
        
        // 3. Generate frictionless ice material via code!
        PhysicsMaterial2D slipperyMat = new PhysicsMaterial2D("LakeIce");
        slipperyMat.friction = iceFriction;
        slipperyMat.bounciness = 0f;
        col.sharedMaterial = slipperyMat;

        if (shatterSound != null || creakSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }
    }

    // --- NEW METHOD: Learns from DynamicWater2D to draw a solid block ---
    void GenerateIceMesh()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();

        // Use the standard sprite shader so it supports the alpha transparency in our iceColor
        if (mr.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) mr.sharedMaterial = new Material(shader);
        }

        Mesh mesh = new Mesh();
        mesh.name = "SolidIceMesh";

        // Because SnowMap.cs stretches the X scale to match currentLakeWidth, 
        // local X from -0.5 to 0.5 perfectly covers the whole lake!
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(-0.5f, 0f, 0f);               // Top Left
        vertices[1] = new Vector3(0.5f, 0f, 0f);                // Top Right
        vertices[2] = new Vector3(-0.5f, -iceThickness, 0f);    // Bottom Left
        vertices[3] = new Vector3(0.5f, -iceThickness, 0f);     // Bottom Right

        int[] triangles = new int[] { 0, 1, 2, 2, 1, 3 };
        Color[] colors = new Color[] { iceColor, iceColor, iceColor, iceColor };
        Vector2[] uvs = new Vector2[] { new Vector2(0,1), new Vector2(1,1), new Vector2(0,0), new Vector2(1,0) };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.uv = uvs;
        
        mf.mesh = mesh;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isShattered) return;

        CarController hitCar = collision.gameObject.GetComponentInParent<CarController>();
        
        if (hitCar != null)
        {
            Rigidbody2D carRb = hitCar.carRigidbody;
            
            // Check for high-speed vertical impacts OR a vehicle that is too heavy!
            float impactForce = Mathf.Abs(collision.relativeVelocity.y);

            if (impactForce >= shatterThreshold || carRb.mass >= massShatterThreshold)
            {
                Shatter();
            }
            else if (impactForce > 2f && creakSound != null && Time.time > nextCreakTime)
            {
                // Play warning creak sound if hit lightly!
                audioSource.pitch = Random.Range(0.8f, 1.1f);
                audioSource.PlayOneShot(creakSound, 0.6f);
                nextCreakTime = Time.time + 1f;
            }
        }
    }

    void Shatter()
    {
        isShattered = true;

        if (shatterParticles != null)
        {
            ParticleSystem clone = Instantiate(shatterParticles, transform.position, Quaternion.identity);
            var shape = clone.shape;
            shape.scale = new Vector3(transform.localScale.x, 1f, 1f);
            clone.Play();
            Destroy(clone.gameObject, 3f);
        }

        if (shatterSound != null)
        {
            GameObject soundObj = new GameObject("LakeShatterSound");
            AudioSource tempAudio = soundObj.AddComponent<AudioSource>();
            tempAudio.clip = shatterSound;
            tempAudio.spatialBlend = 0f;
            tempAudio.pitch = Random.Range(0.8f, 1.1f);
            tempAudio.Play();
            Destroy(soundObj, 3f);
        }

        // NEW: Turn the dynamic water back on so the car falls in with a splash!
        if (hiddenWaterPool != null)
        {
            hiddenWaterPool.SetActive(true);
        }

        Destroy(gameObject);
    }
}