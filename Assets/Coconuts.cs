using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Coconut : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip bonkSound;
    private AudioSource audioSource;
    private bool hasBonked = false;

    [Header("Sand Physics (Game Feel)")]
    [Tooltip("How far the coconut sinks into the sand when driven over")]
    public float sinkDepth = 0.4f;
    [Tooltip("How fast it pushes into the sand")]
    public float sinkSpeed = 6f;
    
    private bool isSinking = false;

    void Start()
    {
        // Safe debris!
        gameObject.tag = "Player";
        
        if (bonkSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isSinking) return; // Only allow it to sink once!

        CarController hitCar = collision.gameObject.GetComponentInParent<CarController>();
        
        if (hitCar != null)
        {
            if (!hasBonked)
            {
                hasBonked = true;
                if (audioSource != null && bonkSound != null)
                {
                    audioSource.pitch = Random.Range(0.8f, 1.2f);
                    audioSource.PlayOneShot(bonkSound, 0.8f);
                }
            }

            // The moment the car's tire hits it, start burying it in the sand!
            StartCoroutine(SinkIntoSandRoutine());
        }
    }

    IEnumerator SinkIntoSandRoutine()
    {
        isSinking = true;

        // 1. Freeze the physics engine for this object so it stops rolling away
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic; 
        }

        // (We purposely leave the Collider2D enabled here so it acts as a speed bump in the sand!)

        // 2. Smoothly animate it pushing down into the sand
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos - new Vector3(0f, sinkDepth, 0f);
        
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * sinkSpeed;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null; // Wait for the next frame
        }
    }
}