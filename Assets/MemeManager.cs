using UnityEngine;
using UnityEngine.Video;

public class MemeManager : MonoBehaviour
{
    public static MemeManager instance;

    public enum MemeEvent { Idle, Happy, Angry, Scared }

    [Header("Video Players")]
    public VideoPlayer streamerPlayer;
    public VideoPlayer driverPlayer;

    [Header("Streamer Cam Memes (Reaction Clips)")]
    public VideoClip[] streamerIdle;
    public VideoClip[] streamerHappy;
    public VideoClip[] streamerAngry;
    public VideoClip[] streamerScared;

    [Header("Driver Seat Memes (Crazy Actions)")]
    public VideoClip[] driverIdle;
    public VideoClip[] driverHappy;
    public VideoClip[] driverAngry;
    public VideoClip[] driverScared;

    private MemeEvent currentEvent = MemeEvent.Idle;
    
    // 1. TRUE RANDOMNESS: This completely ignores the MapGenerator's fixed seed!
    private System.Random rng = new System.Random(); 
    
    private int lastStreamerIndex = -1;
    private int lastDriverIndex = -1;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (streamerPlayer != null) streamerPlayer.loopPointReached += OnStreamerFinished;
        if (driverPlayer != null) driverPlayer.loopPointReached += OnDriverFinished;

        // Force the initial idle state
        TriggerMeme(MemeEvent.Idle, true);
    }

    public void TriggerMeme(MemeEvent memeType, bool forceOverride = false)
    {
        if (currentEvent == MemeEvent.Angry) return; // Game over is permanent
        
        // NEW: Always allow 'Happy' (Fuel/Nitro) to instantly interrupt and restart!
        // We still protect 'Scared' and 'Idle' so they don't stutter every frame.
        if (memeType != MemeEvent.Happy && !forceOverride && currentEvent == memeType) return; 

        currentEvent = memeType;

        // Play Streamer
        VideoClip[] sClips = GetStreamerArray(memeType);
        PlayClip(streamerPlayer, sClips, memeType, ref lastStreamerIndex);

        // Play Driver
        VideoClip[] dClips = GetDriverArray(memeType);
        PlayClip(driverPlayer, dClips, memeType, ref lastDriverIndex);
    }

    void PlayClip(VideoPlayer player, VideoClip[] array, MemeEvent memeType, ref int lastIndexTracker)
    {
        if (player == null || array == null || array.Length == 0) return;

        int newIndex = rng.Next(0, array.Length);
        
        // ANTI-REPEAT: If we have more than 1 video, never play the same one twice in a row!
        if (array.Length > 1 && newIndex == lastIndexTracker)
        {
            newIndex = (newIndex + 1) % array.Length;
        }
        
        lastIndexTracker = newIndex;
        
        // NEW: Force the video player to stop and rewind before playing the instant reaction!
        player.Stop(); 
        
        player.clip = array[newIndex];
        player.isLooping = (memeType == MemeEvent.Idle || memeType == MemeEvent.Angry);
        player.Play();
    }

    VideoClip[] GetStreamerArray(MemeEvent memeType)
    {
        switch (memeType)
        {
            case MemeEvent.Happy: return streamerHappy;
            case MemeEvent.Angry: return streamerAngry;
            case MemeEvent.Scared: return streamerScared;
            default: return streamerIdle;
        }
    }

    VideoClip[] GetDriverArray(MemeEvent memeType)
    {
        switch (memeType)
        {
            case MemeEvent.Happy: return driverHappy;
            case MemeEvent.Angry: return driverAngry;
            case MemeEvent.Scared: return driverScared;
            default: return driverIdle;
        }
    }

    // 3. INDEPENDENT FINISHERS: Stops a short streamer video from cutting off a long driver video!
    void OnStreamerFinished(VideoPlayer vp)
    {
        if (currentEvent != MemeEvent.Idle && currentEvent != MemeEvent.Angry)
        {
            PlayClip(streamerPlayer, streamerIdle, MemeEvent.Idle, ref lastStreamerIndex);
            CheckIfBothIdle();
        }
    }

    void OnDriverFinished(VideoPlayer vp)
    {
        if (currentEvent != MemeEvent.Idle && currentEvent != MemeEvent.Angry)
        {
            PlayClip(driverPlayer, driverIdle, MemeEvent.Idle, ref lastDriverIndex);
            CheckIfBothIdle();
        }
    }

    void CheckIfBothIdle()
    {
        // If both players have successfully returned to playing idle clips, unlock the event manager
        bool streamerIsIdle = (streamerPlayer.isLooping); // Idle clips are set to loop
        bool driverIsIdle = (driverPlayer.isLooping);

        if (streamerIsIdle && driverIsIdle)
        {
            currentEvent = MemeEvent.Idle;
        }
    }
}