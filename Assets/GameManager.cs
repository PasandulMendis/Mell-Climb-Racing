using UnityEngine;
using TMPro; 
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance; 

    [Header("Dynamic Spawning")]
    public GameObject[] carPrefabs;
    public GameObject[] mapPrefabs; 
    public Transform spawnPoint; 
    public CameraController mainCamera; 

    [Header("Game Stats")]
    public int totalCoins = 0;
    public int totalGems = 0;
    public float distanceTraveled = 0f;
    public int highestDistance = 0;
    public bool isGameOver = false;

    [Header("Pause Menu & Audio UI")]
    public GameObject pauseMenuPanel;
    [Tooltip("UI Image component for the Music Button")]
    public Image musicButtonImage;
    [Tooltip("UI Image component for the SFX Button")]
    public Image sfxButtonImage;

    [Header("Audio Icon Sprites")]
    [Tooltip("Icon displayed when Music is enabled")]
    public Sprite musicOnSprite;
    [Tooltip("Icon displayed when Music is muted")]
    public Sprite musicOffSprite;
    [Tooltip("Icon displayed when SFX is enabled")]
    public Sprite sfxOnSprite;
    [Tooltip("Icon displayed when SFX is muted")]
    public Sprite sfxOffSprite;

    [HideInInspector] public bool isPaused = false;

    [Header("Mobile Inputs & Cooldowns")]
    [HideInInspector] public bool isMobileGasPressed = false;
    [HideInInspector] public bool isMobileBrakePressed = false;
    
    [Tooltip("Duration of the Nitro cooldown in seconds")]
    public float nitroCooldownTime = 10f;
    private float currentNitroCooldown = 0f;

    [Header("UI References")]
    public TextMeshProUGUI coinText; 
    public TextMeshProUGUI gemText;
    public TextMeshProUGUI distanceText; 
    public TextMeshProUGUI highScoreText; 
    public GameObject gameOverPanel; 
    public TextMeshProUGUI nitroCountText; 
    
    [Tooltip("The UI Image of the Nitro button to darken during cooldown")]
    public Image nitroButtonIcon;
    [Tooltip("The Green Nitro image set to 'Filled'")]
    public Image hypeNitroFillImage;

    [Header("Dashboard UI")]
    public RectTransform revNeedle; 
    public float minRevAngle = 135f; 
    public float maxRevAngle = -135f;
    [Tooltip("The warning_lit.png image that flashes when hitting the rev limiter")]
    public GameObject revWarningLight; 
    [Tooltip("RPM threshold (0.0 to 1.0) to start vibrating and flashing")]
    public float revWarningThreshold = 0.95f;

    [Header("Fuel Dashboard UI")]
    public RectTransform fuelNeedle; 
    public float minFuelAngle = 135f; 
    public float maxFuelAngle = -135f;
    public GameObject fuelWarningLight; 

    [Header("Milestones & Custom Tracker")]
    public GameObject flagIndicator; 
    public Transform flagStartPoint; 
    public Transform flagEndPoint; 

    public TextMeshProUGUI nextMilestoneText; 
    public GameObject milestonePopupPanel; 
    public TextMeshProUGUI milestonePopupText; 
    
    public float milestoneVisibleRange = 200f;

    [Header("Audio")]
    public AudioClip milestoneSound;

    [HideInInspector] public int totalCoinsThisRun = 0;
    [HideInInspector] public int totalGemsThisRun = 0;
    [HideInInspector] public int runFlips = 0;
    [HideInInspector] public float runAirTime = 0f;
    [HideInInspector] public int runNitrosUsed = 0;

    private int maxMilestoneLevel = 0; 
    private int nextMilestoneDistance = 200;
    private float popupTimer = 0f;
    private int currentMapIndex = 0; 

    [Header("Player Tracking")]
    public Transform playerCar; 
    private Vector3 startPosition;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        Application.targetFrameRate = 120;

        currentMapIndex = PlayerPrefs.GetInt("SelectedMap", 0);

        if (mapPrefabs.Length > currentMapIndex && mapPrefabs[currentMapIndex] != null)
        {
            Instantiate(mapPrefabs[currentMapIndex], Vector3.zero, Quaternion.identity);
        }

        int selectedCar = PlayerPrefs.GetInt("SelectedCar", 0);
        if (carPrefabs.Length > selectedCar && carPrefabs[selectedCar] != null)
        {
            GameObject spawnedCar = Instantiate(carPrefabs[selectedCar], spawnPoint.position, Quaternion.identity);
            
            playerCar = spawnedCar.transform;
            startPosition = playerCar.position;

            if (mainCamera != null) mainCamera.target = playerCar;
        }

        totalCoins = PlayerPrefs.GetInt("SavedCoins", 0);
        totalGems = PlayerPrefs.GetInt("SavedGems", 0);
        
        highestDistance = PlayerPrefs.GetInt("HighestDistance_Map_" + currentMapIndex, 0);
        maxMilestoneLevel = PlayerPrefs.GetInt("MaxMilestone_Map_" + currentMapIndex, 0);
        
        UpdateCoinUI(); 
        UpdateGemUI(); 
        UpdateNitroUI(PlayerPrefs.GetInt("NitroAmount", 0));
        UpdateHypeUI(0f);
        
        if (highScoreText != null) highScoreText.text = "Best: " + highestDistance.ToString() + "m";
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);

        nextMilestoneDistance = GetMilestoneDistance(maxMilestoneLevel + 1);
        
        if (nextMilestoneText != null) nextMilestoneText.text = nextMilestoneDistance.ToString() + "m";
        if (milestonePopupPanel != null) milestonePopupPanel.SetActive(false);
        if (flagIndicator != null) flagIndicator.SetActive(false);

        ApplyMuteState();
    }

    void Update()
    {
        if (isPaused) return;

        // --- NITRO COOLDOWN & VFX LOGIC ---
        if (currentNitroCooldown > 0)
        {
            currentNitroCooldown -= Time.deltaTime;
            
            // Re-enable the button visually when cooldown finishes
            if (currentNitroCooldown <= 0)
            {
                if (nitroButtonIcon != null)
                    nitroButtonIcon.color = Color.white; // Restore normal bright color
                if (hypeNitroFillImage != null)
                    hypeNitroFillImage.color = Color.white; // Restore normal bright color for the fill image
            }
        }

        if (!isGameOver && playerCar != null)
        {
            distanceTraveled = Mathf.Max(0, playerCar.position.x - startPosition.x);
            int currentDistanceInt = Mathf.FloorToInt(distanceTraveled);
            
            if (distanceText != null) distanceText.text = currentDistanceInt.ToString() + "m";

            // --- DASHBOARD REV NEEDLE LOGIC ---
            if (revNeedle != null)
            {
                CarController cc = playerCar.GetComponent<CarController>();
                if (cc != null)
                {
                    float targetAngle = Mathf.Lerp(minRevAngle, maxRevAngle, cc.currentRPM);
                    
                    if (cc.currentRPM >= revWarningThreshold && cc.hasFuel)
                    {
                        targetAngle += Random.Range(-4f, 4f); 
                        
                        if (revWarningLight != null)
                        {
                            bool isBlinking = Mathf.PingPong(Time.time * 15f, 1f) > 0.5f;
                            revWarningLight.SetActive(isBlinking);
                        }
                    }
                    else
                    {
                        if (revWarningLight != null) revWarningLight.SetActive(false);
                    }

                    revNeedle.rotation = Quaternion.Euler(0f, 0f, targetAngle);
                }
            }

            // --- DASHBOARD FUEL LOGIC ---
            FuelSystem fs = playerCar.GetComponent<FuelSystem>();
            CarController carController = playerCar.GetComponent<CarController>();
            
            if (fs != null && carController != null)
            {
                float fuelPercent = Mathf.Clamp01(fs.currentFuel / Mathf.Max(carController.maxFuel, 1f));

                if (fuelNeedle != null)
                {
                    float targetAngle = Mathf.Lerp(minFuelAngle, maxFuelAngle, fuelPercent);
                    fuelNeedle.rotation = Quaternion.Euler(0f, 0f, targetAngle);
                }

                if (fuelWarningLight != null)
                {
                    if (fuelPercent <= 0.2f && fuelPercent > 0f)
                    {
                        bool isBlinking = Mathf.PingPong(Time.time * 6f, 1f) > 0.5f;
                        fuelWarningLight.SetActive(isBlinking);
                    }
                    else if (fuelPercent <= 0f)
                    {
                        fuelWarningLight.SetActive(true);
                    }
                    else
                    {
                        fuelWarningLight.SetActive(false);
                    }
                }
            }

            float distToMilestone = nextMilestoneDistance - distanceTraveled;

            if (flagIndicator != null && flagStartPoint != null && flagEndPoint != null)
            {
                if (distToMilestone <= milestoneVisibleRange && distToMilestone > 0)
                {
                    flagIndicator.SetActive(true);
                    
                    float progressPercentage = 1f - (distToMilestone / milestoneVisibleRange);
                    flagIndicator.transform.position = Vector3.Lerp(flagStartPoint.position, flagEndPoint.position, progressPercentage);
                }
                else
                {
                    flagIndicator.SetActive(false);
                }
            }

            if (currentDistanceInt >= nextMilestoneDistance)
            {
                ReachMilestone();
            }

            if (currentDistanceInt > highestDistance)
            {
                highestDistance = currentDistanceInt;
                if (highScoreText != null) highScoreText.text = "Best: " + highestDistance.ToString() + "m";
            }
        }

        if (popupTimer > 0)
        {
            popupTimer -= Time.deltaTime;
            if (popupTimer <= 0 && milestonePopupPanel != null) milestonePopupPanel.SetActive(false);
        }
    }

    public void SaveGameData()
    {
        PlayerPrefs.SetInt("SavedCoins", totalCoins);
        PlayerPrefs.SetInt("SavedGems", totalGems);
        PlayerPrefs.SetInt("HighestDistance_Map_" + currentMapIndex, highestDistance);
        PlayerPrefs.SetInt("MaxMilestone_Map_" + currentMapIndex, maxMilestoneLevel);
        PlayerPrefs.Save();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveGameData();
    }

    void OnApplicationQuit()
    {
        SaveGameData();
    }

    void OnDisable()
    {
        SaveGameData();
    }

    private int GetMilestoneDistance(int targetLevel)
    {
        if (targetLevel <= 1) return 200; 
        return 200 + ((targetLevel - 1) * 300);
    }

    void ReachMilestone()
    {
        maxMilestoneLevel++;
        
        int reward = maxMilestoneLevel * 1000; 
        AddCoins(reward);

        SaveGameData();

        if (milestonePopupPanel != null)
        {
            milestonePopupPanel.SetActive(true);
            if (milestonePopupText != null) milestonePopupText.text = "STAGE " + maxMilestoneLevel + " REACHED!\n+" + reward + " COINS";
            popupTimer = 3.0f; 
        }

        if (AudioManager.instance != null && milestoneSound != null)
        {
            AudioManager.instance.PlaySFX(milestoneSound, 1f);
        }

        nextMilestoneDistance = GetMilestoneDistance(maxMilestoneLevel + 1);
        if (nextMilestoneText != null) nextMilestoneText.text = nextMilestoneDistance.ToString() + "m";
    }

    public void PauseGame()
    {
        if (isGameOver) return;
        
        SaveGameData();

        isPaused = true;
        Time.timeScale = 0f;
        
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        ApplyMuteState();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    public void RestartGame()
    {
        SaveGameData();

        Time.timeScale = 1f; 
        Application.targetFrameRate = 60;
        int activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        
        if (SceneTransitionManager.instance != null) 
            SceneTransitionManager.instance.LoadScene(activeScene);
        else 
            UnityEngine.SceneManagement.SceneManager.LoadScene(activeScene);
    }

    /// <summary>
    /// Toggles Music state via AudioManager and updates the UI button sprite.
    /// </summary>
    public void ToggleMusic()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.ToggleMusic();
            ApplyMuteState();
        }
    }

    /// <summary>
    /// Toggles SFX state via AudioManager and updates the UI button sprite.
    /// </summary>
    public void ToggleSFX()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.ToggleSFX();
            ApplyMuteState();
        }
    }

    /// <summary>
    /// Reads audio mute statuses and updates Pause Menu button icon sprites (ON/OFF).
    /// </summary>
    public void ApplyMuteState()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.ApplyAudioSettings();

            bool musicMuted = AudioManager.instance.IsMusicMuted();
            bool sfxMuted = AudioManager.instance.IsSFXMuted();

            // --- MUSIC BUTTON SPRITE SWAP ---
            if (musicButtonImage != null)
            {
                if (musicMuted && musicOffSprite != null)
                {
                    musicButtonImage.sprite = musicOffSprite;
                }
                else if (!musicMuted && musicOnSprite != null)
                {
                    musicButtonImage.sprite = musicOnSprite;
                }
            }

            // --- SFX BUTTON SPRITE SWAP ---
            if (sfxButtonImage != null)
            {
                if (sfxMuted && sfxOffSprite != null)
                {
                    sfxButtonImage.sprite = sfxOffSprite;
                }
                else if (!sfxMuted && sfxOnSprite != null)
                {
                    sfxButtonImage.sprite = sfxOnSprite;
                }
            }
        }
    }

    public void PointerDownGas() { isMobileGasPressed = true; }
    public void PointerUpGas() { isMobileGasPressed = false; }
    public void PointerDownBrake() { isMobileBrakePressed = true; }
    public void PointerUpBrake() { isMobileBrakePressed = false; }

    public void MobileNitroPressed()
    {
        // Block nitro activation if currently on cooldown
        if (currentNitroCooldown > 0) return;

        if (playerCar != null)
        {
            CarController cc = playerCar.GetComponent<CarController>();
            if (cc != null) 
            {
                cc.TryActivateNitro();
                
                // Start cooldown timer
                currentNitroCooldown = nitroCooldownTime;

                // Apply darker color VFX immediately to indicate it's on cooldown
                Color cooldownColor = new Color(0.4f, 0.4f, 0.4f, 1f); // Dark Grey color look
                if (nitroButtonIcon != null)
                {
                    nitroButtonIcon.color = cooldownColor; 
                }
                if (hypeNitroFillImage != null)
                {
                    hypeNitroFillImage.color = cooldownColor;
                }
            }
        }
    }

    public void AddCoins(int amount)
    {
        totalCoins += amount;
        totalCoinsThisRun += amount; 
        UpdateCoinUI();
    }

    public void AddGems(int amount)
    {
        totalGems += amount;
        totalGemsThisRun += amount;
        UpdateGemUI();
        
        if (MemeManager.instance != null) MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Happy);
    }

    void UpdateCoinUI()
    {
        if (coinText != null) coinText.text = totalCoins.ToString();
    }

    void UpdateGemUI()
    {
        if (gemText != null) gemText.text = totalGems.ToString();
    }

    public void UpdateNitroUI(int count)
    {
        if (nitroCountText != null) nitroCountText.text = "" + count.ToString();
    }

    public void UpdateHypeUI(float fillPercent)
    {
        if (hypeNitroFillImage != null)
        {
            hypeNitroFillImage.fillAmount = fillPercent;
        }
    }

    public void GameOver(string deathReason)
    {
        if (isGameOver) return;
        isGameOver = true;

        SaveGameData(); 

        if (GameOverManager.instance != null)
        {
            GameOverManager.instance.StartSequence(deathReason);
        }
        else if (gameOverPanel != null) 
        {
            gameOverPanel.SetActive(false);
        }
    }
    
    public void ReturnToMenu()
    {
        SaveGameData(); 

        Time.timeScale = 1f; 
        Application.targetFrameRate = 60; 
        
        if (SceneTransitionManager.instance != null) 
            SceneTransitionManager.instance.LoadScene(1);
        else 
            UnityEngine.SceneManagement.SceneManager.LoadScene(1); 
    }

    public void ReturnToMainMenu()
    {
        SaveGameData(); 

        Time.timeScale = 1f; 
        Application.targetFrameRate = 60; 
        
        if (SceneTransitionManager.instance != null) 
            SceneTransitionManager.instance.LoadScene(0);
        else 
            UnityEngine.SceneManagement.SceneManager.LoadScene(0); 
    }
}