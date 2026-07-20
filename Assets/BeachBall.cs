using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class BeachBall : MonoBehaviour
{
    [Header("Ball Settings")]
    public float wakeUpDistance = 35f;
    public float rollTorque = 80f;
    
    [Header("Audio")]
    public AudioClip bounceSound; 
    
    private Rigidbody2D rb;
    private Transform playerCar;
    private bool isAwake = false;
    private AudioSource audioSource;

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
}