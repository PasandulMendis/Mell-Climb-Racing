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
        // --- NEW: Unlock 120 FPS exclusively for the gameplay scene! ---
        Application.targetFrameRate = 120;

        int selectedMap = PlayerPrefs.GetInt("SelectedMap", 0);
        if (mapPrefabs.Length > selectedMap && mapPrefabs[selectedMap] != null)
        {
            Instantiate(mapPrefabs[selectedMap], Vector3.zero, Quaternion.identity);
        }

        int selectedCar = PlayerPrefs.GetInt("SelectedCar", 0);
        if (carPrefabs.Length > selectedCar && carPrefabs[selectedCar] != null)
        {
            GameObject spawnedCar = Instantiate(carPrefabs[selectedCar], spawnPoint.position, Quaternion.identity);
            
            playerCar = spawnedCar.transform;
            startPosition = playerCar.position;

            if (mainCamera != null)
            {
                mainCamera.target = playerCar;
            }
        }

        totalCoins = PlayerPrefs.GetInt("SavedCoins", 0);
        highestDistance = PlayerPrefs.GetInt("HighestDistance", 0);
        
        UpdateCoinUI(); 
        UpdateNitroUI(PlayerPrefs.GetInt("NitroAmount", 0));
        
        if (highScoreText != null) highScoreText.text = "Best: " + highestDistance.ToString() + "m";
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (!isGameOver && playerCar != null)
        {
            distanceTraveled = Mathf.Max(0, playerCar.position.x - startPosition.x);
            int currentDistanceInt = Mathf.FloorToInt(distanceTraveled);
            
            if (distanceText != null) distanceText.text = currentDistanceInt.ToString() + "m";

            if (currentDistanceInt > highestDistance)
            {
                highestDistance = currentDistanceInt;
                if (highScoreText != null) highScoreText.text = "Best: " + highestDistance.ToString() + "m";
            }
        }
    }

    // --- MOBILE UI INPUT METHODS ---
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
    // -------------------------------

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
        PlayerPrefs.SetInt("HighestDistance", highestDistance);
        PlayerPrefs.Save();
    }
    
    public void ReturnToMenu()
    {
        Time.timeScale = 1f; 
        
        // --- NEW: Drop back to 60 FPS in the menus to save battery life ---
        Application.targetFrameRate = 60; 
        
        UnityEngine.SceneManagement.SceneManager.LoadScene(1); 
    }
}