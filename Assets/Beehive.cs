using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Beehive : MonoBehaviour
{
    [Header("Swarm Settings")]
    [Tooltip("The individual Bee prefab to spawn")]
    public GameObject beePrefab;
    [Tooltip("How many bees burst out of the hive!")]
    public int beeCount = 5;

    [Header("Effects")]
    public AudioClip smashSound;
    public ParticleSystem smashParticles;

    private bool isBroken = false;
    private AudioSource audioSource;

    void Start()
    {
        // Force the collider to be a trigger so it doesn't slow the car down AT ALL!
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        // Tag it as player so it doesn't accidentally trigger a roof game-over if placed weirdly
        gameObject.tag = "Player";

        if (smashSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isBroken) return;

        CarController car = collision.GetComponentInParent<CarController>();
        
        if (car != null)
        {
            isBroken = true;

            // 1. Play the crunch!
            if (audioSource != null && smashSound != null)
            {
                // Spawn a temporary sound object so it plays fully even when the hive is destroyed
                GameObject soundObj = new GameObject("HiveSmashSound");
                AudioSource tempAudio = soundObj.AddComponent<AudioSource>();
                tempAudio.clip = smashSound;
                tempAudio.spatialBlend = 0f;
                tempAudio.pitch = Random.Range(0.8f, 1.2f);
                tempAudio.Play();
                Destroy(soundObj, 2f);
            }

            if (smashParticles != null)
            {
                ParticleSystem clone = Instantiate(smashParticles, transform.position, Quaternion.identity);
                clone.Play();
                Destroy(clone.gameObject, 2f);
            }

            // 2. Spawn the Swarm!
            if (beePrefab != null)
            {
                // --- NEW: Find the active Map so we can grab the current Time of Day tint! ---
                ForestMap currentMap = FindObjectOfType<ForestMap>();

                for (int i = 0; i < beeCount; i++)
                {
                    // Add a tiny random offset so they don't all spawn perfectly inside each other
                    Vector3 spawnOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(0f, 2f), 0f);
                    GameObject newBee = Instantiate(beePrefab, transform.position + spawnOffset, Quaternion.identity);
                    
                    // --- NEW: Apply the Night/Dawn/Dusk tint to the newly spawned Bee! ---
                    if (currentMap != null) currentMap.TintObject(newBee);
                }
            }

            // 3. Trigger the Scared Meme!
            if (MemeManager.instance != null) MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Scared, true, false);

            // 4. Destroy the hive instantly so the car drives right through the empty space
            Destroy(gameObject);
        }
    }
}