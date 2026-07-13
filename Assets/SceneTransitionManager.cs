using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager instance;
    
    [Header("Transition Settings")]
    public float fadeDuration = 0.5f;
    public Color fadeColor = Color.black;

    private Canvas transitionCanvas;
    private Image fadeImage;

    void Awake()
    {
        // Singleton pattern: Ensure only ONE of these ever exists!
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Keep it alive when scenes change
            CreateFadeUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void CreateFadeUI()
    {
        // 1. Auto-generate a Canvas that sorts on top of absolutely everything
        GameObject canvasObj = new GameObject("TransitionCanvas");
        canvasObj.transform.SetParent(this.transform);
        transitionCanvas = canvasObj.AddComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 9999; 

        // 2. Auto-generate the Black Fade Image
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform);
        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f); // Start invisible

        // 3. Stretch it to cover the whole screen perfectly
        RectTransform rect = fadeImage.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        // Disable raycasts initially so we can click buttons!
        fadeImage.raycastTarget = false; 
    }

    public void LoadScene(int sceneIndex)
    {
        StartCoroutine(FadeAndLoad(sceneIndex));
    }

    IEnumerator FadeAndLoad(int sceneIndex)
    {
        // 1. Block the screen so the player can't double-click buttons while fading
        fadeImage.raycastTarget = true;

        // 2. Fade Out (To Black)
        // Note: We use unscaledDeltaTime so the fade still works even if the Pause Menu froze the game!
        float t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / fadeDuration;
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, Mathf.Lerp(0f, 1f, t));
            yield return null;
        }

        // 3. Ensure time isn't frozen from a pause menu, then load the new scene!
        Time.timeScale = 1f; 
        yield return SceneManager.LoadSceneAsync(sceneIndex);

        // 4. Fade In (Back to Gameplay)
        t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / fadeDuration;
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, Mathf.Lerp(1f, 0f, t));
            yield return null;
        }

        // 5. Unblock clicks
        fadeImage.raycastTarget = false;
    }
}