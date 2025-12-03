using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ScreenFader manages camera fade transitions using a canvas overlay.
/// Call FadeToBlack() to fade out, then after room setup, call FadeFromBlack() to fade back in.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;

    // Internal
    private Canvas fadeCanvas;
    private Image fadeImage;
    private bool isFading = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Create or find the fade canvas
        if (fadeCanvas == null)
            SetupFadeCanvas();
    }

    private void SetupFadeCanvas()
    {
        // Look for existing fade canvas
        var existingCanvas = FindObjectOfType<Canvas>();
        if (existingCanvas != null && existingCanvas.gameObject.name == "FadeCanvas")
        {
            fadeCanvas = existingCanvas;
            fadeImage = fadeCanvas.GetComponentInChildren<Image>();
            return;
        }

        // Create new fade canvas
        var canvasGO = new GameObject("FadeCanvas");
        canvasGO.transform.SetParent(transform);
        fadeCanvas = canvasGO.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999; // Ensure it's on top

        var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // Create fade image
        var imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(canvasGO.transform);
        fadeImage = imageGO.AddComponent<Image>();
        fadeImage.color = Color.black;

        var rectTransform = imageGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // Start fully transparent
        var color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;
    }

    /// <summary>
    /// Fade the screen to black over fadeDuration seconds.
    /// </summary>
    public void FadeToBlack()
    {
        StartCoroutine(FadeRoutine(1f)); // 1 = fully opaque black
    }

    /// <summary>
    /// Fade the screen from black back to transparent over fadeDuration seconds.
    /// </summary>
    public void FadeFromBlack()
    {
        StartCoroutine(FadeRoutine(0f)); // 0 = fully transparent
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        if (isFading) yield break;
        isFading = true;

        float elapsed = 0f;
        Color startColor = fadeImage.color;
        float startAlpha = startColor.a;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            var color = fadeImage.color;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            fadeImage.color = color;

            yield return null;
        }

        var finalColor = fadeImage.color;
        finalColor.a = targetAlpha;
        fadeImage.color = finalColor;

        isFading = false;
    }
}
