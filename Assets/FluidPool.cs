using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FluidPool : MonoBehaviour
{
    [Header("Fluid Dynamics")]
    [Tooltip("How hard the water pushes objects up. (Higher = floats more)")]
    public float buoyancyStrength = 15f;
    [Tooltip("How much the water slows you down. (Higher = thicker mud)")]
    public float fluidViscosity = 1.5f;

    [Header("Effects")]
    [Tooltip("Particle system to spawn when hitting the water")]
    public GameObject splashPrefab;
    [Tooltip("Sound to play when splashing")]
    public AudioClip splashSound;

    private Collider2D poolCollider;
    private AudioSource audioSource;

    void Start()
    {
        poolCollider = GetComponent<Collider2D>();
        poolCollider.isTrigger = true; // MUST be a trigger to let the car inside!

        if (splashSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
        }
    }

    // --- 1. THE SPLASH EFFECT ---
    void OnTriggerEnter2D(Collider2D col)
    {
        Rigidbody2D rb = col.attachedRigidbody;
        
        // Only splash if a physical object enters with downward velocity
        if (rb != null && rb.linearVelocity.y < -1f)
        {
            // Calculate exactly where the surface of the water is
            float surfaceY = poolCollider.bounds.max.y;
            Vector3 splashPos = new Vector3(col.bounds.center.x, surfaceY, 0f);

            if (splashPrefab != null)
            {
                Instantiate(splashPrefab, splashPos, Quaternion.identity);
            }

            if (audioSource != null && splashSound != null)
            {
                // Pitch it randomly so multiple wheels splashing sound natural
                audioSource.pitch = Random.Range(0.8f, 1.2f);
                audioSource.PlayOneShot(splashSound, 0.5f);
            }
        }
    }

    // --- 2. ARCHIMEDES' BUOYANCY & VISCOSITY ---
    void OnTriggerStay2D(Collider2D col)
    {
        Rigidbody2D rb = col.attachedRigidbody;
        if (rb == null) return;

        // Where is the top of the water?
        float surfaceY = poolCollider.bounds.max.y;
        
        // How deep is the center of this specific wheel/car body?
        float depth = surfaceY - col.bounds.center.y;

        // If it's submerged
        if (depth > 0)
        {
            // 1. BUOYANCY: The deeper it goes, the harder it gets pushed UP.
            // We multiply by the object's mass so heavy trucks and light barrels both float properly!
            float upwardForce = depth * buoyancyStrength * rb.mass;
            rb.AddForce(Vector2.up * upwardForce, ForceMode2D.Force);

            // 2. VISCOSITY (Drag): Water is thick. It saps your momentum!
            // We slowly shave off velocity every frame you spend in the water.
            rb.linearVelocity *= (1f - (fluidViscosity * Time.fixedDeltaTime));
            rb.angularVelocity *= (1f - (fluidViscosity * Time.fixedDeltaTime));
        }
    }
}