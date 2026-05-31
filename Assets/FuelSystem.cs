using UnityEngine;
using UnityEngine.UI; // We need this to talk to the UI Canvas!

public class FuelSystem : MonoBehaviour
{
    [Header("Fuel Settings")]
    public float maxFuel = 100f;
    public float currentFuel;
    public float fuelDrainRate = 5f; // How much fuel lost per second

    [Header("UI")]
    public Slider fuelSlider; // We will drag our on-screen fuel bar into this slot

    void Start()
    {
        currentFuel = maxFuel;
        
        // Setup the slider max value when the game starts
        if (fuelSlider != null)
        {
            fuelSlider.maxValue = maxFuel;
            fuelSlider.value = currentFuel;
        }
    }

    void Update()
    {
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
        Debug.Log("Fuel Refilled! -> (Mellstroy Happy Meme plays here soon)");
    }

    void OutOfFuel()
    {
        Debug.Log("Out of Fuel! Game Over! -> (Mellstroy Angry Meme plays here soon)");
        
        // Eventually, we will disable the CarController script here so the car stops driving.
    }
}