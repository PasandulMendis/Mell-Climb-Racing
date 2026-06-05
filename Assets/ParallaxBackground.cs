using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxBackground : MonoBehaviour
{
    [Header("Camera Reference")]
    public Transform cam;

    [Header("Parallax Settings")]
    [Tooltip("1 = Moves perfectly with camera (Sky). 0.5 = Moves half speed (Mountains). 0 = Stays still (Ground/Road).")]
    public float parallaxEffect;

    [Header("Infinite Looping Bounds")]
    [Tooltip("Increase this if you see trees/mountains popping in on the edges of wide screens. 2 or 3 is ideal.")]
    public int cloneCount = 3;

    [Header("Vertical Scrolling")]
    [Tooltip("Should the background follow the camera vertically so you don't climb above the mountains/sky?")]
    public bool followCameraY = true;
    [Tooltip("How much the background moves with the camera vertically. Set to 1.0 for Sky so it never sinks!")]
    [Range(0f, 1f)]
    public float verticalParallaxEffect = 0.8f;

    private float length;
    private float startpos;
    private float startposY;

    void Start()
    {
        // Auto-find the main camera if one wasn't assigned
        if (cam == null) 
        {
            cam = Camera.main.transform;
        }
        
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        
        // Calculate size using local sprite bounds to avoid scale distortion bugs
        length = sr.sprite.bounds.size.x;
        
        startpos = transform.position.x;
        startposY = transform.position.y;

        // Auto-Clone: Spawns multiple clones on BOTH sides to prevent edge-popping on wide screens
        for (int i = 1; i <= cloneCount; i++)
        {
            CloneSprite(sr, length * i);
            CloneSprite(sr, -length * i);
        }
    }

    void CloneSprite(SpriteRenderer original, float offset)
    {
        GameObject clone = new GameObject(original.gameObject.name + "_Clone");
        // Attach the clone to this object so it moves with it
        clone.transform.parent = this.transform; 
        
        // Setting localPosition matches the scale of the parent perfectly
        clone.transform.localPosition = new Vector3(offset, 0, 0);
        clone.transform.localScale = Vector3.one;
        clone.transform.localRotation = Quaternion.identity;
        
        // Copy all visual render settings from your original Sprite
        SpriteRenderer cloneSr = clone.AddComponent<SpriteRenderer>();
        cloneSr.sprite = original.sprite;
        cloneSr.color = original.color;
        cloneSr.sortingLayerID = original.sortingLayerID;
        cloneSr.sortingOrder = original.sortingOrder;
        cloneSr.drawMode = original.drawMode;
        cloneSr.size = original.size;
    }

    // Using LateUpdate ensures our background coordinates are calculated AFTER the camera controller moves.
    void LateUpdate()
    {
        if (cam == null) return;

        // Calculate the actual horizontal span of one sprite chunk in world space
        float spriteScaledLength = length * transform.localScale.x;

        // --- THE UNBIASED STATELESS PARALLAX FORMULA ---
        // Instead of shifting a 'startpos' variable inside frame-delayed 'if' blocks 
        // (which causes jittering when camera speeds change), we mathematically 
        // compute the exact seamless offset using continuous division.
        
        float dist = cam.position.x * parallaxEffect;
        float temp = cam.position.x * (1 - parallaxEffect);

        float offset = 0f;
        if (parallaxEffect < 0.99f) // Skip looping calculations for the Sky (parallax effect ~ 1)
        {
            // Floor division guarantees zero floating-point drifting between frames
            offset = Mathf.Floor(temp / spriteScaledLength) * spriteScaledLength;
        }

        // Calculate vertical position so background layers track the camera's height
        float targetY = transform.position.y;
        if (followCameraY)
        {
            float distY = (cam.position.y * verticalParallaxEffect);
            targetY = startposY + distY;
        }

        // Apply position continuously
        transform.position = new Vector3(startpos + offset + dist, targetY, transform.position.z);
    }
}