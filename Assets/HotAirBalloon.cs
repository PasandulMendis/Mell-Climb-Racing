using UnityEngine;

public class HotAirBalloon : MonoBehaviour
{
    [Header("Reactions")]
    public float floatUpSpeed = 10f;
    public float crashDownSpeed = 15f;
    
    [Header("Idle Bobbing")]
    public float bobAmount = 1f;
    public float bobSpeed = 2f;

    private bool isHit = false;
    private int hitState = 0; // 0 = Idle, 1 = Floating Up, 2 = Crashing Down
    private float startY;
    private float randomOffset;

    void Start()
    {
        startY = transform.position.y;
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (!isHit)
        {
            // Gentle idle bobbing up and down
            float newY = startY + Mathf.Sin((Time.time + randomOffset) * bobSpeed) * bobAmount;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
        else if (hitState == 1) // Hit the Basket! (Floats Up)
        {
            transform.position += Vector3.up * floatUpSpeed * Time.deltaTime;
        }
        else if (hitState == 2) // Hit the Air Bag! (Crashes Down)
        {
            transform.position += Vector3.down * crashDownSpeed * Time.deltaTime;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isHit) return; // Only react to the very first hit

        CarController car = collision.gameObject.GetComponentInParent<CarController>();
        if (car != null)
        {
            isHit = true;

            // Check the specific name of the collider we just hit!
            if (collision.otherCollider.gameObject.name.Contains("Basket"))
            {
                // We hit the basket!
                hitState = 1;
                if (MemeManager.instance != null) MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Happy);
            }
            else 
            {
                // We hit the Air Bag!
                hitState = 2;
                
                // Turn off ALL colliders so it falls completely through the ground
                Collider2D[] allColliders = GetComponentsInChildren<Collider2D>();
                foreach (Collider2D col in allColliders) col.enabled = false;

                // Force it into the extreme background so it passes behind the dirt
                SpriteRenderer[] allSprites = GetComponentsInChildren<SpriteRenderer>();
                foreach (SpriteRenderer sr in allSprites) sr.sortingOrder = -20;

                // Scared meme because we just destroyed a hot air balloon!
                if (MemeManager.instance != null) MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Scared, true, false);
            }
            
            // Clean up the object eventually so it doesn't fall/float forever
            Destroy(gameObject, 5f);
        }
    }
}