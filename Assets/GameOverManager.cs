using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager instance;

    [Header("UI Panels")]
    public GameObject gameOverPanel;
    public Image flashImage; // A pure white UI Image that covers the screen
    public RawImage polaroidPhoto; // Uses RawImage to display the Texture2D screenshot

    [Header("Stat Texts")]
    public TextMeshProUGUI reasonText;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI flipsText;
    public TextMeshProUGUI airTimeText;
    public TextMeshProUGUI nitrosText;

    [Header("Animation Settings")]
    public float slowMotionTimeScale = 0.2f;
    public float zoomDuration = 1.0f;
    public float zoomAmount = 4f; // How much closer the camera gets
    public float tallySpeed = 0.5f;

    [Header("UI Fly-Out Settings")]
    [Tooltip("UI Elements that will slide UP off the screen (e.g., Top Counters, RPM)")]
    public RectTransform[] uiElementsToSlideUp;
    [Tooltip("UI Elements that will slide DOWN off the screen (e.g., Pedals, Nitro Button)")]
    public RectTransform[] uiElementsToSlideDown;
    [Tooltip("How many pixels the UI moves to get off screen")]
    public float slideDistance = 1000f; 

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Ensure panels are off at the start
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (flashImage != null)
        {
            flashImage.gameObject.SetActive(true);
            flashImage.color = new Color(1, 1, 1, 0); // Transparent
            flashImage.raycastTarget = false;
        }
    }

    public void StartSequence(string deathReason)
    {
        StartCoroutine(GameOverRoutine(deathReason));
    }

    IEnumerator GameOverRoutine(string deathReason)
    {
        // 1. Enter Slow Motion
        Time.timeScale = slowMotionTimeScale;

        // 2. Take Control of the Camera
        Camera mainCam = Camera.main;
        CameraController camController = mainCam.GetComponent<CameraController>();
        if (camController != null) camController.enabled = false; // Stop the standard zooming

        float initialZoom = mainCam.orthographicSize;
        float targetZoom = Mathf.Max(2f, initialZoom - zoomAmount);
        
        Vector3 initialCamPos = mainCam.transform.position;
        Transform carTransform = GameManager.instance.playerCar;

        // Save the starting positions of the UI elements
        Vector2[] startUpPos = new Vector2[uiElementsToSlideUp.Length];
        for (int i = 0; i < uiElementsToSlideUp.Length; i++) 
            if (uiElementsToSlideUp[i] != null) startUpPos[i] = uiElementsToSlideUp[i].anchoredPosition;

        Vector2[] startDownPos = new Vector2[uiElementsToSlideDown.Length];
        for (int i = 0; i < uiElementsToSlideDown.Length; i++) 
            if (uiElementsToSlideDown[i] != null) startDownPos[i] = uiElementsToSlideDown[i].anchoredPosition;

        // 3. Slow Zoom, Center on Car, and Slide UI Away!
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / zoomDuration;
            
            mainCam.orthographicSize = Mathf.Lerp(initialZoom, targetZoom, t);

            if (carTransform != null)
            {
                Vector3 targetCamPos = new Vector3(carTransform.position.x, carTransform.position.y, initialCamPos.z);
                mainCam.transform.position = Vector3.Lerp(initialCamPos, targetCamPos, t);
            }

            float slideProgress = Mathf.SmoothStep(0f, 1f, t);
            for (int i = 0; i < uiElementsToSlideUp.Length; i++)
            {
                if (uiElementsToSlideUp[i] != null)
                    uiElementsToSlideUp[i].anchoredPosition = startUpPos[i] + new Vector2(0f, slideDistance * slideProgress);
            }
            for (int i = 0; i < uiElementsToSlideDown.Length; i++)
            {
                if (uiElementsToSlideDown[i] != null)
                    uiElementsToSlideDown[i].anchoredPosition = startDownPos[i] + new Vector2(0f, -slideDistance * slideProgress);
            }

            yield return null;
        }

        // 4. FLASH!
        flashImage.color = new Color(1, 1, 1, 1);
        
        // FREEZE TIME COMPLETELY
        Time.timeScale = 0f; 

        // 5. Take the PERFECT SQUARE Snapshot for the Polaroid
        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
        mainCam.targetTexture = rt;
        mainCam.Render();
        RenderTexture.active = rt;

        int sqSize = Mathf.Min(Screen.width, Screen.height);
        int xOffset = (Screen.width - sqSize) / 2;
        int yOffset = (Screen.height - sqSize) / 2;

        Texture2D squareScreenshot = new Texture2D(sqSize, sqSize, TextureFormat.RGB24, false);
        squareScreenshot.ReadPixels(new Rect(xOffset, yOffset, sqSize, sqSize), 0, 0);
        squareScreenshot.Apply();
        
        mainCam.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
        
        if (polaroidPhoto != null) polaroidPhoto.texture = squareScreenshot;

        // 6. Turn on the UI Board
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        // Set Reason Text instantly
        if (reasonText != null) reasonText.text = deathReason;

        // Reset text boxes to their baseline formats
        if (distanceText != null) distanceText.text = "DISTANCE 0M";
        if (coinsText != null) coinsText.text = "COINS +0";
        if (flipsText != null) flipsText.text = "FLIPS 0";
        if (airTimeText != null) airTimeText.text = "AIRTIME 0.0S";
        if (nitrosText != null) nitrosText.text = "NITRO USED 0";

        // 7. Fade the Flash Away
        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.5f;
            flashImage.color = new Color(1, 1, 1, Mathf.Lerp(1f, 0f, t));
            yield return null;
        }

        // 8. Tally up the stats!
        if (GameManager.instance != null)
        {
            yield return StartCoroutine(TallyNumber(distanceText, "DISTANCE ", GameManager.instance.distanceTraveled, "M", false));
            yield return StartCoroutine(TallyNumber(coinsText, "COINS +", GameManager.instance.totalCoinsThisRun, "", false));
            yield return StartCoroutine(TallyNumber(flipsText, "FLIPS ", GameManager.instance.runFlips, "", false));
            yield return StartCoroutine(TallyNumber(airTimeText, "AIRTIME ", GameManager.instance.runAirTime, "S", true));
            yield return StartCoroutine(TallyNumber(nitrosText, "NITRO USED ", GameManager.instance.runNitrosUsed, "", false));
        }
    }

    IEnumerator TallyNumber(TextMeshProUGUI textObj, string prefix, float targetAmount, string suffix, bool isFloat)
    {
        if (textObj == null || targetAmount <= 0) yield break;

        float currentVal = 0;
        float t = 0;
        
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / tallySpeed;
            currentVal = Mathf.Lerp(0, targetAmount, t);
            
            if (isFloat) textObj.text = prefix + currentVal.ToString("F1") + suffix;
            else textObj.text = prefix + Mathf.FloorToInt(currentVal).ToString() + suffix;
            
            yield return null;
        }
        
        if (isFloat) textObj.text = prefix + targetAmount.ToString("F1") + suffix;
        else textObj.text = prefix + Mathf.FloorToInt(targetAmount).ToString() + suffix;
    }

    // --- REWRITTEN: LIVE CAPTURE NATIVE SHARE SYSTEM ---
    public void ShareScreenshot()
    {
        // Start the Coroutine to wait for the UI to finish rendering this frame
        StartCoroutine(CaptureAndShareRoutine());
    }

    private IEnumerator CaptureAndShareRoutine()
    {
        // 1. Wait until the very end of the frame (Ensures the entire UI Canvas is drawn on screen!)
        yield return new WaitForEndOfFrame();

        // 2. Capture the actual screen exactly as the player sees it
        Texture2D finalScreenshot = ScreenCapture.CaptureScreenshotAsTexture();

        // 3. Save it to the device's temporary cache
        string filePath = System.IO.Path.Combine(Application.temporaryCachePath, "shared_ui_screenshot.png");
        System.IO.File.WriteAllBytes(filePath, finalScreenshot.EncodeToPNG());

        // 4. Destroy the texture from memory to prevent memory leaks!
        Destroy(finalScreenshot);

        // 5. Generate the dynamic caption text
        string caption = "I just drove " + GameManager.instance.distanceTraveled.ToString("F0") + "m in Mell Climb Racing! Can you beat my high score?";

        // 6. Use the Native Share Plugin to pop up the Android Share Sheet
        new NativeShare().AddFile(filePath)
            .SetSubject("Mell Climb Racing Score")
            .SetText(caption)
            .Share();
    }
}