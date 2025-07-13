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

        // Start flipping animation
        foreach (Stick stick in stickObjects)
        {
            stick.StartFlip();
        }

        yield return new WaitForSeconds(throwDuration);

        // Snap stick rotations first BEFORE checking IsFaceUp
        foreach (Stick stick in stickObjects)
        {
            stick.StopAndSnapRotation();  // Must run BEFORE IsFaceUp
        }

        // Now check how many are face up
        int actualFaceUpCount = 0;
        foreach (Stick stick in stickObjects)
        {
            if (stick.IsFaceUp())  // Must reflect snapped rotation
                actualFaceUpCount++;
        }

        // Game logic value: treat 0 as 5
        int throwValue = (actualFaceUpCount == 0) ? 5 : actualFaceUpCount;

        // Display and store result
        stickNumberText.text = throwValue.ToString();
        PieceMover.lastStickValue = throwValue;
        PieceMover.moveInProgress = false;

        Debug.Log($"Stick result: {actualFaceUpCount} face-up → throwValue = {throwValue}");

        // Handle turn flow
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
}
