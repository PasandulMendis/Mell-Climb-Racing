using UnityEngine;
using TMPro; 

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
    public float distanceTraveled = 0f;
    public int highestDistance = 0;
    public bool isGameOver = false;

    [Header("Mobile Inputs")]
    [HideInInspector] public bool isMobileGasPressed = false;
    [HideInInspector] public bool isMobileBrakePressed = false;

    [Header("UI References")]
    public TextMeshProUGUI coinText; 
    public TextMeshProUGUI distanceText; 
    public TextMeshProUGUI highScoreText; 
    public GameObject gameOverPanel; 
    public TextMeshProUGUI nitroCountText; 

    [Header("Dashboard UI")]
    public RectTransform revNeedle; // The rotating needle image
    [Tooltip("The Z rotation angle when the car is completely stopped")]
    public float minRevAngle = 135f; 
    [Tooltip("The Z rotation angle when the engine is maxed out")]
    public float maxRevAngle = -135f;

    [Header("Fuel Dashboard UI")]
    public RectTransform fuelNeedle; // The rotating fuel needle image
    [Tooltip("The Z rotation angle when Empty")]
    public float minFuelAngle = 135f; 
    [Tooltip("The Z rotation angle when Full")]
    public float maxFuelAngle = -135f;
    [Tooltip("The warning_lit.png image that flashes when fuel is low")]
    public GameObject fuelWarningLight; 

    [Header("Milestones & Custom Tracker")]
    public GameObject flagIndicator; 
    public Transform flagStartPoint; 
    public Transform flagEndPoint; 

    public TextMeshProUGUI nextMilestoneText; 
    public GameObject milestonePopupPanel; 
    public TextMeshProUGUI milestonePopupText; 
    
    public float milestoneVisibleRange = 200f;

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
        
        highestDistance = PlayerPrefs.GetInt("HighestDistance_Map_" + currentMapIndex, 0);
        maxMilestoneLevel = PlayerPrefs.GetInt("MaxMilestone_Map_" + currentMapIndex, 0);
        
        UpdateCoinUI(); 
        UpdateNitroUI(PlayerPrefs.GetInt("NitroAmount", 0));
        
        if (highScoreText != null) highScoreText.text = "Best: " + highestDistance.ToString() + "m";
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        nextMilestoneDistance = GetMilestoneDistance(maxMilestoneLevel + 1);
        
        if (nextMilestoneText != null) nextMilestoneText.text = nextMilestoneDistance.ToString() + "m";
        if (milestonePopupPanel != null) milestonePopupPanel.SetActive(false);
        if (flagIndicator != null) flagIndicator.SetActive(false);
    }

    void Update()
    {
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
                    // Map the 0.0 -> 1.0 RPM value to the physical angle of the gauge
                    float targetAngle = Mathf.Lerp(minRevAngle, maxRevAngle, cc.currentRPM);
                    revNeedle.rotation = Quaternion.Euler(0f, 0f, targetAngle);
                }
            }

            // --- DASHBOARD FUEL NEEDLE & WARNING LOGIC ---
            FuelSystem fs = playerCar.GetComponent<FuelSystem>();
            CarController carController = playerCar.GetComponent<CarController>();
            
            if (fs != null && carController != null)
            {
                // Calculate percentage (0.0 to 1.0)
                float fuelPercent = Mathf.Clamp01(fs.currentFuel / Mathf.Max(carController.maxFuel, 1f));

                if (fuelNeedle != null)
                {
                    // Fuel = 0 (Empty) -> minFuelAngle | Fuel = 1 (Full) -> maxFuelAngle
                    float targetAngle = Mathf.Lerp(minFuelAngle, maxFuelAngle, fuelPercent);
                    fuelNeedle.rotation = Quaternion.Euler(0f, 0f, targetAngle);
                }

                if (fuelWarningLight != null)
                {
                    if (fuelPercent <= 0.2f && fuelPercent > 0f)
                    {
                        // Blink rapidly if fuel is 20% or lower
                        bool isBlinking = Mathf.PingPong(Time.time * 6f, 1f) > 0.5f;
                        fuelWarningLight.SetActive(isBlinking);
                    }
                    else if (fuelPercent <= 0f)
                    {
                        // Stay solidly lit when completely empty!
                        fuelWarningLight.SetActive(true);
                    }
                    else
                    {
                        // Turn off if we have plenty of fuel
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
        UpdateCoinUI();
    }

    void UpdateCoinUI()
    {
        if (coinText != null) coinText.text = totalCoins.ToString();
    }

    public void UpdateNitroUI(int count)
    {
        if (nitroCountText != null) nitroCountText.text = "Nitro: " + count.ToString();
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        PlayerPrefs.SetInt("SavedCoins", totalCoins);
        PlayerPrefs.SetInt("HighestDistance_Map_" + currentMapIndex, highestDistance);
        PlayerPrefs.Save();
    }
    
    public void ReturnToMenu()
    {
        Time.timeScale = 1f; 
        Application.targetFrameRate = 60; 
        UnityEngine.SceneManagement.SceneManager.LoadScene(1); 
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; 
        Application.targetFrameRate = 60; 
        UnityEngine.SceneManagement.SceneManager.LoadScene(0); 
    }
}