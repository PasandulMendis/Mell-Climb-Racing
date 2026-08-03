using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GiantSeesaw : MonoBehaviour
{
    [Header("Physics Settings")]
    [Tooltip("Shifts the weight of the plank to the left (negative X) so it always rests on the ground!")]
    public float leftWeightOffset = -2f;

    [Header("Audio & Effects")]
    [Tooltip("Play a heavy wood crunch when the plank slams into the dirt")]
    public AudioClip slamSound;
    [Tooltip("Play a creaking sound as it tips")]
    public AudioClip creakSound;

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private float lastSlamTime = 0f;
    private bool isCreaking = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

                // --- AUTO-FIX PHYSICS ISSUES ---
        HingeJoint2D hinge = GetComponent<HingeJoint2D>();
        if (transform.parent != null && hinge != null)
        {
            // Find the Rock Base by looking for the OTHER Rigidbody in your Prefab
            Rigidbody2D[] allRbs = transform.parent.GetComponentsInChildren<Rigidbody2D>();
            foreach (Rigidbody2D potentialRock in allRbs)
            {
                if (potentialRock != rb) 
                {
                    // FIX 1: Stop the rock from sliding away when the car hits it!
                    potentialRock.bodyType = RigidbodyType2D.Static;
                    
                    // FIX 2: Connect the plank to the rock so it stops floating in the sky!
                    hinge.connectedBody = potentialRock;
                    break;
                }
            }
        }
        
        // This is the magic line! It tells gravity that the left side of the plank is heavier.
        // It will automatically tip backward and rest on your stopper colliders!
        rb.centerOfMass = new Vector2(leftWeightOffset, 0f);

        if (slamSound != null || creakSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }

        // Tag the plank as player so it doesn't instantly kill the driver if they bump their head on it
        gameObject.tag = "Player"; 
    }

    void FixedUpdate()
    {
        // Play creaking sound if the seesaw is actively swinging
        if (Mathf.Abs(rb.angularVelocity) > 15f)
        {
            if (!isCreaking && audioSource != null && creakSound != null)
            {
                isCreaking = true;
                audioSource.pitch = Random.Range(0.8f, 1.1f);
                audioSource.PlayOneShot(creakSound, 0.4f);
            }
        }
        else
        {
            isCreaking = false;
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // If the seesaw slams into the dirt ground (or the car drops hard onto it)
        if (Time.time > lastSlamTime + 0.5f && Mathf.Abs(rb.angularVelocity) > 20f)
        {
            // Ignore tiny bumps, only play sound on hard slams
            if (audioSource != null && slamSound != null)
            {
                audioSource.pitch = Random.Range(0.7f, 1f);
                audioSource.PlayOneShot(slamSound, 0.8f);
                lastSlamTime = Time.time;
            }
        }

        // Meme Integration: If the player hits it way too hard and flips it wildly!
        CarController hitCar = col.gameObject.GetComponentInParent<CarController>();
        if (hitCar != null && col.relativeVelocity.magnitude > 15f)
        {
            if (MemeManager.instance != null) MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Scared, true, false);
        }
    }
}