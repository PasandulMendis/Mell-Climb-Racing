using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    [Header("Car Components")]
    public Rigidbody2D carRigidbody;
    public WheelJoint2D backWheel;
    public WheelJoint2D frontWheel;

    [Header("Upgrade Levels (Set values for each level)")]
    public float[] engineSpeedLevels = new float[] { 1500f, 1700f, 1900f, 2100f, 2300f };
    public float[] suspensionFrequencyLevels = new float[] { 4f, 4.5f, 5f, 5.5f, 6f };
    public float[] tireFrictionLevels = new float[] { 1.5f, 1.8f, 2.1f, 2.4f, 2.7f };
    public float[] fuelCapacityLevels = new float[] { 100f, 120f, 140f, 160f, 180f };

    [Header("Upgrade Costs (Match array lengths with levels above)")]
    public int[] engineCostLevels = new int[] { 0, 150, 300, 600, 1000 };
    public int[] suspensionCostLevels = new int[] { 0, 100, 250, 500, 900 };
    public int[] tireCostLevels = new int[] { 0, 120, 280, 550, 950 };
    public int[] fuelCostLevels = new int[] { 0, 80, 200, 400, 800 };

    [Header("Current Stats (Auto-assigned on Start)")]
    public float speed;       
    public float maxFuel;
    public float rotationSpeed = 250f; 

    [Header("Drive Settings")]
    public bool isFourWheelDrive = false; 

    [Header("Nitro System")]
    public float nitroBoostForce = 15000f; 
    public float nitroDuration = 2.0f;     
    public Transform nitroThrusterPoint; 

    [Header("Coasting & Friction")]
    [Tooltip("How much the car slows down when you let go of the gas or run out of fuel")]
    public float coastingBrakeForce = 300f;
    [HideInInspector] public bool hasFuel = true;

    [Header("Meme Settings")]
    public float scaredHeightTrigger = 6f; 

    private int currentNitro;
    private bool isNitroActive = false;
    private float nitroTimer = 0f;

    private float movementInput;

    void Start()
    {
        WheelJoint2D[] allJoints = GetComponents<WheelJoint2D>();
        if (allJoints.Length >= 2 && backWheel == frontWheel)
        {
            backWheel = allJoints[0];
            frontWheel = allJoints[1];
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

        if (engineSpeedLevels.Length > 0) speed = engineSpeedLevels[engIdx];
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
            return;
        }

        movementInput = 0f;
        
        bool mobileGas = GameManager.instance != null && GameManager.instance.isMobileGasPressed;
        bool mobileBrake = GameManager.instance != null && GameManager.instance.isMobileBrakePressed;

        if (mobileGas || (Keyboard.current != null && (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)))
        {
            movementInput = 1f; 
        }
        else if (mobileBrake || (Keyboard.current != null && (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)))
        {
            movementInput = -1f; 
        }

        if (Keyboard.current != null && Keyboard.current.shiftKey.wasPressedThisFrame)
        {
            TryActivateNitro();
        }

        if (isNitroActive)
        {
            nitroTimer -= Time.deltaTime;
            if (nitroTimer <= 0)
            {
                DeactivateNitro();
            }
        }
    }

    public void TryActivateNitro()
    {
        if (currentNitro > 0 && !isNitroActive && (GameManager.instance == null || !GameManager.instance.isGameOver))
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

        if (MemeManager.instance != null)
        {
            MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Happy);
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.UpdateNitroUI(currentNitro);
        }
    }

    void DeactivateNitro()
    {
        isNitroActive = false;
    }

    void FixedUpdate()
    {
        if (GameManager.instance != null && GameManager.instance.isGameOver)
        {
            if (backWheel != null) backWheel.useMotor = false;
            if (frontWheel != null) frontWheel.useMotor = false;
            return;
        }

        // Force input to 0 if we are out of gas, ignoring player/mobile controls
        if (!hasFuel)
        {
            movementInput = 0f;
        }

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
                else
                {
                    frontWheel.useMotor = false;
                }
            }
        }
        else
        {
            // ENGINE BRAKING / COASTING FRICTION
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

        carRigidbody.AddTorque(-movementInput * rotationSpeed * Time.fixedDeltaTime, ForceMode2D.Force);

        if (isNitroActive && hasFuel)
        {
            if (nitroThrusterPoint != null)
            {
                carRigidbody.AddForceAtPosition(transform.right * nitroBoostForce, nitroThrusterPoint.position, ForceMode2D.Force);
            }
            else
            {
                carRigidbody.AddRelativeForce(Vector2.right * nitroBoostForce, ForceMode2D.Force);
            }
        }

        CheckAltitudeForMeme();
    }

    void CheckAltitudeForMeme()
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.down, scaredHeightTrigger);
        bool hitGround = false;

        foreach (var hit in hits)
        {
            if (!hit.collider.isTrigger && !hit.collider.CompareTag("Player"))
            {
                hitGround = true; 
                break;
            }
        }

        if (!hitGround && MemeManager.instance != null)
        {
            MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Scared);
        }
    }
}