using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GarageManager : MonoBehaviour
{
    [Header("UI - Bank")]
    public TextMeshProUGUI totalCoinsText;

    [Header("UI - Levels (Displays: 'Lv 3 / Max 5')")]
    public TextMeshProUGUI engineLevelText;
    public TextMeshProUGUI suspensionLevelText;
    public TextMeshProUGUI tiresLevelText;
    public TextMeshProUGUI fuelLevelText;

    [Header("UI - Upgrade Costs")]
    public TextMeshProUGUI engineCostText;
    public TextMeshProUGUI suspensionCostText;
    public TextMeshProUGUI tiresCostText;
    public TextMeshProUGUI fuelCostText;

    [Header("UI - Nitro")]
    public TextMeshProUGUI nitroAmountText;
    public TextMeshProUGUI nitroCostText;
    public int nitroCost = 250; 

    // Max Purchased Levels
    private int maxEngine, maxSus, maxTire, maxFuel;
    // Currently Equipped Levels
    private int eqEngine, eqSus, eqTire, eqFuel;
    private int nitroAmount;
    
    private int baseCost = 100;

    void Start()
    {
        // Load Max Levels
        maxEngine = PlayerPrefs.GetInt("EngineLevel", 1);
        maxSus = PlayerPrefs.GetInt("SuspensionLevel", 1);
        maxTire = PlayerPrefs.GetInt("TireLevel", 1);
        maxFuel = PlayerPrefs.GetInt("FuelLevel", 1);

        // Load Equipped Levels (Defaults to max if not set)
        eqEngine = PlayerPrefs.GetInt("EquippedEngineLevel", maxEngine);
        eqSus = PlayerPrefs.GetInt("EquippedSuspensionLevel", maxSus);
        eqTire = PlayerPrefs.GetInt("EquippedTireLevel", maxTire);
        eqFuel = PlayerPrefs.GetInt("EquippedFuelLevel", maxFuel);

        nitroAmount = PlayerPrefs.GetInt("NitroAmount", 0);

        UpdateAllUI();
    }

    public void TuneStatUp(string statName)
    {
        switch (statName)
        {
            case "Engine":
                eqEngine = Mathf.Clamp(eqEngine + 1, 1, maxEngine);
                PlayerPrefs.SetInt("EquippedEngineLevel", eqEngine);
                break;
            case "Suspension":
                eqSus = Mathf.Clamp(eqSus + 1, 1, maxSus);
                PlayerPrefs.SetInt("EquippedSuspensionLevel", eqSus);
                break;
            case "Tires":
                eqTire = Mathf.Clamp(eqTire + 1, 1, maxTire);
                PlayerPrefs.SetInt("EquippedTireLevel", eqTire);
                break;
            case "Fuel":
                eqFuel = Mathf.Clamp(eqFuel + 1, 1, maxFuel);
                PlayerPrefs.SetInt("EquippedFuelLevel", eqFuel);
                break;
        }
        
        PlayerPrefs.Save();
        UpdateAllUI();
    }

    public void TuneStatDown(string statName)
    {
        switch (statName)
        {
            case "Engine":
                eqEngine = Mathf.Clamp(eqEngine - 1, 1, maxEngine);
                PlayerPrefs.SetInt("EquippedEngineLevel", eqEngine);
                break;
            case "Suspension":
                eqSus = Mathf.Clamp(eqSus - 1, 1, maxSus);
                PlayerPrefs.SetInt("EquippedSuspensionLevel", eqSus);
                break;
            case "Tires":
                eqTire = Mathf.Clamp(eqTire - 1, 1, maxTire);
                PlayerPrefs.SetInt("EquippedTireLevel", eqTire);
                break;
            case "Fuel":
                eqFuel = Mathf.Clamp(eqFuel - 1, 1, maxFuel);
                PlayerPrefs.SetInt("EquippedFuelLevel", eqFuel);
                break;
        }
        
        PlayerPrefs.Save();
        UpdateAllUI();
    }

    public void UpgradeStat(string statName)
    {
        int totalCoins = PlayerPrefs.GetInt("SavedCoins", 0);
        int cost = 0;

        switch (statName)
        {
            case "Engine": cost = maxEngine * baseCost; break;
            case "Suspension": cost = maxSus * baseCost; break;
            case "Tires": cost = maxTire * baseCost; break;
            case "Fuel": cost = maxFuel * baseCost; break;
        }

        if (totalCoins >= cost)
        {
            totalCoins -= cost;
            PlayerPrefs.SetInt("SavedCoins", totalCoins);

            switch (statName)
            {
                case "Engine": 
                    maxEngine++; eqEngine = maxEngine; 
                    PlayerPrefs.SetInt("EngineLevel", maxEngine); 
                    PlayerPrefs.SetInt("EquippedEngineLevel", eqEngine); 
                    break;
                case "Suspension": 
                    maxSus++; eqSus = maxSus; 
                    PlayerPrefs.SetInt("SuspensionLevel", maxSus); 
                    PlayerPrefs.SetInt("EquippedSuspensionLevel", eqSus); 
                    break;
                case "Tires": 
                    maxTire++; eqTire = maxTire; 
                    PlayerPrefs.SetInt("TireLevel", maxTire); 
                    PlayerPrefs.SetInt("EquippedTireLevel", eqTire); 
                    break;
                case "Fuel": 
                    maxFuel++; eqFuel = maxFuel; 
                    PlayerPrefs.SetInt("FuelLevel", maxFuel); 
                    PlayerPrefs.SetInt("EquippedFuelLevel", eqFuel); 
                    break;
            }

            PlayerPrefs.Save();
            UpdateAllUI();
        }
    }

    public void BuyNitro()
    {
        int totalCoins = PlayerPrefs.GetInt("SavedCoins", 0);
        if (totalCoins >= nitroCost)
        {
            totalCoins -= nitroCost;
            PlayerPrefs.SetInt("SavedCoins", totalCoins);
            nitroAmount++;
            PlayerPrefs.SetInt("NitroAmount", nitroAmount);
            PlayerPrefs.Save();
            UpdateAllUI();
        }
    }

    void UpdateAllUI()
    {
        if (totalCoinsText != null) totalCoinsText.text = PlayerPrefs.GetInt("SavedCoins", 0).ToString();

        // Update Levels to show "Equipped / Max" (e.g., "Lv 2/5")
        if (engineLevelText != null) engineLevelText.text = "Lv " + eqEngine + " / " + maxEngine;
        if (suspensionLevelText != null) suspensionLevelText.text = "Lv " + eqSus + " / " + maxSus;
        if (tiresLevelText != null) tiresLevelText.text = "Lv " + eqTire + " / " + maxTire;
        if (fuelLevelText != null) fuelLevelText.text = "Lv " + eqFuel + " / " + maxFuel;

        // Update Upgrade Costs
        if (engineCostText != null) engineCostText.text = "Cost: " + (maxEngine * baseCost);
        if (suspensionCostText != null) suspensionCostText.text = "Cost: " + (maxSus * baseCost);
        if (tiresCostText != null) tiresCostText.text = "Cost: " + (maxTire * baseCost);
        if (fuelCostText != null) fuelCostText.text = "Cost: " + (maxFuel * baseCost);

        if (nitroAmountText != null) nitroAmountText.text = "Owned: " + nitroAmount;
        if (nitroCostText != null) nitroCostText.text = nitroCost.ToString();
    }

    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }
}