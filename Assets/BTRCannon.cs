using UnityEngine;
using System.Collections; // --- NEW: Required for Coroutines ---
using UnityEngine.Rendering.Universal; // --- NEW: Required for URP 2D Lights ---

public class BTRCannon : MonoBehaviour
{
    [Header("Cannon Setup")]
    [Tooltip("Drag an Empty GameObject here to act as the tip of the barrel where the shot comes from")]
    public Transform firePoint;
    [Tooltip("How far forward the automatic scanner looks for targets")]
    public float fireRange = 20f;
    [Tooltip("Seconds between each automatic shot")]
    public float fireCooldown = 1.5f;
    
    [Header("Force Settings")]
    [Tooltip("How hard the invisible force pushes the target object")]
    public float impactForce = 40f;

    [Header("Visual Effects (Muzzle Flash)")]
    [Tooltip("Drag the SpriteRenderer containing your 'BOOM' image here")]
    public SpriteRenderer boomSprite;
    [Tooltip("Drag the URP Point Light 2D here")]
    public Light2D flashLight;
    [Tooltip("How long the flash stays on screen (seconds)")]
    public float flashDuration = 0.15f;
    
    [Header("Audio")]
    [Tooltip("The BOOM sound of the cannon")]
    public AudioClip fireSound;

    [Header("Meme Integration")]
    [Tooltip("Meme played when the cannon automatically fires")]
    public string memeCategoryFire = "Btr_Fire";

    private AudioSource audioSource;
    private float lastFireTime = 0f;
    private Coroutine flashRoutine; // Prevents the flash from turning off early if firing rapidly

    void Start()
    {
        // Setup the audio source automatically
        if (fireSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 2D Sound
            audioSource.playOnAwake = false;
        }

        // Make sure the flash is turned off when the game starts!
        if (boomSprite != null) boomSprite.enabled = false;
        if (flashLight != null) flashLight.enabled = false;
    }

    void Update()
    {
        // Don't do anything if the game is over
        if (GameManager.instance != null && GameManager.instance.isGameOver) return;

        // Check if the cannon has cooled down and is ready to fire
        if (Time.time > lastFireTime + fireCooldown)
        {
            ScanAndFire();
        }
    }

    void ScanAndFire()
    {
        if (firePoint == null) return;

        // Shoot an invisible raycast straight forward from the barrel
        // We use RaycastAll so we can look through triggers (like coins) to hit the physical object behind them
        RaycastHit2D[] hits = Physics2D.RaycastAll(firePoint.position, firePoint.right, fireRange);

        foreach (RaycastHit2D hit in hits)
        {
            // 1. Ignore invisible triggers (Coins, Water, SpeedShrooms, etc.)
            if (hit.collider.isTrigger) continue;

            // 2. Ignore our own car! (So we don't accidentally shoot our own front bumper)
            if (hit.collider.GetComponentInParent<CarController>() != null) continue;

            // 3. Check if the object we saw has a Rigidbody2D (meaning it can be pushed)
            Rigidbody2D targetRb = hit.collider.attachedRigidbody;
            
            if (targetRb != null && targetRb.bodyType == RigidbodyType2D.Dynamic)
            {
                // We found a valid target! FIRE!
                FireCannon(targetRb);
                lastFireTime = Time.time;
                break; // Stop checking. We only shoot one object per cooldown!
            }
        }
    }

    void FireCannon(Rigidbody2D targetRb)
    {
        // 1. Apply the Invisible Force! 
        // (We multiply by the target's mass, so heavy Cows and light Beach Balls both get blasted equally far)
        Vector2 forceDirection = firePoint.right; 
        targetRb.AddForce(forceDirection * impactForce * targetRb.mass, ForceMode2D.Impulse);

        // 2. Play AAA Effects (Image + Light)
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashMuzzleRoutine());
        
        if (audioSource != null && fireSound != null)
        {
            // Vary the pitch slightly so it doesn't sound repetitive
            audioSource.pitch = Random.Range(0.85f, 1.1f);
            audioSource.PlayOneShot(fireSound, 1f);
        }

        // 3. Trigger the Custom Meme!
        if (MemeManager.instance != null && !string.IsNullOrEmpty(memeCategoryFire))
        {
            MemeManager.instance.TriggerReaction(memeCategoryFire);
        }
    }

    // --- NEW: Coroutine to turn the flash on and off ---
    IEnumerator FlashMuzzleRoutine()
    {
        if (boomSprite != null) boomSprite.enabled = true;
        if (flashLight != null) flashLight.enabled = true;

        yield return new WaitForSeconds(flashDuration);

        if (boomSprite != null) boomSprite.enabled = false;
        if (flashLight != null) flashLight.enabled = false;
    }

    // Optional: Draw a red line in the Unity Editor so you can easily see the cannon's range!
    void OnDrawGizmosSelected()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(firePoint.position, firePoint.right * fireRange);
        }
    }
}