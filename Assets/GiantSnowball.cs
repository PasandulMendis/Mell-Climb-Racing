using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class GiantSnowball : MonoBehaviour
{
    [Header("Snowball Settings")]
    [Tooltip("How close the car needs to be before it wakes up and starts rolling")]
    public float wakeUpDistance = 40f;
    [Tooltip("Initial push to get it rolling down the hill")]
    public float initialPushForce = 50f;

    [Header("Growth Settings")]
    [Tooltip("The absolute maximum size it can grow to")]
    public float maxScale = 4f;
    [Tooltip("How fast it grows while rolling")]
    public float growthRate = 0.5f;
    [Tooltip("How much heavier it gets as it grows (Makes it harder for the car to push!)")]
    public float massGrowthMultiplier = 2f;

    [Header("Effects")]
    public AudioClip crunchSound;
    private AudioSource audioSource;

    [Header("Custom Meme Integration")]
    [Tooltip("Played when the car crashes into the giant snowball from the front")]
    public string memeCategoryHitFront = "Giant_Snow_Ball_Hit_Front";
    [Tooltip("Played when the snowball crushes the car from behind")]
    public string memeCategoryHitBack = "Giant_Snow_Ball_Hit_Back";
    [Tooltip("The exact name of the invisible front collider on your car")]
    public string frontColliderName = "FrontBumper";
    [Tooltip("The exact name of the invisible back collider on your car")]
    public string backColliderName = "BackBumper";

    private Rigidbody2D rb;
    private Transform playerCar;
    private bool isAwake = false;
    private Vector3 startingScale;
    private float startingMass;
    private bool hasTriggeredMeme = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.Sleep(); 

        // Tag as player so it doesn't instantly snap the driver's neck if it rolls over the car
        gameObject.tag = "Player"; 

        startingScale = transform.localScale;
        startingMass = rb.mass;

        if (crunchSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }
    }

    void Update()
    {
        if (isAwake)
        {
            // If it is rolling reasonably fast and hasn't hit max size, GROW IT!
            if (rb.linearVelocity.magnitude > 1f && transform.localScale.x < maxScale)
            {
                float growth = growthRate * Time.deltaTime;
                transform.localScale += new Vector3(growth, growth, 0f);
                
                // Increase the physical mass so a max-size snowball acts like a boulder!
                rb.mass = startingMass + ((transform.localScale.x - startingScale.x) * massGrowthMultiplier);
            }
            return;
        }

        if (playerCar == null)
        {
            if (GameManager.instance != null) playerCar = GameManager.instance.playerCar;
            return;
        }

        if (Vector2.Distance(transform.position, playerCar.position) < wakeUpDistance)
        {
            WakeUp();
        }
    }

    void WakeUp()
    {
        isAwake = true;
        rb.WakeUp();
        rb.AddTorque(initialPushForce, ForceMode2D.Impulse);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isAwake) WakeUp();

        if (audioSource != null && crunchSound != null && collision.relativeVelocity.magnitude > 2f)
        {
            // Pitch down as it gets bigger for a heavier, crunchier sound!
            float pitchOffset = Mathf.InverseLerp(startingScale.x, maxScale, transform.localScale.x);
            audioSource.pitch = Mathf.Lerp(1.2f, 0.6f, pitchOffset);
            audioSource.PlayOneShot(crunchSound, 0.7f);
        }
    }

    // --- NEW: Trigger Event for the Invisible Ghost Bumpers! ---
    void OnTriggerEnter2D(Collider2D col)
    {
        if (!hasTriggeredMeme)
        {
            CarController hitCar = col.GetComponentInParent<CarController>();
            if (hitCar != null)
            {
                hasTriggeredMeme = true; 
                string hitColliderName = col.gameObject.name;
                string categoryToPlay = "";

                if (hitColliderName.Contains(frontColliderName)) categoryToPlay = memeCategoryHitFront;
                else if (hitColliderName.Contains(backColliderName)) categoryToPlay = memeCategoryHitBack;
                
                if (MemeManager.instance != null && !string.IsNullOrEmpty(categoryToPlay))
                {
                    MemeManager.instance.TriggerReaction(categoryToPlay);
                }
            }
        }
    }
}