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

    [Header("Milestones & Custom Tracker")]
    [Tooltip("The flag image that will move")]
    public GameObject flagIndicator; 
    [Tooltip("An empty UI object marking the far LEFT side of your bar")]
    public Transform flagStartPoint; 
    [Tooltip("An empty UI object marking the far RIGHT side of your bar")]
    public Transform flagEndPoint; 

    public TextMeshProUGUI nextMilestoneText; 
    public GameObject milestonePopupPanel; 
    public TextMeshProUGUI milestonePopupText; 
    
    [Tooltip("How many meters away before the flag appears on the bar?")]
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

        // Calculate the next milestone based on the Hill Climb Racing math pattern
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

            // --- THE NEW CUSTOM TRACKER MATH ---
            float distToMilestone = nextMilestoneDistance - distanceTraveled;

            if (flagIndicator != null && flagStartPoint != null && flagEndPoint != null)
            {
                // Only show the flag if we are exactly within range AND haven't passed it yet
                if (distToMilestone <= milestoneVisibleRange && distToMilestone > 0)
                {
                    flagIndicator.SetActive(true);
                    
                    // 200m away = 0.0 (Start Point), 0m away = 1.0 (End Point)
                    float progressPercentage = 1f - (distToMilestone / milestoneVisibleRange);
                    
                    // Smoothly slide the flag between the two points!
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

    // --- HILL CLIMB RACING DISTANCE FORMULA ---
    private int GetMilestoneDistance(int targetLevel)
    {
        if (targetLevel <= 1) return 200; // Level 1 is always 200m
        
        // Every level after 1 adds an extra 300m
        return 200 + ((targetLevel - 1) * 300);
    }

    void ReachMilestone()
    {
        maxMilestoneLevel++;
        PlayerPrefs.SetInt("MaxMilestone_Map_" + currentMapIndex, maxMilestoneLevel);
        
        // Hill Climb Racing Reward Formula (Level * 1000)
        int reward = maxMilestoneLevel * 1000; 
        AddCoins(reward);

        if (milestonePopupPanel != null)
        {
            milestonePopupPanel.SetActive(true);
            if (milestonePopupText != null) milestonePopupText.text = "STAGE " + maxMilestoneLevel + " REACHED!\n+" + reward + " COINS";
            popupTimer = 3.0f; 
        }

        if (MemeManager.instance != null) MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Happy);

        // Calculate the next target distance
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
    
    // Goes to Garage (Scene 1)
    public void ReturnToMenu()
    {
        Time.timeScale = 1f; 
        Application.targetFrameRate = 60; 
        UnityEngine.SceneManagement.SceneManager.LoadScene(1); 
    }

    // Goes to Main Menu (Scene 0)
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; 
        Application.targetFrameRate = 60; 
        UnityEngine.SceneManagement.SceneManager.LoadScene(0); 
    }
}