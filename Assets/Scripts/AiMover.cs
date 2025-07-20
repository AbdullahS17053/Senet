using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiMover : MonoBehaviour
{
    public static AiMover Instance;
    private PieceMover[] aiPieces;
    private bool[] aiScrollUsed = new bool[3];
    private int[] aiScrollIndices = new int[3];

    private int extraScrollIndex = -1;
    private bool extraScrollUsed = false;

    [SerializeField] ScrollData scrollData;

    private void Awake() => Instance = this;

    void Start()
    {
        int count = 0;
        for (int i = 0; i < 20; i++)
        {
            if (PlayerPrefs.GetInt($"scroll_{i}_selected", 0) == 1 && count < 3)
            {
                aiScrollIndices[count] = i;
                aiScrollUsed[count] = false;
                count++;
            }
        }
    }

    public static void StartStickThrow(bool forceReroll = false)
    {
        if (!forceReroll && PieceMover.obsidianUsedThisTurn)
        {
            Debug.Log("Skipping redundant AI stick throw due to Obsidian’s Burden already applied.");
            return;
        }

        if (Instance != null)
            Instance.StartCoroutine(Instance.ThrowSticksAndPlay(forceReroll));
    }

    private IEnumerator ThrowSticksAndPlay(bool forceReroll = false)
    {
        yield return new WaitForSeconds(1f);

        StickThrower stickThrower = FindObjectOfType<StickThrower>();
        if (stickThrower == null)
        {
            Debug.LogError("StickThrower not found!");
            yield break;
        }

        yield return stickThrower.StartCoroutine(stickThrower.ThrowSticksRoutine());

        Debug.Log("AI Stick Value: " + PieceMover.lastStickValue);

        if (!forceReroll)
        {
            yield return new WaitForSeconds(1.5f); // Wait for possible Obsidian reroll
            if (PieceMover.obsidianUsedThisTurn)
            {
                Debug.Log("[AI] Obsidian reroll triggered — stopping current move.");
                yield break;
            }
        }

        yield return new WaitForSeconds(1f); // Optional AI thinking delay

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
        aiPieces = System.Array.FindAll(FindObjectsOfType<PieceMover>(), p => p.isAI);

        int chosenIndex = -1;
        Transform validatedTarget = null;

        for (int i = 0; i < aiPieces.Length; i++)
        {
            PieceMover piece = aiPieces[i];
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
            Debug.LogWarning("AI found no valid move. Passing turn...");
            yield return new WaitForSeconds(0.5f);
            PieceMover.PassTurnImmediately();
        }
    }

    public void UseRandomAiScroll()
    {
        List<int> availableScrollSlots = new List<int>();
        for (int i = 0; i < aiScrollUsed.Length; i++)
        {
            if (!aiScrollUsed[i])
                availableScrollSlots.Add(i);
        }

        bool usedExtra = false;

        // If normal slots are used, fallback to extra scroll if available
        if (availableScrollSlots.Count == 0 && !extraScrollUsed && extraScrollIndex >= 0)
        {
            usedExtra = true;
        }

        int scrollIndexToUse = -1;
        if (usedExtra)
        {
            scrollIndexToUse = extraScrollIndex;
            extraScrollUsed = true;
        }
        else if (availableScrollSlots.Count > 0)
        {
            int chosenSlot = availableScrollSlots[Random.Range(0, availableScrollSlots.Count)];
            aiScrollUsed[chosenSlot] = true;
            scrollIndexToUse = aiScrollIndices[chosenSlot];
        }
        else
        {
            Debug.Log("AI has no scrolls left to use.");
            return;
        }

        if (scrollIndexToUse >= 0 && scrollData != null && scrollIndexToUse < scrollData.scrollEffectKeys.Length)
        {
            string effectKey = scrollData.scrollEffectKeys[scrollIndexToUse];
            Debug.Log($"[AI] used scroll: {effectKey}");

            ScrollManager sm = FindObjectOfType<ScrollManager>();
            if (sm != null && effectKey != "Vault of Shadows")
                sm.lastScrollEffectKey_AI = effectKey;

            ScrollEffectExecutor.Instance.ExecuteEffect(effectKey, true);
        }

        if (PieceMover.lastMoveWasRethrow)
        {
            StartStickThrow(); // AI rethrows
        }
        else
        {
            PieceMover.currentTurn = TurnType.Player;
            PieceMover.ResetTurn();
            PieceMover.Instance.Invoke("UpdateThrowButtonState", 0.1f);
        }

        PieceMover.lastMoveWasRethrow = false;
    }

    public void GrantExtraScroll(int scrollIndex)
    {
        extraScrollIndex = scrollIndex;
        extraScrollUsed = false;
        Debug.Log($"[AI] Granted extra scroll index: {scrollIndex}");
    }
}
