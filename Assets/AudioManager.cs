using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Sources")]
    [Tooltip("Dedicated source for looping background music")]
    public AudioSource musicSource;
    [Tooltip("Dedicated source for UI sounds (Clicks, Buys, Errors)")]
    public AudioSource sfxSource;

    [Header("Background Music (BGM)")]
    [Tooltip("The music that plays continuously across Main Menu and Garage")]
    public AudioClip menuBGM;
    
    [Tooltip("Assign the Map BGM in order of your Map IDs (0 = Village, 1 = Forest, 2 = Snow, etc.)")]
    public AudioClip[] mapBGMs;

    [Header("UI Sounds (Centralized)")]
    [Tooltip("Standard click for basic navigation buttons")]
    public AudioClip standardClickSound;
    [Tooltip("Heavy, satisfying click for buying upgrades")]
    public AudioClip buyUpgradeSound;
    [Tooltip("Exciting sound for hitting the big PLAY button")]
    public AudioClip playButtonSound;
    [Tooltip("Buzzer for when you don't have enough coins")]
    public AudioClip errorSound;

    void Awake()
    {
        // --- SINGLETON PATTERN ---
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Listen to scene loads to switch music
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        // --- SAFETY FIX: Only unregister if THIS is the real, surviving manager! ---
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset the volume in case it was lowered by a Game Over Screen
        if (musicSource != null) musicSource.volume = 1f;

        // Scene 0 = Main Menu, Scene 1 = Garage
        if (scene.buildIndex == 0 || scene.buildIndex == 1)
        {
            // Only play the Menu BGM if it isn't ALREADY playing!
            if (musicSource.clip != menuBGM)
            {
                musicSource.clip = menuBGM;
                musicSource.loop = true;
                musicSource.Play();
            }
            else if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }
        // Scene 2 = Gameplay
        else if (scene.buildIndex == 2)
        {
            int currentMapIndex = PlayerPrefs.GetInt("SelectedMap", 0);

            if (currentMapIndex < mapBGMs.Length && mapBGMs[currentMapIndex] != null)
            {
                musicSource.clip = mapBGMs[currentMapIndex];
                musicSource.loop = true;
                musicSource.Play();
            }
            else
            {
                Debug.LogWarning("AudioManager: No background music assigned for Map Index " + currentMapIndex);
                musicSource.Stop();
            }
        }
    }

    // --- GLOBAL MODIFIERS ---
    
    // Called by GameManager when the player dies
    public void ReduceMusicVolumeOnDeath()
    {
        // Instantly drop volume to 30%
        if (musicSource != null) musicSource.volume = 0.3f;
    }

    // --- CENTRAL UI AUDIO HOOKS ---
    public void PlayStandardClick() { PlaySFX(standardClickSound); }
    public void PlayBuyUpgradeClick() { PlaySFX(buyUpgradeSound); }
    public void PlayPlayButtonClick() { PlaySFX(playButtonSound); }
    public void PlayErrorClick() { PlaySFX(errorSound); }

    // --- GLOBAL SFX HELPER ---
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}