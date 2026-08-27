using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BearTrap : MonoBehaviour
{
    [Header("Trap Settings")]
    public Sprite closedTrapSprite;
    
    [Header("Audio")]
    public AudioClip snapSound;
    public AudioClip tirePopSound;

    [Header("Custom Meme Integration")]
    [Tooltip("Played when the trap successfully pops a fresh tire")]
    public string memeCategoryTirePop = "Tire_Pops";
    
    private SpriteRenderer sr;
    private AudioSource audioSource;
    private bool isSprung = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        
        // Ensure the trap acts as a trigger so the car rolls INTO it, not over it
        GetComponent<BoxCollider2D>().isTrigger = true;

        if (snapSound != null || tirePopSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (isSprung) return;

        // Check if the object that entered the trap is a physical wheel attached to the CarController
        CarController car = col.GetComponentInParent<CarController>();
        
        if (car != null)
        {
            // Make sure it actually hit a wheel collider, not the main car chassis!
            if (col.gameObject.name.ToLower().Contains("wheel") || col.gameObject.name.ToLower().Contains("tire"))
            {
                SnapShut(car, col.gameObject);
            }
        }
    }

    void SnapShut(CarController car, GameObject tire)
    {
        isSprung = true;

        if (sr != null && closedTrapSprite != null)
        {
            sr.sprite = closedTrapSprite;
        }

        bool actuallyPopped = car.PopTire(tire);

        if (audioSource != null)
        {
            if (snapSound != null) audioSource.PlayOneShot(snapSound, 1f);
            
            if (actuallyPopped && tirePopSound != null) audioSource.PlayOneShot(tirePopSound, 0.8f);
        }

        // --- NEW: Custom Meme Integration ---
        if (actuallyPopped && !string.IsNullOrEmpty(memeCategoryTirePop))
        {
            if (MemeManager.instance != null)
            {
                MemeManager.instance.TriggerReaction(memeCategoryTirePop);
            }
        }
    }
}