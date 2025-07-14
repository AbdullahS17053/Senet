using UnityEngine;
using UnityEngine.UI;

public class ScrollManager : MonoBehaviour
{
    [Header("Scroll UI")]
    [SerializeField] private Button[] scrollButtons;
    [SerializeField] private Text[] scrollButtonTexts;
    [SerializeField] private Image[] scrollButtonImages;
    [SerializeField] private GameObject cancelButton;

    [Header("Scroll Data")]
    [SerializeField] private ScrollData scrollData;  // Reference to the ScriptableObject

    private int[] selectedScrollIndices = new int[3];
    private bool[] usedScrollFlags = new bool[3]; // Track used scrolls

    void Start()
    {
        LoadSelectedScrolls();
        SetupScrollButtons();
        SetScrollsInteractable(false);
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
                    usedScrollFlags[count] = false; // initialize as unused
                    count++;
                }
            }
        }

        for (int i = count; i < 3; i++)
        {
            selectedScrollIndices[i] = -1;
            usedScrollFlags[i] = true; // invalid index means unusable
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
                scrollButtons[i].onClick.AddListener(() => OnScrollUsed(capturedIndex));
                scrollButtons[i].interactable = true;
            }
            else
            {
                scrollButtons[i].interactable = false;
            }
        }
    }

    private void OnScrollUsed(int scrollSlotIndex)
    {
        int scrollIndex = selectedScrollIndices[scrollSlotIndex];
        Debug.Log($"Scroll {scrollIndex} used!");

        // Mark this scroll as used
        usedScrollFlags[scrollSlotIndex] = true;
        scrollButtons[scrollSlotIndex].interactable = false;

        // Your scroll effect logic can go here...
        if (scrollData != null && scrollIndex < scrollData.scrollEffectKeys.Length)
        {
            string effectKey = scrollData.scrollEffectKeys[scrollIndex];
            ScrollEffectExecutor.Instance.ExecuteEffect(effectKey, false); // false = player
        }


        SetScrollsInteractable(false); // Hide scrolls and cancel button after use

        // --- Handle turn change or rethrow ---
        if (PieceMover.lastMoveWasRethrow)
        {
            // Player gets another throw
            PieceMover.Instance.Invoke("UpdateThrowButtonState", 0.1f);
        }
        else
        {
            // Turn goes to AI
            PieceMover.currentTurn = TurnType.AI;
            PieceMover.ResetTurn(); // Clear selection
            AiMover.StartStickThrow();
        }

        // Reset flag after processing
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
        
        // Disable throw button when showing scrolls
        if (PieceMover.Instance != null && PieceMover.Instance.throwButton != null)
            PieceMover.Instance.throwButton.SetActive(!state);
    }

    public void OnCancelButtonPressed()
    {
        SetScrollsInteractable(false); // Hide scrolls and cancel button

        if (PieceMover.lastMoveWasRethrow)
        {
            // Player gets another throw
            PieceMover.Instance.Invoke("UpdateThrowButtonState", 0.1f);
        }
        else
        {
            // Turn goes to AI
            PieceMover.currentTurn = TurnType.AI;
            PieceMover.ResetTurn(); // Clear selection
            AiMover.StartStickThrow();
        }

        // Reset this after handling
        PieceMover.lastMoveWasRethrow = false;
    }
    
}
