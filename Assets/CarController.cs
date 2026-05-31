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

    private float movementInput;

    void Start()
    {
        // 1. Read Upgrades from Save Data
        int engineLevel = PlayerPrefs.GetInt("EngineLevel", 1);
        int suspensionLevel = PlayerPrefs.GetInt("SuspensionLevel", 1);
        int tireLevel = PlayerPrefs.GetInt("TireLevel", 1);

        // 2. Apply Engine Upgrade
        speed = 1500f + ((engineLevel - 1) * 200f);

        // 3. Apply Suspension Upgrade (Stiffens the bouncy springs)
        float newFrequency = 3f + ((suspensionLevel - 1) * 0.5f);
        
        JointSuspension2D backSus = backWheel.suspension;
        backSus.frequency = newFrequency;
        backWheel.suspension = backSus;

        JointSuspension2D frontSus = frontWheel.suspension;
        frontSus.frequency = newFrequency;
        frontWheel.suspension = frontSus;

        // 4. Apply Tire Upgrade (Creates a copy of the friction material and boosts grip)
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
    }

    void Update()
    {
        // Stop input if game is over
        if (GameManager.instance != null && GameManager.instance.isGameOver)
        {
            movementInput = 0f;
            return;
        }

        // 1. Get input using the New Input System (Keyboard for testing)
        // We will change this to on-screen UI buttons later for mobile!
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
        }
    }

    void FixedUpdate()
    {
        // Stop motors if game is over
        if (GameManager.instance != null && GameManager.instance.isGameOver)
        {
            backWheel.useMotor = false;
            frontWheel.useMotor = false;
            return;
        }

        // 2. Wheel Motors (Driving)
        if (movementInput != 0)
        {
            // Unity's motor speed is reversed (negative moves right), so we multiply by -1
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
            // Turn off motor so it rolls naturally when no buttons are pressed
            backWheel.useMotor = false;
            frontWheel.useMotor = false;
        }

        // 3. Air Rotation (Flipping)
        // This applies rotational force to the car body. 
        carRigidbody.AddTorque(-movementInput * rotationSpeed * Time.fixedDeltaTime, ForceMode2D.Force);
    }
}