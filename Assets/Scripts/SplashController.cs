using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SplashFader : MonoBehaviour
{
    [SerializeField] private Image blackImage; // The full-screen black UI image
    [SerializeField] private float fadeDuration = 1f; // Duration for fade in/out
    [SerializeField] private float displayDuration = 2f; // How long splash is visible

    private void Start()
    {
        StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        // Fade out black image (reveal splash)
        yield return StartCoroutine(FadeImage(1f, 0f));

        // Show splash screen
        yield return new WaitForSeconds(displayDuration);

        // Fade in black image (cover screen)
        yield return StartCoroutine(FadeImage(0f, 1f));

        // Load next scene
        SceneManager.LoadScene(1);
    }

    private IEnumerator FadeImage(float startAlpha, float endAlpha)
    {
        float elapsed = 0f;
        Color color = blackImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            blackImage.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        blackImage.color = new Color(color.r, color.g, color.b, endAlpha);
    }
}