using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FallingMeteorite : MonoBehaviour
{
    [Header("Fall & Trajectory")]
    [Tooltip("How close the car must be (horizontal distance) to trigger the fall")]
    public float triggerDistance = 60f;
    [Tooltip("How fast it falls downward")]
    public float fallSpeedY = 35f;
    [Tooltip("Horizontal drift. Negative values make it fall diagonally backwards towards the player!")]
    public float fallSpeedX = -15f;
    
    [Header("Cinematic Camera")]
    [Tooltip("How far the camera pulls back when the meteorite is falling")]
    public float cinematicZoomLevel = 15f;

    [Header("Shatter Physics")]
    [Tooltip("How violently the small child meteorites explode outward on impact")]
    public float scatterExplosionForce = 25f;
    [Tooltip("Should the scattered pieces float like moon rocks? (Zero Gravity)")]
    public bool scatteredPiecesFloat = true;

    [Header("Effects")]
    public ParticleSystem explosionParticles;
    public AudioClip explosionSound;

    [Header("Custom Meme Integration")]
    [Tooltip("Triggered when the meteorite spots the car and starts falling")]
    public string memeCategoryAppear = "Meteorite_Appear";
    [Tooltip("Triggered when the meteorite hits the ground or the car")]
    public string memeCategoryExplode = "Meteorite_Explode";

    [Header("Optimization")]
    [Tooltip("How far behind the car before the unexploded meteorite auto-deletes")]
    public float deleteDistance = 200f;
    [Tooltip("Max absolute distance to prevent memory leaks if it falls into the void")]
    public float maxSafeDistance = 400f;

    private Rigidbody2D rb;
    private Transform playerCar;
    private bool isFalling = false;
    private bool hasImpacted = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; // Lock it in the sky!
        
        // Prevent children from instantly colliding with the parent!
        foreach (Transform child in transform)
        {
            Rigidbody2D childRb = child.GetComponent<Rigidbody2D>();
            if (childRb != null)
            {
                childRb.simulated = false; // Completely disable physics until impact
            }
        }
    }

    void Update()
    {
        if (hasImpacted) return; // Removed 'isFalling' from here so it can check distance while falling!

        if (playerCar == null)
        {
            if (GameManager.instance != null && GameManager.instance.playerCar != null)
            {
                playerCar = GameManager.instance.playerCar;
            }
            else
            {
                GameObject car = GameObject.FindGameObjectWithTag("Player");
                if (car != null) playerCar = car.transform;
            }
            return;
        }

        // --- OPTIMIZATION: Auto-Delete if left behind or falls into the void ---
        if (playerCar.position.x - transform.position.x > deleteDistance)
        {
            // The main shell is destroyed, taking all the child scatters with it safely
            Destroy(gameObject);
            return;
        }
        else if (Vector2.Distance(playerCar.position, transform.position) > maxSafeDistance)
        {
            // Fallback: If it fell extremely deep straight down through the deleted chunks
            Destroy(gameObject);
            return;
        }

        if (isFalling) return; // Now we exit if falling, AFTER the distance check has run

        // Check horizontal distance to the car
        if (Mathf.Abs(playerCar.position.x - transform.position.x) < triggerDistance)
        {
            StartFalling();
        }
    }

    void StartFalling()
    {
        isFalling = true;
        
        // Unfreeze the rigid body and manually set its velocity for a perfect diagonal strike
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = new Vector2(fallSpeedX, -fallSpeedY);

        // Tell the camera to zoom out!
        if (Camera.main != null)
        {
            CameraController camControl = Camera.main.GetComponent<CameraController>();
            if (camControl != null)
            {
                camControl.eventZoomTarget = cinematicZoomLevel;
                camControl.eventZoomActive = true;
            }
        }
        // --- NEW: Trigger the Appearance Meme! ---
        if (MemeManager.instance != null && !string.IsNullOrEmpty(memeCategoryAppear))
        {
            MemeManager.instance.TriggerReaction(memeCategoryAppear);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasImpacted) return;
        hasImpacted = true;

        // 1. Release the Children!
        // We put them in an array first so we can safely unparent them without breaking the loop
        Transform[] children = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++) children[i] = transform.GetChild(i);

        foreach (Transform child in children)
        {
            child.SetParent(null); // Unparent so they don't get deleted when the big rock dies
            
            Rigidbody2D childRb = child.GetComponent<Rigidbody2D>();
            if (childRb != null)
            {
                childRb.simulated = true; // Turn physics back on!
                
                if (scatteredPiecesFloat) childRb.gravityScale = 0f; // Act like floating moon rocks
                
                // Explode them outward randomly
                Vector2 explosionDir = new Vector2(Random.Range(-1f, 1f), Random.Range(0.2f, 1f)).normalized;
                childRb.AddForce(explosionDir * scatterExplosionForce, ForceMode2D.Impulse);
                childRb.AddTorque(Random.Range(-100f, 100f));
            }
            
            // ADDED: Attach the new cleanup script to scattered pieces so they don't live in memory forever
            if (child.gameObject.GetComponent<DebrisDistanceDestroyer>() == null)
            {
                child.gameObject.AddComponent<DebrisDistanceDestroyer>();
            }
        }

        // 2. Restore the Camera
        if (Camera.main != null)
        {
            CameraController camControl = Camera.main.GetComponent<CameraController>();
            if (camControl != null) camControl.eventZoomActive = false;
        }

        // 3. Play AAA Effects
        if (explosionParticles != null) Instantiate(explosionParticles, transform.position, Quaternion.identity);
        
        if (explosionSound != null)
        {
            GameObject soundObj = new GameObject("MeteoriteSound");
            AudioSource tempAudio = soundObj.AddComponent<AudioSource>();
            tempAudio.clip = explosionSound;
            tempAudio.pitch = Random.Range(0.8f, 1.1f);
            tempAudio.Play();
            Destroy(soundObj, 3f);
        }

        // 4. Trigger the Custom Meme! (Replaced the old static Scared enum)
        if (MemeManager.instance != null && !string.IsNullOrEmpty(memeCategoryExplode))
        {
            MemeManager.instance.TriggerReaction(memeCategoryExplode);
        }

        // 5. Destroy the big shell
        Destroy(gameObject);
    }
}

// We include a small, dedicated class just for the meteorite debris since it doesn't need 
// all the extra initialization that the FloatingMoonRock script uses.
public class DebrisDistanceDestroyer : MonoBehaviour
{
    public float deleteDistance = 120f;
    public float maxSafeDistance = 250f;
    private Transform playerCar;

    void Update()
    {
        if (playerCar == null)
        {
            if (GameManager.instance != null && GameManager.instance.playerCar != null)
            {
                playerCar = GameManager.instance.playerCar;
            }
            else
            {
                GameObject car = GameObject.FindGameObjectWithTag("Player");
                if (car != null && car != this.gameObject) playerCar = car.transform;
            }
            return;
        }

        if (playerCar.position.x - transform.position.x > deleteDistance)
        {
            Destroy(gameObject);
        }
        else if (Vector2.Distance(playerCar.position, transform.position) > maxSafeDistance)
        {
            Destroy(gameObject);
        }
    }
}