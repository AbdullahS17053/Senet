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
        bool isFirstTime = PlayerPrefs.GetInt(FirstTimeKey, 0) == 1;
        welcomePanel.SetActive(isFirstTime);
        AudioController.Instance.PlayMusic("MainMenu");
        AudioController.Instance.StopMusic("GamePlay");
    }

    public void DiscoverBtn()
    {
        StartCoroutine(PlayTapEffect(() => { Application.OpenURL("https://ancientbloods.co.uk"); }));
    }

    public void PrivacyBtn()
    {
        StartCoroutine(PlayTapEffect(() => { Application.OpenURL("https://ancientbloods.co.uk/privacy-policy/"); }));
    }

    public void SupportBtn()
    {
        StartCoroutine(PlayTapEffect(() => { Application.OpenURL("https://ancientbloods.co.uk/support/"); }));
    }

    public void RemindMeBtn()
    {
        StartCoroutine(PlayTapEffect(() => { welcomePanel.SetActive(false); }));
    }
    public void PermanentlyCloseWelcomePanel()
    {
        StartCoroutine(PlayTapEffect(() =>
        {
            welcomePanel.SetActive(false);
            PlayerPrefs.SetInt(FirstTimeKey, 0);
            PlayerPrefs.Save();
        }));
    }
    public IEnumerator PlayTapEffect(System.Action onComplete)
    {
        AudioController.Instance.PlaySound("ButtonTap");
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

    public void TikTokBtn()
    {
        Application.OpenURL("https://www.tiktok.com/@01aspects");
    }
    public void FbBtn()
    {
        Application.OpenURL("https://www.facebook.com/profile.php?id=61573024288861");
    }
    public void DiscordBtn()
    {
        Application.OpenURL("https://discord.gg/UsRv9GpA");
    }

    public void ShadowGameBtn()
    {
        Application.OpenURL("https://ancientbloods.co.uk/shadowgame/");
    }

}