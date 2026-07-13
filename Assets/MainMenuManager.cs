using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [System.Serializable]
    public class StoreItem
    {
        public string itemName;
        public int price;
        public TextMeshProUGUI buttonText; // The text on the Buy/Select button
        [HideInInspector] public bool isUnlocked;
    }

    [Header("UI - Currency")]
    public TextMeshProUGUI totalCoinsText;

    [Header("Scroll Views")]
    public GameObject carsScrollView;
    public GameObject mapsScrollView;

    [Header("Store Items")]
    public StoreItem[] cars;
    public StoreItem[] maps;

    private int totalCoins;
    private int selectedCarIndex;
    private int selectedMapIndex;

    void Start()
    {
        // Unlock the first car and map by default!
        PlayerPrefs.SetInt("CarUnlocked_0", 1); 
        PlayerPrefs.SetInt("MapUnlocked_0", 1);

        LoadData();
        ShowCarsTab(); // Default to cars tab
    }

    void LoadData()
    {
        totalCoins = PlayerPrefs.GetInt("SavedCoins", 0);
        selectedCarIndex = PlayerPrefs.GetInt("SelectedCar", 0);
        selectedMapIndex = PlayerPrefs.GetInt("SelectedMap", 0);

        if (totalCoinsText != null) 
            totalCoinsText.text = totalCoins.ToString();

        // Load unlock status for Cars
        for (int i = 0; i < cars.Length; i++)
        {
            cars[i].isUnlocked = PlayerPrefs.GetInt("CarUnlocked_" + i, 0) == 1;
        }

        // Load unlock status for Maps
        for (int i = 0; i < maps.Length; i++)
        {
            maps[i].isUnlocked = PlayerPrefs.GetInt("MapUnlocked_" + i, 0) == 1;
        }

        UpdateAllUI();
    }

    // --- TAB SWITCHING ---
    public void ShowCarsTab()
    {
        carsScrollView.SetActive(true);
        mapsScrollView.SetActive(false);
    }

    public void ShowMapsTab()
    {
        carsScrollView.SetActive(false);
        mapsScrollView.SetActive(true);
    }

    // --- BUTTON CLICK LOGIC ---
    public void OnCarButtonClicked(int index)
    {
        if (cars[index].isUnlocked)
        {
            selectedCarIndex = index;
            PlayerPrefs.SetInt("SelectedCar", selectedCarIndex);
        }
        else if (totalCoins >= cars[index].price)
        {
            totalCoins -= cars[index].price;
            PlayerPrefs.SetInt("SavedCoins", totalCoins);
            cars[index].isUnlocked = true;
            PlayerPrefs.SetInt("CarUnlocked_" + index, 1);
            
            // Auto-select after buying
            selectedCarIndex = index;
            PlayerPrefs.SetInt("SelectedCar", selectedCarIndex);
        }
        
        PlayerPrefs.Save();
        LoadData(); // Refresh UI
    }

    public void OnMapButtonClicked(int index)
    {
        if (maps[index].isUnlocked)
        {
            selectedMapIndex = index;
            PlayerPrefs.SetInt("SelectedMap", selectedMapIndex);
        }
        else if (totalCoins >= maps[index].price)
        {
            totalCoins -= maps[index].price;
            PlayerPrefs.SetInt("SavedCoins", totalCoins);
            maps[index].isUnlocked = true;
            PlayerPrefs.SetInt("MapUnlocked_" + index, 1);
            
            selectedMapIndex = index;
            PlayerPrefs.SetInt("SelectedMap", selectedMapIndex);
        }

        PlayerPrefs.Save();
        LoadData();
    }

    void UpdateAllUI()
    {
        // Update Car Buttons
        for (int i = 0; i < cars.Length; i++)
        {
            if (cars[i].buttonText != null)
            {
                if (selectedCarIndex == i) cars[i].buttonText.text = "SELECTED";
                else if (cars[i].isUnlocked) cars[i].buttonText.text = "SELECT";
                else cars[i].buttonText.text = "Buy: " + cars[i].price;
            }
            else
            {
                // ADDED HELPFUL DEBUGGER: This will tell you exactly which slot is missing!
                Debug.LogWarning("UI ERROR: Car at Index [" + i + "] is missing its 'Button Text' reference in the MainMenuManager!");
            }
        }

        // Update Map Buttons
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i].buttonText != null)
            {
                if (selectedMapIndex == i) maps[i].buttonText.text = "SELECTED";
                else if (maps[i].isUnlocked) maps[i].buttonText.text = "SELECT";
                else maps[i].buttonText.text = "Buy: " + maps[i].price;
            }
            else
            {
                Debug.LogWarning("UI ERROR: Map at Index [" + i + "] is missing its 'Button Text' reference in the MainMenuManager!");
            }
        }
    }

    // --- LOAD NEXT SCENE ---
    public void GoToGarage()
    {
        if (SceneTransitionManager.instance != null) 
            SceneTransitionManager.instance.LoadScene(1);
        else 
            SceneManager.LoadScene(1); // Failsafe
    }
}