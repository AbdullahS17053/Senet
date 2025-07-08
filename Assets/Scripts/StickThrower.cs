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
        UpdateThrowButtonState();
    }

    void ThrowStick()
    {
        int stickNumber = ThrowStickAndShowUI();

        // Disable throw button for now (player must move or auto-pass)
        throwButton.gameObject.SetActive(false);

        // ✅ Now check if player has valid moves
        if (!PieceMover.HasAnyValidMove(false)) // false = player
        {
            Debug.Log("Player has no valid moves. Passing turn...");
            PieceMover.PassTurnImmediately();
        }
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

    public void UpdateThrowButtonState()
    {
        if (PieceMover.currentTurn == TurnType.Player)
        {
            // Enable throw button only if not already thrown and move not in progress
            bool canThrow = PieceMover.lastStickValue == 0 && !PieceMover.moveInProgress;
            throwButton.gameObject.SetActive(canThrow);
        }
        else
        {
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
}
