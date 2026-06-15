using UnityEngine;

public class StuntManager : MonoBehaviour
{
    public static StuntManager instance;

    [Header("Air Time Rewards")]
    [Tooltip("Minimum seconds in the air to trigger a reward")]
    public float minimumAirTime = 1.5f;
    [Tooltip("Coins given per second in the air")]
    public int coinsPerAirSecond = 50;

    [Header("Flip Rewards")]
    public int baseFlipReward = 100;
    [Tooltip("Multiplier for chaining flips (e.g. Double Flip = 100 * 2 = 200 coins)")]
    public float multiFlipMultiplier = 1.5f;

    [Header("UI References")]
    [Tooltip("The prefab with the FloatingText script and a TextMeshPro component")]
    public GameObject floatingTextPrefab;
    public Vector3 textSpawnOffset = new Vector3(0f, 2f, 0f);

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    // Called automatically by the CarController when the tires touch the ground!
    public void ProcessAirTime(float airTime, Vector3 carPosition)
    {
        if (airTime >= minimumAirTime)
        {
            // Calculate reward: e.g., 2.5s * 50 = 125 coins
            int reward = Mathf.FloorToInt(airTime * coinsPerAirSecond);
            
            if (GameManager.instance != null) GameManager.instance.AddCoins(reward);
            
            SpawnFloatingText($"AIR TIME {airTime:F1}s\n+{reward}", carPosition, Color.cyan);
            
            if (MemeManager.instance != null) MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Happy);
        }
    }

    // Called automatically by the CarController mid-air the exact moment a 360 rotation completes!
    public void ProcessFlip(string flipType, int flipsInOneJump, Vector3 carPosition)
    {
        // Reward scales up for Double, Triple flips!
        int reward = Mathf.FloorToInt(baseFlipReward * flipsInOneJump * (flipsInOneJump > 1 ? multiFlipMultiplier : 1f));
        
        if (GameManager.instance != null) GameManager.instance.AddCoins(reward);

        string prefix = flipsInOneJump > 1 ? flipsInOneJump + "x " : "";
        Color flipColor = flipsInOneJump > 1 ? Color.magenta : Color.yellow; // Special color for combos!

        SpawnFloatingText($"{prefix}{flipType.ToUpper()}!\n+{reward}", carPosition, flipColor);

        if (MemeManager.instance != null) MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Happy);
    }

    private void SpawnFloatingText(string text, Vector3 position, Color color)
    {
        if (floatingTextPrefab != null)
        {
            GameObject go = Instantiate(floatingTextPrefab, position + textSpawnOffset, Quaternion.identity);
            FloatingText ft = go.GetComponent<FloatingText>();
            if (ft != null)
            {
                ft.Setup(text, color);
            }
        }
        else
        {
            Debug.LogWarning("StuntManager: Missing Floating Text Prefab!");
        }
    }
}