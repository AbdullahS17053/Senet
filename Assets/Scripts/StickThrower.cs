using UnityEngine;
using UnityEngine.UI;

public class StickThrower : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] Button throwButton;
    [SerializeField] Image stickImageDisplay;
    [SerializeField] Text stickNumberText;

    [Header("Stick Images (named 1, 2, 3, 4, 5)")]
    [SerializeField] Sprite[] stickSprites; // Add 5 images with names 1 to 5 in the Inspector

    void Start()
    {
        throwButton.onClick.AddListener(ThrowStick);
        ResetUI();
    }

    void ThrowStick()
    {
        // Randomly pick one stick (1 to 5)
        int randomIndex = Random.Range(0, stickSprites.Length);
        Sprite selectedSprite = stickSprites[randomIndex];

        // Get stick number from sprite name
        int stickNumber;
        if (!int.TryParse(selectedSprite.name, out stickNumber))
        {
            Debug.LogError("Stick image name is not a number!");
            return;
        }

        // Show sprite and number
        stickImageDisplay.enabled = true;
        stickImageDisplay.sprite = selectedSprite;
        stickNumberText.text = stickNumber.ToString();

        // Rules for enabling/disabling throw button
        if (stickNumber == 1 || stickNumber == 4)
        {
            throwButton.gameObject.SetActive(true); // Throw again
        }
        else
        {
            throwButton.gameObject.SetActive(false); // End turn
        }
    }

    public void ResetUI()
    {
        stickImageDisplay.sprite = null;
        stickImageDisplay.enabled = false;
        stickNumberText.text = "";
        throwButton.interactable = true;
    }
}