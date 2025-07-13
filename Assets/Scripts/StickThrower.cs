using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StickThrower : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] public Button throwButton;
    [SerializeField] Text stickNumberText;

    [Header("Stick References")]
    [SerializeField] List<Stick> stickObjects;

    [SerializeField] Material highlightMaterial;
    [SerializeField] float throwDuration = 1f;

    private int randomThrowValue;

    void Start()
    {
        throwButton.onClick.AddListener(() => StartCoroutine(ThrowSticksRoutine()));
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
        throwButton.gameObject.SetActive(false);

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
        PieceMover.moveInProgress = false;

        Debug.Log($"[Post-throw] Applied throwValue = {randomThrowValue}");

        if (!PieceMover.HasAnyValidMove(false))
        {
            Debug.Log("No valid move — passing turn.");
            PieceMover.PassTurnImmediately();
        }
    }

    public void UpdateThrowButtonState()
    {
        if (PieceMover.currentTurn == TurnType.Player)
        {
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
        stickNumberText.text = "";
        throwButton.interactable = true;
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
}
