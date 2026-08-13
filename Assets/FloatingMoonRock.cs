using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FloatingMoonRock : MonoBehaviour
{
    [Header("Zero-Gravity Settings")]
    [Tooltip("How fast the rocks naturally drift around in zero-G")]
    public float maxDriftSpeed = 0.5f;
    [Tooltip("How fast they slowly spin before being hit")]
    public float maxSpinSpeed = 15f;

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
}