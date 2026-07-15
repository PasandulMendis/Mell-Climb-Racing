using UnityEngine;

public class Scarecrow : MonoBehaviour
{
    [Header("Hat Setup")]
    [Tooltip("Drag the Hat child object here")]
    public Transform hatTransform;
    
    [Header("Interaction Settings")]
    [Tooltip("How fast the car must be going to blow the hat off")]
    public float requiredSpeed = 15f;
    [Tooltip("How close the car needs to be to trigger the wind")]
    public float triggerDistance = 8f;

    [Header("Wind Physics (Hat)")]
    public float upwardSpeed = 4f;
    [Tooltip("How wide the hat sways left and right")]
    public float windSwayAmount = 3f;
    [Tooltip("How fast the hat sways")]
    public float windSwaySpeed = 2f;

    private bool isHatBlownOff = false;
    private Transform playerCar;
    private Rigidbody2D carRb;
    
    // Variables for the erratic wind math
    private float windTime = 0f;
    private float startX;
    private float randomWindOffset;

    void Start()
    {
        // Randomize the wind pattern so it doesn't look identical every time
        randomWindOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (!isHatBlownOff)
        {
            if (playerCar == null || carRb == null)
            {
                if (GameManager.instance != null && GameManager.instance.playerCar != null)
                {
                    playerCar = GameManager.instance.playerCar;
                    carRb = playerCar.GetComponent<Rigidbody2D>();
                }
                return;
            }

            // Check if the car is close enough
            if (Vector2.Distance(transform.position, playerCar.position) < triggerDistance)
            {
                // Check if the car is going fast enough!
                if (carRb.linearVelocity.magnitude >= requiredSpeed)
                {
                    BlowHatOff();
                }
            }
        }
        else
        {
            if (hatTransform != null)
            {
                windTime += Time.deltaTime * windSwaySpeed;
                
                // Move UP constantly
                float newY = hatTransform.position.y + (upwardSpeed * Time.deltaTime);
                
                // Sway LEFT and RIGHT using Perlin Noise for erratic, unpredictable wind
                float noise = Mathf.PerlinNoise(windTime, randomWindOffset) - 0.5f; // Results in -0.5 to 0.5
                float newX = startX + (noise * windSwayAmount);

                hatTransform.position = new Vector3(newX, newY, hatTransform.position.z);
                
                // Add a little bit of random rotation to make it look like it's tumbling
                hatTransform.Rotate(0f, 0f, noise * 200f * Time.deltaTime);
            }
        }
    }

    void BlowHatOff()
    {
        isHatBlownOff = true;
        
        if (hatTransform != null)
        {
            startX = hatTransform.position.x;
            // Unparent the hat so it moves independently of the scarecrow body
            hatTransform.SetParent(null);
            
            // NEW: Destroy the hat after 10 seconds so it doesn't clutter the hierarchy forever!
            Destroy(hatTransform.gameObject, 10f);
        }
        
        // Optional: Trigger a happy meme for doing something cool!
        if (MemeManager.instance != null) MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Happy);
    }
}