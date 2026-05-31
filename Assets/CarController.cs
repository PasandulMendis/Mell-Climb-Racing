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