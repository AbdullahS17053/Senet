using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("UI Panels")] [SerializeField] private GameObject scrollPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    [SerializeField] private GameObject teachingsPanel;
    
    [SerializeField] private Text feedbackText;
    
    [Header("Reward System")]
    [SerializeField] private GameObject rewardsPanel;
    [SerializeField] private GameObject rewardBackPanel;
    [SerializeField] private Button[] rewardScrollButtons; // 3 scroll buttons shown as reward choices
    [SerializeField] private Image rewardBackImage;         // image component on rewardBackPanel
    [SerializeField] private Button rewardBackButton;       // button with the scroll back sprite
    [SerializeField] private ScrollData scrollData;         // reference to ScrollData asset

    private int rewardScrollIndex = -1; // index of scroll the player is about to unlock



    [Header("Tap Effect Settings")] [SerializeField]
    private float scaleAmount = 1.1f;

    [SerializeField] private float scaleDuration = 0.1f;
    [SerializeField] private float afterEffectDelay = 0.1f;

    [SerializeField] private GameObject stickThrower;

    public static GameManager Instance;

    public void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject); // Prevent duplicates
    }

    void Start()
    {
        rewardBackButton.onClick.AddListener(OnRewardBackConfirmed);
    }
    public void PlayBtn()
    {
        StartCoroutine(PlayTapEffect(() =>
        {
            if (HasSelectedThreeScrolls())
            {
                SceneManager.LoadScene(2);
            }
            else
            {
                if (scrollPanel != null)
                    scrollPanel.SetActive(true);

                if (feedbackText != null)
                {
                    feedbackText.text = "Select 3 scrolls before starting the game.";
                    CancelInvoke(nameof(ClearFeedback)); // In case a previous timer is running
                    Invoke(nameof(ClearFeedback), 5f);  // Clear after 5 seconds
                }
            }
        }));
    }


    public void ScrollBtn()
    {
        StartCoroutine(PlayTapEffect(() => { scrollPanel.SetActive(true); }));

    }

    public void TeachingsBtn()
    {
        StartCoroutine(PlayTapEffect(() => { teachingsPanel.SetActive(true); }));
    }

    public void BackBtn()
    {
        StartCoroutine(PlayTapEffect(() => { scrollPanel.SetActive(false); }));
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

    public void CloseTeachingsPanel()
    {
        StartCoroutine(PlayTapEffect(() => { teachingsPanel.SetActive(false); }));
    }

    public void MenuBtn()
    {
        SceneManager.LoadScene(1);
    }

    public void ReplayBtn()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void CheckForWinCondition()
    {
        int playerCount = 0;
        int aiCount = 0;

        PieceMover[] allPieces = FindObjectsOfType<PieceMover>();
        foreach (var piece in allPieces)
        {
            if (piece.isAI) aiCount++;
            else playerCount++;
        }

        if (aiCount == 1)
        {
            Debug.Log("AI Wins!");
            if (losePanel != null)
            {
                losePanel.SetActive(true);
                stickThrower.SetActive(false);
            }
        }
        else if (playerCount == 1)
        {
            Debug.Log("Player Wins!");
            if (winPanel != null)
            {
                SetupRewardScrolls();
                stickThrower.SetActive(false);
            }
        }
    }
    private bool HasSelectedThreeScrolls()
    {
        int selectedCount = 0;

        for (int i = 0; i < 20; i++)
        {
            if (PlayerPrefs.GetInt($"scroll_{i}_selected", 0) == 1)
            {
                selectedCount++;
                if (selectedCount >= 3)
                    return true;
            }
        }

        return false;
    }

    private void ClearFeedback()
    {
        if (feedbackText != null)
            feedbackText.text = "";
    }
    private void SetupRewardScrolls()
    {
        rewardsPanel.SetActive(true);

        int count = 0;
        for (int i = 0; i < scrollData.scrollSprites.Length && count < 3; i++)
        {
            bool isUnlocked = PlayerPrefs.GetInt($"scroll_{i}_unlocked", 0) == 1;
            if (!isUnlocked)
            {
                int capturedIndex = i;
                rewardScrollButtons[count].image.sprite = scrollData.scrollSprites[i];
                rewardScrollButtons[count].onClick.RemoveAllListeners();
                rewardScrollButtons[count].onClick.AddListener(() => OnRewardScrollSelected(capturedIndex));
                rewardScrollButtons[count].gameObject.SetActive(true);
                count++;
            }
        }

        // If fewer than 3 locked scrolls exist, hide extra buttons
        for (int i = count; i < rewardScrollButtons.Length; i++)
        {
            rewardScrollButtons[i].gameObject.SetActive(false);
        }
    }
    private void OnRewardScrollSelected(int index)
    {
        rewardScrollIndex = index;

        if (rewardBackImage != null && index < scrollData.scrollBacks.Length)
        {
            rewardBackImage.sprite = scrollData.scrollBacks[index];
        }

        rewardBackPanel.SetActive(true);
    }
    private void OnRewardBackConfirmed()
    {
        if (rewardScrollIndex >= 0)
        {
            PlayerPrefs.SetInt($"scroll_{rewardScrollIndex}_unlocked", 1);
            PlayerPrefs.Save();
        }

        rewardBackPanel.SetActive(false);
        rewardsPanel.SetActive(false);
        winPanel.SetActive(true);
    }

    public void GotoRewards()
    {
        rewardBackPanel.SetActive(false);
    }
}



