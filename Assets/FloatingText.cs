using UnityEngine;
using TMPro; // Required for TextMeshPro!

public class FloatingText : MonoBehaviour
{
    [Header("Animation Settings")]
    public float moveSpeed = 2.5f;
    public float destroyTime = 2f;
    public Vector3 randomOffsetRange = new Vector3(0.5f, 0.5f, 0f);

    private TextMeshPro textMesh;
    private Color alphaColor;
    private float timer = 0f;
    private Vector3 initialScale;

    public void Setup(string text, Color color)
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh != null)
        {
            textMesh.text = text;
            textMesh.color = color;
            alphaColor = color;
        }

        // Add a slight random offset so multiple text popups don't perfectly overlap
        transform.position += new Vector3(
            Random.Range(-randomOffsetRange.x, randomOffsetRange.x),
            Random.Range(-randomOffsetRange.y, randomOffsetRange.y),
            0f
        );

        initialScale = transform.localScale;
        transform.localScale = Vector3.zero; // Start at 0 scale for the pop animation

        // Clean up the object after its lifespan
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        timer += Time.deltaTime;

        // 1. Float Upwards
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // 2. Scale "Pop" Animation (Grows quickly, then settles)
        if (timer < 0.2f)
        {
            float scaleProgress = timer / 0.2f;
            // Overshoot slightly for a bouncy effect
            transform.localScale = Vector3.Lerp(Vector3.zero, initialScale * 1.2f, scaleProgress);
        }
        else if (timer < 0.4f)
        {
            float settleProgress = (timer - 0.2f) / 0.2f;
            transform.localScale = Vector3.Lerp(initialScale * 1.2f, initialScale, settleProgress);
        }

        // 3. Fade Out near the end of its life
        if (timer > (destroyTime / 2f) && textMesh != null)
        {
            float fadeProgress = (timer - (destroyTime / 2f)) / (destroyTime / 2f);
            alphaColor.a = Mathf.Lerp(1f, 0f, fadeProgress);
            textMesh.color = alphaColor;
        }
    }
}