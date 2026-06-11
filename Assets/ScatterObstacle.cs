using UnityEngine;
using System.Collections.Generic; // Required for using Lists

// This forces Unity to require a Rigidbody2D on whatever you attach this to!
[RequireComponent(typeof(Rigidbody2D))]
public class ScatterObstacle : MonoBehaviour
{
    [Header("Impact Polish (AAA Juice)")]
    [Tooltip("How hard does the car need to hit this to trigger sounds/particles?")]
    public float heavyImpactThreshold = 5f;
    
    [Tooltip("Particle system (e.g. wood splinters or rock dust) to play on heavy hit")]
    public ParticleSystem impactParticles;
    
    [Tooltip("Sound of wood crunching or rocks smashing")]
    public AudioClip heavyImpactSound;
    [Tooltip("Light tapping sound for small collisions")]
    public AudioClip lightImpactSound;

    [Header("Destruction & Shatter")]
    [Tooltip("Should the object completely break and disappear on heavy impact?")]
    public bool destroyOnHeavyImpact = false;
    [Tooltip("Particle system for physical debris chunks (e.g. broken log pieces)")]
    public ParticleSystem debrisParticles;

    [Header("Meme Integration")]
    [Tooltip("If the car hits this very hard, should we trigger the Scared meme?")]
    public bool triggerScaredMemeOnImpact = true;

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private bool hasBeenHitHard = false; // Prevents the meme/sound from triggering 100 times in a row

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // FIX: Tag this object as "Player" so it acts as "safe debris" and doesn't trigger the Roof Game Over!
        gameObject.tag = "Player";

        // Optional: Ensure mass is reasonably heavy so the car has to plow through it
        if (rb.mass < 0.5f) rb.mass = 2f; 

        // Auto-setup AudioSource
        if (heavyImpactSound != null || lightImpactSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 2D Sound
            audioSource.playOnAwake = false;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Only react to the player's car.
        // Since the obstacle is also tagged "Player" now, we check for CarController so logs don't trigger each other!
        CarController hitCar = collision.gameObject.GetComponentInParent<CarController>();
        
        if (collision.gameObject.CompareTag("Player") && hitCar != null)
        {
            // Calculate the impact force
            float impactVelocity = collision.relativeVelocity.magnitude;

            if (impactVelocity >= heavyImpactThreshold)
            {
                // --- HEAVY IMPACT (AAA CRUNCH) ---
                if (!hasBeenHitHard)
                {
                    hasBeenHitHard = true; // Only trigger the big reaction once per obstacle

                    if (triggerScaredMemeOnImpact && MemeManager.instance != null)
                    {
                        MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Scared, true, false);
                    }

                    if (destroyOnHeavyImpact)
                    {
                        // 1. Play Impact Dust/Splinters (Spawn a clone so we don't destroy the asset!)
                        if (impactParticles != null)
                        {
                            ParticleSystem impactClone = Instantiate(impactParticles, transform.position, Quaternion.identity);
                            impactClone.Play();
                            Destroy(impactClone.gameObject, 3f); // Clean up the clone later
                        }

                        // 2. Play Physical Debris Particles (Spawn a clone!)
                        if (debrisParticles != null)
                        {
                            ParticleSystem debrisClone = Instantiate(debrisParticles, transform.position, Quaternion.identity);
                            debrisClone.Play();
                            Destroy(debrisClone.gameObject, 4f); // Give debris time to fall and fade
                        }

                        // 3. Play Sound (Create temporary object to play sound since this object is dying)
                        if (heavyImpactSound != null)
                        {
                            GameObject soundObj = new GameObject("ImpactSound");
                            AudioSource tempAudio = soundObj.AddComponent<AudioSource>();
                            tempAudio.clip = heavyImpactSound;
                            tempAudio.spatialBlend = 0f; // 2D
                            tempAudio.pitch = Random.Range(0.8f, 1.2f);
                            tempAudio.volume = 0.8f;
                            tempAudio.Play();
                            Destroy(soundObj, heavyImpactSound.length + 0.1f);
                        }

                        // 4. Destroy the original object!
                        Destroy(gameObject);
                    }
                    else
                    {
                        // --- NORMAL SCATTER BEHAVIOR (Object rolls away but doesn't break) ---
                        if (impactParticles != null)
                        {
                            ParticleSystem impactClone = Instantiate(impactParticles, transform.position, Quaternion.identity);
                            impactClone.Play();
                            Destroy(impactClone.gameObject, 3f);
                        }

                        if (audioSource != null && heavyImpactSound != null)
                        {
                            audioSource.pitch = Random.Range(0.8f, 1.2f); // Pitch variance for realism
                            audioSource.PlayOneShot(heavyImpactSound, 0.8f);
                        }

                        // Optional: Slightly reduce the mass after the initial heavy hit so it scatters easier
                        rb.mass *= 0.8f; 
                    }
                }
            }
            else if (impactVelocity > 1f && !hasBeenHitHard)
            {
                // --- LIGHT IMPACT (Bumping into it slowly) ---
                if (audioSource != null && lightImpactSound != null && !audioSource.isPlaying)
                {
                    audioSource.pitch = Random.Range(0.9f, 1.1f);
                    audioSource.PlayOneShot(lightImpactSound, 0.3f);
                }
            }
        }
    }
}