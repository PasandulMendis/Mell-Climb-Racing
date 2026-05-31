using UnityEngine;
using TMPro; // We use TextMeshPro because it makes text look high-quality and crisp!

public class GameManager : MonoBehaviour
{
    // This is called a "Singleton". It allows ANY other script to talk to the GameManager easily!
    public static GameManager instance; 

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
    public TextMeshProUGUI nitroCountText; // Show how many nitros we have during gameplay

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
        
        UpdateCoinUI(); 
        
        // Setup initial Nitro UI
        UpdateNitroUI(PlayerPrefs.GetInt("NitroAmount", 0));
        
        if (highScoreText != null)
        {
            highScoreText.text = "Best: " + highestDistance.ToString() + "m";
        }
        
        if (playerCar != null)
        {
            startPosition = playerCar.position;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Removed the Main Menu and Time.timeScale freeze from here
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

    public void UpdateNitroUI(int count)
    {
        if (nitroCountText != null)
        {
            nitroCountText.text = "Nitro: " + count.ToString();
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
        // Now this will load the Garage scene (Scene index 0) instead of reloading the game!
        Time.timeScale = 1f; 
        UnityEngine.SceneManagement.SceneManager.LoadScene(0); 
    }

    // Removed the StartGame() method from here
}