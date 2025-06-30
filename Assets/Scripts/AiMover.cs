using System.Collections;
using UnityEngine;

public class AiMover : MonoBehaviour
{
    public static AiMover Instance;
    private PieceMover[] aiPieces;

    private void Awake()
    {
        Instance = this;
    }

    public static void StartStickThrow()
    {
        if (Instance != null)
            Instance.StartCoroutine(Instance.ThrowSticksAndPlay());
    }

    private IEnumerator ThrowSticksAndPlay()
    {
        yield return new WaitForSeconds(1f); // Optional delay before stick throw

        StickThrower stickThrower = FindObjectOfType<StickThrower>();
        if (stickThrower != null)
        {
            int stickValue = stickThrower.ThrowStickAndShowUI();
            Debug.Log("AI Stick Value: " + stickValue);
        }
        else
        {
            Debug.LogError("StickThrower not found in scene!");
            yield break;
        }

        yield return new WaitForSeconds(1f); // Delay before movement

        StartCoroutine(ExecuteAiTurn());
    }

    public static void StartAITurn()
    {
        StartStickThrow(); // Instead of moving immediately, throw sticks first
    }

    private IEnumerator ExecuteAiTurn()
    {
        aiPieces = FindObjectsOfType<PieceMover>();
        int chosenIndex = -1;
        int targetIndex = -1;

        for (int i = 0; i < aiPieces.Length; i++)
        {
            if (!aiPieces[i].isAI) continue;

            int currentIndex = aiPieces[i].GetCurrentTileIndex();
            if (currentIndex == -1) continue;

            int proposedIndex = currentIndex + PieceMover.lastStickValue;
            if (proposedIndex >= 30) continue;

            Transform tile = aiPieces[i].transform.parent.parent.GetChild(proposedIndex);
            PieceMover occupying = tile.GetComponentInChildren<PieceMover>();

            // Can move if tile is empty or has player piece (replace)
            if (occupying == null || occupying.isAI == false)
            {
                chosenIndex = i;
                targetIndex = proposedIndex;
                break;
            }
        }

        if (chosenIndex != -1)
        {
            Transform targetTile = aiPieces[chosenIndex].transform.parent.parent.GetChild(targetIndex);
            yield return aiPieces[chosenIndex].StartCoroutine(aiPieces[chosenIndex].MoveToTile(targetTile));
        }
        else
        {
            // No valid move found — switch turn back to player
            yield return new WaitForSeconds(0.5f);
            PieceMover.currentTurn = TurnType.Player;

            // Optional: update throw button if handled in PieceMover
            GameObject throwBtn = GameObject.FindWithTag("ThrowButton");
            if (throwBtn != null)
                throwBtn.SetActive(true);
        }
    }
}
