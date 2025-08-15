using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class StickThrower : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] public Button throwButton;
    [SerializeField] public Button skipButton;
    //[SerializeField] Text stickNumberText;
    [SerializeField] private Image stickNumberImage;
    [SerializeField] private Sprite defaultSprite;

    [Header("Stick References")]
    [SerializeField] List<Stick> stickObjects;
    [SerializeField] private GameObject stickVisualParent;
    
    [Header("Sound References")]
    [SerializeField] AudioSource throwSound;
    
    [SerializeField] Material highlightMaterial;
    [SerializeField] float throwDuration = 1f;

    [Header("Sands of Esna")] [SerializeField]
    private GameObject optionsParent;
    [SerializeField] private Button optionBtn;
    [SerializeField] private Button optionBtn1;
    [SerializeField] Image[] numberImages;
    

    private int randomThrowValue;
    public static bool obsidianCapNextMove_Player = false;
    public static bool obsidianCapNextMove_AI = false;
    private bool canThrow;

    public static StickThrower Instance;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates
        }
        AudioController.Instance.StopMusic("MainMenu");
        AudioController.Instance.PlayMusic("GamePlay");
        skipButton.gameObject.SetActive(false);
        PieceMover.lastMoveWasRethrow = true;
        PieceMover.lastStickValue = 0;
    }
    
    
    void Start()
    {
        canThrow = true;
        throwButton.gameObject.SetActive(true);
        throwButton.onClick.AddListener(() => StartCoroutine(ThrowSticksRoutine()));
        skipButton.onClick.AddListener(OnSkipPressed); // NEW
        skipButton.gameObject.SetActive(false);

        PieceMover.highlightMaterial = highlightMaterial;
        PieceMover.currentTurn = TurnType.Player;
        stickNumberImage.sprite = defaultSprite;
        ResetUI();
        UpdateThrowButtonState();
    }

    void Update()
    {
        UpdateThrowButtonState();
    }
    public void AutoThrowWithOptions()
    {
        StartCoroutine(AutoThrowWithOptionsRoutine(PieceMover.currentTurn == TurnType.AI));
    }

    private IEnumerator AutoThrowWithOptionsRoutine(bool isAI)
    {
        stickNumberImage.sprite = defaultSprite;
        //stickNumberText.text = "";
        stickVisualParent.SetActive(true);
        throwButton.gameObject.SetActive(false);
        skipButton.gameObject.SetActive(false);

        AudioController.Instance.PlaySound("StickThrow");

        int faceUpCount = Random.Range(0, 5);
        int rolledValue = (faceUpCount == 0) ? 5 : faceUpCount;

        List<int> indices = new List<int>();
        for (int i = 0; i < stickObjects.Count; i++)
            indices.Add(i);
        Shuffle(indices);

        for (int i = 0; i < stickObjects.Count; i++)
        {
            bool shouldBeFaceUp = (i < faceUpCount);
            stickObjects[indices[i]].PrepareFaceUpState(shouldBeFaceUp);
            stickObjects[indices[i]].StartFlip();
        }

        yield return new WaitForSeconds(throwDuration);

        foreach (Stick stick in stickObjects)
            stick.StopAndSnapRotation();

        // Assign to optionBtn
        int otherValue;
        do
        {
            otherValue = Random.Range(1, 6);
        } while (otherValue == rolledValue);

        optionBtn.image.sprite = numberImages[rolledValue - 1].sprite;
        optionBtn1.image.sprite = numberImages[otherValue - 1].sprite;

        optionsParent.SetActive(true);

        if (isAI)
        {
            optionBtn.onClick.RemoveAllListeners();
            optionBtn1.onClick.RemoveAllListeners();

            yield return new WaitForSeconds(1f);

            int chosen = Random.Range(0, 2) == 0 ? rolledValue : otherValue;
            Debug.Log("[Sands of Esna] AI selected value: " + chosen);
            OnOptionSelected(chosen);
        }
        else
        {
            optionBtn.onClick.RemoveAllListeners();
            optionBtn1.onClick.RemoveAllListeners();

            optionBtn.onClick.AddListener(() => OnOptionSelected(rolledValue));
            optionBtn1.onClick.AddListener(() => OnOptionSelected(otherValue));
        }
    }

    private void OnOptionSelected(int value)
    {
        Debug.Log("Selected stick value: " + value);
        PieceMover.lastStickValue = value;
        optionsParent.SetActive(false);
        //stickNumberText.text = value.ToString();
        stickNumberImage.sprite = numberImages[value - 1].sprite;

        if (!PieceMover.HasAnyValidMove(false) && PieceMover.currentTurn == TurnType.Player)
        {
            Debug.Log("No valid move — passing turn.");
            PieceMover.PassTurnImmediately();
        }
        else if(!PieceMover.HasAnyValidMove(true) && PieceMover.currentTurn == TurnType.AI)
        {
            Debug.Log("AI has no valid move — passing turn.");
            PieceMover.PassTurnImmediately();
        }
        else
        {
            PieceMover.moveInProgress = false;
            if (PieceMover.currentTurn == TurnType.AI)
            {
                Debug.Log("AI starting move after Sands of Esna throw");
                StartCoroutine(AiMover.Instance.ExecuteAiTurn());
            }
        }



        StartCoroutine(HideStickVisualsAfterDelay());
    }


    public IEnumerator ThrowSticksRoutine()
    {
        AudioController.Instance.PlaySound("StickThrow");
        throwButton.gameObject.SetActive(false);
        skipButton.gameObject.SetActive(true); // Show skip after throwing

        // Step 1: Random number of face-up sticks (0–4)
        int faceUpCount = Random.Range(0, 5);
        randomThrowValue = (faceUpCount == 0) ? 5 : faceUpCount;

        Debug.Log($"[Pre-throw] Face-up: {faceUpCount}, throwValue: {randomThrowValue}");

        // Shuffle sticks and assign face-up state
        List<int> indices = new List<int>();
        for (int i = 0; i < stickObjects.Count; i++)
            indices.Add(i);
        Shuffle(indices);

        for (int i = 0; i < stickObjects.Count; i++)
        {
            bool shouldBeFaceUp = (i < faceUpCount);
            stickObjects[indices[i]].PrepareFaceUpState(shouldBeFaceUp);
            stickObjects[indices[i]].StartFlip();
        }

        yield return new WaitForSeconds(throwDuration);

        foreach (Stick stick in stickObjects)
        {
            stick.StopAndSnapRotation(); // snap to correct state
        }

        // Default assignment
        int finalValue = randomThrowValue;

// 🧱 Obsidian’s Burden effect (max cap to 2)
        if (PieceMover.currentTurn == TurnType.Player && obsidianCapNextMove_Player)
        {
            if (finalValue > 2)
            {
                Debug.Log("[Obsidian’s Burden] Player's roll capped from " + finalValue + " to 2");
                finalValue = 2;
            }

            obsidianCapNextMove_Player = false;
        }
        else if (PieceMover.currentTurn == TurnType.AI && obsidianCapNextMove_AI)
        {
            if (finalValue > 2)
            {
                Debug.Log("[Obsidian’s Burden] AI's roll capped from " + finalValue + " to 2");
                finalValue = 2;
            }

            obsidianCapNextMove_AI = false;
        }

        PieceMover.lastStickValue = finalValue;
        //stickNumberText.text = finalValue.ToString();
        stickNumberImage.sprite = numberImages[finalValue - 1].sprite;


// Apply Horus Retreat penalty
        if (PieceMover.horusPenaltyPending)
        {
            PieceMover.lastStickValue = Mathf.Max(0, PieceMover.lastStickValue - 1);
            Debug.Log("[Horus Retreat] Applied -1 penalty. New stick value: " + PieceMover.lastStickValue);
            PieceMover.horusPenaltyPending = false; // Clear after applying
        }


        PieceMover.moveInProgress = false;

        Debug.Log($"[Post-throw] Applied throwValue = {randomThrowValue}");

        if (!PieceMover.HasAnyValidMove(false))
        {
            Debug.Log("No valid move — passing turn.");
            PieceMover.PassTurnImmediately();
        }

        StartCoroutine(HideStickVisualsAfterDelay());
    }

    public void UpdateThrowButtonState()
    {
        if (PieceMover.currentTurn == TurnType.Player)
        {
            canThrow = PieceMover.lastStickValue == 0 && !PieceMover.moveInProgress;
            if (!PieceMover.sandsOfEsnaPlayer)
            {
                throwButton.gameObject.SetActive(canThrow);
            }
            skipButton.gameObject.SetActive(!canThrow);
        }
        else
        {
            throwButton.gameObject.SetActive(false);
            skipButton.gameObject.SetActive(false);
        }
    }

    public void ResetUI()
    {
        //stickNumberText.text = "";
        throwButton.interactable = true;
        throwButton.gameObject.SetActive(true);
        skipButton.gameObject.SetActive(false); // Hide on reset
    }

    public void OnSkipPressed()
    {
        Debug.Log("Skip pressed — passing turn.");
        //PieceMover.PassTurnImmediately();
        AudioController.Instance.PlaySound("ButtonTap");
        PieceMover.ResetTurn();
        PieceMover.currentTurn = TurnType.AI;
        UpdateThrowButtonState();

        if (PieceMover.Instance != null)
        {
            PieceMover.Instance.ShowTemporaryTurnMessage(
                PieceMover.currentTurn == TurnType.Player ? "Player" : "Opponent"
            );
        }

        ShowStickVisuals();
        AiMover.StartStickThrow();
    }


    //Call this from PieceMover when a rethrow happens
    public void EnableThrowButtonAfterRethrow()
    {
        throwButton.gameObject.SetActive(true);
        skipButton.gameObject.SetActive(false);
    }

    // Utility to shuffle stick indices
    private void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            int temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }
    public void ShowStickVisuals()
    {
        if (stickVisualParent != null)
            stickVisualParent.SetActive(true);
    }

    public void HideStickVisuals()
    {
        /*if (stickVisualParent != null)
            stickVisualParent.SetActive(false);*/
    }
    private IEnumerator HideStickVisualsAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        HideStickVisuals(); // only visuals disappear, text stays
    }

}
