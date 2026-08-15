using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
public class MoonGeyser : MonoBehaviour
{
    [Header("Mass-Based Hover Physics")]
    public float lightWeightThreshold = 4f;
    public float heavyWeightThreshold = 8f;
    [Tooltip("How strongly the geyser pushes a lightweight vehicle upward (e.g. 35)")]
    public float lightCarHoverForce = 35f;
    [Tooltip("How strongly it pushes a heavy truck. Keep this low so it barely lifts! (e.g. 5)")]
    public float heavyCarHoverForce = 5f;

    [Header("Geyser Cycle Timing")]
    public float minActiveTime = 2f;
    public float maxActiveTime = 5f;
    public float minInactiveTime = 3f;
    public float maxInactiveTime = 6f;

    [Header("Visuals")]
    [Tooltip("Drag the child Particle System here")]
    public ParticleSystem dustParticles;

    private bool isActive = false;

    void Start()
    {
        // Force it to be a trigger so the car NEVER crashes into it!
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        
        // Start the random on/off cycle
        StartCoroutine(GeyserRoutine());
    }

    IEnumerator GeyserRoutine()
    {
        while (true)
        {
            // --- INACTIVE STATE ---
            isActive = false;
            if (dustParticles != null) dustParticles.Stop();
            
            yield return new WaitForSeconds(Random.Range(minInactiveTime, maxInactiveTime));

            // --- ACTIVE STATE ---
            isActive = true;
            if (dustParticles != null) dustParticles.Play();
            
            yield return new WaitForSeconds(Random.Range(minActiveTime, maxActiveTime));
        }
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        // Do nothing if the geyser is sleeping
        if (!isActive) return;

        // Ensure we are only lifting the car
        if (collision.CompareTag("Player"))
        {
            Rigidbody2D rb = collision.attachedRigidbody;
            if (rb != null)
            {
                // Calculate where the car sits between light (0.0) and heavy (1.0)
                float carMass = rb.mass;
                float massLerp = Mathf.InverseLerp(lightWeightThreshold, heavyWeightThreshold, carMass);
                
                // Calculate the exact hover force based on the car's mass
                float currentHoverForce = Mathf.Lerp(lightCarHoverForce, heavyCarHoverForce, massLerp);

                // We multiply by rb.mass so the resulting acceleration exactly matches your tuned values!
                rb.AddForce(transform.up * currentHoverForce * rb.mass, ForceMode2D.Force);
            }
        }
    }
}