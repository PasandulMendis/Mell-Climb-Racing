using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class UIButtonPop : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Animation Settings")]
    [Tooltip("How small the button shrinks when your finger is pressing it down")]
    public float pressedScale = 0.9f;

    [Tooltip("How big the button 'pops' out when you let go before settling")]
    public float releasePopScale = 1.1f;

    [Tooltip("How fast the pop animation takes to complete in seconds")]
    public float popDuration = 0.15f;

    [Header("Base Scale")]
    [Tooltip("Leave as 1, 1, 1 unless your button naturally has a weird scale size")]
    public Vector3 baseScale = Vector3.one;

    private RectTransform rectTransform;
    private bool isPressed = false;
    private Coroutine animRoutine;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        if (rectTransform != null) rectTransform.localScale = baseScale;
        isPressed = false;
    }

    void OnDisable()
    {
        isPressed = false;
        if (rectTransform != null) rectTransform.localScale = baseScale;
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(SmoothScale(baseScale * pressedScale, popDuration * 0.5f));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed) return;
        isPressed = false;

        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(PopRoutine());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isPressed)
        {
            isPressed = false;
            if (animRoutine != null) StopCoroutine(animRoutine);
            animRoutine = StartCoroutine(SmoothScale(baseScale, popDuration));
        }
    }


    IEnumerator SmoothScale(Vector3 target, float duration)
    {
        Vector3 start = rectTransform.localScale;
        float t = 0;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            rectTransform.localScale = Vector3.Lerp(start, target, t / duration);
            yield return null;
        }
        rectTransform.localScale = target;
    }

    IEnumerator PopRoutine()
    {
        Vector3 start = rectTransform.localScale;
        Vector3 peak = baseScale * releasePopScale;
        float t = 0;
        float upTime = popDuration * 0.4f;

        while (t < upTime)
        {
            t += Time.unscaledDeltaTime;
            rectTransform.localScale = Vector3.Lerp(start, peak, t / upTime);
            yield return null;
        }

        t = 0;
        float downTime = popDuration * 0.6f;

        while (t < downTime)
        {
            t += Time.unscaledDeltaTime;
            rectTransform.localScale = Vector3.Lerp(peak, baseScale, t / downTime);
            yield return null;
        }

        rectTransform.localScale = baseScale;
    }
}