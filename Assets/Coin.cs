using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Coin Properties")]
    public int coinValue = 5; 

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
        if (isMagnetized && !isFlyingToUI && playerTransform != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, magnetSpeed * Time.deltaTime);
        }
        
        else if (isFlyingToUI && targetUI_Icon != null && mainCam != null)
        {
            Vector3 screenTarget = targetUI_Icon.position; 
            screenTarget.z = Mathf.Abs(mainCam.transform.position.z - transform.position.z);
            Vector3 worldTarget = mainCam.ScreenToWorldPoint(screenTarget);

            transform.position = Vector3.MoveTowards(transform.position, worldTarget, uiFlySpeed * Time.deltaTime);
            
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * shrinkTargetScale, Time.deltaTime * 10f);

            if (Vector3.Distance(transform.position, worldTarget) < 0.5f)
            {
                FinishCollection();
            }
        }
    }

    public void MagnetizeTo(Transform player, RectTransform uiIcon)
    {
        if (isMagnetized || isFlyingToUI) return; 

        playerTransform = player;
        
        if (targetUI_Icon == null) targetUI_Icon = uiIcon;
        
        isMagnetized = true;

        transform.SetParent(null);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isFlyingToUI) return; 

        if (other.CompareTag("Player") || other.GetComponentInParent<CarController>() != null) 
        {
            if (coinCollider != null) coinCollider.enabled = false;
            
            isMagnetized = false;
            isFlyingToUI = true;
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