using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class BeachBall : MonoBehaviour
{
    [Header("Ball Settings")]
    public float wakeUpDistance = 35f;
    public float rollTorque = 80f;
    
    [Header("Audio")]
    public AudioClip bounceSound; 

    [Header("Custom Meme Integration")]
    [Tooltip("Played when the car hits the beach ball from the front")]
    public string memeCategoryHitFront = "Beach_Ball_Hit_Front";
    [Tooltip("Played when the beach ball hits the car from behind")]
    public string memeCategoryHitBack = "Beach_Ball_Hit_Back";
    [Tooltip("The exact name of the invisible front collider on your car")]
    public string frontColliderName = "FrontBumper";
    [Tooltip("The exact name of the invisible back collider on your car")]
    public string backColliderName = "BackBumper";
    
    private Rigidbody2D rb;
    private Transform playerCar;
    private bool isAwake = false;
    private AudioSource audioSource;
    private bool hasTriggeredMeme = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.Sleep(); 

        // Safe Debris - Won't kill the player if it lands on their head
        gameObject.tag = "Player";

        if (bounceSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }
    }

    void Update()
    {
        if (isAwake) return;

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
        
        // Push it down the hill towards the player
        rb.AddTorque(rollTorque, ForceMode2D.Impulse);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isAwake) WakeUp();

        // Play that classic rubber rubber bounce sound
        if (audioSource != null && bounceSound != null && collision.relativeVelocity.magnitude > 2f)
        {
            audioSource.pitch = Random.Range(0.8f, 1.3f);
            audioSource.PlayOneShot(bounceSound, 0.6f);
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