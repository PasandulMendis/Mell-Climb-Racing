using UnityEngine;

public class FuelSystem : MonoBehaviour
{
    [Header("Fuel Settings")]
    public float currentFuel;
    [Tooltip("How fast fuel drains per second")]
    public float fuelDrainRate = 5f;

    [Header("Audio")]
    public AudioClip refuelSound;

    [Header("Custom Meme Integration")]
    [Tooltip("Played when the fuel hits 0 and you start coasting")]
    public string memeCategoryEmpty = "Fuel_Empty";
    [Tooltip("Played if you manage to grab a fuel can WHILE coasting on empty!")]
    public string memeCategoryClutchRefuel = "Clutch_Refuel";
    [Tooltip("Played on a normal fuel refill")]
    public string memeCategoryStandardRefuel = "Fuel_Refill_Standard";
    [Tooltip("Played when the car finally stops rolling and the game ends")]
    public string memeCategoryGameOver = "Game_Over_Fuel";

    [HideInInspector] public bool isOutOfFuel = false;

    private CarController carController;

    void Start()
    {
        // Find the CarController script on this car
        carController = GetComponent<CarController>();
        
        // Set initial fuel to the max fuel defined by our Garage Upgrades!
        if (carController != null)
        {
            currentFuel = carController.maxFuel;
        }
    }

    void Update()
    {
        // Stop draining if the game is already over
        if (GameManager.instance != null && GameManager.instance.isGameOver) return;
        if (carController == null) return;

        if (!isOutOfFuel)
        {
            // 1. Drain fuel over time
            currentFuel -= fuelDrainRate * Time.deltaTime;

            // 2. Clamp fuel between 0 and our upgraded maxFuel
            currentFuel = Mathf.Clamp(currentFuel, 0f, carController.maxFuel);

            // 3. Check for empty tank
            if (currentFuel <= 0)
            {
                OutOfFuel();
            }
        }
        else
        {
            // --- NEW: DEATH BY COASTING ---
            // If we are out of fuel, wait until the car completely stops rolling!
            if (carController.carRigidbody.linearVelocity.magnitude < 0.1f)
            {
                OutOfFuelDeath();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("FuelCan"))
        {
            RefillFuel();
            
            // Destroy the fuel can so it disappears
            Destroy(other.gameObject);
        }
    }

    public void RefillFuel()
    {
        if (carController != null)
        {
            currentFuel = carController.maxFuel;
            carController.hasFuel = true; // Wake the engine back up!
        }
        
        // --- NEW: CLUTCH REFUEL LOGIC ---
        if (isOutOfFuel)
        {
            // We were completely empty and coasting, but we made it!
            if (MemeManager.instance != null && !string.IsNullOrEmpty(memeCategoryClutchRefuel))
            {
                MemeManager.instance.TriggerReaction(memeCategoryClutchRefuel);
            }
        }
        else
        {
            // Just a normal top-up
            if (MemeManager.instance != null && !string.IsNullOrEmpty(memeCategoryStandardRefuel))
            {
                MemeManager.instance.TriggerReaction(memeCategoryStandardRefuel);
            }
        }

        isOutOfFuel = false;

        // Play the satisfying glug-glug refuel sound!
        if (AudioManager.instance != null && refuelSound != null)
        {
            AudioManager.instance.PlaySFX(refuelSound, 1f);
        }
    }

    void OutOfFuel()
    {
        if (isOutOfFuel) return;
        
        Debug.Log("Fuel Empty! Coasting to a stop...");
        isOutOfFuel = true;
        
        // Turn off the engine so we start coasting and applying friction
        if (carController != null)
        {
            carController.hasFuel = false;
        }

        // --- NEW: START PANICKING! ---
        if (MemeManager.instance != null && !string.IsNullOrEmpty(memeCategoryEmpty))
        {
            MemeManager.instance.TriggerReaction(memeCategoryEmpty);
        }
    }

    void OutOfFuelDeath()
    {
        if (GameManager.instance != null && GameManager.instance.isGameOver) return;

        // --- NEW: GAME OVER MEME ---
        if (MemeManager.instance != null && !string.IsNullOrEmpty(memeCategoryGameOver))
        {
            // Pass 'true' to signify this is a permanent game over state!
            MemeManager.instance.TriggerReaction(memeCategoryGameOver, true);
        }

        if (GameManager.instance != null)
        {
            // NEW: Pass the reason!
            GameManager.instance.GameOver("OUT OF FUEL!");
        }
    }
}