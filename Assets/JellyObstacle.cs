using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class JellyObstacle : MonoBehaviour
{
    [Header("Bounce Back Physics")]
    [Tooltip("How hard the car gets pushed backward when hitting this object")]
    public float pushbackForceX = 25000f;
    [Tooltip("How hard the car gets popped slightly upward (prevents getting stuck)")]
    public float pushbackForceY = 8000f;

    [Header("Jelly Animation")]
    [Tooltip("How flat it pancakes before melting into the ground")]
    public float squishAmount = 0.3f;
    [Tooltip("How fast the melt animation plays")]
    public float animationSpeed = 15f;
    
    [Header("Juice & Effects")]
    [Tooltip("Sound of hitting a solid rubbery tree/object")]
    public AudioClip thudSound;
    [Tooltip("Optional: Leaves falling or dust popping out")]
    public ParticleSystem impactParticles; 

    private Vector3 originalScale;
    private bool hasBeenHit = false; // NEW: Prevents double hits and ensures it only melts once
    private AudioSource audioSource;
    private Collider2D col2D;

    void Start()
    {
        originalScale = transform.localScale;
        col2D = GetComponent<Collider2D>();

        // Tag this object as "Player" so it acts as "safe debris". 
        // This stops it from triggering a Game Over if it bumps the driver's head!
        gameObject.tag = "Player"; 

        if (thudSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Detect if the car hit us
        CarController car = collision.gameObject.GetComponentInParent<CarController>();
        
        if (car != null && collision.gameObject.CompareTag("Player"))
        {
            // Only allow this to trigger exactly once!
            if (!hasBeenHit) 
            {
                hasBeenHit = true;
                
                // 1. INSTANTLY turn off the collider so the car can drive past it!
                if (col2D != null) col2D.enabled = false;

                Rigidbody2D carRb = car.GetComponent<Rigidbody2D>();

                if (carRb != null)
                {
                    // 2. Calculate bounce direction (opposite of where the car is facing/moving)
                    float directionX = Mathf.Sign(carRb.linearVelocity.x) > 0 ? -1f : 1f;
                    
                    // 3. Dampen the car's current speed so the bounce actually works against high speeds
                    carRb.linearVelocity = new Vector2(carRb.linearVelocity.x * 0.2f, carRb.linearVelocity.y); 
                    
                    // 4. Apply the explosive backward force!
                    carRb.AddForce(new Vector2(directionX * pushbackForceX, pushbackForceY), ForceMode2D.Impulse);

                    // 5. Play the AAA Premium Shrink Animation
                    StartCoroutine(SquishAndMeltRoutine());
                    
                    // 6. Play Effects
                    if (impactParticles != null) impactParticles.Play();
                    if (audioSource != null && thudSound != null)
                    {
                        audioSource.pitch = Random.Range(0.8f, 1.2f);
                        audioSource.PlayOneShot(thudSound, 0.8f);
                    }

                    // 7. FIX: Trigger the "Angry" meme because the player just got completely trolled!
                    if (MemeManager.instance != null) MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Angry, true, false);
                }
            }
        }
    }

    IEnumerator SquishAndMeltRoutine()
    {
        Vector3 startPos = transform.position;
        Vector3 squishedScale = new Vector3(originalScale.x * 1.5f, originalScale.y * squishAmount, originalScale.z);
        
        // Automatically find the exact height of the tree so we can anchor the squish to the bottom!
        float halfHeight = 2f; 
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) halfHeight = sr.bounds.extents.y;

        float t = 0;
        
        // 1. Quick explosive squish (Anchored to the dirt)
        while (t < 1f)
        {
            t += Time.deltaTime * animationSpeed * 1.5f; 
            transform.localScale = Vector3.Lerp(originalScale, squishedScale, t);
            
            // MATH FIX: Shift the Y position downward as it shrinks so the bottom never lifts off the ground
            float scaleDifference = 1f - (transform.localScale.y / originalScale.y);
            float dropAmount = scaleDifference * halfHeight;
            transform.position = startPos - new Vector3(0f, dropAmount, 0f);
            
            yield return null;
        }

        // 2. Melt into the ground (Sink downward into the dirt while shrinking to 0)
        t = 0;
        Vector3 zeroScale = new Vector3(0f, 0f, originalScale.z);
        Vector3 squishedPos = transform.position;
        
        // Slightly slower melt
        while (t < 1f)
        {
            t += Time.deltaTime * animationSpeed * 0.8f;
            transform.localScale = Vector3.Lerp(squishedScale, zeroScale, t);
            
            // Actively pull the tree down into the dirt (Y-axis) as it shrinks away
            transform.position = squishedPos - new Vector3(0f, t * halfHeight, 0f);
            
            yield return null;
        }

        // 3. Delete the tree entirely so it doesn't leave invisible clutter in the level
        Destroy(gameObject);
    }
}