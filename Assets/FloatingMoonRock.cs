using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FloatingMoonRock : MonoBehaviour
{
    [Header("Zero-Gravity Settings")]
    [Tooltip("How fast the rocks naturally drift around in zero-G")]
    public float maxDriftSpeed = 0.5f;
    [Tooltip("How fast they slowly spin before being hit")]
    public float maxSpinSpeed = 15f;

    [Header("Optimization (Memory & Visual)")]
    [Tooltip("How far behind the car before this object auto-deletes")]
    public float deleteDistance = 120f;
    [Tooltip("Max absolute distance to prevent memory leaks if it flies into deep space")]
    public float maxSafeDistance = 250f;

    [Header("Custom Meme Integration")]
    [Tooltip("Triggered when the car bumps into the floating rock")]
    public string memeCategoryHit = "MoonRock_Hit";
    private Transform playerCar;
    private bool hasTriggeredMeme = false; // Prevents spamming the meme if it bounces rapidly


    void Start()
    {
        // 1. Setup the Physics
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // Perfect zero-G!
        rb.linearDamping = 0.5f; // Adds a tiny bit of "space friction" so they don't fly away forever when hit

        // Tag it as Player so bouncing them off the car's roof doesn't trigger a Game Over
        gameObject.tag = "Player";

        // 2. Give it a tiny bit of natural space drift!
        rb.linearVelocity = new Vector2(Random.Range(-maxDriftSpeed, maxDriftSpeed), Random.Range(-maxDriftSpeed, maxDriftSpeed));
        rb.angularVelocity = Random.Range(-maxSpinSpeed, maxSpinSpeed);
    }

    void Update()
    {
        // Ensure we always know where the car is
        if (playerCar == null)
        {
            if (GameManager.instance != null && GameManager.instance.playerCar != null)
            {
                playerCar = GameManager.instance.playerCar;
            }
            else
            {
                // Fallback finder
                GameObject car = GameObject.FindGameObjectWithTag("Player");
                if (car != null && car != this.gameObject) playerCar = car.transform;
            }
            return;
        }

        // Distance Check 1: Delete if the car has driven far past it on the X axis
        if (playerCar.position.x - transform.position.x > deleteDistance)
        {
            Destroy(gameObject);
        }
        // Distance Check 2: Fallback delete if it somehow flies extremely far away in any direction (Memory leak prevention)
        else if (Vector2.Distance(playerCar.position, transform.position) > maxSafeDistance)
        {
            Destroy(gameObject);
        }
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasTriggeredMeme) return;

        // Check if whatever we just hit belongs to the car!
        // We look for the CarController script, but we don't need to modify it to do this.
        if (collision.gameObject.GetComponentInParent<CarController>() != null)
        {
            hasTriggeredMeme = true;

            // Trigger the custom meme!
            if (MemeManager.instance != null && !string.IsNullOrEmpty(memeCategoryHit))
            {
                MemeManager.instance.TriggerReaction(memeCategoryHit);
            }
        }
    }
}