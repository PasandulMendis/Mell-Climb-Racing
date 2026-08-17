using UnityEngine;
using System.Collections.Generic;

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

    [Header("Ground Tag Settings")]
    [Tooltip("Tags of ground objects that should NOT spawn dust (e.g., 'Wood' for bridges).")]
    public List<string> disallowedGroundTags = new List<string> { "Wood" };

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
        
        // Ensure the particle system emission is initially turned off but the system itself is "Playing"
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
        
        // Get the car's current speed and check if actively accelerating
        float currentSpeed = carController.carRigidbody.linearVelocity.magnitude;
        bool isAccelerating = carController.currentRPM > 0.3f;

        // Check if the wheel is touching a disallowed tag (like a "Wood" bridge)
        bool touchingDisallowedGround = false;
        if (isCurrentlyGrounded && disallowedGroundTags.Count > 0 && targetWheel.wheelJoint != null)
        {
            // Get the collider of the wheel itself
            Collider2D wheelCol = targetWheel.wheelJoint.connectedBody != null ? 
                targetWheel.wheelJoint.connectedBody.GetComponent<Collider2D>() : 
                targetWheel.wheelJoint.GetComponent<Collider2D>();
            
            if (wheelCol != null)
            {
                // Create a small circle check slightly larger than the wheel to see what we are touching
                float checkRadius = wheelCol.bounds.extents.y * 1.2f;
                Collider2D[] hits = Physics2D.OverlapCircleAll(wheelCol.bounds.center, checkRadius);
                
                foreach (var hit in hits)
                {
                    // Ignore the car itself
                    if (hit.gameObject != carController.gameObject && !hit.transform.IsChildOf(carController.transform))
                    {
                        if (disallowedGroundTags.Contains(hit.tag))
                        {
                            touchingDisallowedGround = true;
                            break;
                        }
                    }
                }
            }
        }

        // 1. LANDING EFFECT (Puff of dust when hitting the ground)
        if (enableLandingPuff && isCurrentlyGrounded && !wasGroundedLastFrame && !touchingDisallowedGround)
        {
            dustParticleSystem.Play();
        }

        // 2. CONTINUOUS TRAIL EFFECT (Dust trailing while driving or accelerating)
        var emission = dustParticleSystem.emission;
        
        // Trail is active if we are on ALLOWED ground AND (moving fast enough OR actively pushing the gas pedal)
        if (isCurrentlyGrounded && !touchingDisallowedGround && (currentSpeed > minSpeedForTrail || isAccelerating))
        {
            // Force the system to stay awake if it fell asleep
            if (!dustParticleSystem.isPlaying) 
            {
                dustParticleSystem.Play();
            }

            // Turn on continuous emission
            emission.enabled = true;
            emission.rateOverTime = continuousEmissionRate;
        }
        else
        {
            // Turn off emission when in the air, completely stopped, or driving on a bridge
            emission.enabled = false;
        }

        // Store the grounded state for the next frame to detect future landings
        wasGroundedLastFrame = isCurrentlyGrounded;
    }
}