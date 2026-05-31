using UnityEngine;

public class Coin : MonoBehaviour
{
    public int coinValue = 5; // How much this specific coin is worth

    void OnTriggerEnter2D(Collider2D other)
    {
        // We only want the Player (Car) to collect the coin
        if (other.CompareTag("Player")) 
        {
            // Give the coins to the GameManager
            GameManager.instance.AddCoins(coinValue);
            
            // Destroy the coin so it vanishes from the world
            Destroy(gameObject);
        }
    }
}