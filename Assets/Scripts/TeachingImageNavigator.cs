using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TeachingImageNavigator : MonoBehaviour
{
    [Header("UI References")] [SerializeField]
    Image teachingsImage; // Image component to display teaching images

    [SerializeField] Button leftButton; // Button to go to the previous image
    [SerializeField] Button rightButton; // Button to go to the next image

    [Header("Images Array")] [SerializeField]
    Sprite[] teachingSprites; // Array of teaching images (sprites)

    private int currentIndex = 0;

    private float scaleDuration = 0.1f;
    private float afterEffectDelay = 0.1f;
    private float scaleAmount = 1.1f;

    private void Start()
    {
        UpdateImageAndButtons();

        leftButton.onClick.AddListener(LeftButtonClick);
        rightButton.onClick.AddListener(RightButtonClick);
    }

    void ShowPreviousImage()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateImageAndButtons();
        }
    }

    void ShowNextImage()
    {
        if (currentIndex < teachingSprites.Length - 1)
        {
            currentIndex++;
            UpdateImageAndButtons();
        }
    }

    void UpdateImageAndButtons()
    {
        teachingsImage.sprite = teachingSprites[currentIndex];

        // Disable left button if first image
        leftButton.interactable = currentIndex > 0;

        // Disable right button if last image
        rightButton.interactable = currentIndex < teachingSprites.Length - 1;
    }

    void RightButtonClick()
    {
        StartCoroutine(PlayTapEffect(() => { ShowNextImage(); }));
    }

    void LeftButtonClick()
    {
        StartCoroutine(PlayTapEffect(() => { ShowPreviousImage(); }));
    }

    IEnumerator PlayTapEffect(System.Action onComplete)
    {
        GameObject clicked = EventSystem.current.currentSelectedGameObject;
        if (clicked == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Transform transform = clicked.transform;

        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * scaleAmount;

        float timer = 0f;
        while (timer < scaleDuration)
        {
            transform.localScale = Vector3.Lerp(originalScale, targetScale, timer / scaleDuration);
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localScale = targetScale;

        timer = 0f;
        while (timer < scaleDuration)
        {
            transform.localScale = Vector3.Lerp(targetScale, originalScale, timer / scaleDuration);
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localScale = originalScale;

        onComplete?.Invoke();
    }
}