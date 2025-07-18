using UnityEngine;
using UnityEngine.UI;

public class ScrollManager : MonoBehaviour
{
    [Header("Scroll UI")]
    [SerializeField] private Button[] scrollButtons;
    [SerializeField] private Text[] scrollButtonTexts;
    [SerializeField] private Image[] scrollButtonImages;
    [SerializeField] private GameObject cancelButton;

    [Header("Scroll Panel")]
    [SerializeField] private GameObject scrollPanel;
    [SerializeField] private Button scrollBackButton;
    [SerializeField] private Image scrollBackButtonImage;

    [Header("Scroll Data")]
    [SerializeField] private ScrollData scrollData;

    private int[] selectedScrollIndices = new int[3];
    private bool[] usedScrollFlags = new bool[3];

    private int selectedSlotForPanel = -1; // Store which scroll slot was clicked

    void Start()
    {
        LoadSelectedScrolls();
        SetupScrollButtons();
        SetScrollsInteractable(false);

        // Hide panel initially
        if (scrollPanel != null)
            scrollPanel.SetActive(false);
    }

    void Update()
    {
        bool isPlayerTurn = PieceMover.currentTurn == TurnType.Player;
        foreach (var btn in scrollButtons)
            btn.gameObject.SetActive(isPlayerTurn);
    }

    private void LoadSelectedScrolls()
    {
        int count = 0;
        for (int i = 0; i < 20; i++)
        {
            if (PlayerPrefs.GetInt($"scroll_{i}_selected", 0) == 1)
            {
                if (count < 3)
                {
                    selectedScrollIndices[count] = i;
                    usedScrollFlags[count] = false;
                    count++;
                }
            }
        }

        for (int i = count; i < 3; i++)
        {
            selectedScrollIndices[i] = -1;
            usedScrollFlags[i] = true;
        }
    }

    private void SetupScrollButtons()
    {
        if (scrollData == null || scrollData.scrollSprites == null)
        {
            Debug.LogWarning("ScrollData ScriptableObject is not assigned.");
            return;
        }

        for (int i = 0; i < scrollButtons.Length; i++)
        {
            int scrollIndex = selectedScrollIndices[i];

            if (scrollButtonTexts != null && i < scrollButtonTexts.Length)
                scrollButtonTexts[i].text = scrollIndex >= 0 ? $"Scroll {scrollIndex + 1}" : "None";

            if (scrollButtonImages != null && i < scrollButtonImages.Length && scrollIndex >= 0 && scrollIndex < scrollData.scrollSprites.Length)
            {
                scrollButtonImages[i].sprite = scrollData.scrollSprites[scrollIndex];
                scrollButtonImages[i].preserveAspect = true;
            }

            scrollButtons[i].onClick.RemoveAllListeners();
            int capturedIndex = i;
            if (scrollIndex >= 0 && !usedScrollFlags[capturedIndex])
            {
                scrollButtons[i].onClick.AddListener(() => OpenScrollPanel(capturedIndex));
                scrollButtons[i].interactable = true;
            }
            else
            {
                scrollButtons[i].interactable = false;
            }
        }
    }

    // New: Open scroll panel and show back sprite
    private void OpenScrollPanel(int scrollSlotIndex)
    {
        selectedSlotForPanel = scrollSlotIndex;

        int scrollIndex = selectedScrollIndices[scrollSlotIndex];

        if (scrollBackButtonImage != null && scrollIndex >= 0 && scrollIndex < scrollData.scrollBacks.Length)
        {
            scrollBackButtonImage.sprite = scrollData.scrollBacks[scrollIndex];
            scrollBackButtonImage.preserveAspect = true;
        }

        // Set listener for back button
        scrollBackButton.onClick.RemoveAllListeners();
        scrollBackButton.onClick.AddListener(() => OnScrollUsed(scrollSlotIndex));

        if (scrollPanel != null)
            scrollPanel.SetActive(true);

        //SetScrollsInteractable(false); // hide main buttons
    }

    private void OnScrollUsed(int scrollSlotIndex)
    {
        int scrollIndex = selectedScrollIndices[scrollSlotIndex];
        Debug.Log($"Scroll {scrollIndex} used!");

        usedScrollFlags[scrollSlotIndex] = true;
        scrollButtons[scrollSlotIndex].interactable = false;

        // Execute scroll effect
        if (scrollData != null && scrollIndex < scrollData.scrollEffectKeys.Length)
        {
            string effectKey = scrollData.scrollEffectKeys[scrollIndex];
            ScrollEffectExecutor.Instance.ExecuteEffect(effectKey, false); // false = player
        }

        if (scrollPanel != null)
            scrollPanel.SetActive(false);

        // Handle turn or rethrow
        if (PieceMover.lastMoveWasRethrow)
        {
            PieceMover.Instance.Invoke("UpdateThrowButtonState", 0.1f);
        }
        else
        {
            PieceMover.currentTurn = TurnType.AI;
            PieceMover.ResetTurn();
            AiMover.StartStickThrow();
        }

        PieceMover.lastMoveWasRethrow = false;
    }

    public void SetScrollsInteractable(bool state)
    {
        for (int i = 0; i < scrollButtons.Length; i++)
        {
            if (selectedScrollIndices[i] >= 0 && !usedScrollFlags[i])
                scrollButtons[i].interactable = state;
            else
                scrollButtons[i].interactable = false;
        }

        if (cancelButton != null)
            cancelButton.SetActive(state);

        if (PieceMover.Instance != null && PieceMover.Instance.throwButton != null)
            PieceMover.Instance.throwButton.SetActive(!state);
    }

    public void OnCancelButtonPressed()
    {
        SetScrollsInteractable(false);

        if (PieceMover.lastMoveWasRethrow)
        {
            PieceMover.Instance.Invoke("UpdateThrowButtonState", 0.1f);
        }
        else
        {
            PieceMover.currentTurn = TurnType.AI;
            PieceMover.ResetTurn();
            AiMover.StartStickThrow();
        }

        PieceMover.lastMoveWasRethrow = false;
    }

    public void BackBtn()
    {
        scrollPanel.SetActive(false);
    }
}
