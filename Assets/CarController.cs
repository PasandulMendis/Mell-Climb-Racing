using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    [Header("Car Components")]
    public Rigidbody2D carRigidbody;
    public WheelJoint2D backWheel;
    public WheelJoint2D frontWheel;

    [Header("Upgrade Levels")]
    public float[] engineSpeedLevels = new float[] { 1500f, 1700f, 1900f, 2100f, 2300f };
    public float[] suspensionFrequencyLevels = new float[] { 4f, 4.5f, 5f, 5.5f, 6f };
    public float[] tireFrictionLevels = new float[] { 1.5f, 1.8f, 2.1f, 2.4f, 2.7f };
    public float[] fuelCapacityLevels = new float[] { 100f, 120f, 140f, 160f, 180f };

    [Header("Upgrade Costs")]
    public int[] engineCostLevels = new int[] { 0, 150, 300, 600, 1000 };
    public int[] suspensionCostLevels = new int[] { 0, 100, 250, 500, 900 };
    public int[] tireCostLevels = new int[] { 0, 120, 280, 550, 950 };
    public int[] fuelCostLevels = new int[] { 0, 80, 200, 400, 800 };

    [Header("Current Stats (Auto-assigned)")]
    public float speed;       
    public float maxFuel;
    public float rotationSpeed = 250f; 

    [Header("Drive Settings")]
    public bool isFourWheelDrive = false; 

    [Header("Air Control & Stunts")]
    [Tooltip("Multiply rotation speed when all wheels leave the ground for better flips!")]
    public float airRotationMultiplier = 1.5f;
    [Tooltip("Coins rewarded for completing a 360 degree flip")]
    public int flipCoinBonus = 100;

    [Header("Nitro System")]
    public float nitroBoostForce = 15000f; 
    public float nitroDuration = 2.0f;     
    public Transform nitroThrusterPoint; 

    [Header("Coasting & Friction")]
    [Tooltip("How much the car slows down when you let go of the gas or run out of fuel")]
    public float coastingBrakeForce = 300f;
    [HideInInspector] public bool hasFuel = true;

    [Header("Audio & Effects")]
    public AudioSource engineAudioSource;
    public ParticleSystem exhaustParticleSystem;
    public float minPitch = 0.8f;
    public float maxPitch = 2.5f;
    public float pitchMultiplier = 0.05f;

    [Header("Rev Limiter Settings")]
    [Tooltip("RPM required to trigger the rev limiter sound (0.0 to 1.0)")]
    public float revLimiterThreshold = 0.95f;
    [Tooltip("How much the audio pitch drops when bouncing off the limiter")]
    public float revLimiterPitchDrop = 0.4f;
    [Tooltip("How fast the limiter stutters (bounces)")]
    public float revLimiterBounceSpeed = 25f;

    [Header("Wheel Dirt Particles")]
    public ParticleSystem backWheelDirt;
    public ParticleSystem frontWheelDirt;

    [Header("Meme Settings")]
    public float scaredHeightTrigger = 6f; 

    // Expose RPM for the UI Dashboard
    [HideInInspector] public float currentRPM = 0f;

    private int currentNitro;
    private bool isNitroActive = false;
    private float nitroTimer = 0f;

    private float movementInput;
    private float rpmCap = 1f; // Ratio of current speed vs absolute max speed

    // Physical ground contact tracking
    private WheelContact backContact;
    private WheelContact frontContact;
    private bool backGrounded = false;
    private bool frontGrounded = false;

    // Stunt State Tracking
    private float totalAirRotation = 0f;
    private float lastZRotation = 0f;
    private bool wasAirborneLastFrame = false;

    void Start()
    {
        WheelJoint2D[] allJoints = GetComponents<WheelJoint2D>();
        if (allJoints.Length >= 2 && backWheel == frontWheel)
        {
            backWheel = allJoints[0];
            frontWheel = allJoints[1];
        }

        // --- Auto-Setup Ground Contact Scripts ---
        if (backWheel != null && backWheel.connectedBody != null)
        {
            backContact = backWheel.connectedBody.gameObject.GetComponent<WheelContact>();
            if (backContact == null) backContact = backWheel.connectedBody.gameObject.AddComponent<WheelContact>();
        }
        if (frontWheel != null && frontWheel.connectedBody != null)
        {
            frontContact = frontWheel.connectedBody.gameObject.GetComponent<WheelContact>();
            if (frontContact == null) frontContact = frontWheel.connectedBody.gameObject.AddComponent<WheelContact>();
        }

        int carID = PlayerPrefs.GetInt("SelectedCar", 0);

        int engineLevel = PlayerPrefs.GetInt("Car_" + carID + "_EquippedEngineLevel", 1);
        int suspensionLevel = PlayerPrefs.GetInt("Car_" + carID + "_EquippedSuspensionLevel", 1);
        int tireLevel = PlayerPrefs.GetInt("Car_" + carID + "_EquippedTireLevel", 1);
        int fuelLevel = PlayerPrefs.GetInt("Car_" + carID + "_EquippedFuelLevel", 1);

        int engIdx = Mathf.Clamp(engineLevel - 1, 0, Mathf.Max(0, engineSpeedLevels.Length - 1));
        int susIdx = Mathf.Clamp(suspensionLevel - 1, 0, Mathf.Max(0, suspensionFrequencyLevels.Length - 1));
        int tireIdx = Mathf.Clamp(tireLevel - 1, 0, Mathf.Max(0, tireFrictionLevels.Length - 1));
        int fuelIdx = Mathf.Clamp(fuelLevel - 1, 0, Mathf.Max(0, fuelCapacityLevels.Length - 1));

        if (engineSpeedLevels.Length > 0) 
        {
            speed = engineSpeedLevels[engIdx];
            
            // --- NEW: Calculate the RPM Limit Ratio based on upgrades! ---
            float absoluteMaxSpeed = engineSpeedLevels[engineSpeedLevels.Length - 1];
            rpmCap = speed / absoluteMaxSpeed; // e.g., Level 1 = 1500 / 2300 = ~0.65 (65%)
        }
        
        if (fuelCapacityLevels.Length > 0) maxFuel = fuelCapacityLevels[fuelIdx];

        if (suspensionFrequencyLevels.Length > 0)
        {
            float newFrequency = suspensionFrequencyLevels[susIdx];
            float newDamping = 0.6f;

            if (backWheel != null)
            {
                JointSuspension2D backSus = backWheel.suspension;
                backSus.frequency = newFrequency;
                backSus.dampingRatio = newDamping; 
                backWheel.suspension = backSus;
            }

            if (frontWheel != null)
            {
                JointSuspension2D frontSus = frontWheel.suspension;
                frontSus.frequency = newFrequency;
                frontSus.dampingRatio = newDamping; 
                frontWheel.suspension = frontSus;
            }
        }

        if (tireFrictionLevels.Length > 0 && backWheel != null && frontWheel != null)
        {
            CircleCollider2D backCol = backWheel.connectedBody != null ? backWheel.connectedBody.GetComponent<CircleCollider2D>() : null;
            CircleCollider2D frontCol = frontWheel.connectedBody != null ? frontWheel.connectedBody.GetComponent<CircleCollider2D>() : null;
            
            if (backCol != null && frontCol != null)
            {
                PhysicsMaterial2D tireMat = new PhysicsMaterial2D("UpgradedGrip");
                tireMat.friction = tireFrictionLevels[tireIdx];
                tireMat.bounciness = 0f;
                
                backCol.sharedMaterial = tireMat;
                frontCol.sharedMaterial = tireMat;
            }
        }

        currentNitro = PlayerPrefs.GetInt("NitroAmount", 0);
    }

    void Update()
    {
        if (GameManager.instance != null && GameManager.instance.isGameOver)
        {
            movementInput = 0f;
            HandleAudioAndEffects();
            HandleDirtParticles();
            return;
        }

        movementInput = 0f;
        
        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) movementInput = 1f; 
            else if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) movementInput = -1f; 
            
            if (Keyboard.current.shiftKey.wasPressedThisFrame) TryActivateNitro();
        }

        if (GameManager.instance != null)
        {
            if (GameManager.instance.isMobileGasPressed) movementInput = 1f;
            else if (GameManager.instance.isMobileBrakePressed) movementInput = -1f;
        }

        if (!hasFuel) movementInput = 0f; 

        if (isNitroActive)
        {
            nitroTimer -= Time.deltaTime;
            if (nitroTimer <= 0) DeactivateNitro();
        }

        HandleAudioAndEffects();
        HandleDirtParticles();
    }

    void HandleAudioAndEffects()
    {
        // --- DASHBOARD RPM CALCULATION (Engine Load & Fake Gears) ---
        float targetRPM = 0.1f; // Engine idle

        if (hasFuel)
        {
            if (isNitroActive)
            {
                targetRPM = 1.0f * rpmCap; // Nitro pushes engine to its upgrade cap
            }
            else if (movementInput > 0)
            {
                if (!backGrounded)
                {
                    // Engine freely revs to max (limited by upgrades) when in the air!
                    targetRPM = 1.0f * rpmCap; 
                }
                else
                {
                    // Simulate gears: divide speed into chunks of 10. 
                    float speedMagnitude = carRigidbody.linearVelocity.magnitude;
                    float gearSize = 10f; 
                    float gearProgress = (speedMagnitude % gearSize) / gearSize;
                    
                    targetRPM = Mathf.Lerp(0.4f, 0.95f, gearProgress) * rpmCap;
                }
            }
            else if (movementInput < 0)
            {
                targetRPM = 0.5f * rpmCap; // Rev slightly when reversing/braking
            }
        }
        
        // Apply smooth needle movement
        float lerpSpeed = (movementInput > 0 && !backGrounded) ? 15f : 8f;
        currentRPM = Mathf.Lerp(currentRPM, targetRPM, Time.deltaTime * lerpSpeed);

        // --- ENGINE AUDIO ---
        if (engineAudioSource != null)
        {
            if (hasFuel && (GameManager.instance == null || !GameManager.instance.isGameOver))
            {
                // Tie the audio pitch directly to the RPM needle for realistic gear shifting sounds!
                float targetPitch = Mathf.Lerp(minPitch, maxPitch, currentRPM);

                // --- REV LIMITER AUDIO BOUNCE ---
                if (currentRPM >= revLimiterThreshold && movementInput > 0)
                {
                    // Rapidly drop the pitch to simulate an engine bouncing off the limiter
                    if (Mathf.PingPong(Time.time * revLimiterBounceSpeed, 1f) > 0.5f)
                    {
                        targetPitch -= revLimiterPitchDrop; 
                    }
                }

                engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, targetPitch, Time.deltaTime * 10f);
            }
            else
            {
                engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, 0f, Time.deltaTime * 2f);
            }
        }

        // --- EXHAUST SMOKE ---
        if (exhaustParticleSystem != null)
        {
            var emission = exhaustParticleSystem.emission;
            if (hasFuel && (GameManager.instance == null || !GameManager.instance.isGameOver))
            {
                if (movementInput > 0) emission.rateOverTime = 30f; 
                else emission.rateOverTime = 5f; 
            }
            else
            {
                emission.rateOverTime = 0f; 
            }
        }
    }

    void HandleDirtParticles()
    {
        if (backWheelDirt != null)
        {
            var em = backWheelDirt.emission;
            if (backGrounded && movementInput != 0 && hasFuel) em.rateOverTime = 30f;
            else em.rateOverTime = 0f;
        }

        if (frontWheelDirt != null)
        {
            var em = frontWheelDirt.emission;
            if (frontGrounded && movementInput != 0 && hasFuel && isFourWheelDrive) em.rateOverTime = 30f;
            else em.rateOverTime = 0f;
        }
    }

    public void UpdateDirtColor(Color newColor)
    {
        if (backWheelDirt != null)
        {
            var main = backWheelDirt.main;
            main.startColor = newColor;
        }
        if (frontWheelDirt != null)
        {
            var main = frontWheelDirt.main;
            main.startColor = newColor;
        }
    }

    public void TryActivateNitro()
    {
        if (currentNitro > 0 && !isNitroActive)
        {
            ActivateNitro();
        }
    }

    void ActivateNitro()
    {
        isNitroActive = true;
        currentNitro--; 
        PlayerPrefs.SetInt("NitroAmount", currentNitro); 
        PlayerPrefs.Save();
        nitroTimer = nitroDuration;

        if (MemeManager.instance != null) MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Happy);
        if (GameManager.instance != null) GameManager.instance.UpdateNitroUI(currentNitro);
    }

    void DeactivateNitro()
    {
        isNitroActive = false;
    }

    void FixedUpdate()
    {
        // 1. Gather physics-based ground state from the wheel colliders directly
        backGrounded = backContact != null ? backContact.CheckGrounded() : false;
        frontGrounded = frontContact != null ? frontContact.CheckGrounded() : false;

        if (GameManager.instance != null && GameManager.instance.isGameOver)
        {
            if (backWheel != null) backWheel.useMotor = false;
            if (frontWheel != null) frontWheel.useMotor = false;
            return;
        }

        // Both wheels must be fully off the ground for stunt progression and flips
        bool isAirborne = !backGrounded && !frontGrounded;

        // Balancing control is enabled as long as at least ONE wheel is off the ground
        bool canRotate = !backGrounded || !frontGrounded;

        if (movementInput != 0)
        {
            JointMotor2D motor = new JointMotor2D();
            motor.motorSpeed = movementInput * speed * -1f; 
            motor.maxMotorTorque = 10000f;

            if (backWheel != null)
            {
                backWheel.motor = motor;
                backWheel.useMotor = true;
            }
            
            if (frontWheel != null)
            {
                if (isFourWheelDrive)
                {
                    frontWheel.motor = motor;
                    frontWheel.useMotor = true;
                }
                else frontWheel.useMotor = false;
            }
        }
        else
        {
            JointMotor2D brakeMotor = new JointMotor2D();
            brakeMotor.motorSpeed = 0f;
            brakeMotor.maxMotorTorque = coastingBrakeForce;

            if (backWheel != null)
            {
                backWheel.motor = brakeMotor;
                backWheel.useMotor = true;
            }
            if (frontWheel != null)
            {
                frontWheel.motor = brakeMotor;
                frontWheel.useMotor = true;
            }
        }

        // Apply balancing torque when tilting conditions are met
        if (canRotate)
        {
            float flipTorque = movementInput * rotationSpeed * airRotationMultiplier * carRigidbody.mass * 50f;
            carRigidbody.AddTorque(flipTorque, ForceMode2D.Force);
        }

        if (isNitroActive && hasFuel)
        {
            if (nitroThrusterPoint != null) carRigidbody.AddForceAtPosition(transform.right * nitroBoostForce, nitroThrusterPoint.position, ForceMode2D.Force);
            else carRigidbody.AddRelativeForce(Vector2.right * nitroBoostForce, ForceMode2D.Force);
        }

        HandleStunts(isAirborne);
        CheckAltitudeForMeme();
    }

    void HandleStunts(bool isAirborne)
    {
        if (isAirborne)
        {
            if (!wasAirborneLastFrame)
            {
                totalAirRotation = 0f;
                lastZRotation = transform.eulerAngles.z;
            }

            float currentZ = transform.eulerAngles.z;
            float deltaZ = Mathf.DeltaAngle(lastZRotation, currentZ);
            totalAirRotation += deltaZ;
            lastZRotation = currentZ;

            if (Mathf.Abs(totalAirRotation) >= 360f)
            {
                string flipType = totalAirRotation > 0 ? "Backflip" : "Frontflip";
                Debug.Log(flipType + " completed! +" + flipCoinBonus + " Coins!");

                if (GameManager.instance != null) GameManager.instance.AddCoins(flipCoinBonus);
                if (MemeManager.instance != null) MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Happy);

                totalAirRotation -= Mathf.Sign(totalAirRotation) * 360f;
            }
        }
        else
        {
            totalAirRotation = 0f;
        }

        wasAirborneLastFrame = isAirborne;
    }

    void CheckAltitudeForMeme()
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.down, scaredHeightTrigger);
        bool hitGround = false;

        foreach (var hit in hits)
        {
            if (!hit.collider.isTrigger && !hit.collider.CompareTag("Player") && !hit.collider.transform.IsChildOf(this.transform))
            {
                hitGround = true; 
                break;
            }
        }

        if (!hitGround && MemeManager.instance != null) MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Scared);
    }
}

// --- CRITICAL HELPER COMPONENT FOR BULLETPROOF GROUND DETECTION ---
// (Make sure you don't miss copying this part at the very bottom!)
public class WheelContact : MonoBehaviour
{
    private bool touchedThisFrame = false;

    // Fired by Unity's physics system when a physical wheel collider is touching anything
    void OnCollisionStay2D(Collision2D collision)
    {
        // Ignore colliders on the player's chassis or child components (like the car's own frame)
        if (!collision.collider.CompareTag("Player") && !collision.collider.transform.IsChildOf(transform.root))
        {
            touchedThisFrame = true;
        }
    }

    public bool CheckGrounded()
    {
        bool wasGrounded = touchedThisFrame;
        touchedThisFrame = false; // Reset for next FixedUpdate cycle
        return wasGrounded;
    }
}