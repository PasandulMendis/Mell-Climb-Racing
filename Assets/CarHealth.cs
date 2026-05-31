using UnityEngine;

public class CarHealth : MonoBehaviour
{
    private bool isDead = false;

    // We will attach this script to a tiny collider on the ROOF of the car
    void OnTriggerEnter2D(Collider2D other)
    {
        // If the roof hits the Ground (which is usually on the "Default" layer or a custom "Ground" layer)
        // and we aren't already dead...
        if (!isDead && !other.CompareTag("Player") && !other.CompareTag("FuelCan")) 
        {
            // Specifically checking if we hit something that isn't a trigger (like a coin)
            if (!other.isTrigger)
            {
                Die("Neck Snapped!");
            }
        }
    }

    public void Die(string reason)
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Game Over: " + reason);

        // Tell the Game Manager the game is over!
        GameManager.instance.GameOver();

        // Optional: Disable driving when dead
        CarController controller = GetComponentInParent<CarController>();
        if (controller != null)
        {
            controller.enabled = false;
        }
    }
}