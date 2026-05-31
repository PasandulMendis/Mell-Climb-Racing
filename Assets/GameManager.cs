using UnityEngine;
using TMPro; // We use TextMeshPro because it makes text look high-quality and crisp!

public class GameManager : MonoBehaviour
{
    // This is called a "Singleton". It allows ANY other script to talk to the GameManager easily!
    public static GameManager instance; 

    [Header("Game Stats")]
    public int totalCoins = 0;
    public float distanceTraveled = 0f;
    public int highestDistance = 0; // NEW: Track the high score!
    public bool isGameOver = false;

    [Header("UI References")]
    public TextMeshProUGUI coinText; 
    public TextMeshProUGUI distanceText; // Slot for our new distance score
    public TextMeshProUGUI highScoreText; // NEW: Slot for the highest distance record
    public GameObject gameOverPanel; // The UI panel that pops up

    [Header("Player Tracking")]
    public Transform playerCar;
    private Vector3 startPosition;

    void Awake()
    {
        // Setup the Singleton
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Load saved coins and high score from the device!
        totalCoins = PlayerPrefs.GetInt("SavedCoins", 0);
        highestDistance = PlayerPrefs.GetInt("HighestDistance", 0);
        
        UpdateCoinUI(); // Set text to saved coins when game starts
        
        // Display the high score immediately
        if (highScoreText != null)
        {
            highScoreText.text = "Best: " + highestDistance.ToString() + "m";
        }
        
        // Setup distance tracking
        if (playerCar != null)
        {
            startPosition = playerCar.position;
        }

        // Hide the game over panel when the game starts
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (!isGameOver && playerCar != null)
        {
            // Calculate distance based on how far right the car has moved
            distanceTraveled = Mathf.Max(0, playerCar.position.x - startPosition.x);
            int currentDistanceInt = Mathf.FloorToInt(distanceTraveled);
            
            // Update the UI (formatting to show whole numbers + 'm' for meters)
            if (distanceText != null)
            {
                distanceText.text = currentDistanceInt.ToString() + "m";
            }

            // Check if we beat the high score during the run!
            if (currentDistanceInt > highestDistance)
            {
                highestDistance = currentDistanceInt;
                if (highScoreText != null)
                {
                    highScoreText.text = "Best: " + highestDistance.ToString() + "m";
                }
            }
        }
    }

    // Other scripts (like the Coin) will call this function!
    public void AddCoins(int amount)
    {
        totalCoins += amount;
        UpdateCoinUI();
    }

    void UpdateCoinUI()
    {
        if (coinText != null)
        {
            coinText.text = totalCoins.ToString();
        }
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        // Show the Game Over UI
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // SAVE THE COINS AND HIGH SCORE! 
        // totalCoins already includes what we loaded at the start + what we collected
        PlayerPrefs.SetInt("SavedCoins", totalCoins);
        PlayerPrefs.SetInt("HighestDistance", highestDistance);
        PlayerPrefs.Save();
        
        Debug.Log("Total Saved Coins is now: " + PlayerPrefs.GetInt("SavedCoins", 0));
    }
    
    // We will link this to a UI Button later
    public void ReturnToMenu()
    {
        // For now, let's just reload the current scene to restart!
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}