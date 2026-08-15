using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Coin Properties")]
    public int coinValue = 5; 

    [Header("Audio")]
    public AudioClip collectSound;

    [Header("Magnetism Settings")]
    [Tooltip("How fast the coin sucks towards the car initially")]
    public float magnetSpeed = 15f;

    [Header("UI Animation Settings")]
    [Tooltip("How fast the coin flies from the car to the UI icon")]
    [Range(10f, 100f)]
    public float uiFlySpeed = 25f;
    [Tooltip("How much the coin shrinks as it flies to the UI")]
    public float shrinkTargetScale = 0.3f;

    private Transform playerTransform;
    private RectTransform targetUI_Icon;
    private Camera mainCam;
    
    // State Tracking
    private bool isMagnetized = false;
    private bool isFlyingToUI = false;
    private Collider2D coinCollider;

    void Start()
    {
        coinCollider = GetComponent<Collider2D>();
        mainCam = Camera.main;
        
        // --- THE FIX: ALWAYS KNOW WHERE THE UI IS ---
        // This completely prevents the "Zombie Coin" freeze bug!
        GameObject uiIconObj = GameObject.Find("CoinIcon");
        if (uiIconObj != null)
        {
            targetUI_Icon = uiIconObj.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogWarning("Coin: Could not find 'CoinIcon' in the scene! Make sure your UI image is named exactly this.");
        }

        isMagnetized = false;
        isFlyingToUI = false;
    }

    void Update()
    {
        // State 1: Sucking towards the car
        if (isMagnetized && !isFlyingToUI && playerTransform != null)
        {
            // Move directly towards the car's center
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, magnetSpeed * Time.deltaTime);
        }
        
        // State 2: Flying up to the UI Coin Icon
        else if (isFlyingToUI && targetUI_Icon != null && mainCam != null)
        {
            // Dynamically calculate the UI position every single frame. 
            // If you move the UI on the screen, the coins will perfectly curve to follow it!
            Vector3 screenTarget = targetUI_Icon.position; 
            screenTarget.z = Mathf.Abs(mainCam.transform.position.z - transform.position.z);
            Vector3 worldTarget = mainCam.ScreenToWorldPoint(screenTarget);

            // Fly towards the UI position
            transform.position = Vector3.MoveTowards(transform.position, worldTarget, uiFlySpeed * Time.deltaTime);
            
            // Shrink down visually to make it look like it's going into the UI
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * shrinkTargetScale, Time.deltaTime * 10f);

            // Once it reaches the UI icon, actually give the money and destroy the object
            if (Vector3.Distance(transform.position, worldTarget) < 0.5f)
            {
                FinishCollection();
            }
        }
    }

    // Called by the CarController when it drives within the "Magnet Radius"
    public void MagnetizeTo(Transform player, RectTransform uiIcon)
    {
        if (isMagnetized || isFlyingToUI) return; 

        playerTransform = player;
        
        // Only overwrite the target UI if we haven't found it automatically in Start()
        if (targetUI_Icon == null) targetUI_Icon = uiIcon;
        
        isMagnetized = true;

        // Detach from parent (the terrain chunk) so it doesn't get destroyed if the chunk unloads early
        transform.SetParent(null);
    }

    // The physical collision trigger (when the car runs directly over it)
    void OnTriggerEnter2D(Collider2D other)
    {
        if (isFlyingToUI) return; 

        // --- FIXED: Only check for the CarController! Ignore the "Player" tag so livestock can't steal coins! ---
        if (other.GetComponentInParent<CarController>() != null) 
        {
            if (coinCollider != null) coinCollider.enabled = false;
            
            isMagnetized = false;
            isFlyingToUI = true;

            // --- MOVED: Play the SFX instantly upon physical touch! ---
            if (AudioManager.instance != null && collectSound != null)
            {
                AudioManager.instance.PlaySFX(collectSound, 0.7f);
            }

            // --- NEW FIX: Parent to camera so it never gets left behind at high speeds! ---
            if (mainCam != null) transform.SetParent(mainCam.transform, true);
        }
    }

    void FinishCollection()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.AddCoins(coinValue);
        }
        
        Destroy(gameObject);
    }
}