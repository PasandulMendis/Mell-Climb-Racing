using UnityEngine;

    [RequireComponent(typeof(Rigidbody2D))]
    public class Seagull : MonoBehaviour
    {
        [Header("Seagull Settings")]
        public float minFlySpeed = 12f;
        public float maxFlySpeed = 22f;
        public float turnSpeed = 3f;
        public float wakeUpDistance = 80f;
        [Tooltip("How close it needs to get to start stealing")]
        public float stealRadius = 4f;
        
        [Header("Flight Path (Wobble)")]
        [Tooltip("How wide the bird weaves left and right as it dives")]
        public float pathWobbleAmount = 3f;
        [Tooltip("How fast it weaves")]
        public float pathWobbleSpeed = 2f;
        
        public AudioClip seagullCry;

        [Header("Steal Mechanics (Over Time)")]
        public int coinsPerStealTick = 5;
        [Tooltip("How long the seagull stays hovering over the car stealing coins (seconds)")]
        public float stealDuration = 9f;
        [Tooltip("How often a steal tick occurs while stealing (seconds)")]
        public float timeBetweenSteals = 1f;
        [Tooltip("How high above the car it hovers while stealing")]
        public float stealHoverHeight = 2.5f;

        [Header("Flight & Fleeing")]
        [Tooltip("How fast it shoots up into the sky after stealing is finished")]
        public float fleeSpeed = 40f;
        
        [Header("UI Feedback")]
        [Tooltip("Drag your Floating Text prefab here")]
        public GameObject floatingTextPrefab;

        private Transform playerCar;
        private Rigidbody2D rb;
        private bool isAwake = false;
        private bool isStealing = false;
        private bool isFleeing = false;
        
        private float stealTimer = 0f;
        private float nextStealTick = 0f;
        
        private float currentFlySpeed;
        private float randomWobbleOffset;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            // Float in the sky with 0 gravity until the player gets close!
            rb.gravityScale = 0f;
            
            // Assign random flight speeds and wobble patterns for each individual bird!
            currentFlySpeed = Random.Range(minFlySpeed, maxFlySpeed);
            randomWobbleOffset = Random.Range(0f, 100f);
            
            if (GameManager.instance != null) playerCar = GameManager.instance.playerCar;

            if (seagullCry != null)
            {
                AudioSource audio = gameObject.AddComponent<AudioSource>();
                audio.spatialBlend = 0f;
                audio.pitch = Random.Range(0.9f, 1.2f);
                audio.PlayOneShot(seagullCry, 0.7f);
            }
        }

        void FixedUpdate()
        {
            if (playerCar == null || isFleeing) return;

            // Wake up if the car gets close
            if (!isAwake)
            {
                if (Vector2.Distance(transform.position, playerCar.position) < wakeUpDistance)
                {
                    isAwake = true;
                }
                return;
            }

            // --- STATE: STEALING OVER TIME ---
            if (isStealing)
            {
                // Hover slightly above the car
                Vector2 targetPos = (Vector2)playerCar.position + new Vector2(0, stealHoverHeight);
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.fixedDeltaTime * 10f);

                // Level out rotation to match the car while hovering
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, playerCar.eulerAngles.z), Time.fixedDeltaTime * 5f);
                
                // Ensure it continues to face the correct way based on its flip
                if (playerCar.GetComponent<Rigidbody2D>().linearVelocity.x > 0) transform.localScale = new Vector3(-1, 1, 1);
                else transform.localScale = new Vector3(1, 1, 1);

                // Handle the Stealing Timer
                stealTimer += Time.fixedDeltaTime;
                
                if (stealTimer >= nextStealTick)
                {
                    PerformStealTick();
                    nextStealTick += timeBetweenSteals;
                }

                // If we've finished stealing, start fleeing
                if (stealTimer >= stealDuration)
                {
                    StartFleeing();
                    return;
                }

                return; // while stealing, skip homing logic
            }

            // --- STATE: HOMING MISSILE (WITH RANDOM WOBBLE) ---
            if (Vector2.Distance(transform.position, playerCar.position) <= stealRadius)
            {
                StartStealing();
                return;
            }

            Vector2 direction = (playerCar.position - transform.position).normalized;
            
            // Calculate a perpendicular vector (90 degrees off the main direction)
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            
            // Use a sine wave to push the bird side-to-side dynamically
            float wobble = Mathf.Sin((Time.time + randomWobbleOffset) * pathWobbleSpeed) * pathWobbleAmount;
            
            // Blend the direct path with the wobble path!
            Vector2 wobbledDirection = (direction + (perpendicular * wobble)).normalized;
            Vector2 desiredVelocity = wobbledDirection * currentFlySpeed;
            
            // Smoothly steer towards the car using the wobbled velocity
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desiredVelocity, Time.fixedDeltaTime * turnSpeed);

            // --- VISUAL POLISH: ROTATING TO FACE TARGET ---
            // Because your sprite is drawn facing West (-X) by default:
            if (rb.linearVelocity.x > 0) // Moving East (Right)
            {
                transform.localScale = new Vector3(-1, 1, 1); // Flip horizontally to face right
                float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, angle), Time.fixedDeltaTime * 8f);
            }
            else // Moving West (Left)
            {
                transform.localScale = new Vector3(1, 1, 1); // Keep normal facing
                float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
                // Subtract 180 to align the West-facing sprite with the dive angle
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, angle - 180f), Time.fixedDeltaTime * 8f);
            }
        }

        void StartStealing()
        {
            if (isStealing) return;
            isStealing = true;
            
            stealTimer = 0f;
            nextStealTick = 0f; // Force an immediate steal on the very first frame!

            // Disable colliders so it doesn't bump the car while hovering
            Collider2D[] cols = GetComponentsInChildren<Collider2D>();
            foreach (Collider2D col in cols) col.enabled = false;
        }

        void PerformStealTick()
        {
            if (GameManager.instance != null && GameManager.instance.totalCoins >= coinsPerStealTick)
            {
                GameManager.instance.AddCoins(-coinsPerStealTick);
                
                if (floatingTextPrefab != null)
                {
                    // Spawn the text slightly higher so it doesn't overlap the bird
                    GameObject textObj = Instantiate(floatingTextPrefab, transform.position + new Vector3(0, 1.5f, 0), Quaternion.identity);
                    FloatingText ft = textObj.GetComponent<FloatingText>();
                    if (ft != null) ft.Setup("-" + coinsPerStealTick.ToString(), Color.red);
                }

                if (MemeManager.instance != null) MemeManager.instance.TriggerMeme(MemeManager.MemeEvent.Angry, true, false);
            }
        }

        void StartFleeing()
        {
            isFleeing = true;
            isStealing = false;

            // Fly straight up into the sky incredibly fast!
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(0f, fleeSpeed); 

            // Point straight up
            transform.rotation = Quaternion.Euler(0, 0, transform.localScale.x == 1 ? -90f : 90f);

            // Destroy after 3 seconds once it is off-screen
            Destroy(gameObject, 3f);
        }
    }