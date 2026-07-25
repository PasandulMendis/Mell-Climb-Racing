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
    public int totalGems = 0; // --- NEW ---
    public float distanceTraveled = 0f;
    public int highestDistance = 0;
    public bool isGameOver = false;

    [Header("Pause Menu & Audio")]
    public GameObject pauseMenuPanel;
    public TextMeshProUGUI muteButtonText;
    [HideInInspector] public bool isPaused = false;

    [Header("Mobile Inputs")]
    [HideInInspector] public bool isMobileGasPressed = false;
    [HideInInspector] public bool isMobileBrakePressed = false;

    [Header("UI References")]
    public TextMeshProUGUI coinText; 
    public TextMeshProUGUI gemText; // --- NEW: Drag your Game Scene Gem Text here ---
    public TextMeshProUGUI distanceText; 
    public TextMeshProUGUI highScoreText; 
    public GameObject gameOverPanel; 
    public TextMeshProUGUI nitroCountText; 
    [Tooltip("The Green Nitro image set to 'Filled'")]
    public Image hypeNitroFillImage; // --- NEW ---

    [Header("Dashboard UI")]
    public RectTransform revNeedle; // The rotating needle image
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

    // --- NEW: STAT TRACKING FOR GAME OVER SCREEN ---
    [HideInInspector] public int totalCoinsThisRun = 0;
    [HideInInspector] public int totalGemsThisRun = 0; // --- NEW ---
    [HideInInspector] public int runFlips = 0;
    [HideInInspector] public float runAirTime = 0f;
    [HideInInspector] public int runNitrosUsed = 0;
    // -----------------------------------------------

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
        Application.targetFrameRate = 60;

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
        totalGems = PlayerPrefs.GetInt("SavedGems", 0); // --- NEW ---
        
        highestDistance = PlayerPrefs.GetInt("HighestDistance_Map_" + currentMapIndex, 0);
        maxMilestoneLevel = PlayerPrefs.GetInt("MaxMilestone_Map_" + currentMapIndex, 0);
        
        UpdateCoinUI(); 
        UpdateGemUI(); // --- NEW ---
        UpdateNitroUI(PlayerPrefs.GetInt("NitroAmount", 0));
        UpdateHypeUI(0f); // --- NEW: Start hype meter at 0% ---
        
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
        if (isPaused) return; // Prevent game logic from running while paused

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
            // ----------------------------------

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

    private int GetMilestoneDistance(int targetLevel)
    {
        if (targetLevel <= 1) return 200; 
        return 200 + ((targetLevel - 1) * 300);
    }

    void ReachMilestone()
    {
        maxMilestoneLevel++;
        PlayerPrefs.SetInt("MaxMilestone_Map_" + currentMapIndex, maxMilestoneLevel);
        
        int reward = maxMilestoneLevel * 1000; 
        AddCoins(reward);

        if (milestonePopupPanel != null)
        {
            milestonePopupPanel.SetActive(true);
            if (milestonePopupText != null) milestonePopupText.text = "STAGE " + maxMilestoneLevel + " REACHED!\n+" + reward + " COINS";
            popupTimer = 3.0f; 
        }

        if (MemeManager.instance != null) MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Happy);

        nextMilestoneDistance = GetMilestoneDistance(maxMilestoneLevel + 1);
        if (nextMilestoneText != null) nextMilestoneText.text = nextMilestoneDistance.ToString() + "m";
    }

    // --- PAUSE MENU LOGIC ---
    public void PauseGame()
    {
        if (isGameOver) return; // Don't allow pausing if dead
        
        isPaused = true;
        Time.timeScale = 0f; // Freeze all physics and delta time
        
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Unfreeze the game
        
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; 
        Application.targetFrameRate = 60;
        int activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        
        if (SceneTransitionManager.instance != null) 
            SceneTransitionManager.instance.LoadScene(activeScene);
        else 
            UnityEngine.SceneManagement.SceneManager.LoadScene(activeScene);
    }

    public void ToggleMute()
    {
        bool isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
        PlayerPrefs.SetInt("IsMuted", !isMuted ? 1 : 0);
        PlayerPrefs.Save();
        
        ApplyMuteState();
        
        // Also force the MemeManager to update its VideoPlayer volume immediately
        if (MemeManager.instance != null)
        {
            MemeManager.instance.ApplyMuteState();
        }
    }

    public void ApplyMuteState()
    {
        bool isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
        
        // Globally mutes all standard AudioSources in the scene (Engine, Impacts, Coins)
        AudioListener.volume = isMuted ? 0f : 1f;
        
        if (muteButtonText != null)
        {
            muteButtonText.text = isMuted ? "UNMUTE" : "MUTE";
        }
    }

    // --- INPUTS & GENERAL ---
    public void PointerDownGas() { isMobileGasPressed = true; }
    public void PointerUpGas() { isMobileGasPressed = false; }
    public void PointerDownBrake() { isMobileBrakePressed = true; }
    public void PointerUpBrake() { isMobileBrakePressed = false; }

    public void MobileNitroPressed()
    {
        if (playerCar != null)
        {
            CarController cc = playerCar.GetComponent<CarController>();
            if (cc != null) cc.TryActivateNitro();
        }
    }

    public void AddCoins(int amount)
    {
        totalCoins += amount;
        totalCoinsThisRun += amount; // Track coins earned SPECIFICALLY this run!
        UpdateCoinUI();
    }

    // --- NEW METHOD ---
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
        if (nitroCountText != null) nitroCountText.text = "Nitro: " + count.ToString();
    }

    public void UpdateHypeUI(float fillPercent)
    {
        if (hypeNitroFillImage != null)
        {
            hypeNitroFillImage.fillAmount = fillPercent;
        }
    }

    // UPDATED: Now accepts the reason for dying!
    public void GameOver(string deathReason)
    {
        if (isGameOver) return;
        isGameOver = true;

        PlayerPrefs.SetInt("SavedCoins", totalCoins);
        PlayerPrefs.SetInt("SavedGems", totalGems); // --- NEW: Save the gems! ---
        PlayerPrefs.SetInt("HighestDistance_Map_" + currentMapIndex, highestDistance);
        PlayerPrefs.Save();

        // Check if we have the new cinematic manager!
        if (GameOverManager.instance != null)
        {
            GameOverManager.instance.StartSequence(deathReason);
        }
        else if (gameOverPanel != null) 
        {
            // Fallback to old behavior if you haven't set up the new script yet
            gameOverPanel.SetActive(true);
        }
    }
    
    public void ReturnToMenu()
    {
        Time.timeScale = 1f; 
        Application.targetFrameRate = 60; 
        
        if (SceneTransitionManager.instance != null) 
            SceneTransitionManager.instance.LoadScene(1);
        else 
            UnityEngine.SceneManagement.SceneManager.LoadScene(1); 
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; 
        Application.targetFrameRate = 60; 
        
        if (SceneTransitionManager.instance != null) 
            SceneTransitionManager.instance.LoadScene(0);
        else 
            UnityEngine.SceneManagement.SceneManager.LoadScene(0); 
    }
}