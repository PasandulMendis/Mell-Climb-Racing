using UnityEngine;
using System.Collections; // --- NEW: Required for Coroutines! ---


[RequireComponent(typeof(Collider2D))]
public class BlackHolePortal : MonoBehaviour
{
    [Header("Portal Link")]
    [Tooltip("The destination portal. The MapGenerator will assign this automatically!")]
    public BlackHolePortal linkedPortal;

    [Header("Teleport Settings")]
    [Tooltip("How long after teleporting before the portal can be used again (Prevents infinite loops!)")]
    public float cooldown = 1.0f;
    [HideInInspector] public float lastTeleportTime = -999f; 
    
    [Header("Cinematic Transition")]
    public float scaleDownTime = 0.3f;
    public float cameraPanTime = 0.8f;
    public float scaleUpTime = 0.3f;


    [Header("Effects")]
    public AudioClip teleportSound;

    private AudioSource audioSource;
    private Collider2D col;
    private bool isActivelyTeleporting = false; // Prevents double-triggers


    void Start()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true; // Must be a trigger!

        if (teleportSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isActivelyTeleporting) return;
        if (Time.time < lastTeleportTime + cooldown) return;
        if (linkedPortal == null) return; 

        CarController car = collision.GetComponentInParent<CarController>();

        if (car != null)
        {
            StartCoroutine(TeleportSequence(car));
        }
    }

    IEnumerator TeleportSequence(CarController car)
    {
        // 1. Lock both portals instantly so no physics glitches happen
        isActivelyTeleporting = true;
        linkedPortal.isActivelyTeleporting = true;
        
        // 2. Freeze the entire car (including the bouncy wheels!)
        Rigidbody2D[] allRbs = car.GetComponentsInChildren<Rigidbody2D>();
        foreach (Rigidbody2D rb in allRbs) rb.simulated = false;

        Vector3 originalScale = car.transform.localScale;
        
        if (audioSource != null && teleportSound != null) 
            audioSource.PlayOneShot(teleportSound, 0.8f);

        // 3. SCALE DOWN (Suck into the black hole)
        float t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / scaleDownTime;
            car.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }

        // Move the physical car object silently while it is invisible
        car.transform.position = linkedPortal.transform.position;

        // 4. PAN CAMERA SMOOTHLY
        Camera mainCam = Camera.main;
        CameraController camController = mainCam.GetComponent<CameraController>();
        
        if (camController != null) camController.isCutsceneActive = true;

        Vector3 camStart = mainCam.transform.position;
        Vector3 camEnd = linkedPortal.transform.position + (camController != null ? camController.offset : new Vector3(0, 0, -10f));
        
        t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / cameraPanTime;
            // SmoothStep creates a beautiful ease-in and ease-out glide!
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            mainCam.transform.position = Vector3.Lerp(camStart, camEnd, smoothT);
            yield return null;
        }

        if (linkedPortal.audioSource != null && teleportSound != null) 
            linkedPortal.audioSource.PlayOneShot(teleportSound, 0.8f);

        // 5. SCALE UP (Spit out of the destination portal)
        t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / scaleUpTime;
            car.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);
            yield return null;
        }

        // 6. Restore Physics and Camera
        if (camController != null) camController.isCutsceneActive = false;
        foreach (Rigidbody2D rb in allRbs) rb.simulated = true;

        // 7. Trigger the Meme!
        if (MemeManager.instance != null) MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Scared, true, false);

        // 8. Start the cooldowns so they don't bounce back instantly
        this.lastTeleportTime = Time.time;
        linkedPortal.lastTeleportTime = Time.time;
        isActivelyTeleporting = false;
        linkedPortal.isActivelyTeleporting = false;
    }
}