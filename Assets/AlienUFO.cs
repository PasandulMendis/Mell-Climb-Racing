using UnityEngine;
using UnityEngine.Rendering.Universal; // Required for URP 2D Lights!

public class AlienUFO : MonoBehaviour
{
    [Header("UFO Sprites (Animation)")]
    public Sprite lightsOffSprite;
    public Sprite lightsOnSprite;
    [Tooltip("How fast the lights flicker (seconds)")]
    public float flickerRate = 0.15f;

    [Header("Spotlight (URP 2D Light)")]
    [Tooltip("Drag the child Light2D component here")]
    public Light2D ufoSpotlight;

    [Header("Chase Settings")]
    [Tooltip("Must be faster than the car's average speed to catch it!")]
    public float chaseSpeed = 35f;
    [Tooltip("Seconds before the UFO gives up and vanishes")]
    public float chaseTimeout = 12f;
    [Tooltip("How high above the car the UFO hovers")]
    public float hoverHeight = 4.5f;

    [Header("Mass-Based Levitation")]
    public float lightWeightThreshold = 4f;
    public float heavyWeightThreshold = 8f;
    [Tooltip("How fast it lifts a lightweight vehicle")]
    public float lightCarLiftSpeed = 6f;
    [Tooltip("How fast it lifts a heavy truck")]
    public float heavyCarLiftSpeed = 1.5f;
    [Tooltip("How long it holds a lightweight vehicle in the air")]
    public float lightCarLiftDuration = 3f;
    [Tooltip("How long it holds a heavy truck before dropping it")]
    public float heavyCarLiftDuration = 1.2f;

    [Header("Flee Settings")]
    [Tooltip("How fast the UFO vanishes into the sky")]
    public float fleeSpeed = 50f;

    private enum UFOState { Chasing, Levitating, Fleeing }
    private UFOState currentState = UFOState.Chasing;

    private Transform playerCar;
    private Rigidbody2D carRb;
    private SpriteRenderer sr;
    private CarController carController;

    private float stateTimer = 0f;
    private float flickerTimer = 0f;
    private bool isLightSpriteOn = false;
    private float originalCarGravity;
    
    // Track the calculated speeds for this specific grab
    private float currentLiftSpeed;
    private float currentLiftDuration;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        
        if (GameManager.instance != null && GameManager.instance.playerCar != null)
        {
            playerCar = GameManager.instance.playerCar;
            carRb = playerCar.GetComponent<Rigidbody2D>();
            carController = playerCar.GetComponent<CarController>();
        }

        // Ensure the spotlight is off during the initial chase!
        if (ufoSpotlight != null) 
        {
            ufoSpotlight.enabled = false; 
        }
    }

    void Update()
    {
        // 1. CONSTANT FLICKER ANIMATION
        flickerTimer += Time.deltaTime;
        if (flickerTimer >= flickerRate)
        {
            flickerTimer = 0f;
            isLightSpriteOn = !isLightSpriteOn;
            if (sr != null) sr.sprite = isLightSpriteOn ? lightsOnSprite : lightsOffSprite;
        }

        if (playerCar == null || carRb == null) return;

        stateTimer += Time.deltaTime;

        // 2. UFO BEHAVIOR STATE MACHINE
        switch (currentState)
        {
            case UFOState.Chasing:
                // Target a point directly above the car
                Vector3 targetPos = playerCar.position + new Vector3(0, hoverHeight, 0);
                transform.position = Vector3.MoveTowards(transform.position, targetPos, chaseSpeed * Time.deltaTime);

                // If it catches the car, grab it!
                if (Vector2.Distance(transform.position, targetPos) < 0.5f)
                {
                    StartLevitation();
                }
                // If it takes too long to catch the car, give up and flee
                else if (stateTimer > chaseTimeout)
                {
                    StartFleeing();
                }
                break;

            case UFOState.Levitating:
                // --- NEW: NITRO ESCAPE OVERRIDE ---
                // If the player uses a Nitro, the UFO instantly loses its grip and flees!
                if (carController != null && carController.isNitroActive)
                {
                    ReleaseCar();
                    break;
                }

                // Move UFO straight up using the calculated speed based on car mass
                transform.position += Vector3.up * currentLiftSpeed * Time.deltaTime;
                
                // Forcibly drag the car up with it (overriding standard physics)
                carRb.transform.position = transform.position - new Vector3(0, hoverHeight, 0);
                carRb.linearVelocity = Vector2.zero; // Kill momentum
                carRb.angularVelocity *= 0.9f; // Smooth out crazy spinning

                if (stateTimer > currentLiftDuration)
                {
                    ReleaseCar();
                }
                break;

            case UFOState.Fleeing:
                // Fly straight up into the stratosphere incredibly fast
                transform.position += Vector3.up * fleeSpeed * Time.deltaTime;
                break;
        }
    }

    void StartLevitation()
    {
        currentState = UFOState.Levitating;
        stateTimer = 0f;
        
        float carMass = carRb.mass;
        
        // InverseLerp returns 0.0 if it is exactly light threshold, and 1.0 if it is heavy threshold
        float massLerp = Mathf.InverseLerp(lightWeightThreshold, heavyWeightThreshold, carMass);
        
        // Lerp perfectly calculates the values in between the minimum and maximum!
        currentLiftSpeed = Mathf.Lerp(lightCarLiftSpeed, heavyCarLiftSpeed, massLerp);
        currentLiftDuration = Mathf.Lerp(lightCarLiftDuration, heavyCarLiftDuration, massLerp);

        // Save the moon gravity and set car gravity to zero so it's weightless!
        originalCarGravity = carRb.gravityScale;
        carRb.gravityScale = 0f;

        if (ufoSpotlight != null)
        {
            ufoSpotlight.enabled = true;
            ufoSpotlight.color = Color.red; // RED = Danger/Grabbing
        }

        // Trigger Scared meme!
        if (MemeManager.instance != null) MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Scared, true, false);
    }

    void ReleaseCar()
    {
        currentState = UFOState.Fleeing;
        
        // Restore gravity so the car drops back to the moon surface!
        carRb.gravityScale = originalCarGravity;
        
        if (ufoSpotlight != null)
        {
            ufoSpotlight.color = Color.yellow; // YELLOW = Fleeing/Passive
        }

        Destroy(gameObject, 3f); // Delete object once it flies off screen
    }

    void StartFleeing()
    {
        currentState = UFOState.Fleeing;
        
        if (ufoSpotlight != null)
        {
            ufoSpotlight.enabled = true;
            ufoSpotlight.color = Color.yellow;
        }

        Destroy(gameObject, 3f);
    }
}