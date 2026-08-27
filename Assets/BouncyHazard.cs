using UnityEngine;
using System.Collections;

// Removed [RequireComponent] so you can easily swap Box for Polygon Collider without Unity yelling at you!
public class BouncyHazard : MonoBehaviour
{
    [Header("Bounce Physics")]
    [Tooltip("The minimum upward force applied, even if you roll onto it slowly.")]
    public float baseBounceForce = 15f;
    [Tooltip("Forward boost to prevent getting stuck bouncing straight up and down!")]
    public float forwardBounceForce = 10f;
    [Tooltip("Multiplier for how much extra bounce you get based on how fast you fall onto it.")]
    public float fallingVelocityMultiplier = 1.5f;
    [Tooltip("Maximum total upward force to prevent launching the car out of the game bounds.")]
    public float maxBounceForce = 60f;

    [Header("Juice & Effects")]
    [Tooltip("Sound effect to play when bouncing (e.g., a cartoon boing or rubber thud)")]
    public AudioClip bounceSound;
    [Tooltip("Optional particle effect (e.g., a puff of dust or air rings)")]
    public ParticleSystem bounceParticles;
    
    [Tooltip("How intensely the object visually squishes when hit (1.0 = normal, 0.5 = half height)")]
    public float squishAmount = 0.5f;
    [Tooltip("How fast it springs back to normal shape")]
    public float squishSpeed = 15f;

    [Header("Custom Meme Integration")]
    [Tooltip("Played when the car actually gets launched into the air")]
    public string memeCategoryBounce = "Car_Bounce";

    private AudioSource audioSource;
    private Vector3 originalScale;
    private bool isSquishing = false;
    private float lastBounceTime = 0f;

    void Start()
    {
        originalScale = transform.localScale;

        if (bounceSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 2D Sound
            audioSource.playOnAwake = false;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if it's the player and ensure we don't double-trigger if two wheels hit simultaneously
        if (collision.gameObject.CompareTag("Player") && Time.time > lastBounceTime + 0.1f)
        {
            Rigidbody2D rb = collision.rigidbody;
            
            if (rb != null)
            {
                lastBounceTime = Time.time;

                // 1. Calculate the physics!
                // How fast was the car moving downwards?
                float downwardSpeed = Mathf.Max(0f, -collision.relativeVelocity.y);
                
                // Combine base force with velocity multiplier
                float totalUpwardForce = baseBounceForce + (downwardSpeed * fallingVelocityMultiplier);
                totalUpwardForce = Mathf.Clamp(totalUpwardForce, baseBounceForce, maxBounceForce);

                // Figure out which direction the car is currently moving so we push them the right way!
                float directionX = (rb.linearVelocity.x < -0.1f) ? -1f : 1f;

                // 2. Apply the explosive upward AND forward force!
                // We reset their vertical velocity first so gravity doesn't fight the bounce
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                
                Vector2 finalForce = new Vector2(directionX * forwardBounceForce, totalUpwardForce);
                
                // We use ForceMode2D.Impulse for an instant, explosive launch!
                // We multiply by rb.mass so heavy trucks and light bikes bounce equally well!
                rb.AddForce(finalForce * rb.mass, ForceMode2D.Impulse);

                // --- NEW: Trigger Contextual Meme for the bounce! ---
                if (MemeManager.instance != null && !string.IsNullOrEmpty(memeCategoryBounce))
                {
                    MemeManager.instance.TriggerReaction(memeCategoryBounce);
                }

                // 3. Play the AAA Visuals and Audio
                if (bounceParticles != null) bounceParticles.Play();
                
                if (audioSource != null && bounceSound != null)
                {
                    audioSource.pitch = Random.Range(0.9f, 1.2f);
                    audioSource.PlayOneShot(bounceSound, 0.8f);
                }

                // Trigger the Squash and Stretch animation
                if (!isSquishing)
                {
                    StartCoroutine(SquashAndStretchRoutine());
                }
            }
        }
    }

    // This cleanly animates the object squishing down and popping back up!
    IEnumerator SquashAndStretchRoutine()
    {
        isSquishing = true;

        // Target squish scale (Wider X, Shorter Y)
        Vector3 squishedScale = new Vector3(originalScale.x * (1f + (1f - squishAmount)), originalScale.y * squishAmount, originalScale.z);

        // Squish Down instantly
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * squishSpeed * 2f; // Down is faster
            transform.localScale = Vector3.Lerp(originalScale, squishedScale, t);
            yield return null;
        }

        // Spring Back Up smoothly
        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * squishSpeed;
            transform.localScale = Vector3.Lerp(squishedScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
        isSquishing = false;
    }
}