using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic; // Required for using Lists

[System.Serializable]
public class CarWheel
{
    [Tooltip("Just a name to help you organize in the inspector (e.g., 'Back Left')")]
    public string wheelName = "Wheel";
    
    [Tooltip("The WheelJoint2D component for this specific wheel")]
    public WheelJoint2D wheelJoint;
    
    [Tooltip("If TRUE, the engine powers this wheel. If FALSE, it just rolls (Dead wheel)")]
    public bool isDriven = true;
    
    [Tooltip("Dirt particle system assigned to this specific wheel")]
    public ParticleSystem dirtParticle;

    // Internal states managed by the script
    [HideInInspector] public WheelContact contactScript;
    [HideInInspector] public bool isGrounded = false;
    [HideInInspector] public bool isPopped = false;
}

public class CarController : MonoBehaviour
{
    [Header("Car Components (Multi-Wheel Support)")]
    public Rigidbody2D carRigidbody;
    
    [Tooltip("Add all your vehicle's wheels here. Supports 2, 4, 6, 8+ wheels!")]
    public List<CarWheel> wheels = new List<CarWheel>();

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
    public float revLimiterThreshold = 0.95f;
    public float revLimiterPitchDrop = 0.4f;
    public float revLimiterBounceSpeed = 25f;

    [Header("Meme Settings")]
    public float scaredHeightTrigger = 6f; 

    [Header("Coin Magnetism")]
    public float magnetRadius = 6f;
    public RectTransform targetUIIcon;

    [Header("Lighting")]
    public GameObject headlights;

    [Header("Tire Popping (Bear Traps)")]
    public Sprite flatTireSprite;
    public float flatTireScale = 0.75f;
    public float flatTireFriction = 0.1f;

    [HideInInspector] public float currentRPM = 0f;

    private int currentNitro;
    private bool isNitroActive = false;
    private float nitroTimer = 0f;

    private float movementInput;
    private float rpmCap = 1f;

    private float totalAirRotation = 0f;
    private float lastZRotation = 0f;
    private bool wasAirborneLastFrame = false;
    
    private float currentAirTime = 0f;
    private int flipsThisJump = 0;

    private float hypeProgress = 0f;
    private bool hasFreeNitro = false;

    void Start()
    {
        // Auto-assign wheels if the list is completely empty
        if (wheels.Count == 0)
        {
            WheelJoint2D[] allJoints = GetComponents<WheelJoint2D>();
            for (int i = 0; i < allJoints.Length; i++)
            {
                CarWheel newWheel = new CarWheel();
                newWheel.wheelName = "Auto-Assigned Wheel " + (i + 1);
                newWheel.wheelJoint = allJoints[i];
                newWheel.isDriven = true; // Default to driven
                wheels.Add(newWheel);
            }
        }

        // Initialize wheel contacts dynamically
        foreach (CarWheel w in wheels)
        {
            if (w.wheelJoint != null && w.wheelJoint.connectedBody != null)
            {
                w.contactScript = w.wheelJoint.connectedBody.gameObject.GetComponent<WheelContact>();
                if (w.contactScript == null) 
                    w.contactScript = w.wheelJoint.connectedBody.gameObject.AddComponent<WheelContact>();
            }
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
            float absoluteMaxSpeed = engineSpeedLevels[engineSpeedLevels.Length - 1];
            rpmCap = speed / absoluteMaxSpeed;
        }
        
        if (fuelCapacityLevels.Length > 0) maxFuel = fuelCapacityLevels[fuelIdx];

        if (suspensionFrequencyLevels.Length > 0)
        {
            float newFrequency = suspensionFrequencyLevels[susIdx];
            float newDamping = 0.6f;

            foreach (CarWheel w in wheels)
            {
                if (w.wheelJoint != null)
                {
                    JointSuspension2D sus = w.wheelJoint.suspension;
                    sus.frequency = newFrequency;
                    sus.dampingRatio = newDamping; 
                    w.wheelJoint.suspension = sus;
                }
            }
        }

        if (tireFrictionLevels.Length > 0)
        {
            PhysicsMaterial2D tireMat = new PhysicsMaterial2D("UpgradedGrip");
            tireMat.friction = tireFrictionLevels[tireIdx];
            tireMat.bounciness = 0f;
            
            foreach (CarWheel w in wheels)
            {
                if (w.wheelJoint != null && w.wheelJoint.connectedBody != null)
                {
                    CircleCollider2D col = w.wheelJoint.connectedBody.GetComponent<CircleCollider2D>();
                    if (col != null) col.sharedMaterial = tireMat;
                }
            }
        }

        currentNitro = PlayerPrefs.GetInt("NitroAmount", 0);

        if (targetUIIcon == null)
        {
            GameObject foundIcon = GameObject.Find("CoinIcon");
            if (foundIcon != null) targetUIIcon = foundIcon.GetComponent<RectTransform>();
            else Debug.LogWarning("CarController: Could not find 'CoinIcon' in the scene hierarchy!");
        }

        ToggleHeadlights(false);
    }

    public void ToggleHeadlights(bool state)
    {
        if (headlights != null) headlights.SetActive(state);
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
        CheckCoinMagnet(); 
    }

    void HandleAudioAndEffects()
    {
        float targetRPM = 0.1f; 
        
        // Determine if AT LEAST ONE wheel is on the ground
        bool isAnyWheelGrounded = false;
        foreach (CarWheel w in wheels)
        {
            if (w.isGrounded) isAnyWheelGrounded = true;
        }

        if (hasFuel)
        {
            if (isNitroActive)
            {
                targetRPM = 1.0f * rpmCap;
            }
            else if (movementInput > 0)
            {
                if (!isAnyWheelGrounded)
                {
                    targetRPM = 1.0f * rpmCap; 
                }
                else
                {
                    float speedMagnitude = carRigidbody.linearVelocity.magnitude;
                    float gearSize = 10f; 
                    float gearProgress = (speedMagnitude % gearSize) / gearSize;
                    
                    targetRPM = Mathf.Lerp(0.4f, 0.95f, gearProgress) * rpmCap;
                }
            }
            else if (movementInput < 0)
            {
                targetRPM = 0.5f * rpmCap;
            }
        }
        
        float lerpSpeed = (movementInput > 0 && !isAnyWheelGrounded) ? 15f : 8f;
        currentRPM = Mathf.Lerp(currentRPM, targetRPM, Time.deltaTime * lerpSpeed);

        if (engineAudioSource != null)
        {
            if (hasFuel && (GameManager.instance == null || !GameManager.instance.isGameOver))
            {
                float targetPitch = Mathf.Lerp(minPitch, maxPitch, currentRPM);

                if (currentRPM >= revLimiterThreshold && movementInput > 0)
                {
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
        foreach (CarWheel w in wheels)
        {
            if (w.dirtParticle != null)
            {
                var em = w.dirtParticle.emission;
                // Only spawn dirt if wheel is on ground, gas is pressed, has fuel, AND is a driven wheel
                if (w.isGrounded && movementInput != 0 && hasFuel && w.isDriven) 
                    em.rateOverTime = 30f;
                else 
                    em.rateOverTime = 0f;
            }
        }
    }

    public void UpdateDirtColor(Color newColor)
    {
        foreach (CarWheel w in wheels)
        {
            if (w.dirtParticle != null)
            {
                var main = w.dirtParticle.main;
                main.startColor = newColor;
            }
        }
    }

    public void TryActivateNitro()
    {
        if (isNitroActive) return;

        if (hasFreeNitro)
        {
            hasFreeNitro = false;
            hypeProgress = 0f;
            if (GameManager.instance != null) GameManager.instance.UpdateHypeUI(0f);
            
            ActivateNitro(true);
        }
        else if (currentNitro > 0)
        {
            ActivateNitro(false);
        }
    }

    void ActivateNitro(bool isFree)
    {
        isNitroActive = true;
        
        if (!isFree)
        {
            currentNitro--; 
            PlayerPrefs.SetInt("NitroAmount", currentNitro); 
            PlayerPrefs.Save();
        }

        nitroTimer = nitroDuration;

        if (GameManager.instance != null) GameManager.instance.runNitrosUsed++;
        if (MemeManager.instance != null) MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Happy);
        if (GameManager.instance != null) GameManager.instance.UpdateNitroUI(currentNitro);
    }

    public void AddHype(float amount)
    {
        if (hasFreeNitro) return;

        hypeProgress += amount;
        
        if (hypeProgress >= 0.99f) 
        {
            hypeProgress = 1f;
            hasFreeNitro = true;
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.UpdateHypeUI(hypeProgress);
        }
    }

    void DeactivateNitro()
    {
        isNitroActive = false;
    }

    void CheckCoinMagnet()
    {
        if (targetUIIcon == null) return;

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, magnetRadius);
        foreach (var hit in hitColliders)
        {
            Coin coinScript = hit.GetComponent<Coin>();
            if (coinScript != null)
            {
                coinScript.MagnetizeTo(transform, targetUIIcon);
            }
        }
    }

    void FixedUpdate()
    {
        bool isAirborne = true;
        bool canRotate = false;

        // Check grounded status for all configured wheels
        foreach (CarWheel w in wheels)
        {
            w.isGrounded = w.contactScript != null ? w.contactScript.CheckGrounded() : false;
            
            // If any wheel touches ground, car is not fully airborne
            if (w.isGrounded) isAirborne = false; 
            
            // If any wheel leaves ground, car can flip/rotate
            if (!w.isGrounded) canRotate = true;  
        }

        if (GameManager.instance != null && GameManager.instance.isGameOver)
        {
            foreach (CarWheel w in wheels)
            {
                if (w.wheelJoint != null) w.wheelJoint.useMotor = false;
            }
            return;
        }

        if (movementInput != 0)
        {
            JointMotor2D motor = new JointMotor2D();
            motor.motorSpeed = movementInput * speed * -1f; 
            motor.maxMotorTorque = 10000f;

            foreach (CarWheel w in wheels)
            {
                if (w.wheelJoint != null)
                {
                    if (w.isDriven) // Dedicated Wheel Drive logic
                    {
                        w.wheelJoint.motor = motor;
                        w.wheelJoint.useMotor = true;
                    }
                    else
                    {
                        w.wheelJoint.useMotor = false;
                    }
                }
            }
        }
        else
        {
            JointMotor2D brakeMotor = new JointMotor2D();
            brakeMotor.motorSpeed = 0f;
            brakeMotor.maxMotorTorque = coastingBrakeForce;

            foreach (CarWheel w in wheels)
            {
                if (w.wheelJoint != null)
                {
                    w.wheelJoint.motor = brakeMotor;
                    w.wheelJoint.useMotor = true; // Apply coasting brakes to ALL wheels for stability
                }
            }
        }

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
                currentAirTime = 0f;
                flipsThisJump = 0;
            }

            currentAirTime += Time.deltaTime;

            float currentZ = transform.eulerAngles.z;
            float deltaZ = Mathf.DeltaAngle(lastZRotation, currentZ);
            totalAirRotation += deltaZ;
            lastZRotation = currentZ;

            if (Mathf.Abs(totalAirRotation) >= 360f)
            {
                flipsThisJump++;
                AddHype(0.2f); 

                string flipType = totalAirRotation > 0 ? "Backflip" : "Frontflip";
                
                if (StuntManager.instance != null)
                {
                    StuntManager.instance.ProcessFlip(flipType, flipsThisJump, transform.position);
                }
                else 
                {
                    if (GameManager.instance != null) GameManager.instance.AddCoins(flipCoinBonus);
                    if (MemeManager.instance != null) MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Happy);
                }

                totalAirRotation -= Mathf.Sign(totalAirRotation) * 360f;
            }
        }
        else
        {
            if (wasAirborneLastFrame)
            {
                if (StuntManager.instance != null)
                {
                    StuntManager.instance.ProcessAirTime(currentAirTime, transform.position);
                }
            }
            
            totalAirRotation = 0f;
            currentAirTime = 0f;
            flipsThisJump = 0;
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

    public bool PopTire(GameObject tireObject)
    {
        bool poppedThisFrame = false;

        // Loop through all wheels to check which one matched the bear trap
        foreach (CarWheel w in wheels)
        {
            if (w.wheelJoint != null && w.wheelJoint.connectedBody != null)
            {
                if (w.wheelJoint.connectedBody.gameObject == tireObject)
                {
                    if (!w.isPopped)
                    {
                        w.isPopped = true;
                        ApplyFlatTire(w.wheelJoint.connectedBody.gameObject);
                        poppedThisFrame = true;
                    }
                }
            }
        }

        if (poppedThisFrame)
        {
            if (MemeManager.instance != null) MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Angry, true, false);
            return true;
        }

        return false;
    }

    private void ApplyFlatTire(GameObject wheelObj)
    {
        SpriteRenderer sr = wheelObj.GetComponent<SpriteRenderer>();
        if (sr != null && flatTireSprite != null) sr.sprite = flatTireSprite;

        CircleCollider2D col = wheelObj.GetComponent<CircleCollider2D>();
        if (col != null) col.radius *= flatTireScale;

        if (col != null)
        {
            PhysicsMaterial2D flatMat = new PhysicsMaterial2D("FlatTire");
            flatMat.friction = flatTireFriction;
            flatMat.bounciness = 0f;
            col.sharedMaterial = flatMat;
        }
    }
}

public class WheelContact : MonoBehaviour
{
    private bool touchedThisFrame = false;

    void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player") && !collision.collider.transform.IsChildOf(transform.root))
        {
            touchedThisFrame = true;
        }
    }

    public bool CheckGrounded()
    {
        bool wasGrounded = touchedThisFrame;
        touchedThisFrame = false;
        return wasGrounded;
    }
}