using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    [Header("Scroll Data")]
    [SerializeField] private ScrollData scrollData;

    [Header("UI References")]
    [SerializeField] private Text[] buttonTexts;
    [SerializeField] private Image[] scrollButtonImages;
    [SerializeField] private Text[] scrollNameTexts;              // New: Display scroll names
    [SerializeField] private Text scrollText;
    [SerializeField] private Button[] selectButtons;              // New: Separate select/unselect buttons
    [SerializeField] private Button[] scrollButtons;              // New: Separate scroll image buttons

    [Header("Toaster Settings")]
    [SerializeField] private GameObject toasterPrefab;
    [SerializeField] private Transform toasterParent;
    [SerializeField] private float toasterDuration = 2f;
    [SerializeField] private float bounceScale = 1.2f;
    [SerializeField] private float bounceSpeed = 0.1f;

    private const int maxSelectableScrolls = 3;
    private List<int> selectedScrollIndices = new List<int>();
    private bool[] isScrollBackVisible;
    private GameObject currentToaster;

    void Start()
    {
        isScrollBackVisible = new bool[scrollData.scrollSprites.Length];

        InitializeDefaultScrolls();
        LoadSelectedScrolls();
        UpdateInventoryUI();
        UpdateScrollText();

        for (int i = 0; i < scrollButtons.Length; i++)
        {
            int capturedIndex = i;
            scrollButtons[i].onClick.AddListener(() => ToggleScrollFrontBack(capturedIndex));
        }

        for (int i = 0; i < selectButtons.Length; i++)
        {
            int capturedIndex = i;
            selectButtons[i].onClick.AddListener(() => ToggleSelectScroll(capturedIndex));
        }
    }

    private void InitializeDefaultScrolls()
    {
        int count = scrollData.scrollSprites.Length;

        for (int i = 0; i < count; i++)
        {
            string unlockKey = $"scroll_{i}_unlocked";
            string selectKey = $"scroll_{i}_selected";

            if (i < maxSelectableScrolls)
            {
                PlayerPrefs.SetInt(unlockKey, 1);
                PlayerPrefs.SetInt(selectKey, 1);
            }
            else
            {
                PlayerPrefs.SetInt(unlockKey, 0);
                PlayerPrefs.SetInt(selectKey, 0);
            }
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

    public void ToggleSelectScroll(int index)
    {
        if (scrollData == null || index >= scrollData.scrollSprites.Length)
        {
            Debug.LogWarning("Invalid scroll index.");
            return;
        }

        string unlockedKey = $"scroll_{index}_unlocked";
        if (PlayerPrefs.GetInt(unlockedKey, 0) != 1)
        {
            ShowToaster("Scroll not unlocked yet!");
            return;
        }

        bool isSelected = selectedScrollIndices.Contains(index);

        if (isSelected)
        {
            PlayerPrefs.SetInt($"scroll_{index}_selected", 0);
            selectedScrollIndices.Remove(index);
            ShowToaster("Scroll Unselected");
        }
        else
        {
            if (selectedScrollIndices.Count >= maxSelectableScrolls)
            {
                ShowToaster("You can only select 3 scrolls!");
                return;
            }

            PlayerPrefs.SetInt($"scroll_{index}_selected", 1);
            selectedScrollIndices.Add(index);
            ShowToaster("Scroll Selected");
        }

        PlayerPrefs.Save();
        LoadSelectedScrolls();
        UpdateInventoryUI();
        UpdateScrollText();
    }

    private void UpdateInventoryUI()
    {
        for (int i = 0; i < scrollData.scrollSprites.Length; i++)
        {
            bool isUnlocked = PlayerPrefs.GetInt($"scroll_{i}_unlocked", 0) == 1;
            bool isSelected = PlayerPrefs.GetInt($"scroll_{i}_selected", 0) == 1;

            if (i < buttonTexts.Length)
            {
                if (!isUnlocked)
                {
                    buttonTexts[i].text = "Locked";
                    buttonTexts[i].color = Color.red;
                }
                else if (isSelected)
                {
                    buttonTexts[i].text = "Selected";
                    buttonTexts[i].color = Color.green;
                }
                else
                {
                    buttonTexts[i].text = "Select";
                    buttonTexts[i].color = Color.yellow;
                }
            }

            if (i < scrollButtonImages.Length)
            {
                scrollButtonImages[i].sprite = isScrollBackVisible[i] ? scrollData.scrollBacks[i] : scrollData.scrollSprites[i];
                scrollButtonImages[i].preserveAspect = true;
            }

            if (i < scrollNameTexts.Length && scrollData.scrollNames.Length > i)
            {
                scrollNameTexts[i].text = scrollData.scrollNames[i];
            }
        }
    }

    private void UpdateScrollText()
    {
        if (scrollText != null)
        {
            scrollText.text = $"Selected: {selectedScrollIndices.Count}/3";
        }
    }

    private void ToggleScrollFrontBack(int index)
    {
        if (index < 0 || index >= isScrollBackVisible.Length) return;

        isScrollBackVisible[index] = !isScrollBackVisible[index];
        UpdateInventoryUI();
    }

    private void ShowToaster(string message)
    {
        if (currentToaster != null)
        {
            Destroy(currentToaster);
        }

        if (toasterPrefab != null && toasterParent != null)
        {
            GameObject toasterInstance = Instantiate(toasterPrefab, toasterParent);
            Text toasterText = toasterInstance.GetComponentInChildren<Text>();
            if (toasterText != null)
            {
                toasterText.text = message;
                StartCoroutine(AnimateToaster(toasterText.transform, toasterInstance));
            }
            currentToaster = toasterInstance;
            Destroy(toasterInstance, toasterDuration + (bounceSpeed * 4));
        }
        else
        {
            Debug.LogWarning("Toaster Prefab or Parent not assigned.");
        }
    }

    private IEnumerator AnimateToaster(Transform toasterTransform, GameObject toaster)
    {
        Vector3 originalScale = toasterTransform.localScale;
        float timer = 0f;
        while (timer < bounceSpeed * 2)
        {
            timer += Time.deltaTime;
            float scaleFactor = Mathf.Lerp(0f, bounceScale, timer / bounceSpeed);
            toasterTransform.localScale = originalScale * scaleFactor;
            yield return null;
        }

        timer = 0f;
        while (timer < bounceSpeed * 2)
        {
            timer += Time.deltaTime;
            float scaleFactor = Mathf.Lerp(bounceScale, 1f, timer / bounceSpeed);
            toasterTransform.localScale = originalScale * scaleFactor;
            yield return null;
        }

        yield return new WaitForSeconds(toasterDuration);

        timer = 0f;
        while (timer < bounceSpeed * 2)
        {
            timer += Time.deltaTime;
            float scaleFactor = Mathf.Lerp(1f, bounceScale, timer / bounceSpeed);
            toasterTransform.localScale = originalScale * scaleFactor;
            yield return null;
        }

        toasterTransform.localScale = Vector3.zero;
        if (toaster != null)
        {
            Destroy(toaster);
            currentToaster = null;
        }
    }
}
