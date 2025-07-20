using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    [Header("Scroll Data")]
    [SerializeField] private ScrollData scrollData;

    [Header("UI References")]
    [SerializeField] private Button[] scrollButtons;

    [Header("Select Panel")]
    [SerializeField] private GameObject selectPanel;
    [SerializeField] private Image scrollBackImage;
    [SerializeField] private Button selectButton;
    [SerializeField] private Sprite selectSprite;
    [SerializeField] private Sprite unselectSprite;
    [SerializeField] private Sprite lockedSprite;

    private const int maxSelectableScrolls = 3;
    private List<int> selectedScrollIndices = new List<int>();
    private int currentIndex = -1;

    private void Start()
    {
        InitializeDefaultScrolls();
        LoadSelectedScrolls();

        for (int i = 0; i < scrollButtons.Length; i++)
        {
            int capturedIndex = i;
            scrollButtons[i].onClick.AddListener(() => OnScrollButtonClicked(capturedIndex));
        }

        selectButton.onClick.AddListener(OnSelectButtonClicked);
        selectPanel.SetActive(false);
    }

    private void InitializeDefaultScrolls()
    {
        for (int i = 0; i < scrollData.scrollSprites.Length; i++)
        {
            bool isUnlocked = i < 10; // ✅ Only first 10 unlocked
            PlayerPrefs.SetInt($"scroll_{i}_unlocked", isUnlocked ? 1 : 0);

            // Select first 3 from unlocked scrolls only
            if (isUnlocked && i < maxSelectableScrolls)
                PlayerPrefs.SetInt($"scroll_{i}_selected", 1);
            else
                PlayerPrefs.SetInt($"scroll_{i}_selected", 0);
        }

        PlayerPrefs.Save();
    }



    private void LoadSelectedScrolls()
    {
        selectedScrollIndices.Clear();
        int count = 0;

        for (int i = 0; i < scrollData.scrollSprites.Length; i++)
        {
            if (PlayerPrefs.GetInt($"scroll_{i}_selected", 0) == 1)
            {
                selectedScrollIndices.Add(i);
                PlayerPrefs.SetInt($"selected_scroll_{count}", i);
                count++;
                if (count >= maxSelectableScrolls) break;
            }
        }

        for (int i = count; i < maxSelectableScrolls; i++)
        {
            PlayerPrefs.SetInt($"selected_scroll_{i}", -1);
        }

        PlayerPrefs.Save();
    }

    private void OnScrollButtonClicked(int index)
    {
        currentIndex = index;

        bool isUnlocked = PlayerPrefs.GetInt($"scroll_{index}_unlocked", 0) == 1;
        bool isSelected = PlayerPrefs.GetInt($"scroll_{index}_selected", 0) == 1;

        // Set back image
        if (scrollBackImage != null && index < scrollData.scrollBacks.Length)
        {
            scrollBackImage.sprite = scrollData.scrollBacks[index];
        }

        // Set select button sprite
        if (!isUnlocked)
        {
            selectButton.image.sprite = lockedSprite;
            selectButton.interactable = false;
        }
        else
        {
            selectButton.interactable = true;
            selectButton.image.sprite = isSelected ? unselectSprite : selectSprite;
        }

        selectPanel.SetActive(true);
    }

    private void OnSelectButtonClicked()
    {
        if (currentIndex < 0 || currentIndex >= scrollData.scrollSprites.Length) return;

        bool isUnlocked = PlayerPrefs.GetInt($"scroll_{currentIndex}_unlocked", 0) == 1;
        bool isSelected = PlayerPrefs.GetInt($"scroll_{currentIndex}_selected", 0) == 1;

        if (!isUnlocked) return;

        if (isSelected)
        {
            PlayerPrefs.SetInt($"scroll_{currentIndex}_selected", 0);
        }
        else
        {
            if (selectedScrollIndices.Count >= maxSelectableScrolls)
                return;

            PlayerPrefs.SetInt($"scroll_{currentIndex}_selected", 1);
        }

        PlayerPrefs.Save();
        LoadSelectedScrolls();

        // Update button sprite again
        bool nowSelected = PlayerPrefs.GetInt($"scroll_{currentIndex}_selected", 0) == 1;
        selectButton.image.sprite = nowSelected ? unselectSprite : selectSprite;
    }

    public void CloseSelctPanel()
    {
        selectPanel.SetActive(false);
    }
}
