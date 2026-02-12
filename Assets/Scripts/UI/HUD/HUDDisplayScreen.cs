using UnityEngine;
using UnityEngine.UI;

public class HUDDisplayScreen : MonoBehaviour
{
    [SerializeField] private RawImage backgroundColorImage;
    [SerializeField] private RawImage staticImage;

    private void Start()
    {
        staticImage.rectTransform.localScale = new Vector3(1f, 0f, 1f);
        Invoke(nameof(ShowHUD), Random.Range(0f, 1f));
    }

    public void ShowHUD()
    {
        StopAllCoroutines();
        StartCoroutine(BlinkScreen(0f, 0.2f, 0f, 1f));
        StartCoroutine(FadeScreen(1f, 3f, 1f, 0.01f));
    }

    public void HideHUD()
    {
        StopAllCoroutines();
        StartCoroutine(FadeScreen(0f, 3f, 1f, 0.01f));
        StartCoroutine(BlinkScreen(4f, 0.2f, 1f, 0f));
    }

    private System.Collections.IEnumerator FadeScreen(float delay, float duration, float startAlpha, float endAlpha)
    {
        float elapsed = 0f;
        while (elapsed < duration + delay)
        {
            elapsed += Time.deltaTime;
            if (elapsed < delay)
            {
                yield return null;
                continue;
            }
            Color colorBackground = backgroundColorImage.color;
            colorBackground.a = Mathf.Lerp(startAlpha, endAlpha, (elapsed - delay) / duration);
            backgroundColorImage.color = colorBackground;
            Color colorStatic = staticImage.color;
            colorStatic.a = Mathf.Lerp(startAlpha, endAlpha, (elapsed - delay) / duration);
            staticImage.color = colorStatic;
            yield return null;
        }
        Color finalBackgroundColor = backgroundColorImage.color;
        finalBackgroundColor.a = endAlpha;
        backgroundColorImage.color = finalBackgroundColor;
        Color finalStaticColor = staticImage.color;
        finalStaticColor.a = endAlpha;
        staticImage.color = finalStaticColor;
    }

    private System.Collections.IEnumerator BlinkScreen(float delay, float duration, float startScale, float endScale)
    {
        float elapsed = 0f;
        while (elapsed < duration + delay)
        {
            elapsed += Time.deltaTime;
            if (elapsed < delay)
            {
                yield return null;
                continue;
            }

            float t = Mathf.Sqrt((elapsed - delay) / duration);
            float scale = Mathf.Lerp(startScale, endScale, t);
            staticImage.rectTransform.localScale = new Vector3(1f, scale, 1f);
            yield return null;
        }
        staticImage.rectTransform.localScale = new Vector3(1f, endScale, 1f);
    }
}
