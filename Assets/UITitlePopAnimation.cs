using UnityEngine;
using System.Collections;

public class UITitlePop : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("How long to wait before popping. 0.15s lets the panel fade in first!")]
    public float delay = 0.15f;

    [Tooltip("How fast the animation plays")]
    public float popDuration = 0.4f;

    [Tooltip("How big it stretches before settling back down (1.2 = 20% bigger)")]
    public float popSize = 1.2f;

    private Vector3 originalScale;
    private Coroutine animRoutine;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    void OnEnable()
    {
        transform.localScale = Vector3.zero;

        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(PopRoutine());
    }

    void OnDisable()
    {
        if (animRoutine != null) StopCoroutine(animRoutine);

        transform.localScale = originalScale;
    }

    IEnumerator PopRoutine()
    {
        if (delay > 0) yield return new WaitForSecondsRealtime(delay);

        float t = 0;
        float upTime = popDuration * 0.6f;
        float downTime = popDuration * 0.4f;

        while (t < upTime)
        {
            t += Time.unscaledDeltaTime;
            float progress = t / upTime;

            float ease = 1f - Mathf.Pow(1f - progress, 3f);
            transform.localScale = originalScale * Mathf.Lerp(0f, popSize, ease);
            yield return null;
        }

        t = 0;
        while (t < downTime)
        {
            t += Time.unscaledDeltaTime;
            float progress = t / downTime;

            float ease = progress * progress * (3f - 2f * progress);
            transform.localScale = originalScale * Mathf.Lerp(popSize, 1f, ease);
            yield return null;
        }

        transform.localScale = originalScale;
    }
}