using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StickThrower : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] public Button throwButton;
    [SerializeField] private Button skipButton;        // NEW
    [SerializeField] Text stickNumberText;

    [Header("Stick References")]
    [SerializeField] List<Stick> stickObjects;
    [SerializeField] private GameObject stickVisualParent;
    
    [Header("Sound References")]
    [SerializeField] AudioSource throwSound;
    
    [SerializeField] Material highlightMaterial;
    [SerializeField] float throwDuration = 1f;

    private int randomThrowValue;

    void Start()
    {
        throwButton.onClick.AddListener(() => StartCoroutine(ThrowSticksRoutine()));
        skipButton.onClick.AddListener(OnSkipPressed); // NEW

        PieceMover.highlightMaterial = highlightMaterial;

        ResetUI();
        UpdateThrowButtonState();
    }

    void Update()
    {
        UpdateThrowButtonState();
    }

    public IEnumerator ThrowSticksRoutine()
    {
        throwSound.Play();
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

        stickNumberText.text = randomThrowValue.ToString();
        PieceMover.lastStickValue = randomThrowValue;

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
            bool canThrow = PieceMover.lastStickValue == 0 && !PieceMover.moveInProgress;
            throwButton.gameObject.SetActive(canThrow);
            skipButton.gameObject.SetActive(!canThrow); // Opposite visibility
        }
        else
        {
            throwButton.gameObject.SetActive(false);
            skipButton.gameObject.SetActive(false);
        }
    }

    public void ResetUI()
    {
        stickNumberText.text = "";
        throwButton.interactable = true;
        throwButton.gameObject.SetActive(true);
        skipButton.gameObject.SetActive(false); // Hide on reset
    }

    private void OnSkipPressed()
    {
        Debug.Log("Skip pressed — passing turn.");
        PieceMover.PassTurnImmediately();
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
        if (stickVisualParent != null)
            stickVisualParent.SetActive(false);
    }
    private IEnumerator HideStickVisualsAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        HideStickVisuals(); // only visuals disappear, text stays
    }

}
