using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject welcomePanel;

    private const string FirstTimeKey = "HasPlayedBefore";
    
    [Header("Tap Effect Settings")] [SerializeField]
    private float scaleAmount = 1.1f;

    [SerializeField] private float scaleDuration = 0.1f;
    [SerializeField] private float afterEffectDelay = 0.1f;

    private void Awake()
    {
        bool isFirstTime = PlayerPrefs.GetInt(FirstTimeKey, 0) == 0;
        if (isFirstTime)
        {
            welcomePanel.SetActive(true);
            PlayerPrefs.SetInt(FirstTimeKey, 1);
            PlayerPrefs.Save();
        }
        else
        {
            welcomePanel.SetActive(false);
        }
    }

    public void DiscoverBtn()
    {
        StartCoroutine(PlayTapEffect(() => { welcomePanel.SetActive(false); }));
    }

    public void RemindMeBtn()
    {
        StartCoroutine(PlayTapEffect(() => { welcomePanel.SetActive(false); }));
    }
    public IEnumerator PlayTapEffect(System.Action onComplete)
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

        yield return new WaitForSeconds(afterEffectDelay);

        onComplete?.Invoke();
    }
}