using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target to Follow")]
    public Transform target; // This will be your Car

    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0f, 2f, -10f); // How far away the camera sits
    public float smoothSpeed = 5f; // How smoothly the camera catches up

    void LateUpdate()
    {
        // Check if we actually have a target (prevents errors if the car is destroyed)
        if (target != null)
        {
            // Calculate where the camera *wants* to be
            Vector3 desiredPosition = target.position + offset;

            // Use Vector3.Lerp to smoothly glide from current position to desired position
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            // Apply the new position
            transform.position = smoothedPosition;
        }
    }
}