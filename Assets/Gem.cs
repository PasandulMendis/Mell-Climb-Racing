using UnityEngine;

public class Gem : MonoBehaviour
{
    [Header("Gem Properties")]
    public int gemValue = 1; 

    [Header("Magnetism Settings")]
    public float magnetSpeed = 15f;

    [Header("UI Animation Settings")]
    [Range(10f, 100f)]
    public float uiFlySpeed = 25f;
    public float shrinkTargetScale = 0.3f;

    private Transform playerTransform;
    private RectTransform targetUI_GemIcon; // Specifically looks for the Gem UI
    private Camera mainCam;
    
    private bool isMagnetized = false;
    private bool isFlyingToUI = false;
    private Collider2D gemCollider;

    void Start()
    {
        gemCollider = GetComponent<Collider2D>();
        mainCam = Camera.main;
        
        // Find the purple Gem UI icon on the screen
        GameObject uiIconObj = GameObject.Find("GemIcon");
        if (uiIconObj != null)
        {
            targetUI_GemIcon = uiIconObj.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogWarning("Gem: Could not find 'GemIcon' in the scene! Make sure your UI image is named exactly this.");
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
        else if (isFlyingToUI && targetUI_GemIcon != null && mainCam != null)
        {
            Vector3 screenTarget = targetUI_GemIcon.position; 
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
        if (targetUI_GemIcon == null) targetUI_GemIcon = uiIcon;
        
        isMagnetized = true;
        transform.SetParent(null);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isFlyingToUI) return; 

        // --- FIXED: Only check for the CarController! Ignore the "Player" tag so livestock can't steal gems! ---
        if (other.GetComponentInParent<CarController>() != null) 
        {
            if (gemCollider != null) gemCollider.enabled = false;
            
            isMagnetized = false;
            isFlyingToUI = true;

            // --- NEW FIX: Parent to camera so it never gets left behind at high speeds! ---
            if (mainCam != null) transform.SetParent(mainCam.transform, true);
        }
    }

    void FinishCollection()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.AddGems(gemValue);
        }
        
        Destroy(gameObject);
    }
}