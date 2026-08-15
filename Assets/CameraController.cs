using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target to Follow")]
    public Transform target; // This will be your Car

    [Header("Position Settings")]
    public Vector3 offset = new Vector3(0f, 2f, -10f); 
    // Positional smoothing removed to keep the car perfectly locked in the center!

    [Header("Dynamic Zoom Settings")]
    [Tooltip("The normal camera size when stopped or moving slowly.")]
    public float defaultZoom = 7f;
    [Tooltip("How far the camera zooms out at top speed.")]
    public float maxZoomOut = 11f;
    [Tooltip("The speed required to start zooming out.")]
    public float minSpeedForZoom = 15f;
    [Tooltip("The speed required to reach maximum zoom out.")]
    public float maxSpeedForZoom = 35f;
    [Tooltip("How smoothly the camera transitions between zoom levels.")]
    public float zoomSmoothSpeed = 3f;

    [HideInInspector] public bool isCutsceneActive = false; // --- NEW: Lets other scripts take control! ---
    [HideInInspector] public bool eventZoomActive = false;  // --- NEW: For Meteorites to pull the camera back! ---
    [HideInInspector] public float eventZoomTarget = 15f; 

    private Camera cam;
    private Rigidbody2D targetRb;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (isCutsceneActive) return; // --- NEW: Stop strict tracking during teleports! ---

        // Check if we actually have a target
        if (target != null)
        {
            // 1. STRICT POSITION LOCK
            // Directly apply the position without Lerp so the car never drives off-screen
            transform.position = target.position + offset;

            // 2. DYNAMIC SPEED ZOOM
            if (cam != null)
            {
                // Grab the Rigidbody2D from the target if we haven't already
                if (targetRb == null)
                {
                    targetRb = target.GetComponent<Rigidbody2D>();
                }

                if (targetRb != null)
                {
                    float targetZoom = defaultZoom;

                    if (eventZoomActive)
                    {
                        // OVERRIDE: Meteorite or event is forcing a massive zoom out!
                        targetZoom = eventZoomTarget;
                    }
                    else
                    {
                        // NORMAL: Get the car's current speed
                        float currentSpeed = targetRb.linearVelocity.magnitude;
                        float speedPercent = Mathf.InverseLerp(minSpeedForZoom, maxSpeedForZoom, currentSpeed);
                        targetZoom = Mathf.Lerp(defaultZoom, maxZoomOut, speedPercent);
                    }

                    // Smoothly transition the camera's orthographic size (zoom)
                    cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomSmoothSpeed * Time.deltaTime);
                }
            }
        }
    }
}