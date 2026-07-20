using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class Icicle : MonoBehaviour
{
    [Header("Drop Mechanics")]
    [Tooltip("How close the car needs to be (on the X axis) to trigger the drop check")]
    public float triggerDistance = 15f;
    [Tooltip("Probability (0.0 to 1.0) that the icicle actually falls. 1 = always falls.")]
    public float dropChance = 0.6f;
    [Tooltip("How fast gravity pulls it down (Make it heavy so it falls fast!)")]
    public float dropGravityScale = 4f;

    [Header("Effects")]
    public ParticleSystem shatterParticles;
    public AudioClip shatterSound;

    private Rigidbody2D rb;
    private Transform playerCar;
    private bool hasChecked = false;
    private bool isFalling = false;
    private AudioSource audioSource;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Start frozen in place on the ceiling
        // FIXED: Moved to Awake so it freezes BEFORE the first physics frame!
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        
        // Tag as Player so it acts as safe debris if it hits the car's roof
        gameObject.tag = "Player";

        if (shatterSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }
    }

    void Update()
    {
        if (hasChecked || isFalling) return;

        if (playerCar == null)
        {
            if (GameManager.instance != null && GameManager.instance.playerCar != null)
                playerCar = GameManager.instance.playerCar;
            return;
        }

        // Check if the car is driving underneath it!
        float distanceX = Mathf.Abs(playerCar.position.x - transform.position.x);
        
        if (distanceX <= triggerDistance)
        {
            hasChecked = true;
            
            // Random chance to actually fall (keeps the player guessing!)
            if (Random.value <= dropChance)
            {
                TriggerDrop();
            }
        }
    }

    void TriggerDrop()
    {
        isFalling = true;
        
        // Unfreeze it and let gravity pull it down like a spear!
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = dropGravityScale;

        // NEW: Do not destroy instantly on touch! Let it rest on the ground and melt after 10 seconds.
        Destroy(gameObject, 10f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isFalling) return;

        // 1. INSTANT DEATH CHECK (If it hits the driver's head!)
        CarHealth driverHealth = collision.gameObject.GetComponent<CarHealth>();
        if (driverHealth == null) driverHealth = collision.gameObject.GetComponentInParent<CarHealth>();

        if (driverHealth != null && (collision.gameObject.name.ToLower().Contains("head") || collision.gameObject.name.ToLower().Contains("driver")))
        {
            driverHealth.Die("IMPALED BY ICICLE!");
            PlayShatterEffects();
            Destroy(gameObject); // Destroy it because it shattered on the player
            return;
        }

        // 2. NORMAL CAR HIT (Bounces off the roof/hood)
        CarController hitCar = collision.gameObject.GetComponentInParent<CarController>();
        
        if (hitCar != null)
        {
            PlayShatterEffects();

            // Meme Integration
            if (MemeManager.instance != null)
            {
                MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Angry, true, false);
            }
            
            // NOTICE: We do NOT destroy the icicle here anymore! It will bounce off the car naturally.
        }
    }

    void PlayShatterEffects()
    {
        if (shatterParticles != null)
        {
            ParticleSystem clone = Instantiate(shatterParticles, transform.position, Quaternion.identity);
            clone.Play();
            Destroy(clone.gameObject, 2f);
        }

        if (audioSource != null && shatterSound != null)
        {
            // Spawn temporary sound object so it finishes playing even when the icicle is destroyed
            GameObject soundObj = new GameObject("IcicleShatter");
            AudioSource tempAudio = soundObj.AddComponent<AudioSource>();
            tempAudio.clip = shatterSound;
            tempAudio.spatialBlend = 0f;
            tempAudio.pitch = Random.Range(0.8f, 1.2f);
            tempAudio.Play();
            Destroy(soundObj, 2f);
        }
    }
}