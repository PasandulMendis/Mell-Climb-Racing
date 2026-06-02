using UnityEngine;
using UnityEngine.UI; // We need this to talk to the UI Canvas!

public class FuelSystem : MonoBehaviour
{
    [Header("Fuel Settings")]
    public float maxFuel = 100f;
    public float currentFuel;
    public float fuelDrainRate = 5f; // How much fuel lost per second

    private bool isOutOfFuel = false;
    private Rigidbody2D carRb;

    [Header("UI")]
    public Slider fuelSlider; // We will drag our on-screen fuel bar into this slot

    void Start()
    {
        // --- NEW: Find the UI Slider automatically since this is a Prefab! ---
        if (fuelSlider == null)
        {
            GameObject uiBar = GameObject.Find("FuelBar");
            if (uiBar != null)
            {
                fuelSlider = uiBar.GetComponent<Slider>();
            }
        }

        // --- NEW: Read Fuel Upgrade for THIS SPECIFIC CAR ---
        int carID = PlayerPrefs.GetInt("SelectedCar", 0);
        int maxFuelLevel = PlayerPrefs.GetInt("Car_" + carID + "_FuelLevel", 1);
        int fuelLevel = PlayerPrefs.GetInt("Car_" + carID + "_EquippedFuelLevel", maxFuelLevel);
        
        maxFuel = 100f + ((fuelLevel - 1) * 20f);

        currentFuel = maxFuel;
        carRb = GetComponent<Rigidbody2D>();
        
        // Setup the slider max value when the game starts
        if (fuelSlider != null)
        {
            fuelSlider.maxValue = maxFuel;
            fuelSlider.value = currentFuel;
        }
    }

    void Update()
    {
        // If out of fuel, wait for the car to stop moving before triggering Game Over
        if (isOutOfFuel)
        {
            if (carRb != null && carRb.linearVelocity.magnitude <= 0.5f)
            {
                OutOfFuelDeath();
            }
            return; // Stop draining fuel
        }

        // Drain fuel over time
        if (currentFuel > 0)
        {
            currentFuel -= fuelDrainRate * Time.deltaTime;
            
            // Update the UI Slider every frame
            if (fuelSlider != null)
            {
                fuelSlider.value = currentFuel;
            }
        }
        else
        {
            currentFuel = 0;
            if (fuelSlider != null)
            {
                fuelSlider.value = 0;
            }
            OutOfFuel();
        }
    }

    // This is called when the car hits a trigger collider (like a pickup)
    void OnTriggerEnter2D(Collider2D other)
    {
        // Make sure the object we hit has the Tag "FuelCan"
        if (other.CompareTag("FuelCan"))
        {
            RefillFuel();
            
            // Destroy the fuel can so we can't pick it up twice
            Destroy(other.gameObject); 
        }
    }

    void RefillFuel()
    {
        currentFuel = maxFuel;
        Debug.Log("Fuel Refilled!");
        
        // TRIGGER HAPPY MEME
        if (MemeManager.instance != null)
        {
            MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Happy);
        }
    }

    void OutOfFuel()
    {
        Debug.Log("Fuel Empty! Coasting to a stop...");
        isOutOfFuel = true;
        
        // Disable driving input so the player can't accelerate anymore
        CarController controller = GetComponent<CarController>();
        if (controller != null)
        {
            controller.enabled = false;
            controller.backWheel.useMotor = false;
            controller.frontWheel.useMotor = false;
        }
    }

    void OutOfFuelDeath()
    {
        Debug.Log("Car stopped. Game Over! -> (Mellstroy Angry Meme)");
        GetComponentInChildren<CarHealth>().Die("Out of Gas!");
        this.enabled = false; // Stop checking
    }
}