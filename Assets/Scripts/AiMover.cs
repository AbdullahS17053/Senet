using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiMover : MonoBehaviour
{
    public static AiMover Instance;
    private PieceMover[] aiPieces;
    private bool[] aiScrollUsed = new bool[3];
    private int[] aiScrollIndices = new int[3]; // same as player's selectedScrollIndices
    
    [SerializeField] ScrollData scrollData;


    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        // Copy player's scroll indices from PlayerPrefs
        int count = 0;
        for (int i = 0; i < 20; i++)
        {
            if (PlayerPrefs.GetInt($"scroll_{i}_selected", 0) == 1)
            {
                if (count < 3)
                {
                    aiScrollIndices[count] = i;
                    aiScrollUsed[count] = false;
                    count++;
                }
            }
        }
    }

    public static void StartStickThrow()
    {
        if (Instance != null)
            Instance.StartCoroutine(Instance.ThrowSticksAndPlay());
    }

    private IEnumerator ThrowSticksAndPlay()
    {
        yield return new WaitForSeconds(1f); // Optional delay

        StickThrower stickThrower = FindObjectOfType<StickThrower>();
        if (stickThrower != null)
        {
            yield return stickThrower.StartCoroutine(stickThrower.ThrowSticksRoutine()); // run actual flipping
            Debug.Log("AI Stick Value: " + PieceMover.lastStickValue);

        }
        else
        {
            Debug.LogError("StickThrower not found in scene!");
            yield break;
        }

        yield return new WaitForSeconds(1f); // Delay to simulate thinking

        if (!PieceMover.HasAnyValidMove(true))
        {
            Debug.Log("AI has no valid moves. Passing turn...");
            PieceMover.PassTurnImmediately();
            yield break;
        }

        StartCoroutine(ExecuteAiTurn());
    }

    public static void StartAITurn()
    {
        StartStickThrow();
    }

    private IEnumerator ExecuteAiTurn()
    {
        aiPieces = FindObjectsOfType<PieceMover>();

        int chosenIndex = -1;
        Transform validatedTarget = null;

        for (int i = 0; i < aiPieces.Length; i++)
        {
            PieceMover piece = aiPieces[i];
            if (!piece.isAI) continue;

            int currentIndex = piece.GetCurrentTileIndex();
            if (currentIndex == -1) continue;

            int proposedIndex = currentIndex + PieceMover.lastStickValue;
            if (proposedIndex >= 30) continue;

            if (PieceMover.IsValidMove(piece, proposedIndex, out validatedTarget))
            {
                chosenIndex = i;
                break;
            }
        }

        if (chosenIndex != -1 && validatedTarget != null)
        {
            yield return aiPieces[chosenIndex].StartCoroutine(aiPieces[chosenIndex].MoveToTile(validatedTarget));
        }
        else
        {
            Debug.LogWarning("AI found no valid move during execution. Passing turn...");
            yield return new WaitForSeconds(0.5f);
            PieceMover.PassTurnImmediately();
        }
    }
    public void UseRandomAiScroll()
    {
        // Collect unused scroll indices
        List<int> unusedIndices = new List<int>();
        for (int i = 0; i < aiScrollUsed.Length; i++)
        {
            if (!aiScrollUsed[i])
                unusedIndices.Add(i);
        }

        if (unusedIndices.Count == 0) return;

        int randomChoice = unusedIndices[Random.Range(0, unusedIndices.Count)];
        int scrollIndex = aiScrollIndices[randomChoice];
        string scrollName = scrollData.scrollNames[scrollIndex];

        aiScrollUsed[randomChoice] = true;

        Debug.Log($"AI used scroll {scrollIndex}");

        // Apply the scroll's effect here, if needed
        if (scrollData != null && scrollIndex < scrollData.scrollEffectKeys.Length)
        {
            string effectKey = scrollData.scrollEffectKeys[scrollIndex];
            ScrollEffectExecutor.Instance.ExecuteEffect(effectKey, true); // true = AI
        }

        // --- Handle turn switching or rethrow logic ---
        bool getsAnotherTurn = PieceMover.lastMoveWasRethrow;

        if (getsAnotherTurn)
        {
            AiMover.StartStickThrow(); // AI rethrows
        }
        else
        {
            PieceMover.currentTurn = TurnType.Player;
            PieceMover.ResetTurn();
            PieceMover.Instance.Invoke("UpdateThrowButtonState", 0.1f);
        }

        PieceMover.lastMoveWasRethrow = false; // Reset after use
    }


}
