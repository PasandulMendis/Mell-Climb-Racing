using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [System.Serializable]
    public class StoreItem
    {
        public string itemName;
        public int price;
        
        [Header("Card UI Elements")]
        public TextMeshProUGUI statusText; // The dynamic text (Equipped, Owned, Locked)
        public Image statusIcon;           // The small dynamic icon next to the text
        public GameObject lockedOverlay;   // The black transparent background overlay
        public RectTransform lockIconTransform; // Dedicated Lock Icon Transform (shakes when coins insufficient)
        
        [Header("Power / Upgrade Level UI (Cars Only)")]
        public GameObject powerContainer;  // Parent object containing the power bar (hidden when locked)
        public Image powerFillImage;       // UI Image set to Image Type: Filled (Horizontal)
        public TextMeshProUGUI powerText;   // TMP displaying "LV 24/40"

        [Header("Best Record UI (Maps Only)")]
        public GameObject bestDistanceContainer; // Parent object containing best distance text (hidden when locked)
        public TextMeshProUGUI bestDistanceText; // TMP displaying "BEST: 1250m"
        
        [Header("Interactive Buttons")]
        public Button buyButton;           // Dedicated Buy button placed on top of overlay
        public Button selectButton;        // Dedicated Select/Card button active when vehicle is unlocked
        
        [HideInInspector] public bool isUnlocked;
        [HideInInspector] public Vector2 originalLockPos;
        [HideInInspector] public Coroutine shakeCoroutine;
    }

    [Header("Car Data Integration")]
    [Tooltip("Drag all your Car Prefabs here in the same order as the cars store array to calculate dynamic max levels.")]
    public CarController[] carPrefabs;

    [Header("UI - Currency & Status")]
    public TextMeshProUGUI totalCoinsText;

    [Header("UI - Unlock Counters")]
    public TextMeshProUGUI carsUnlockedText; // Place this TMP as a child under carsScrollView
    public TextMeshProUGUI mapsUnlockedText; // Place this TMP as a child under mapsScrollView

    [Header("Scroll Views")]
    public GameObject carsScrollView;
    public GameObject mapsScrollView;

    [Header("Store Items")]
    public StoreItem[] cars;
    public StoreItem[] maps;

    [Header("Status Styling (Colors & Sprites)")]
    public Color equippedColor = new Color(0.4f, 0.8f, 0.4f); // Green-ish default
    public Color ownedColor = Color.white;
    public Color lockedColor = new Color(0.8f, 0.2f, 0.2f);   // Red-ish default

    public Sprite equippedSprite;
    public Sprite ownedSprite;
    public Sprite lockedSprite;

    private int totalCoins;
    private int selectedCarIndex;
    private int selectedMapIndex;

    void Start()
    {
        Application.targetFrameRate = 120;
        
        // Unlock the first car and map by default!
        PlayerPrefs.SetInt("CarUnlocked_0", 1); 
        PlayerPrefs.SetInt("MapUnlocked_0", 1);

        SetupButtonListeners();
        LoadData();
        ShowCarsTab(); // Default to cars tab
    }

    private void SetupButtonListeners()
    {
        // Setup Cars button listeners and cache lock positions
        for (int i = 0; i < cars.Length; i++)
        {
            int index = i;
            if (cars[i].buyButton != null)
            {
                cars[i].buyButton.onClick.RemoveAllListeners();
                cars[i].buyButton.onClick.AddListener(() => OnBuyCarButtonClicked(index));
            }
            if (cars[i].selectButton != null)
            {
                cars[i].selectButton.onClick.RemoveAllListeners();
                cars[i].selectButton.onClick.AddListener(() => OnCarButtonClicked(index));
            }
            if (cars[i].lockIconTransform != null)
            {
                cars[i].originalLockPos = cars[i].lockIconTransform.anchoredPosition;
            }
        }

        // Setup Maps button listeners and cache lock positions
        for (int i = 0; i < maps.Length; i++)
        {
            int index = i;
            if (maps[i].buyButton != null)
            {
                maps[i].buyButton.onClick.RemoveAllListeners();
                maps[i].buyButton.onClick.AddListener(() => OnBuyMapButtonClicked(index));
            }
            if (maps[i].selectButton != null)
            {
                maps[i].selectButton.onClick.RemoveAllListeners();
                maps[i].selectButton.onClick.AddListener(() => OnMapButtonClicked(index));
            }
            if (maps[i].lockIconTransform != null)
            {
                maps[i].originalLockPos = maps[i].lockIconTransform.anchoredPosition;
            }
        }
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

    public void ShowCarsTab()
    {
        if (carsScrollView != null) carsScrollView.SetActive(true);
        if (mapsScrollView != null) mapsScrollView.SetActive(false);
    }

    public void ShowMapsTab()
    {
        if (carsScrollView != null) carsScrollView.SetActive(false);
        if (mapsScrollView != null) mapsScrollView.SetActive(true);
    }

    public void OnBuyCarButtonClicked(int index)
    {
        if (index < 0 || index >= cars.Length) return;
        StoreItem car = cars[index];

        if (!car.isUnlocked)
        {
            if (totalCoins >= car.price)
            {
                totalCoins -= car.price;
                PlayerPrefs.SetInt("SavedCoins", totalCoins);
                car.isUnlocked = true;
                PlayerPrefs.SetInt("CarUnlocked_" + index, 1);
                
                // Auto-select after buying
                selectedCarIndex = index;
                PlayerPrefs.SetInt("SelectedCar", selectedCarIndex);
                
                PlayerPrefs.Save();
                LoadData(); // Refresh UI
            }
            else
            {
                // Insufficient coins! Trigger decline shake animation on lock icon
                if (car.shakeCoroutine != null)
                    StopCoroutine(car.shakeCoroutine);
                
                car.shakeCoroutine = StartCoroutine(ShakeLockIconCoroutine(car));
            }
        }
    }

    public void OnCarButtonClicked(int index)
    {
        if (index < 0 || index >= cars.Length) return;

        if (cars[index].isUnlocked)
        {
            selectedCarIndex = index;
            PlayerPrefs.SetInt("SelectedCar", selectedCarIndex);
            PlayerPrefs.Save();
            LoadData(); // Refresh UI
        }
    }

    public void OnBuyMapButtonClicked(int index)
    {
        if (index < 0 || index >= maps.Length) return;
        StoreItem map = maps[index];

        if (!map.isUnlocked)
        {
            if (totalCoins >= map.price)
            {
                totalCoins -= map.price;
                PlayerPrefs.SetInt("SavedCoins", totalCoins);
                map.isUnlocked = true;
                PlayerPrefs.SetInt("MapUnlocked_" + index, 1);
                
                selectedMapIndex = index;
                PlayerPrefs.SetInt("SelectedMap", selectedMapIndex);
                
                PlayerPrefs.Save();
                LoadData();
            }
            else
            {
                // Insufficient coins! Trigger decline shake animation on lock icon
                if (map.shakeCoroutine != null)
                    StopCoroutine(map.shakeCoroutine);
                
                map.shakeCoroutine = StartCoroutine(ShakeLockIconCoroutine(map));
            }
        }
    }

    public void OnMapButtonClicked(int index)
    {
        if (index < 0 || index >= maps.Length) return;

        if (maps[index].isUnlocked)
        {
            selectedMapIndex = index;
            PlayerPrefs.SetInt("SelectedMap", selectedMapIndex);
            PlayerPrefs.Save();
            LoadData();
        }
    }

    private IEnumerator ShakeLockIconCoroutine(StoreItem item)
    {
        if (item.lockIconTransform == null) yield break;

        // Ensure original position is captured if not already set
        if (item.originalLockPos == Vector2.zero)
        {
            item.originalLockPos = item.lockIconTransform.anchoredPosition;
        }

        RectTransform target = item.lockIconTransform;
        Vector2 startPos = item.originalLockPos;

        float duration = 0.35f;
        float elapsed = 0f;
        float magnitude = 14f; // Horizontal pixel shake intensity

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percentComplete = elapsed / duration;
            // Smoothly decrease vibration towards the end
            float damper = 1.0f - Mathf.Clamp01(4.0f * percentComplete - 3.0f);

            // Fast horizontal oscillation with subtle vertical wiggle
            float x = Mathf.Sin(elapsed * 55f) * magnitude * damper;
            float y = Mathf.Cos(elapsed * 45f) * (magnitude * 0.25f) * damper;

            target.anchoredPosition = startPos + new Vector2(x, y);
            yield return null;
        }

        // Return precisely to original position
        target.anchoredPosition = startPos;
        item.shakeCoroutine = null;
    }

    void UpdateAllUI()
    {
        // Update Car Buttons/Cards
        for (int i = 0; i < cars.Length; i++)
        {
            UpdateCardVisuals(cars[i], selectedCarIndex == i);
            UpdateCarPowerVisuals(i, cars[i]);
        }

        // Update Map Buttons/Cards
        for (int i = 0; i < maps.Length; i++)
        {
            UpdateCardVisuals(maps[i], selectedMapIndex == i);
            UpdateMapDistanceVisuals(i, maps[i]);
        }

        // Update Unlocked Counters Text
        UpdateUnlockCounters();
    }

    private void UpdateCarPowerVisuals(int carIndex, StoreItem item)
    {
        if (item.powerFillImage == null && item.powerText == null && item.powerContainer == null)
            return;

        // Toggle container visibility based on unlock status
        if (item.powerContainer != null)
        {
            item.powerContainer.SetActive(item.isUnlocked);
        }

        int totalMaxLimit = 40; // Fallback if prefabs are not assigned

        // Calculate maximum available levels dynamically from CarController arrays
        if (carPrefabs != null && carIndex >= 0 && carIndex < carPrefabs.Length && carPrefabs[carIndex] != null)
        {
            CarController carPrefab = carPrefabs[carIndex];
            int limitEngine = (carPrefab.engineSpeedLevels != null && carPrefab.engineSpeedLevels.Length > 0) ? carPrefab.engineSpeedLevels.Length : 10;
            int limitSus = (carPrefab.suspensionFrequencyLevels != null && carPrefab.suspensionFrequencyLevels.Length > 0) ? carPrefab.suspensionFrequencyLevels.Length : 10;
            int limitTire = (carPrefab.tireFrictionLevels != null && carPrefab.tireFrictionLevels.Length > 0) ? carPrefab.tireFrictionLevels.Length : 10;
            int limitFuel = (carPrefab.fuelCapacityLevels != null && carPrefab.fuelCapacityLevels.Length > 0) ? carPrefab.fuelCapacityLevels.Length : 10;

            totalMaxLimit = limitEngine + limitSus + limitTire + limitFuel;
        }

        // Fetch saved upgrade levels matching GarageManager keys
        int maxEngine = PlayerPrefs.GetInt("Car_" + carIndex + "_EngineLevel", 1);
        int maxSus = PlayerPrefs.GetInt("Car_" + carIndex + "_SuspensionLevel", 1);
        int maxTire = PlayerPrefs.GetInt("Car_" + carIndex + "_TireLevel", 1);
        int maxFuel = PlayerPrefs.GetInt("Car_" + carIndex + "_FuelLevel", 1);

        int eqEngine = PlayerPrefs.GetInt("Car_" + carIndex + "_EquippedEngineLevel", maxEngine);
        int eqSus = PlayerPrefs.GetInt("Car_" + carIndex + "_EquippedSuspensionLevel", maxSus);
        int eqTire = PlayerPrefs.GetInt("Car_" + carIndex + "_EquippedTireLevel", maxTire);
        int eqFuel = PlayerPrefs.GetInt("Car_" + carIndex + "_EquippedFuelLevel", maxFuel);

        int currentPowerLevel = eqEngine + eqSus + eqTire + eqFuel;

        // Fill bar using Image.fillAmount (Prevents stretching!)
        if (item.powerFillImage != null)
        {
            item.powerFillImage.type = Image.Type.Filled;
            item.powerFillImage.fillMethod = Image.FillMethod.Horizontal;
            item.powerFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            item.powerFillImage.fillAmount = Mathf.Clamp01((float)currentPowerLevel / totalMaxLimit);
        }

        // Format level text (e.g., LV 24/40 or LV 8/10)
        if (item.powerText != null)
        {
            item.powerText.text = $"LV {currentPowerLevel}/{totalMaxLimit}";
        }
    }

    private void UpdateMapDistanceVisuals(int mapIndex, StoreItem item)
    {
        if (item.bestDistanceText == null && item.bestDistanceContainer == null)
            return;

        // Toggle container visibility based on unlock status
        if (item.bestDistanceContainer != null)
        {
            item.bestDistanceContainer.SetActive(item.isUnlocked);
        }

        // Fetch highest distance recorded for this map from GameManager's save key
        int bestDistance = PlayerPrefs.GetInt("HighestDistance_Map_" + mapIndex, 0);

        // Format best distance text (e.g., BEST: 1250m)
        if (item.bestDistanceText != null)
        {
            item.bestDistanceText.text = $"{bestDistance}M";
        }
    }

    private void UpdateUnlockCounters()
    {
        // Calculate unlocked cars count
        if (carsUnlockedText != null)
        {
            int unlockedCarsCount = 0;
            for (int i = 0; i < cars.Length; i++)
            {
                if (cars[i].isUnlocked) unlockedCarsCount++;
            }
            carsUnlockedText.text = $"UNLOCKED: {unlockedCarsCount} / {cars.Length}";
        }

        // Calculate unlocked maps count
        if (mapsUnlockedText != null)
        {
            int unlockedMapsCount = 0;
            for (int i = 0; i < maps.Length; i++)
            {
                if (maps[i].isUnlocked) unlockedMapsCount++;
            }
            mapsUnlockedText.text = $"UNLOCKED: {unlockedMapsCount} / {maps.Length}";
        }
    }

    private void UpdateCardVisuals(StoreItem item, bool isEquipped)
    {
        // 1. Handle Overlay
        if (item.lockedOverlay != null)
        {
            item.lockedOverlay.SetActive(!item.isUnlocked);
        }

        // 2. Handle Buy Button
        if (item.buyButton != null)
        {
            if (item.selectButton == null || item.buyButton.gameObject != item.selectButton.gameObject)
            {
                item.buyButton.gameObject.SetActive(!item.isUnlocked);
            }
            else
            {
                item.buyButton.enabled = !item.isUnlocked;
            }
        }

        // 3. Handle Select Button
        if (item.selectButton != null)
        {
            item.selectButton.enabled = item.isUnlocked;
        }

        // 4. Handle Pop Animation
        TogglePopAnimation(item);

        // 5. Handle Dynamic Text & Icons
        if (item.statusText != null && item.statusIcon != null)
        {
            if (isEquipped)
            {
                item.statusText.text = "EQUIPPED";
                item.statusText.color = equippedColor;
                if (equippedSprite != null) item.statusIcon.sprite = equippedSprite;
            }
            else if (item.isUnlocked)
            {
                item.statusText.text = "OWNED";
                item.statusText.color = ownedColor;
                if (ownedSprite != null) item.statusIcon.sprite = ownedSprite;
            }
            else
            {
                item.statusText.text = "LOCKED";
                item.statusText.color = lockedColor;
                if (lockedSprite != null) item.statusIcon.sprite = lockedSprite;
            }
        }
    }

    private void TogglePopAnimation(StoreItem item)
    {
        if (item.selectButton != null)
        {
            UIButtonPop[] pops = item.selectButton.GetComponents<UIButtonPop>();
            foreach (UIButtonPop pop in pops)
            {
                if (pop != null)
                {
                    pop.enabled = item.isUnlocked;
                }
            }
        }

        if (item.buyButton != null && (item.selectButton == null || item.buyButton.gameObject != item.selectButton.gameObject))
        {
            UIButtonPop buyPop = item.buyButton.GetComponent<UIButtonPop>();
            if (buyPop != null)
            {
                buyPop.enabled = !item.isUnlocked;
            }
        }
    }

    public void GoToGarage()
    {
        if (SceneTransitionManager.instance != null) 
            SceneTransitionManager.instance.LoadScene(1);
        else 
            SceneManager.LoadScene(1); // Failsafe
    }
}