using UnityEngine;
using UnityEngine.Video; // We need this to play .mp4 files!

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

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        // Subscribe to the exact moment the videos finish playing
        if (streamerPlayer != null)
        {
            streamerPlayer.loopPointReached += OnVideoFinished;
        }
        if (driverPlayer != null)
        {
            driverPlayer.loopPointReached += OnVideoFinished;
        }

        SetMeme(MemeEvent.Idle);
    }

    public void TriggerMeme(MemeEvent memeType)
    {
        // Don't overwrite the Game Over meme
        if (currentEvent == MemeEvent.Angry) return;
        
        // Don't restart the meme if it's already playing!
        if (currentEvent == memeType) return;

        SetMeme(memeType);
    }

    void SetMeme(MemeEvent memeType)
    {
        currentEvent = memeType;
        
        VideoClip[] selectedStreamerArray = streamerIdle;
        VideoClip[] selectedDriverArray = driverIdle;

        switch (memeType)
        {
            case MemeEvent.Idle: 
                selectedStreamerArray = streamerIdle; selectedDriverArray = driverIdle; break;
            case MemeEvent.Happy: 
                selectedStreamerArray = streamerHappy; selectedDriverArray = driverHappy; break;
            case MemeEvent.Angry: 
                selectedStreamerArray = streamerAngry; selectedDriverArray = driverAngry; break;
            case MemeEvent.Scared: 
                selectedStreamerArray = streamerScared; selectedDriverArray = driverScared; break;
        }

        // Apply random Streamer meme
        if (streamerPlayer != null && selectedStreamerArray.Length > 0)
        {
            streamerPlayer.clip = selectedStreamerArray[Random.Range(0, selectedStreamerArray.Length)];
            streamerPlayer.isLooping = (memeType == MemeEvent.Idle || memeType == MemeEvent.Angry);
            streamerPlayer.Play();
        }
        
        // Apply random Driver meme
        if (driverPlayer != null && selectedDriverArray.Length > 0)
        {
            driverPlayer.clip = selectedDriverArray[Random.Range(0, selectedDriverArray.Length)];
            driverPlayer.isLooping = (memeType == MemeEvent.Idle || memeType == MemeEvent.Angry);
            driverPlayer.Play();
        }
    }

    // This is called automatically by Unity the exact millisecond a video ends!
    void OnVideoFinished(VideoPlayer vp)
    {
        // If the video that finished wasn't Idle or Angry, return to Idle!
        if (currentEvent != MemeEvent.Idle && currentEvent != MemeEvent.Angry)
        {
            SetMeme(MemeEvent.Idle);
        }
    }
}