using UnityEngine;
using UnityEngine.Video;
using System.Collections; // --- NEW: Required for Coroutines! ---

[RequireComponent(typeof(VideoPlayer))]
public class MemeScreen : MonoBehaviour
{
    public enum ScreenRole { Driver, Passenger }
    
    [Header("Screen Settings")]
    [Tooltip("Who is watching this screen? Driver or Passenger?")]
    public ScreenRole role = ScreenRole.Driver;

    [Header("Animation (Juice)")]
    [Tooltip("How fast the screen squishes and stretches when changing memes")]
    public float wobbleSpeed = 0.08f;

    private VideoPlayer videoPlayer;
    private int currentPriority = -1;
    private bool isMustFinish = false;
    private bool isGameOverPermanent = false;
    private int lastPlayedIndex = -1;

    // --- NEW: Animation Trackers ---
    private Vector3 originalScale;
    private Coroutine wobbleCoroutine;

    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.loopPointReached += OnVideoFinished;
        
        // Save the baseline size of the UI element so it always returns to normal!
        originalScale = transform.localScale;
    }

    void OnEnable()
    {
        // Tell the central manager that this screen exists!
        if (MemeManager.instance != null) MemeManager.instance.RegisterScreen(this);
    }

    void OnDisable()
    {
        if (MemeManager.instance != null) MemeManager.instance.UnregisterScreen(this);
    }

    public void TryPlayReaction(MemeCategory category, bool isGameOver)
    {
        if (isGameOverPermanent) return; // We are dead. Stop reacting.

        // --- THE NEW PRIORITY LOGIC ---
        // 1. Ignore lower tier events entirely
        if (category.priorityTier < currentPriority) return; 
        
        // 2. Ignore SAME tier events entirely (e.g., Gem won't interrupt Fuel)
        if (category.priorityTier == currentPriority && !isGameOver) return; 

        // 3. Ignore HIGHER tier events if the current video is strictly locked to finish
        if (category.priorityTier > currentPriority && isMustFinish && !isGameOver) return;

        if (isGameOver) isGameOverPermanent = true;

        // --- GET CLIPS BASED ON ROLE ---
        VideoClip[] clipArray = GetClipArray(category);
        if (clipArray == null || clipArray.Length == 0) return;

        // --- RANDOM SELECTION WITH ANTI-REPEAT ---
        int clipIndex = Random.Range(0, clipArray.Length);
        if (clipArray.Length > 1 && clipIndex == lastPlayedIndex)
        {
            clipIndex = (clipIndex + 1) % clipArray.Length;
        }
        lastPlayedIndex = clipIndex;

        // --- PLAY THE VIDEO ---
        videoPlayer.Stop(); // Rewind
        videoPlayer.clip = clipArray[clipIndex];
        
        // --- NEW FIX: Check our new loopsInfinitely boolean! ---
        videoPlayer.isLooping = (category.loopsInfinitely || category.categoryName == MemeManager.instance.idleCategoryName || isGameOver);
        
        // Lock in the state
        currentPriority = category.priorityTier;
        isMustFinish = category.forceFullPlaytime;

        videoPlayer.Play();

        // --- NEW: Trigger the Squash & Stretch Animation! ---
        if (wobbleCoroutine != null) StopCoroutine(wobbleCoroutine);
        wobbleCoroutine = StartCoroutine(WobbleRoutine());
    }

    // --- NEW: The AAA UI Pop Animation ---
    IEnumerator WobbleRoutine()
    {
        float t = 0;
        
        // Phase 1: Squish down (Wider X, Shorter Y)
        Vector3 squish = new Vector3(originalScale.x * 1.15f, originalScale.y * 0.8f, originalScale.z);
        while (t < wobbleSpeed)
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(originalScale, squish, t / wobbleSpeed);
            yield return null;
        }

        // Phase 2: Stretch up (Thinner X, Taller Y)
        t = 0;
        Vector3 stretch = new Vector3(originalScale.x * 0.9f, originalScale.y * 1.15f, originalScale.z);
        while (t < wobbleSpeed * 1.2f)
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(squish, stretch, t / (wobbleSpeed * 1.2f));
            yield return null;
        }

        // Phase 3: Settle back to normal
        t = 0;
        while (t < wobbleSpeed)
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(stretch, originalScale, t / wobbleSpeed);
            yield return null;
        }

        // Guarantee it ends exactly where it started
        transform.localScale = originalScale;
    }

    VideoClip[] GetClipArray(MemeCategory category)
    {
        switch (role)
        {
            case ScreenRole.Driver: return category.driverClips;
            case ScreenRole.Passenger: return category.passengerClips;
            default: return null;
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        // --- FIX: Do NOT go back to idle if we are dead, or if the video is designed to loop! ---
        if (isGameOverPermanent || vp.isLooping) return;

        // When a normal reaction finishes, go back to Idle!
        if (currentPriority > 0)
        {
            currentPriority = -1;
            isMustFinish = false;
            if (MemeManager.instance != null) MemeManager.instance.ReturnToIdle(this);
        }
    }

    public void ApplyMute(bool isMuted)
    {
        if (videoPlayer != null) videoPlayer.SetDirectAudioVolume(0, isMuted ? 0f : 1f);
    }
}