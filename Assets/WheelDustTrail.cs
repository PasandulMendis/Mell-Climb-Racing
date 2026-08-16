using UnityEngine;

public class WheelDustTrail : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("The Particle System on your downloaded Dust Prefab.")]
    public ParticleSystem dustParticleSystem;
    
    [Tooltip("Reference to your main car controller. Will auto-detect if left empty.")]
    public CarController carController;

    [Header("Dust Settings")]
    [Tooltip("0 = First wheel in your list (usually Back Wheel), 1 = Second wheel (Front).")]
    public int targetWheelIndex = 0;
    
    [Tooltip("Minimum speed the car must be moving to spawn a continuous trail.")]
    public float minSpeedForTrail = 0.5f;

    [Tooltip("How dense the dust trail should be when driving. Increase this if you only see small spots!")]
    public float continuousEmissionRate = 50f;

    [Tooltip("If true, plays a big puff of dust when the wheel lands after a jump.")]
    public bool enableLandingPuff = true;

    [Header("Map Color Settings")]
    [Tooltip("If true, the dust color will change based on the currently selected map.")]
    public bool useMapColors = true;
    
    [Tooltip("Matches GameManager.cs exactly! The key used to load the current map.")]
    public string mapPlayerPrefsKey = "SelectedMap";

    [Tooltip("Assign colors here. Element 0 = Map 0 (Dirt), Element 1 = Map 1 (Snow), etc.")]
    public Color[] mapDustColors;

    // Internal state to track when we hit the ground
    private bool wasGroundedLastFrame = true;

    void Start()
    {
        // Auto-assign the particle system if you attached this script directly to the prefab
        if (dustParticleSystem == null)
        {
            dustParticleSystem = GetComponent<ParticleSystem>();
        }

        // Auto-assign the CarController by looking at the parent objects
        if (carController == null)
        {
            carController = GetComponentInParent<CarController>();
        }
        
        // Ensure the particle system emission is initially turned off
        if (dustParticleSystem != null)
        {
            var emission = dustParticleSystem.emission;
            emission.enabled = false;

            // CRITICAL FIX: The particle system MUST be in a "Playing" state for emission toggling to work!
            if (!dustParticleSystem.isPlaying)
            {
                dustParticleSystem.Play();
            }

            // Apply the map-specific dust color as soon as the game starts
            ApplyMapDustColor();
        }
    }

    private void ApplyMapDustColor()
    {
        // Skip if the feature is disabled, or no colors were set up in the Inspector
        if (!useMapColors || mapDustColors == null || mapDustColors.Length == 0 || dustParticleSystem == null) return;

        // Matches exactly how GameManager.cs loads the map!
        int mapIndex = PlayerPrefs.GetInt(mapPlayerPrefsKey, 0);

        // Failsafe: Make sure we don't cause an error if the map index is higher than the colors you assigned
        int safeIndex = Mathf.Clamp(mapIndex, 0, mapDustColors.Length - 1);

        // Fetch the color
        Color selectedColor = mapDustColors[safeIndex];

        // CRITICAL FIX: Unity's color picker defaults to 0 Alpha (invisible)! 
        // We force it to 1 (fully visible) so you don't accidentally get invisible dust.
        selectedColor.a = 1f;

        // Apply the color to the particle system's main settings
        var main = dustParticleSystem.main;
        main.startColor = new ParticleSystem.MinMaxGradient(selectedColor);
    }

    void Update()
    {
        // Safety check to ensure everything is connected and the wheel exists
        if (carController == null || dustParticleSystem == null) return;
        if (carController.wheels.Count <= targetWheelIndex) return;

        // Get the current state of the specific wheel (e.g., the back wheel)
        CarWheel targetWheel = carController.wheels[targetWheelIndex];
        bool isCurrentlyGrounded = targetWheel.isGrounded;
        
        // Get the car's current speed
        float currentSpeed = carController.carRigidbody.linearVelocity.magnitude;

        // Check if the car is actively accelerating using your Dashboard RPM logic!
        // Your GameManager idles at low RPM, anything > 0.3 means they are hitting the gas.
        bool isAccelerating = carController.currentRPM > 0.3f;

        // 1. LANDING EFFECT (Puff of dust when hitting the ground)
        if (enableLandingPuff && isCurrentlyGrounded && !wasGroundedLastFrame)
        {
            // Forces the particle system to play a burst immediately
            dustParticleSystem.Play();
        }

        // 2. CONTINUOUS TRAIL EFFECT (Dust trailing while driving or accelerating)
        var emission = dustParticleSystem.emission;
        
        // Trail is active if we are on the ground AND (moving fast enough OR actively pushing the gas pedal)
        if (isCurrentlyGrounded && (currentSpeed > minSpeedForTrail || isAccelerating))
        {
            // Ensure the system is actually playing (in case it stopped automatically)
            if (!dustParticleSystem.isPlaying) 
            {
                dustParticleSystem.Play();
            }

            // Turn on continuous emission while driving on the ground
            emission.enabled = true;
            
            // Override the prefab's rate to ensure a thick, continuous trail instead of tiny spots
            emission.rateOverTime = continuousEmissionRate;
        }
        else
        {
            // Turn off emission when in the air or completely stopped
            emission.enabled = false;
        }

        // Store the grounded state for the next frame to detect future landings
        wasGroundedLastFrame = isCurrentlyGrounded;
    }
}