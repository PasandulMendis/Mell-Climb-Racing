using UnityEngine;
using UnityEngine.UI;

public class MemeManager : MonoBehaviour
{
    public static MemeManager instance;

    public enum MemeEvent { Idle, Happy, Angry, Scared }

    [Header("Meme Displays")]
    public Image streamerCam; // 16:9 UI Image on the Canvas
    public SpriteRenderer driverSeat; // 1:1 Sprite attached to the car

    [Header("Meme Graphics (Drag your sprites here)")]
    public Sprite[] idleMemes;
    public Sprite[] happyMemes;
    public Sprite[] angryMemes;
    public Sprite[] scaredMemes;

    private float memeTimer = 0f;
    private MemeEvent currentEvent = MemeEvent.Idle;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        SetMeme(MemeEvent.Idle);
    }

    void Update()
    {
        // Count down the timer. When it hits 0, return to the Idle meme.
        if (memeTimer > 0)
        {
            memeTimer -= Time.deltaTime;
            if (memeTimer <= 0 && currentEvent != MemeEvent.Idle)
            {
                SetMeme(MemeEvent.Idle);
            }
        }
    }

    public void TriggerMeme(MemeEvent memeType, float duration = 2f)
    {
        // Don't overwrite the Game Over meme
        if (currentEvent == MemeEvent.Angry) return;

        SetMeme(memeType);
        
        if (memeType != MemeEvent.Angry) 
        {
            memeTimer = duration; // Revert back to idle after 'duration' seconds
        }
        else
        {
            memeTimer = 0f; // Angry (Game Over) stays forever
        }
    }

    void SetMeme(MemeEvent memeType)
    {
        currentEvent = memeType;
        Sprite[] selectedArray = idleMemes;

        switch (memeType)
        {
            case MemeEvent.Idle: selectedArray = idleMemes; break;
            case MemeEvent.Happy: selectedArray = happyMemes; break;
            case MemeEvent.Angry: selectedArray = angryMemes; break;
            case MemeEvent.Scared: selectedArray = scaredMemes; break;
        }

        if (selectedArray.Length > 0)
        {
            // Pick a random meme from the correct category
            Sprite randomMeme = selectedArray[Random.Range(0, selectedArray.Length)];
            
            if (streamerCam != null) streamerCam.sprite = randomMeme;
            if (driverSeat != null) driverSeat.sprite = randomMeme;
        }
    }
}