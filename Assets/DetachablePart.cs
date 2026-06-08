using UnityEngine;

// This forces Unity to make sure the object has SOME kind of Joint2D on it (Spring, Fixed, Wheel, etc.)
[RequireComponent(typeof(Joint2D))]
public class DetachablePart : MonoBehaviour
{
    [Header("Detachment Effects")]
    [Tooltip("Optional: Sparks or dust to play when the part snaps off!")]
    public ParticleSystem breakParticles;
    [Tooltip("Optional: A metal snapping or thud sound")]
    public AudioClip breakSound;
    
    [Header("Meme Integration")]
    [Tooltip("Should Mellstroy get angry when this falls off?")]
    public bool triggerAngryMeme = true;

    private AudioSource audioSource;
    private bool hasDetached = false;

    void Start()
    {
        // Tag this object as "Player" so it doesn't trigger the CarHealth Game Over while in the truck!
        gameObject.tag = "Player";

        // Auto-setup an invisible AudioSource for the crunch sound
        if (breakSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = breakSound;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }
    }

    // --- THE MAGIC UNITY PHYSICS FUNCTION ---
    // This triggers automatically the exact millisecond ANY Joint's "Break Force" limit is exceeded!
    void OnJointBreak2D(Joint2D brokenJoint)
    {
        if (hasDetached) return;
        hasDetached = true;

        Debug.Log(gameObject.name + " has violently detached from the vehicle!");

        // 1. Play Visual & Audio Effects
        if (breakParticles != null) breakParticles.Play();
        if (audioSource != null) audioSource.Play();

        // 2. Trigger the Angry Reaction
        if (triggerAngryMeme && MemeManager.instance != null)
        {
            // Trigger Angry, override current meme, but DO NOT trigger permanent Game Over loop!
            MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Angry, true, false);
        }

        // 3. Unparent it so it stays behind on the dirt track!
        transform.SetParent(null);
        
        // FIX: We KEEP the tag as "Player" even after it detaches!
        // This makes it "safe debris" so bouncing off your own roof doesn't trigger a Game Over.
        // gameObject.tag = "Untagged"; <- Removed!

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Optional: Make it slightly lighter so it doesn't act like a massive boulder for the back wheels
            rb.mass *= 0.5f; 
        }
    }
}