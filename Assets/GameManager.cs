using UnityEngine;
using TMPro; // We use TextMeshPro because it makes text look high-quality and crisp!

public class GameManager : MonoBehaviour
{
    // This is called a "Singleton". It allows ANY other script to talk to the GameManager easily!
    public static GameManager instance; 

    [Header("Game Stats")]
    public int totalCoins = 0;

    [Header("UI References")]
    public TextMeshProUGUI coinText; // Slot for our UI text

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
        UpdateCoinUI(); // Set text to 0 when game starts
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
}