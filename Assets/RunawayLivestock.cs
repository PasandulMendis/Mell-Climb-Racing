using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class RunawayLivestock : MonoBehaviour
{
    [Header("Livestock Settings")]
    [Tooltip("How close the car needs to be before the sheep/cow wakes up and starts rolling")]
    public float wakeUpDistance = 30f;
    [Tooltip("How much initial spin to give it so it rolls down the hill")]
    public float rollTorque = 50f;
    
    [Header("Audio")]
    public AudioClip animalSound; // "Baa" or "Moo"

    [Header("Custom Meme Integration")]
    [Tooltip("Played when the car hits the livestock from the front")]
    public string memeCategoryHitFront = "Live_Stock_Hit_Front";
    [Tooltip("Played when the livestock hits the car from behind")]
    public string memeCategoryHitBack = "Live_Stock_Hit_Back";
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
        
        // Put it to sleep initially so it doesn't roll away before the player gets there!
        rb.Sleep(); 

        // Tag as player so hitting it doesn't instantly snap your neck
        gameObject.tag = "Player";

        if (animalSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }
    }

    void Update()
    {
        if (isAwake) return; // Stop checking once it's awake

        if (playerCar == null)
        {
            if (GameManager.instance != null) playerCar = GameManager.instance.playerCar;
            return;
        }

        // Check the distance to the player
        if (Vector2.Distance(transform.position, playerCar.position) < wakeUpDistance)
        {
            WakeUp();
        }
    }

    void WakeUp()
    {
        isAwake = true;
        rb.WakeUp();
        
        // Apply torque based on which side of the hill it's on to force it to roll!
        rb.AddTorque(rollTorque, ForceMode2D.Impulse);

        if (audioSource != null && animalSound != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.3f);
            audioSource.PlayOneShot(animalSound, 0.7f);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isAwake) WakeUp(); // Wake up if hit by a flying object/car prematurely
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