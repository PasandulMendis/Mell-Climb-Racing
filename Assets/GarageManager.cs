using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GarageManager : MonoBehaviour
{
    [Header("Car Data Integration")]
    [Tooltip("Drag all your Car Prefabs here in the same order as their IDs (0, 1, 2...). GarageManager reads the array lengths from these prefabs to set max levels.")]
    public CarController[] carPrefabs;

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
    
    private int carID; // Tracks which car we are upgrading
    private CarController activeCar; // Reference to the currently loaded car prefab

    // Absolute limits read from the CarController arrays
    private int limitEngine, limitSus, limitTire, limitFuel;

    void Start()
    {
        carID = PlayerPrefs.GetInt("SelectedCar", 0);

        // --- NEW: Calculate limits dynamically from the assigned prefabs ---
        if (carPrefabs != null && carID < carPrefabs.Length && carPrefabs[carID] != null)
        {
            activeCar = carPrefabs[carID];
            limitEngine = Mathf.Max(1, activeCar.engineSpeedLevels.Length);
            limitSus = Mathf.Max(1, activeCar.suspensionFrequencyLevels.Length);
            limitTire = Mathf.Max(1, activeCar.tireFrictionLevels.Length);
            limitFuel = Mathf.Max(1, activeCar.fuelCapacityLevels.Length);
        }
        else
        {
            Debug.LogWarning("Car Prefabs not assigned in GarageManager! Defaulting limits to 10.");
            limitEngine = limitSus = limitTire = limitFuel = 10;
        }

        // Load Max Levels for THIS car
        maxEngine = PlayerPrefs.GetInt("Car_" + carID + "_EngineLevel", 1);
        maxSus = PlayerPrefs.GetInt("Car_" + carID + "_SuspensionLevel", 1);
        maxTire = PlayerPrefs.GetInt("Car_" + carID + "_TireLevel", 1);
        maxFuel = PlayerPrefs.GetInt("Car_" + carID + "_FuelLevel", 1);

        // Failsafe: Ensure loaded max levels do not exceed new array limits (if you decreased array sizes later)
        maxEngine = Mathf.Clamp(maxEngine, 1, limitEngine);
        maxSus = Mathf.Clamp(maxSus, 1, limitSus);
        maxTire = Mathf.Clamp(maxTire, 1, limitTire);
        maxFuel = Mathf.Clamp(maxFuel, 1, limitFuel);

        // Load Equipped Levels for THIS car (Defaults to max if not set)
        eqEngine = PlayerPrefs.GetInt("Car_" + carID + "_EquippedEngineLevel", maxEngine);
        eqSus = PlayerPrefs.GetInt("Car_" + carID + "_EquippedSuspensionLevel", maxSus);
        eqTire = PlayerPrefs.GetInt("Car_" + carID + "_EquippedTireLevel", maxTire);
        eqFuel = PlayerPrefs.GetInt("Car_" + carID + "_EquippedFuelLevel", maxFuel);

        // Failsafe for equipped levels
        eqEngine = Mathf.Clamp(eqEngine, 1, maxEngine);
        eqSus = Mathf.Clamp(eqSus, 1, maxSus);
        eqTire = Mathf.Clamp(eqTire, 1, maxTire);
        eqFuel = Mathf.Clamp(eqFuel, 1, maxFuel);

        nitroAmount = PlayerPrefs.GetInt("NitroAmount", 0);

        UpdateAllUI();
    }

    public void TuneStatUp(string statName)
    {
        switch (statName)
        {
            case "Engine": eqEngine = Mathf.Clamp(eqEngine + 1, 1, maxEngine); PlayerPrefs.SetInt("Car_" + carID + "_EquippedEngineLevel", eqEngine); break;
            case "Suspension": eqSus = Mathf.Clamp(eqSus + 1, 1, maxSus); PlayerPrefs.SetInt("Car_" + carID + "_EquippedSuspensionLevel", eqSus); break;
            case "Tires": eqTire = Mathf.Clamp(eqTire + 1, 1, maxTire); PlayerPrefs.SetInt("Car_" + carID + "_EquippedTireLevel", eqTire); break;
            case "Fuel": eqFuel = Mathf.Clamp(eqFuel + 1, 1, maxFuel); PlayerPrefs.SetInt("Car_" + carID + "_EquippedFuelLevel", eqFuel); break;
        }
        
        PlayerPrefs.Save();
        UpdateAllUI();
    }

    public void TuneStatDown(string statName)
    {
        switch (statName)
        {
            case "Engine": eqEngine = Mathf.Clamp(eqEngine - 1, 1, maxEngine); PlayerPrefs.SetInt("Car_" + carID + "_EquippedEngineLevel", eqEngine); break;
            case "Suspension": eqSus = Mathf.Clamp(eqSus - 1, 1, maxSus); PlayerPrefs.SetInt("Car_" + carID + "_EquippedSuspensionLevel", eqSus); break;
            case "Tires": eqTire = Mathf.Clamp(eqTire - 1, 1, maxTire); PlayerPrefs.SetInt("Car_" + carID + "_EquippedTireLevel", eqTire); break;
            case "Fuel": eqFuel = Mathf.Clamp(eqFuel - 1, 1, maxFuel); PlayerPrefs.SetInt("Car_" + carID + "_EquippedFuelLevel", eqFuel); break;
        }
        
        PlayerPrefs.Save();
        UpdateAllUI();
    }

    // Helper to safely get the cost from the array and prevent out-of-bounds errors
    private int GetSafeCost(int[] costArray, int nextLevel)
    {
        if (costArray == null || costArray.Length == 0) return 999999;
        if (nextLevel < costArray.Length) return costArray[nextLevel];
        return costArray[costArray.Length - 1]; // Failsafe: return last available cost
    }

    public void UpgradeStat(string statName)
    {
        if (activeCar == null) return;

        int totalCoins = PlayerPrefs.GetInt("SavedCoins", 0);
        int cost = 0;
        bool canUpgrade = false;

        // Check if we haven't reached the limit set by the CarController arrays
        switch (statName)
        {
            case "Engine": canUpgrade = (maxEngine < limitEngine); if (canUpgrade) cost = GetSafeCost(activeCar.engineCostLevels, maxEngine); break;
            case "Suspension": canUpgrade = (maxSus < limitSus); if (canUpgrade) cost = GetSafeCost(activeCar.suspensionCostLevels, maxSus); break;
            case "Tires": canUpgrade = (maxTire < limitTire); if (canUpgrade) cost = GetSafeCost(activeCar.tireCostLevels, maxTire); break;
            case "Fuel": canUpgrade = (maxFuel < limitFuel); if (canUpgrade) cost = GetSafeCost(activeCar.fuelCostLevels, maxFuel); break;
        }

        if (canUpgrade && totalCoins >= cost)
        {
            totalCoins -= cost;
            PlayerPrefs.SetInt("SavedCoins", totalCoins);

            switch (statName)
            {
                case "Engine": maxEngine++; eqEngine = maxEngine; PlayerPrefs.SetInt("Car_" + carID + "_EngineLevel", maxEngine); PlayerPrefs.SetInt("Car_" + carID + "_EquippedEngineLevel", eqEngine); break;
                case "Suspension": maxSus++; eqSus = maxSus; PlayerPrefs.SetInt("Car_" + carID + "_SuspensionLevel", maxSus); PlayerPrefs.SetInt("Car_" + carID + "_EquippedSuspensionLevel", eqSus); break;
                case "Tires": maxTire++; eqTire = maxTire; PlayerPrefs.SetInt("Car_" + carID + "_TireLevel", maxTire); PlayerPrefs.SetInt("Car_" + carID + "_EquippedTireLevel", eqTire); break;
                case "Fuel": maxFuel++; eqFuel = maxFuel; PlayerPrefs.SetInt("Car_" + carID + "_FuelLevel", maxFuel); PlayerPrefs.SetInt("Car_" + carID + "_EquippedFuelLevel", eqFuel); break;
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

        // Update Levels to show "Equipped / Max Purchased"
        if (engineLevelText != null) engineLevelText.text = "Lv " + eqEngine + " / " + maxEngine;
        if (suspensionLevelText != null) suspensionLevelText.text = "Lv " + eqSus + " / " + maxSus;
        if (tiresLevelText != null) tiresLevelText.text = "Lv " + eqTire + " / " + maxTire;
        if (fuelLevelText != null) fuelLevelText.text = "Lv " + eqFuel + " / " + maxFuel;

        // Update Upgrade Costs - Displays "MAX" if the limit is reached
        if (activeCar != null)
        {
            if (engineCostText != null) engineCostText.text = (maxEngine >= limitEngine) ? "MAX" : "Cost: " + GetSafeCost(activeCar.engineCostLevels, maxEngine);
            if (suspensionCostText != null) suspensionCostText.text = (maxSus >= limitSus) ? "MAX" : "Cost: " + GetSafeCost(activeCar.suspensionCostLevels, maxSus);
            if (tiresCostText != null) tiresCostText.text = (maxTire >= limitTire) ? "MAX" : "Cost: " + GetSafeCost(activeCar.tireCostLevels, maxTire);
            if (fuelCostText != null) fuelCostText.text = (maxFuel >= limitFuel) ? "MAX" : "Cost: " + GetSafeCost(activeCar.fuelCostLevels, maxFuel);
        }

        if (nitroAmountText != null) nitroAmountText.text = "Owned: " + nitroAmount;
        if (nitroCostText != null) nitroCostText.text = nitroCost.ToString();
    }

    public void StartGame()
    {
        // Change this from 1 to 2, because GameScene is now Scene index 2!
        SceneManager.LoadScene(2);
    }

    // --- NEW: Go Back to Main Menu ---
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(0); 
    }
}