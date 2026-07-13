using UnityEngine;

public class FuelSystem : MonoBehaviour
{
    [Header("Fuel Settings")]
    public float currentFuel;
    [Tooltip("How fast fuel drains per second")]
    public float fuelDrainRate = 5f;

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
        
        isOutOfFuel = false;
        Debug.Log("Fuel Refilled!");

        if (MemeManager.instance != null)
        {
            MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Happy);
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
    }

    void OutOfFuelDeath()
    {
        if (GameManager.instance != null && GameManager.instance.isGameOver) return;
        
        Debug.Log("Game Over: Out of Fuel and Stopped!");
        
        if (MemeManager.instance != null) 
        {
            MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Angry);
        }

        if (GameManager.instance != null)
        {
            // NEW: Pass the reason!
            GameManager.instance.GameOver("OUT OF GAS!");
        }
    }
}