using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RadiationPool : MonoBehaviour
{
    [Header("Toxicity Settings")]
    [Tooltip("How many seconds the car can stay in the sludge before melting")]
    public float timeToMelt = 3.0f;

    private float exposureTimer = 0f;

    void OnTriggerStay2D(Collider2D col)
    {
        // Check if the physical object touching the sludge belongs to the car
        CarController car = col.GetComponentInParent<CarController>();
        if (car != null)
        {
            // Increase the timer using FixedDeltaTime (since this is a physics callback)
            exposureTimer += Time.fixedDeltaTime;

            // Trigger the Scared Meme (but don't force override so it can loop naturally)
            if (MemeManager.instance != null) 
            {
                MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Scared, false, false);
            }

            // Has the toxicity meter filled up?
            if (exposureTimer >= timeToMelt)
            {
                if (GameManager.instance != null && !GameManager.instance.isGameOver)
                {
                    // Game Over with our custom Cyberpunk death reason!
                    GameManager.instance.GameOver("DRIVER MELTED!");

                    // Switch to the Angry meme permanently for the Game Over screen
                    if (MemeManager.instance != null) 
                    {
                        MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Angry, true, true);
                    }
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        CarController car = col.GetComponentInParent<CarController>();
        if (car != null)
        {
            // If they manage to drive out in time, instantly flush the toxicity meter!
            exposureTimer = 0f;
        }
    }
}