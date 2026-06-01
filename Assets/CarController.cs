using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    [Header("Car Components")]
    public Rigidbody2D carRigidbody;
    public WheelJoint2D backWheel;
    public WheelJoint2D frontWheel;

    [Header("Car Stats (Upgradable later)")]
    public float speed = 1500f;       // Engine power
    public float rotationSpeed = 250f; // How fast you flip in the air

    [Header("Nitro System")]
    public float nitroBoostForce = 15000f; // The physical ROCKET push force!
    public float nitroDuration = 2.0f;     // Lasts for 2 seconds
    public Transform nitroThrusterPoint; // NEW: Move this in Unity to balance the car!
    private int currentNitro;
    private bool isNitroActive = false;
    private float nitroTimer = 0f;

    private float movementInput;

    void Start()
    {
        // 1. Read Upgrades from Save Data (Now checking Equipped levels!)
        int maxEngine = PlayerPrefs.GetInt("EngineLevel", 1);
        int engineLevel = PlayerPrefs.GetInt("EquippedEngineLevel", maxEngine);

        int maxSus = PlayerPrefs.GetInt("SuspensionLevel", 1);
        int suspensionLevel = PlayerPrefs.GetInt("EquippedSuspensionLevel", maxSus);

        int maxTire = PlayerPrefs.GetInt("TireLevel", 1);
        int tireLevel = PlayerPrefs.GetInt("EquippedTireLevel", maxTire);

        // 2. Apply Engine Upgrade
        speed = 1500f + ((engineLevel - 1) * 200f);

        // 3. Apply Suspension Upgrade
        float newFrequency = 3f + ((suspensionLevel - 1) * 0.5f);
        JointSuspension2D backSus = backWheel.suspension;
        backSus.frequency = newFrequency;
        backWheel.suspension = backSus;
        JointSuspension2D frontSus = frontWheel.suspension;
        frontSus.frequency = newFrequency;
        frontWheel.suspension = frontSus;

        // 4. Apply Tire Upgrade
        CircleCollider2D backCol = backWheel.GetComponent<CircleCollider2D>();
        CircleCollider2D frontCol = frontWheel.GetComponent<CircleCollider2D>();
        
        if (backCol != null && backCol.sharedMaterial != null)
        {
            PhysicsMaterial2D tireMat = new PhysicsMaterial2D("UpgradedGrip");
            tireMat.friction = 1.5f + ((tireLevel - 1) * 0.3f);
            tireMat.bounciness = 0f;
            
            backCol.sharedMaterial = tireMat;
            frontCol.sharedMaterial = tireMat;
        }

        // 5. Setup Nitro
        currentNitro = PlayerPrefs.GetInt("NitroAmount", 0);
    }

    void Update()
    {
        // Stop input if game is over
        if (GameManager.instance != null && GameManager.instance.isGameOver)
        {
            movementInput = 0f;
            return;
        }

        // 1. Get input
        movementInput = 0f;
        
        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                movementInput = 1f; // Gas / Frontflip
            }
            else if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                movementInput = -1f; // Brake / Backflip
            }

            // Nitro Input (Shift Key)
            if (Keyboard.current.shiftKey.wasPressedThisFrame && currentNitro > 0 && !isNitroActive)
            {
                ActivateNitro();
            }
        }

        // Nitro Timer Logic
        if (isNitroActive)
        {
            nitroTimer -= Time.deltaTime;
            if (nitroTimer <= 0)
            {
                DeactivateNitro();
            }
        }
    }

    void ActivateNitro()
    {
        isNitroActive = true;
        currentNitro--; 
        
        PlayerPrefs.SetInt("NitroAmount", currentNitro); 
        PlayerPrefs.Save();

        nitroTimer = nitroDuration;

        // TRIGGER HAPPY/HYPE MEME!
        if (MemeManager.instance != null)
        {
            MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Happy, nitroDuration);
        }

        // Update Game UI
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
        // Stop physics if game is over
        if (GameManager.instance != null && GameManager.instance.isGameOver)
        {
            backWheel.useMotor = false;
            frontWheel.useMotor = false;
            return;
        }

        // 2. Wheel Motors (Driving)
        if (movementInput != 0)
        {
            JointMotor2D motor = new JointMotor2D();
            motor.motorSpeed = movementInput * speed * -1f; 
            motor.maxMotorTorque = 10000f;

            backWheel.motor = motor;
            frontWheel.motor = motor;
            
            backWheel.useMotor = true;
            frontWheel.useMotor = true;
        }
        else
        {
            backWheel.useMotor = false;
            frontWheel.useMotor = false;
        }

        // 3. Air Rotation (Flipping)
        carRigidbody.AddTorque(-movementInput * rotationSpeed * Time.fixedDeltaTime, ForceMode2D.Force);

        // 4. THE NEW ROCKET NITRO!
        if (isNitroActive)
        {
            if (nitroThrusterPoint != null)
            {
                // Applies the forward force exactly at your custom point to prevent nosedives!
                carRigidbody.AddForceAtPosition(transform.right * nitroBoostForce, nitroThrusterPoint.position, ForceMode2D.Force);
            }
            else
            {
                // Fallback to center of mass if no point is assigned
                carRigidbody.AddRelativeForce(Vector2.right * nitroBoostForce, ForceMode2D.Force);
            }
        }
    }
}