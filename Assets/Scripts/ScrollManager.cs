using UnityEngine;
using UnityEngine.UI;

public class ScrollManager : MonoBehaviour
{
    [Header("Scroll UI")]
    [SerializeField] private Button[] scrollButtons;
    [SerializeField] private Text[] scrollButtonTexts;
    [SerializeField] private Image[] scrollButtonImages;

    [Header("Scroll Data")]
    [SerializeField] private ScrollData scrollData;  // Reference to the ScriptableObject

    private int[] selectedScrollIndices = new int[3];

    void Start()
    {
        LoadSelectedScrolls();
        SetupScrollButtons();
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
                    count++;
                }
            }
        }

        for (int i = count; i < 3; i++)
            selectedScrollIndices[i] = -1;
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
            int capturedIndex = scrollIndex;
            if (capturedIndex >= 0)
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

    private void OnScrollUsed(int scrollIndex)
    {
        Debug.Log($"Scroll {scrollIndex} used!");

        for (int i = 0; i < scrollButtons.Length; i++)
        {
            if (selectedScrollIndices[i] == scrollIndex)
            {
                scrollButtons[i].interactable = false;
                break;
            }
        }
    }
}
