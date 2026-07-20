using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class IceBlock : MonoBehaviour
{
    [Header("Ice Physics")]
    [Tooltip("How slippery the ice block is on the ground (0 = perfectly slippery, acts like a hockey puck)")]
    [Range(0f, 1f)] public float iceFriction = 0.05f;
    [Tooltip("Bounciness of the ice block")]
    [Range(0f, 1f)] public float iceBounciness = 0.1f;

    [Header("Shatter Mechanics")]
    [Tooltip("How hard the car must hit to completely shatter the block")]
    public float shatterThreshold = 8f;
    
    [Header("Effects")]
    public ParticleSystem shatterParticles;
    public AudioClip shatterSound;
    [Tooltip("Optional: Sound when the ice blocks slide against each other or the car")]
    public AudioClip slideSound; 

    private Rigidbody2D rb;
    private Collider2D col;
    private AudioSource audioSource;
    private bool isShattered = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        
        // Tag as Player so it acts as safe debris if it flies onto the car's roof
        gameObject.tag = "Player"; 

        // 1. CREATE CUSTOM PHYSICS MATERIAL VIA CODE!
        // This ensures the blocks slip and slide flawlessly without you having to manually configure Unity materials.
        PhysicsMaterial2D slipperyMat = new PhysicsMaterial2D("SlipperyIce");
        slipperyMat.friction = iceFriction;
        slipperyMat.bounciness = iceBounciness;
        col.sharedMaterial = slipperyMat;

        if (shatterSound != null || slideSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }
    }

    void Update()
    {
        // Safety check 1: If the block falls completely through the map, destroy it
        if (transform.position.y < -50f) 
        {
            Destroy(gameObject);
            return;
        }

        // Safety check 2: If the block is left way behind the camera, destroy it to save memory
        if (Camera.main != null)
        {
            float distanceBehindCamera = Camera.main.transform.position.x - transform.position.x;
            
            // If the block is 60 units behind the camera, delete it. 
            // (You can increase this number if you want them to stick around longer!)
            if (distanceBehindCamera > 60f)
            {
                Destroy(gameObject);
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isShattered) return;

        CarController hitCar = collision.gameObject.GetComponentInParent<CarController>();
        
        if (hitCar != null)
        {
            float impact = collision.relativeVelocity.magnitude;
            
            // 2. SHATTER LOGIC (High speed impact)
            if (impact >= shatterThreshold)
            {
                Shatter();
            }
            // 3. SLIDE LOGIC (Low speed bumping)
            else if (impact > 1f && audioSource != null && slideSound != null)
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.pitch = Random.Range(0.9f, 1.2f);
                    audioSource.PlayOneShot(slideSound, 0.4f);
                }
            }
        }
    }

    void Shatter()
    {
        isShattered = true;

        if (shatterParticles != null)
        {
            ParticleSystem clone = Instantiate(shatterParticles, transform.position, Quaternion.identity);
            clone.Play();
            // Destroy the particle system after it finishes playing
            Destroy(clone.gameObject, clone.main.duration);
        }

        // Play the sound at the block's position right before destroying it
        if (shatterSound != null)
        {
            AudioSource.PlayClipAtPoint(shatterSound, transform.position);
        }

        // Destroy the ice block itself
        Destroy(gameObject);
    }
}