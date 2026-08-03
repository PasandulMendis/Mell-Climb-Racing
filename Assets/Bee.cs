using UnityEngine;

public class Bee : MonoBehaviour
{
    [Header("Flight Settings")]
    public float flySpeed = 15f;
    [Tooltip("How erratically the bee buzzes left and right")]
    public float buzzWidth = 2f;
    [Tooltip("How fast the bee buzzes")]
    public float buzzSpeed = 15f;
    
    [Header("Swarm & Drain Settings")]
    [Tooltip("How far the car must drive from the hive before the swarm gives up")]
    public float maxChaseDistance = 60f;
    [Tooltip("How much fuel this SINGLE bee drains per second (Multiply by 5 for the whole swarm!)")]
    public float fuelDrainPerSecond = 2f;
    
    private Transform playerCar;
    private FuelSystem fuelSystem;
    
    private bool isVanishing = false;
    private Vector3 hivePosition;
    private Vector3 originalScale;
    
    // Gives each bee a unique flight pattern so they don't clump together
    private float randomBuzzOffset;
    private Vector3 targetHoverOffset;

    void Start()
    {
        randomBuzzOffset = Random.Range(0f, 100f);
        
        // Remember where the hive was so we can measure distance!
        hivePosition = transform.position; 
        originalScale = transform.localScale;
        
        // Pick a random spot around the car to hover (some high, some low, some behind)
        targetHoverOffset = new Vector3(Random.Range(-3f, 1f), Random.Range(0f, 3f), 0f);

        if (GameManager.instance != null && GameManager.instance.playerCar != null)
        {
            playerCar = GameManager.instance.playerCar;
            fuelSystem = playerCar.GetComponent<FuelSystem>();
        }
    }

    void Update()
    {
        if (playerCar == null) return;

        if (!isVanishing)
        {
            // 1. Calculate erratic buzzing movement
            float buzzX = Mathf.Sin((Time.time + randomBuzzOffset) * buzzSpeed) * buzzWidth;
            float buzzY = Mathf.Cos((Time.time + randomBuzzOffset) * buzzSpeed * 0.8f) * (buzzWidth * 0.5f);
            
            // 2. Find the target position relative to the car
            Vector3 targetPos = playerCar.position + targetHoverOffset + new Vector3(buzzX, buzzY, 0f);

            // 3. Fly towards it!
            transform.position = Vector3.MoveTowards(transform.position, targetPos, flySpeed * Time.deltaTime);

            // --- FIX: Correct Sprite Flipping for Right-Facing Images ---
            if (targetPos.x > transform.position.x) 
            {
                // Moving Right: Use positive scale
                transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
            }
            else 
            {
                // Moving Left: Use negative scale (flip)
                transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
            }

            // 4. Drain the Fuel!
            if (fuelSystem != null && !fuelSystem.isOutOfFuel)
            {
                // We directly subtract the fuel. FuelSystem.cs already clamps it at 0 so it won't break!
                fuelSystem.currentFuel -= fuelDrainPerSecond * Time.deltaTime;
            }

            // 5. --- FIX: Check Distance from Hive instead of Time ---
            if (Vector2.Distance(playerCar.position, hivePosition) >= maxChaseDistance)
            {
                isVanishing = true;
            }
        }
        else
        {
            // --- FIX: Shrink to vanish instead of flying to the sky ---
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * 10f);
            
            // Destroy once it is practically invisible
            if (Mathf.Abs(transform.localScale.x) <= 0.05f)
            {
                Destroy(gameObject);
            }
        }
    }
}