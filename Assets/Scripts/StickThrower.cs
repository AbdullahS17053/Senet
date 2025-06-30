using UnityEngine;
using UnityEngine.UI;

public class StickThrower : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] public Button throwButton;
    [SerializeField] Image stickImageDisplay;
    [SerializeField] Text stickNumberText;

    [Header("Stick Images (named 1, 2, 3, 4, 5)")]
    [SerializeField] Sprite[] stickSprites;

    [SerializeField] Material highlightMaterial;

    void Start()
    {
        throwButton.onClick.AddListener(ThrowStick);
        PieceMover.highlightMaterial = highlightMaterial;
        ResetUI();

        UpdateThrowButtonState();
    }

    void Update()
    {
        // Continuously check and toggle throw button based on current turn
        UpdateThrowButtonState();
    }

    void ThrowStick()
    {
        // Randomly pick one stick (1 to 5)
        int randomIndex = Random.Range(0, stickSprites.Length);
        Sprite selectedSprite = stickSprites[randomIndex];

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

        // Set stick value globally
        PieceMover.lastStickValue = stickNumber;
        PieceMover.moveInProgress = false;

        // Disable throw button for now (player must move piece)
        throwButton.gameObject.SetActive(false);

        // Optional: auto-end turn if no move is possible (not included here)
    }

    void UpdateThrowButtonState()
    {
        if (PieceMover.currentTurn == TurnType.Player)
        {
            // Enable throw button only if no value is set and no move is happening
            bool canThrow = PieceMover.lastStickValue == 0 && !PieceMover.moveInProgress;
            throwButton.gameObject.SetActive(canThrow);
        }
        else
        {
            // AI's turn — hide button
            throwButton.gameObject.SetActive(false);
        }
    }

    public void ResetUI()
    {
        stickImageDisplay.sprite = null;
        stickImageDisplay.enabled = false;
        stickNumberText.text = "";
        throwButton.interactable = true;
    }
    public int ThrowStickAndShowUI()
    {
        // Randomly pick one stick (1 to 5)
        int randomIndex = Random.Range(0, stickSprites.Length);
        Sprite selectedSprite = stickSprites[randomIndex];

        int stickNumber;
        if (!int.TryParse(selectedSprite.name, out stickNumber))
        {
            Debug.LogError("Stick image name is not a number!");
            return 0;
        }

        // Show sprite and number
        stickImageDisplay.enabled = true;
        stickImageDisplay.sprite = selectedSprite;
        stickNumberText.text = stickNumber.ToString();

        // Set global stick value
        PieceMover.lastStickValue = stickNumber;
        PieceMover.moveInProgress = false;

        return stickNumber;
    }

}
