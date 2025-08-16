using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        for (int i = 0; i < 3; i++)
        {
            int index = PlayerPrefs.GetInt($"selected_scroll_{i}", -1);
            if (index >= 0)
            {
                aiScrollIndices[i] = index;
                aiScrollUsed[i] = false;
                Debug.Log($"[AI Scroll Init] Slot {i}: {scrollData.scrollEffectKeys[index]}");
            }
            else
            {
                aiScrollIndices[i] = -1;
                aiScrollUsed[i] = true; // mark as already used if not selected
            }
        }
    }


    public static void StartStickThrow()
    {
        // ✅ Prevent AI stick throw if turn has changed
        if (PieceMover.currentTurn != TurnType.AI)
        {
            Debug.LogWarning("AI tried to throw sticks but it's no longer AI's turn.");
            return;
        }

        if (Instance != null)
            Instance.StartCoroutine(Instance.ThrowSticksAndPlay());
    }


    private IEnumerator ThrowSticksAndPlay(bool forceReroll = false)
    {
        // ✅ Check before anything happens
        if (PieceMover.currentTurn != TurnType.AI)
        {
            Debug.LogWarning("[AI] Turn changed before stick throw — skipping AI move.");
            yield break;
        }

        StickThrower stickThrower = FindObjectOfType<StickThrower>();
        if (stickThrower == null)
        {
            Debug.LogError("StickThrower not found!");
            yield break;
        }

        yield return new WaitForSeconds(1f); // Optional thinking delay before throw

        // ✅ Check again before throwing, in case scrolls changed turn during delay
        if (PieceMover.currentTurn != TurnType.AI)
        {
            Debug.LogWarning("[AI] Turn changed before stick throw (post-delay) — skipping.");
            PieceMover.Instance.ShowTemporaryTurnMessage(PieceMover.currentTurn == TurnType.Player ? "Player" : "Opponent");
            yield break;
        }

        stickThrower.ShowStickVisuals();
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

        // ✅ Final check before executing AI move
        if (PieceMover.currentTurn != TurnType.AI)
        {
            Debug.LogWarning("[AI] Turn changed after stick throw — aborting AI move.");
            yield break;
        }

        yield return new WaitForSeconds(1f); // Optional thinking delay

        if (!PieceMover.HasValidMoveForAI())
        {
            Debug.Log("AI has no valid moves. Passing turn...");
            yield return StartCoroutine(PieceMover.Instance.PassTurnImmediately());
            stickThrower.ShowStickVisuals();
            if (PieceMover.Instance != null && PieceMover.Instance.gameObject != null)
            {
                PieceMover.Instance.ShowTemporaryTurnMessage(PieceMover.currentTurn == TurnType.Player ? "Player Turn" : "AI Turn");
            }
            else
            {
                Debug.LogWarning("[AI] Tried to show turn message, but PieceMover.Instance is null or destroyed.");
            }

            yield break;
        }

        StartCoroutine(ExecuteAiTurn());
    }



    public static void StartAITurn()
    {
        if (PieceMover.sandsOfEsnaAI)
        {
            StickThrower.Instance.AutoThrowWithOptions();
        }
        else if (PieceMover.currentTurn == TurnType.AI)
        {
            StartStickThrow();
        }
    }

    public IEnumerator ExecuteAiTurn()
    {
        aiPieces = System.Array.FindAll(FindObjectsOfType<PieceMover>(), p => p.isAI && p.frozenTurnsRemaining == 0);

        List<(PieceMover piece, Transform target, int targetIndex)> validMoves = new();
        foreach (var piece in aiPieces)
        {
            int currentIndex = piece.GetCurrentTileIndex();
            if (currentIndex == -1) continue;

            int proposedIndex = currentIndex + PieceMover.lastStickValue;
            if (proposedIndex > 30) continue;

            if (PieceMover.IsValidMove(piece, proposedIndex, out Transform validatedTarget))
                validMoves.Add((piece, validatedTarget, proposedIndex));
        }

        if (validMoves.Count == 0)
        {
            Debug.Log("AI has no valid moves. Passing turn...");
            yield return StartCoroutine(PieceMover.Instance.PassTurnImmediately());
            yield break;
        }

        Transform senusret1 = ScrollEffectExecutor.Instance?.senusretMarkedTile1;
        Transform senusret2 = ScrollEffectExecutor.Instance?.senusretMarkedTile2;

        var nonSenusretMoves = validMoves
            .Where(m => m.target != senusret1 && m.target != senusret2)
            .ToList();
        var senusretMoves = validMoves
            .Where(m => m.target == senusret1 || m.target == senusret2)
            .ToList();

        // Try normal moves first, fallback to Senusret moves
        if (!TryMakePriorityMove(nonSenusretMoves))
            TryMakePriorityMove(senusretMoves);

        yield break;
    }

    private bool TryMakePriorityMove(List<(PieceMover piece, Transform target, int targetIndex)> moves)
    {
        if (moves.Count == 0) return false;

        // 1️⃣ Trigger tiles
        foreach (var move in moves)
        {
            var marker = move.target.GetComponent<TileMarker>();
            if (marker != null && marker.isTriggerTile)
            {
                Debug.Log("[AI] Moving to trigger tile.");
                move.piece.StartCoroutine(move.piece.MoveToTile(move.target));
                return true;
            }
        }

        // 2️⃣ Last tile
        foreach (var move in moves)
        {
            if (move.targetIndex == 30)
            {
                Debug.Log("[AI] Moving to virtual last tile.");
                move.piece.StartCoroutine(move.piece.MoveToTile(move.target));
                return true;
            }
        }

        // 3️⃣ Swap with player
        foreach (var move in moves)
        {
            var occupyingPiece = PieceMover.GetPieceOnTile(move.target);
            if (occupyingPiece != null && !occupyingPiece.isAI)
            {
                Debug.Log("[AI] Swapping with player piece.");
                move.piece.StartCoroutine(move.piece.MoveToTile(move.target));
                return true;
            }
        }

        // 4️⃣ Furthest move
        var furthestMove = moves.OrderByDescending(m => m.targetIndex).First();
        Debug.Log("[AI] Moving piece closest to end.");
        furthestMove.piece.StartCoroutine(furthestMove.piece.MoveToTile(furthestMove.target));
        return true;
    }



    public IEnumerator UseRandomAiScroll()
    {
        // Check if AI scrolls are blocked
        if (PieceMover.aiScrollsDisabled)
        {
            Debug.Log("[AI] Scrolls are disabled by Dominion of Kamo.");
            yield break;
        }

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
            yield break;
        }

        if (scrollIndexToUse >= 0 && scrollData != null && scrollIndexToUse < scrollData.scrollEffectKeys.Length)
        {
            string effectKey = scrollData.scrollEffectKeys[scrollIndexToUse];
            Debug.Log($"[AI] used scroll: {effectKey}");

            ScrollManager sm = FindObjectOfType<ScrollManager>();
            if (sm != null && effectKey != "Vault of Shadows" && effectKey != "Echo of the Twin")
                sm.lastScrollEffectKey_AI = effectKey;

            ScrollEffectExecutor.Instance.ShowTemporaryMessage($"Opponent using {effectKey}");
            yield return new WaitForSeconds(2f); // Delay before executing
            ScrollEffectExecutor.Instance.ExecuteEffect(effectKey, true);
        }
    }
    public void GrantExtraScroll(int scrollIndex)
    {
        extraScrollIndex = scrollIndex;
        extraScrollUsed = false;
        Debug.Log($"[AI] Granted extra scroll index: {scrollIndex}");
    }
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

}
