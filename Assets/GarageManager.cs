using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GarageManager : MonoBehaviour
{
    [Header("UI - Bank")]
    public TextMeshProUGUI totalCoinsText;

    [Header("UI - Levels")]
    public TextMeshProUGUI engineLevelText;
    public TextMeshProUGUI suspensionLevelText;
    public TextMeshProUGUI tiresLevelText;
    public TextMeshProUGUI fuelLevelText;

    [Header("UI - Costs")]
    public TextMeshProUGUI engineCostText;
    public TextMeshProUGUI suspensionCostText;
    public TextMeshProUGUI tiresCostText;
    public TextMeshProUGUI fuelCostText;

    private int engineLevel;
    private int suspensionLevel;
    private int tiresLevel;
    private int fuelLevel;
    
    private int baseCost = 100; // Formula: Level * baseCost

    void Start()
    {
        // Load saved levels
        engineLevel = PlayerPrefs.GetInt("EngineLevel", 1);
        suspensionLevel = PlayerPrefs.GetInt("SuspensionLevel", 1);
        tiresLevel = PlayerPrefs.GetInt("TireLevel", 1);
        fuelLevel = PlayerPrefs.GetInt("FuelLevel", 1);

        UpdateAllUI();
    }

    public void UpgradeStat(string statName)
    {
        int totalCoins = PlayerPrefs.GetInt("SavedCoins", 0);
        int cost = 0;

        // Determine cost based on which stat we are upgrading
        switch (statName)
        {
            case "Engine": cost = engineLevel * baseCost; break;
            case "Suspension": cost = suspensionLevel * baseCost; break;
            case "Tires": cost = tiresLevel * baseCost; break;
            case "Fuel": cost = fuelLevel * baseCost; break;
        }

        // Check if we can afford it
        if (totalCoins >= cost)
        {
            totalCoins -= cost;
            PlayerPrefs.SetInt("SavedCoins", totalCoins);

            // Level up and save
            switch (statName)
            {
                case "Engine": 
                    engineLevel++; 
                    PlayerPrefs.SetInt("EngineLevel", engineLevel); 
                    break;
                case "Suspension": 
                    suspensionLevel++; 
                    PlayerPrefs.SetInt("SuspensionLevel", suspensionLevel); 
                    break;
                case "Tires": 
                    tiresLevel++; 
                    PlayerPrefs.SetInt("TireLevel", tiresLevel); 
                    break;
                case "Fuel": 
                    fuelLevel++; 
                    PlayerPrefs.SetInt("FuelLevel", fuelLevel); 
                    break;
            }

            PlayerPrefs.Save();
            UpdateAllUI();
        }
        else
        {
            Debug.Log("Not enough coins!");
        }
    }

    void UpdateAllUI()
    {
        // Update Bank
        if (totalCoinsText != null) 
            totalCoinsText.text = PlayerPrefs.GetInt("SavedCoins", 0).ToString();

        // Update Levels
        if (engineLevelText != null) engineLevelText.text = "Lvl " + engineLevel;
        if (suspensionLevelText != null) suspensionLevelText.text = "Lvl " + suspensionLevel;
        if (tiresLevelText != null) tiresLevelText.text = "Lvl " + tiresLevel;
        if (fuelLevelText != null) fuelLevelText.text = "Lvl " + fuelLevel;

        // Update Costs
        if (engineCostText != null) engineCostText.text = (engineLevel * baseCost).ToString();
        if (suspensionCostText != null) suspensionCostText.text = (suspensionLevel * baseCost).ToString();
        if (tiresCostText != null) tiresCostText.text = (tiresLevel * baseCost).ToString();
        if (fuelCostText != null) fuelCostText.text = (fuelLevel * baseCost).ToString();
    }

    public void StartGame()
    {
        // Load the Gameplay scene (Assuming it is Build Index 1)
        SceneManager.LoadScene(1);
    }
}