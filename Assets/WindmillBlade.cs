using UnityEngine;

public class WindmillBlade : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = -150f;
    [Tooltip("How fast the blade recovers to its default speed after being hit")]
    public float recoveryStrength = 2f;
    public bool randomStartAngle = true;

    [Header("Impact Settings")]
    [Tooltip("How heavy the blade feels to the car. Higher = harder to drive through.")]
    public float bladeMass = 3f;
    [Tooltip("Play a heavy wood crunch sound on hit")]
    public AudioClip swatSound;

    private AudioSource audioSource;
    private Rigidbody2D rb;
    private Collider2D myCollider;

    void Start()
    {
        myCollider = GetComponent<Collider2D>();

        // 1. Get or Add the Rigidbody
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        
        // 2. THE MAGIC FIX: Make it Dynamic so the car's physical mass can push it!
        rb.bodyType = RigidbodyType2D.Dynamic;
        
        // 3. Freeze X and Y so it doesn't fly away into the sky when hit. It acts like a perfect hinge!
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY;
        
        rb.gravityScale = 0f;
        rb.mass = bladeMass;

        if (randomStartAngle)
        {
            rb.rotation = Random.Range(0f, 360f);
        }
        
        // Set initial speed
        rb.angularVelocity = rotationSpeed;

        if (swatSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }
    }

    void FixedUpdate()
    {
        // 4. THE RECOVERY: Smoothly pull the blade's rotation speed back to the default over time
        rb.angularVelocity = Mathf.Lerp(rb.angularVelocity, rotationSpeed, Time.fixedDeltaTime * recoveryStrength);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // NEW HOTFIX: Ignore the ground, the windmill base, and anything that isn't the Car or Livestock!
        CarController car = collision.gameObject.GetComponentInParent<CarController>();
        RunawayLivestock livestock = collision.gameObject.GetComponent<RunawayLivestock>();

        if (car == null && livestock == null)
        {
            if (myCollider != null) Physics2D.IgnoreCollision(myCollider, collision.collider);
            return; // Stop running the rest of the code!
        }

        // 5. THE IMPACT
        if (car != null)
        {
            // Unity's built-in physics engine will automatically handle spinning the blade! 
            // We just need to handle the sound and the memes.
            
            float impactForce = collision.relativeVelocity.magnitude;

            if (audioSource != null && swatSound != null)
            {
                // Only play the sound if the car hits it with decent speed
                if (impactForce > 2f)
                {
                    audioSource.pitch = Random.Range(0.8f, 1.2f);
                    audioSource.PlayOneShot(swatSound, 0.8f);
                }
            }

            // If the car slams into the blade at super high speeds, trigger the Scared meme!
            if (impactForce > 15f && MemeManager.instance != null)
            {
                MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Scared, true, false);
            }
        }
    }
}