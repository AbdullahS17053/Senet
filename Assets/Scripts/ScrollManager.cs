using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ScrollManager : MonoBehaviour
{
    [Header("Scroll UI")] [SerializeField] public Button[] scrollButtons;
    [SerializeField] private Text[] scrollButtonTexts;
    [SerializeField] private Image[] scrollButtonImages;
    [SerializeField] public GameObject cancelButton;

    [Header("Scroll Panel")] [SerializeField]
    private GameObject scrollPanel;

    [SerializeField] private Button scrollBackButton;
    [SerializeField] private Image scrollBackButtonImage;

    [Header("Scroll Data")] [SerializeField]
    public ScrollData scrollData;

    public int[] selectedScrollIndices = new int[3];
    private bool[] usedScrollFlags = new bool[3];

    private int selectedSlotForPanel = -1; // Store which scroll slot was clicked

    public List<int> usedScrollHistory = new List<int>(); // Track used/discarded scrolls this match

    public string lastScrollEffectKey_Player = null;
    public string lastScrollEffectKey_AI = null;

    [Header("Extra Scroll")] [SerializeField]
    private Button extraScrollButton;

    [SerializeField] private Image extraScrollButtonImage;
    private int extraScrollIndex = -1;
    private bool extraScrollUsed = false;

    public static ScrollManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;  
        }
        else
        {
            return;
        }
    }


    void Start()
    {
        LoadSelectedScrolls();
        SetupScrollButtons();
        SetScrollsInteractable(false);

        // Hide panel initially
        if (scrollPanel != null)
            scrollPanel.SetActive(false);

        if (extraScrollButton != null)
            extraScrollButton.gameObject.SetActive(false);

        extraScrollButton.onClick.AddListener(OnExtraScrollClicked);


    }

    void Update()
    {
        bool isPlayerTurn = PieceMover.currentTurn == TurnType.Player;

        foreach (var btn in scrollButtons)
            btn.gameObject.SetActive(isPlayerTurn);

        if (extraScrollButton != null)
            extraScrollButton.gameObject.SetActive(isPlayerTurn && !extraScrollUsed && extraScrollIndex >= 0 &&
                                                   !PieceMover.playerScrollsDisabled);
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

            if (scrollButtonImages != null && i < scrollButtonImages.Length && scrollIndex >= 0 &&
                scrollIndex < scrollData.scrollSprites.Length)
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
        if (PieceMover.playerScrollsDisabled)
        {
            Debug.Log("[ScrollManager] Scrolls are disabled by Dominion of Kamo.");
            return;
        }

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

        if (!usedScrollHistory.Contains(selectedScrollIndices[scrollSlotIndex]))
            usedScrollHistory.Add(selectedScrollIndices[scrollSlotIndex]);

        usedScrollFlags[scrollSlotIndex] = true;
        scrollButtons[scrollSlotIndex].interactable = false;

        // Execute scroll effect
        if (scrollData != null && scrollIndex < scrollData.scrollEffectKeys.Length)
        {
            string effectKey = scrollData.scrollEffectKeys[scrollIndex];

            // Store the scroll being used so it can be echoed later
            if (effectKey != "Vault of Shadows" && effectKey != "Echo of the Twin")
            {
                lastScrollEffectKey_Player = effectKey;
            }

            ScrollEffectExecutor.Instance.ExecuteEffect(effectKey, false); // false = Player

        }


        if (scrollPanel != null)
        {
            scrollPanel.SetActive(false);
            cancelButton.SetActive(false);
            SetScrollsInteractable(false);
        }

        if (scrollData.scrollEffectKeys[scrollIndex] == "Earthbound’s Step")
        {
            return;
        }

        // Handle turn or rethrow
        if (PieceMover.lastMoveWasRethrow)
        {
            PieceMover.Instance.Invoke("UpdateThrowButtonState", 0.1f);
        }
        else
        {
            PieceMover.currentTurn = TurnType.AI;
            PieceMover.ResetTurn();
            PieceMover.Instance.Invoke("UpdateThrowButtonState", 0.1f);
            AiMover.StartStickThrow();
        }

        PieceMover.lastMoveWasRethrow = false;
    }

    public void SetScrollsInteractable(bool state)
    {
        if (PieceMover.playerScrollsDisabled)
            state = false;

        for (int i = 0; i < scrollButtons.Length; i++)
        {
            if (selectedScrollIndices[i] >= 0 && !usedScrollFlags[i])
            {
                scrollButtons[i].interactable = state;

                // Optional: Gray out visually if disabled by Dominion
                ColorBlock cb = scrollButtons[i].colors;
                cb.normalColor = state ? Color.white : new Color(0.5f, 0.5f, 0.5f);
                scrollButtons[i].colors = cb;
            }
            else
            {
                scrollButtons[i].interactable = false;
            }
        }

        if (!extraScrollUsed && extraScrollIndex >= 0 && !PieceMover.playerScrollsDisabled)
        {
            extraScrollButton.interactable = state;
            extraScrollButton.gameObject.SetActive(true);
        }
        else
        {
            extraScrollButton.interactable = false;
            extraScrollButton.gameObject.SetActive(false);
        }

        if (cancelButton != null)
            cancelButton.SetActive(state);

        if (PieceMover.Instance != null && PieceMover.Instance.throwButton != null)
            PieceMover.Instance.throwButton.SetActive(!state);
    }


    public void RestoreUsedScroll(int scrollIndex)
    {
        for (int i = 0; i < selectedScrollIndices.Length; i++)
        {
            if (selectedScrollIndices[i] == scrollIndex && usedScrollFlags[i])
            {
                usedScrollFlags[i] = false;
                scrollButtons[i].interactable = true;
                if (usedScrollHistory.Contains(scrollIndex))
                    usedScrollHistory.Remove(scrollIndex);
                Debug.Log($"Scroll {scrollIndex} has been restored!");
                break;
            }
        }
    }

    public void OnCancelButtonPressed()
    {
        AudioController.Instance.Play("ButtonTap");
        SetScrollsInteractable(false);

        if (PieceMover.lastMoveWasRethrow)
        {
            PieceMover.Instance.Invoke("UpdateThrowButtonState", 0.1f);
            if (PieceMover.sandsOfEsnaPlayer)
            {
                StickThrower.Instance.AutoThrowWithOptions();
            }
        }
        else
        {
            PieceMover.currentTurn = TurnType.AI;
            PieceMover.ResetTurn();
            AiMover.StartAITurn();
        }

        PieceMover.lastMoveWasRethrow = false;
        PieceMover.Instance.ShowTemporaryTurnMessage(PieceMover.currentTurn == TurnType.Player
            ? "Player Turn"
            : "AI Turn");
        FindAnyObjectByType<StickThrower>().GetComponent<StickThrower>().ShowStickVisuals();
    }

    private void OnExtraScrollClicked()
    {
        if (extraScrollUsed || extraScrollIndex < 0 || PieceMover.playerScrollsDisabled) return;


        // Set up back image
        if (scrollBackButtonImage != null && extraScrollIndex < scrollData.scrollBacks.Length)
        {
            scrollBackButtonImage.sprite = scrollData.scrollBacks[extraScrollIndex];
            scrollBackButtonImage.preserveAspect = true;
        }

        // Set scroll panel active
        if (scrollPanel != null)
            scrollPanel.SetActive(true);

        // Remove previous listeners and assign a new one for extra scroll usage
        scrollBackButton.onClick.RemoveAllListeners();
        scrollBackButton.onClick.AddListener(() => OnExtraScrollUsed());
    }

    private void OnExtraScrollUsed()
    {
        if (extraScrollUsed || extraScrollIndex < 0) return;

        extraScrollUsed = true;
        extraScrollButton.interactable = false;
        extraScrollButton.gameObject.SetActive(false);

        string effectKey = scrollData.scrollEffectKeys[extraScrollIndex];
        Debug.Log($"[Player] used extra scroll from Heka’s Blessing: {effectKey}");

        // Store the scroll being used so it can be echoed later
        if (effectKey != "Vault of Shadows" && effectKey != "Echo of the Twin")
        {
            lastScrollEffectKey_Player = effectKey;
        }


        ScrollEffectExecutor.Instance.ExecuteEffect(effectKey, false);

        if (scrollPanel != null)
            scrollPanel.SetActive(false);

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

    public void GrantHekasBlessingScroll(bool isAI)
    {
        List<int> eligibleScrolls = new List<int>();

        for (int i = 0; i < scrollData.scrollSprites.Length; i++)
        {
            if (PlayerPrefs.GetInt($"scroll_{i}_unlocked", 0) == 1 &&
                System.Array.IndexOf(selectedScrollIndices, i) == -1 &&
                i != extraScrollIndex) // prevent re-selection
            {
                eligibleScrolls.Add(i);
            }
        }

        if (eligibleScrolls.Count == 0)
        {
            Debug.LogWarning("No eligible scrolls for Heka's Blessing.");
            return;
        }

        int randomScroll = eligibleScrolls[Random.Range(0, eligibleScrolls.Count)];

        if (isAI)
        {
            AiMover.Instance.GrantExtraScroll(randomScroll);
            Debug.Log($"[AI] received Heka's Blessing scroll: {randomScroll}");
        }
        else
        {
            extraScrollIndex = randomScroll;
            extraScrollUsed = false;

            if (extraScrollButtonImage != null)
            {
                extraScrollButtonImage.sprite = scrollData.scrollSprites[randomScroll];
                extraScrollButtonImage.preserveAspect = true;
            }

            extraScrollButton.interactable = true;
            extraScrollButton.gameObject.SetActive(true);
        }
    }


    public void BackBtn()
    {
        scrollPanel.SetActive(false);
    }
}
