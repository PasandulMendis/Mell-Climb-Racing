using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

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

    // Delegate and event for external scripts (e.g. Car Engine/Nitro audio) to respond to mute toggles
    public delegate void AudioSettingsChangedHandler(bool isMusicMuted, bool isSFXMuted);
    public static event AudioSettingsChangedHandler OnAudioSettingsChanged;

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

    void Start()
    {
        // Apply saved mute preferences when AudioManager boots
        ApplyAudioSettings();
    }

    void Update()
    {
        // --- CONTINUOUS SAFETY GUARD FOR DYNAMICALLY SPAWNED SOUNDS ---
        // Catches newly instantiated AudioSources (Coins, Gems, Engine, Nitros, Explosions, PlayClipAtPoint)
        if (IsSFXMuted())
        {
            MuteAllSceneSFX();
        }
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
        // Re-apply audio settings on scene load
        ApplyAudioSettings();

        // Reset the music volume in case it was lowered by a Game Over Screen
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

    /// <summary>
    /// Reads saved preferences and applies mute states independently to Music and SFX.
    /// Mutes all scene AudioSources and VideoPlayers under the SFX channel.
    /// </summary>
    public void ApplyAudioSettings()
    {
        bool musicMuted = IsMusicMuted();
        bool sfxMuted = IsSFXMuted();

        if (musicSource != null)
        {
            musicSource.mute = musicMuted;
        }

        if (sfxSource != null)
        {
            sfxSource.mute = sfxMuted;
        }

        if (sfxMuted)
        {
            MuteAllSceneSFX();
        }
        else
        {
            UnmuteAllSceneSFX();
        }

        // Notify external scripts about state changes
        OnAudioSettingsChanged?.Invoke(musicMuted, sfxMuted);
    }

    /// <summary>
    /// Force mutes all non-music AudioSources and VideoPlayers currently in the active scene.
    /// Includes inactive objects so components that activate later remain muted.
    /// </summary>
    private void MuteAllSceneSFX()
    {
        // Find all AudioSources including inactive prefabs/objects
        AudioSource[] allSources = FindObjectsOfType<AudioSource>(true);
        foreach (AudioSource src in allSources)
        {
            if (src != musicSource)
            {
                src.mute = true;
            }
        }

        // Mute VideoPlayer audio (e.g., MemeManager videos)
        VideoPlayer[] allVideoPlayers = FindObjectsOfType<VideoPlayer>(true);
        foreach (VideoPlayer vp in allVideoPlayers)
        {
            for (ushort i = 0; i < vp.controlledAudioTrackCount; i++)
            {
                vp.SetDirectAudioMute(i, true);
            }
        }
    }

    /// <summary>
    /// Unmutes non-music AudioSources and VideoPlayers when SFX is enabled.
    /// </summary>
    private void UnmuteAllSceneSFX()
    {
        AudioSource[] allSources = FindObjectsOfType<AudioSource>(true);
        foreach (AudioSource src in allSources)
        {
            if (src != musicSource)
            {
                src.mute = false;
            }
        }

        VideoPlayer[] allVideoPlayers = FindObjectsOfType<VideoPlayer>(true);
        foreach (VideoPlayer vp in allVideoPlayers)
        {
            for (ushort i = 0; i < vp.controlledAudioTrackCount; i++)
            {
                vp.SetDirectAudioMute(i, false);
            }
        }
    }

    /// <summary>
    /// Toggles Background Music mute state and saves to PlayerPrefs.
    /// </summary>
    public void ToggleMusic()
    {
        SetMusicMuted(!IsMusicMuted());
    }

    /// <summary>
    /// Toggles Sound Effects (SFX) mute state and saves to PlayerPrefs.
    /// </summary>
    public void ToggleSFX()
    {
        SetSFXMuted(!IsSFXMuted());
    }

    public void SetMusicMuted(bool mute)
    {
        PlayerPrefs.SetInt("IsMusicMuted", mute ? 1 : 0);
        PlayerPrefs.Save();
        ApplyAudioSettings();
    }

    public void SetSFXMuted(bool mute)
    {
        PlayerPrefs.SetInt("IsSFXMuted", mute ? 1 : 0);
        PlayerPrefs.Save();
        ApplyAudioSettings();
    }

    public bool IsMusicMuted()
    {
        return PlayerPrefs.GetInt("IsMusicMuted", 0) == 1;
    }

    public bool IsSFXMuted()
    {
        return PlayerPrefs.GetInt("IsSFXMuted", 0) == 1;
    }

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
        if (IsSFXMuted()) return;

        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}