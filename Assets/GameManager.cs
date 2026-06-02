using UnityEngine;
using TMPro; 

public class GameManager : MonoBehaviour
{
    public static GameManager instance; 

    [Header("Dynamic Spawning")]
    public GameObject[] carPrefabs;
    public GameObject[] mapPrefabs;
    public Transform spawnPoint; // Where the car drops in
    public CameraController mainCamera; // We need to tell the camera what to follow!

    [Header("Game Stats")]
    public int totalCoins = 0;
    public float distanceTraveled = 0f;
    public int highestDistance = 0;
    public bool isGameOver = false;

    [Header("UI References")]
    public TextMeshProUGUI coinText; 
    public TextMeshProUGUI distanceText; 
    public TextMeshProUGUI highScoreText; 
    public GameObject gameOverPanel; 
    public TextMeshProUGUI nitroCountText; 

    [Header("Player Tracking")]
    public Transform playerCar; // This will be assigned automatically now
    private Vector3 startPosition;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 1. SPAWN THE MAP
        int selectedMap = PlayerPrefs.GetInt("SelectedMap", 0);
        if (mapPrefabs.Length > selectedMap && mapPrefabs[selectedMap] != null)
        {
            // Spawn the map at the center of the world (0,0,0)
            Instantiate(mapPrefabs[selectedMap], Vector3.zero, Quaternion.identity);
        }

        // 2. SPAWN THE CAR
        int selectedCar = PlayerPrefs.GetInt("SelectedCar", 0);
        if (carPrefabs.Length > selectedCar && carPrefabs[selectedCar] != null)
        {
            // Spawn the car at the specific spawn point
            GameObject spawnedCar = Instantiate(carPrefabs[selectedCar], spawnPoint.position, Quaternion.identity);
            
            playerCar = spawnedCar.transform;
            startPosition = playerCar.position;

            // Tell the camera to follow our newly spawned car!
            if (mainCamera != null)
            {
                mainCamera.target = playerCar;
            }
        }

        // Load stats
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
        UnityEngine.SceneManagement.SceneManager.LoadScene(1); // Return to Garage
    }
}