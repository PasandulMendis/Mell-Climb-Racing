using UnityEngine;

public class SpeedShroom : MonoBehaviour
{
    [Header("Visuals & Blending")]
    [Tooltip("Drag the child GameObject containing the glowing sprite here")]
    public SpriteRenderer glowRenderer;
    [Tooltip("How far away the car is when the mushroom starts glowing")]
    public float maxGlowDistance = 25f; 

    [Header("Boost Mechanics")]
    [Tooltip("How close the car must be to trigger the boost (e.g. 2 = right on top of it)")]
    public float boostDistance = 2f; 
    [Tooltip("How hard it shoots the car forward!")]
    public float boostForce = 15000f; 
    [Tooltip("A little vertical pop so the car lifts off the ground slightly!")]
    public float verticalLift = 2000f; 

    [Header("Effects")]
    public ParticleSystem popParticles; // E.g., blue spores flying outward
    public AudioClip boostSound;

    [Header("Custom Meme Integration")]
    [Tooltip("Played when the car gets a massive speed boost")]
    public string memeCategorySuccess = "Boost_Success";
    [Tooltip("Played when a heavy car barely speeds up from the shroom")]
    public string memeCategoryFail = "Boost_Fail";
    [Tooltip("How much the car's speed must instantly increase to count as a success")]
    public float successSpeedThreshold = 5f;

    private Transform playerCar;
    private Rigidbody2D carRb;
    private AudioSource audioSource;
    private bool hasBoosted = false;

    [HideInInspector] public bool isActiveAtNight = true; // Set by the MapGenerator!

    void Start()
    {
        // Start completely transparent (no glow)
        if (glowRenderer != null)
        {
            Color c = glowRenderer.color;
            c.a = 0f;
            glowRenderer.color = c;
        }

        if (boostSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        if (hasBoosted) return; // Stop checking once consumed
        if (!isActiveAtNight) return; // --- NEW: Do nothing if it's daytime! ---

        // Find the car if we haven't already
        if (playerCar == null)
        {
            if (GameManager.instance != null && GameManager.instance.playerCar != null)
            {
                playerCar = GameManager.instance.playerCar;
                carRb = playerCar.GetComponent<Rigidbody2D>();
            }
            return;
        }

        // Calculate absolute distance between the car and the shroom
        float distance = Vector2.Distance(transform.position, playerCar.position);

        // 1. THE GLOW BLENDING MATH
        if (distance <= maxGlowDistance && glowRenderer != null)
        {
            // Converts distance into a 0.0 to 1.0 percentage
            float glowPercent = 1f - (distance / maxGlowDistance);
            
            // Optional AAA Polish: Square the percentage so it gets exponentially brighter at the very end!
            glowPercent = Mathf.Pow(glowPercent, 2f); 
            
            Color c = glowRenderer.color;
            c.a = Mathf.Clamp01(glowPercent);
            glowRenderer.color = c;
        }
        else if (glowRenderer != null && glowRenderer.color.a > 0)
        {
            // Reset to 0 if we backed away
            Color c = glowRenderer.color;
            c.a = 0f;
            glowRenderer.color = c;
        }

        // 2. THE BOOST TRIGGER
        if (distance <= boostDistance)
        {
            TriggerBoost();
        }
    }

    void TriggerBoost()
    {
        hasBoosted = true;

        float speedIncrease = 0f; // Track how much the speed actually changed

        if (carRb != null)
        {
            // Record speed BEFORE the boost
            float speedBefore = carRb.linearVelocity.magnitude;

            // We use transform.right to push the car in whatever direction it is currently facing
            Vector2 force = (carRb.transform.right * boostForce) + (Vector3.up * verticalLift);
            
            // Impulse instantly applies the massive speed boost
            carRb.AddForce(force, ForceMode2D.Impulse);

            // Record speed AFTER the boost and calculate the difference!
            float speedAfter = carRb.linearVelocity.magnitude;
            speedIncrease = speedAfter - speedBefore;
        }

        // Play the juicy effects
        if (popParticles != null) popParticles.Play();

        if (audioSource != null && boostSound != null)
        {
            audioSource.pitch = Random.Range(1.1f, 1.4f);
            audioSource.PlayOneShot(boostSound, 0.8f);
        }

        // Instantly shut the glow off to visually show it was "consumed"
        if (glowRenderer != null)
        {
            glowRenderer.gameObject.SetActive(false);
        }

        // --- NEW: Trigger Contextual Meme based on actual acceleration! ---
        if (MemeManager.instance != null) 
        {
            if (speedIncrease >= successSpeedThreshold)
            {
                if (!string.IsNullOrEmpty(memeCategorySuccess))
                    MemeManager.instance.TriggerReaction(memeCategorySuccess);
            }
            else
            {
                if (!string.IsNullOrEmpty(memeCategoryFail))
                    MemeManager.instance.TriggerReaction(memeCategoryFail);
            }
        }
    }
}