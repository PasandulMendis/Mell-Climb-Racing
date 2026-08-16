using UnityEngine;
using UnityEngine.Rendering.Universal; // Required for URP Light2D

public class NitroEffect : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The CarController to read the Nitro state from")]
    public CarController carController;
    public SpriteRenderer flameSprite;
    public Light2D flameLight;
    public AudioSource nitroAudio;

    [Header("Sprite Animation")]
    [Tooltip("Drop your 4 flame sprites here in order")]
    public Sprite[] flameFrames;
    [Tooltip("Frames per second for the flame animation")]
    public float animationFPS = 24f;
    
    [Header("Light Settings")]
    [Tooltip("How fast the light flickers chaotically")]
    public float flickerSpeed = 30f;
    public float minLightIntensity = 1f;
    public float maxLightIntensity = 3f;

    private float randomNoiseOffset;
    private float frameTimer;
    private int currentFrameIndex;

    void Start()
    {
        if (carController == null) carController = GetComponentInParent<CarController>();
        if (flameSprite == null) flameSprite = GetComponent<SpriteRenderer>();
        if (flameLight == null) flameLight = GetComponent<Light2D>();
        if (nitroAudio == null) nitroAudio = GetComponent<AudioSource>();

        randomNoiseOffset = Random.Range(0f, 100f);

        if (flameSprite != null) flameSprite.enabled = false;
        if (flameLight != null) flameLight.enabled = false;
    }

    void Update()
    {
        if (carController != null && carController.isNitroActive)
        {
            // 1. Turn on visuals and audio when Nitro is active
            if (!flameSprite.enabled) flameSprite.enabled = true;
            if (!flameLight.enabled) flameLight.enabled = true;
            
            if (nitroAudio != null && !nitroAudio.isPlaying) 
            {
                nitroAudio.Play();
            }

            // 2. Animate the Sprites (Sequential Swapping)
            if (flameFrames.Length > 0)
            {
                frameTimer += Time.deltaTime;
                float timePerFrame = 1f / animationFPS;

                if (frameTimer >= timePerFrame)
                {
                    frameTimer -= timePerFrame;
                    currentFrameIndex = (currentFrameIndex + 1) % flameFrames.Length;
                    flameSprite.sprite = flameFrames[currentFrameIndex];
                }
            }

            // 3. Animate the Light (Chaotic Perlin Noise)
            if (flameLight != null)
            {
                float noise = Mathf.PerlinNoise((Time.time + randomNoiseOffset) * flickerSpeed, 0f);
                flameLight.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, noise);
            }
        }
        else
        {
            // Turn everything off the moment Nitro is released
            if (flameSprite != null && flameSprite.enabled) flameSprite.enabled = false;
            if (flameLight != null && flameLight.enabled) flameLight.enabled = false;
            
            if (nitroAudio != null && nitroAudio.isPlaying) 
            {
                nitroAudio.Stop();
            }
            
            // Reset animation so it always starts on the first frame next time
            frameTimer = 0f;
            currentFrameIndex = 0;
        }
    }
}