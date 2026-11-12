using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageTransparencyFader : MonoBehaviour
{
    [SerializeField] private Color startColor = Color.white;
    [SerializeField] private Color targetColor = Color.black;
    
    [SerializeField] private float delay = 1f;
    [SerializeField] private float fadeDuration = 1.5f;

    private Image targetImage;
    private Coroutine activeFadeCoroutine;

    void Awake()
    {
        targetImage = GetComponent<Image>();
    }

    void Start()
    {
        targetImage.color = startColor;

        StartFade(targetColor, fadeDuration);
    }

    public void StartFade(Color newTargetColor, float duration)
    {
        if (activeFadeCoroutine != null)
        {
            StopCoroutine(activeFadeCoroutine);
        }

        activeFadeCoroutine = StartCoroutine(Fade(targetImage.color, newTargetColor, duration));
    }

    private IEnumerator Fade(Color startColor, Color endColor, float duration)
    {
        if (duration <= 0f)
        {
            targetImage.color = endColor;
            activeFadeCoroutine = null;
            yield break;
        }

        yield return new WaitForSecondsRealtime(delay);

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Mathf.Min(Time.deltaTime, 0.05f);

            float linearT = Mathf.Clamp01(elapsedTime / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, linearT);

            targetImage.color = Color.Lerp(startColor, endColor, smoothT);

            yield return null;
        }

        targetImage.color = endColor;
        activeFadeCoroutine = null;
    }
}