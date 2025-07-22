using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class ScrollEffectExecutor : MonoBehaviour
{
    public static ScrollEffectExecutor Instance;
    public Transform senusretMarkedTile; // The tile marked by Senusret Path
    [SerializeField] private Material selectedTileMaterial;
    [SerializeField] private Text scrollFeedbackText;




    private void Awake() => Instance = this;

    public void ExecuteEffect(string effectKey, bool isAI)
    {
        ShowTemporaryMessage($"{(isAI ? "AI" : "Player")} used {effectKey}");

        switch (effectKey)
        {
            case "Earthbound’s Step":
                StartCoroutine(EarthboundsStepEffect(isAI));
                break;
            case "Sylvan Shield":
                StartCoroutine(SylvanShieldEffect(isAI));
                break;
            case "Senusret Path":
                StartCoroutine(SenusretPathEffect(isAI));
                break;
            case "Skyspark Swap":
                StartCoroutine(SkysparkSwapEffect());
                break;
            case "Gift of Jahi":
                StartCoroutine(GiftOfJahiEffect(isAI));
                break;
            case "Obsidian’s Burden":
                StartCoroutine(ObsidianBurdenEffect());
                break;
            case "Horus Retreat":
                StartCoroutine(HorusRetreatEffect(isAI));
                break;
            case "Anippe’s Grace":
                StartCoroutine(AnippesGraceEffect(isAI));
                break;
            case "Vault of Shadows":
                StartCoroutine(VaultOfShadowsEffect(isAI));
                break;
            case "Heka’s Blessing":
                StartCoroutine(HekasBlessingEffect(isAI));
                break;
            default:
                Debug.LogWarning("No effect found for key: " + effectKey);
                break;
        }
    }

    private IEnumerator EarthboundsStepEffect(bool isAI)
{
    Debug.Log($"[{(isAI ? "AI" : "Player")}] activating Earthbound’s Step");

    List<PieceMover> movable = new List<PieceMover>();
    PieceMover[] allPieces = GameObject.FindObjectsOfType<PieceMover>();

    foreach (var piece in allPieces)
    {
        if (piece.isAI == isAI && piece != PieceMover.selectedPiece)
        {
            int currentIndex = piece.GetCurrentTileIndex();
            int nextIndex = currentIndex + 1;

            if (nextIndex < 30)
            {
                Transform dummy;
                if (PieceMover.IsValidMove(piece, nextIndex, out dummy))
                    movable.Add(piece);
            }
        }
    }

    if (movable.Count == 0)
    {
        Debug.Log("No valid second piece for Earthbound’s Step.");
        yield return new WaitForSeconds(0.3f);
        yield break;
    }

    if (isAI)
    {
        yield return new WaitForSeconds(0.5f); // AI "thinking"
        PieceMover aiPiece = movable[Random.Range(0, movable.Count)];
        int nextIndex = aiPiece.GetCurrentTileIndex() + 1;
        Transform targetTile = GameObject.Find("Board").transform.GetChild(nextIndex);
        yield return aiPiece.StartCoroutine(aiPiece.MoveToTile(targetTile));

        // ✅ Handle turn transition based on rethrow status
        yield return new WaitForSeconds(0.5f); // Small pause after move

        if (PieceMover.lastMoveWasRethrow)
        {
            AiMover.StartStickThrow(); // Give AI another turn
        }
        else
        {
            PieceMover.currentTurn = TurnType.Player;
            PieceMover.Instance?.ShowTemporaryTurnMessage("Player Turn");
            PieceMover.Instance?.Invoke("UpdateThrowButtonState", 0.1f);
        }
    }
    else
    {
        PieceMover chosen = null;
        yield return StartCoroutine(SelectPieceByTouch(movable, result => chosen = result));

        if (chosen != null)
        {
            int nextIndex = chosen.GetCurrentTileIndex() + 1;
            Transform targetTile = GameObject.Find("Board").transform.GetChild(nextIndex);
            yield return chosen.StartCoroutine(chosen.MoveToTile(targetTile));
        }
    }
}


    private IEnumerator SylvanShieldEffect(bool isAI)
    {
        Debug.Log($"[{(isAI ? "AI" : "Player")}] activating Sylvan Shield");

        List<PieceMover> candidates = new List<PieceMover>();
        PieceMover[] allPieces = GameObject.FindObjectsOfType<PieceMover>();

        foreach (var piece in allPieces)
        {
            if (piece.isAI == isAI && !piece.isProtected)
                candidates.Add(piece);
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("No available pieces to protect.");
            yield return new WaitForSeconds(0.3f);

            yield break;
        }

        PieceMover selected = null;

        if (isAI)
        {
            selected = candidates[Random.Range(0, candidates.Count)];
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            yield return StartCoroutine(SelectPieceByTouch(candidates, result => selected = result));
        }

        if (selected != null)
        {
            selected.isProtected = true;

            // Visual effect
            Renderer rend = selected.GetComponent<Renderer>();
            Material shieldMat = new Material(selected.originalMaterial);
            shieldMat.color = Color.magenta;
            rend.material = shieldMat;

            StartCoroutine(RemoveProtectionNextTurn(!isAI, selected));
        }
    }

    private IEnumerator RemoveProtectionNextTurn(bool fromPlayer, PieceMover protectedPiece)
    {
        TurnType ownerTurn = fromPlayer ? TurnType.Player : TurnType.AI;
        TurnType opponentTurn = fromPlayer ? TurnType.AI : TurnType.Player;

        // Wait until opponent gets turn
        while (PieceMover.currentTurn != opponentTurn)
            yield return null;

        // Then wait until it comes back to the owner
        while (PieceMover.currentTurn != ownerTurn)
            yield return null;

        // Now remove protection after opponent's full turn
        if (protectedPiece != null && protectedPiece.isProtected)
        {
            protectedPiece.isProtected = false;

            Renderer rend = protectedPiece.GetComponent<Renderer>();
            if (protectedPiece.originalMaterial != null)
                rend.material = protectedPiece.originalMaterial;

            Debug.Log($"[Sylvan Shield] Protection removed from: {protectedPiece.name} after opponent’s turn.");
        }
    }


    private IEnumerator SelectPieceByTouch(List<PieceMover> candidates, System.Action<PieceMover> callback)
    {
        PieceMover selected = null;

        // Track original materials
        Dictionary<PieceMover, Material> originalMaterials = new Dictionary<PieceMover, Material>();

        // Highlight candidates with a clone of the highlight material
        foreach (var p in candidates)
        {
            Renderer rend = p.GetComponent<Renderer>();
            if (rend != null && p.originalMaterial != null)
            {
                originalMaterials[p] = rend.material; // Save actual current material
                Material highlightClone = new Material(PieceMover.highlightMaterial ?? p.originalMaterial);
                rend.material = highlightClone;
            }
        }

        while (selected == null)
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    PieceMover tapped = hit.collider.GetComponent<PieceMover>();
                    if (tapped != null && candidates.Contains(tapped))
                    {
                        selected = tapped;
                    }
                }
            }

            yield return null;
        }

        // Restore the original materials exactly
        foreach (var kvp in originalMaterials)
        {
            Renderer rend = kvp.Key.GetComponent<Renderer>();
            if (rend != null)
                rend.material = kvp.Value;
        }

        callback?.Invoke(selected);
    }


    private IEnumerator SelectTileByTouch(List<Transform> candidates, System.Action<Transform> callback)
{
    Transform selectedTile = null;
    Dictionary<Transform, Material> originalMaterials = new Dictionary<Transform, Material>();
    Dictionary<Transform, Color> originalColors = new Dictionary<Transform, Color>();

    // Highlight candidate tiles and store original materials and colors
    foreach (Transform t in candidates)
    {
        Renderer r = t.GetComponent<Renderer>();
        if (r != null)
        {
            originalMaterials[t] = r.material;
            originalColors[t] = r.material.color;
            r.material.color = Color.yellow;
        }
    }

    // Wait for user touch selection
    while (selectedTile == null)
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
            RaycastHit[] hits = Physics.RaycastAll(ray);

            foreach (RaycastHit hit in hits)
            {
                GameObject hitObject = hit.collider.gameObject;

                // Ignore any GameObject with tag "Piece"
                if (hitObject.CompareTag("Piece"))
                    continue;

                // Check if the tapped object is a valid tile
                if (candidates.Contains(hit.transform))
                {
                    selectedTile = hit.transform;
                    break;
                }
            }
        }

        yield return null;
    }

    // Restore materials and colors after selection
    foreach (Transform t in candidates)
    {
        Renderer r = t.GetComponent<Renderer>();
        TileMarker marker = t.GetComponent<TileMarker>();

        if (r != null)
        {
            if (marker != null && marker.isSkysparkTile)
            {
                r.material.color = Color.red; // Keep Skyspark red
            }
            else if (marker != null && marker.isTriggerTile && marker.triggerMaterial != null)
            {
                r.material = marker.triggerMaterial;
            }
            else if (originalMaterials.ContainsKey(t))
            {
                r.material = originalMaterials[t];
                r.material.color = originalColors[t]; // Restore original color
            }
        }
    }

    callback?.Invoke(selectedTile);
}



    private IEnumerator SenusretPathEffect(bool isAI)
    {
        Debug.Log($"[{(isAI ? "AI" : "Player")}] activating Senusret Path");

        List<Transform> boardTiles = new List<Transform>();
        Transform board = GameObject.Find("Board").transform;

        for (int i = 16; i < board.childCount; i++) // only tiles > 15
        {
            boardTiles.Add(board.GetChild(i));
        }

        if (boardTiles.Count == 0)
        {
            Debug.LogWarning("No valid tiles beyond 15.");

            yield break;
        }

        Transform chosenTile = null;

        if (isAI)
        {
            yield return new WaitForSeconds(0.5f);
            chosenTile = boardTiles[Random.Range(0, boardTiles.Count)];
        }
        else
        {
            yield return StartCoroutine(SelectTileByTouch(boardTiles, result => chosenTile = result));
        }

        //Set and visually mark the tile AFTER selection
        senusretMarkedTile = chosenTile;
        chosenTile.GetComponent<TileMarker>().senusretTile = true;
        if (chosenTile != null)
        {
            Renderer rend = chosenTile.GetComponent<Renderer>();
            if (rend != null)
            {
                // You can use a serialized "senusretMarkedMaterial" here instead of .color directly if needed
                rend.material.color = Color.magenta;
            }

            Debug.Log("Senusret Path marked at tile: " + chosenTile.name);
        }

    }
    private IEnumerator SkysparkSwapEffect()
    {
        Debug.Log("[Skyspark Swap] Activating square 26 as Skyspark Swap.");

        Transform board = GameObject.Find("Board").transform;
        Transform tile26 = board.GetChild(25); // Index 25 = tile 26

        TileMarker marker = tile26.GetComponent<TileMarker>();
        if (marker == null)
            marker = tile26.gameObject.AddComponent<TileMarker>();

        marker.isSkysparkTile = true;

        // Visual indication (red)
        Renderer r = tile26.GetComponent<Renderer>();
        if (r != null)
            r.material.color = Color.red;

        yield return new WaitForSeconds(0.5f);

    }
    private IEnumerator GiftOfJahiEffect(bool isAI)
    {
        Debug.Log($"[{(isAI ? "AI" : "Player")}] activating Gift of Jahi");

        ScrollManager scrollManager = FindObjectOfType<ScrollManager>();
        if (scrollManager == null)
        {
            Debug.LogWarning("ScrollManager not found!");
            yield return new WaitForSeconds(0.3f);

            yield break;
        }

        List<int> usedScrolls = new List<int>(scrollManager.usedScrollHistory);
        if (usedScrolls.Count == 0)
        {
            Debug.Log("No used scrolls to recover.");
            yield return new WaitForSeconds(0.3f);

            yield break;
        }

        yield return new WaitForSeconds(0.5f); // Optional delay

        int recoveredScroll;

        if (isAI)
        {
            recoveredScroll = usedScrolls[Random.Range(0, usedScrolls.Count)];
        }
        else
        {
            recoveredScroll = usedScrolls[usedScrolls.Count - 2]; // Most recently used
        }

        scrollManager.RestoreUsedScroll(recoveredScroll);
        Debug.Log($"[{(isAI ? "AI" : "Player")}] recovered scroll index: {recoveredScroll}");

    }
    private IEnumerator ObsidianBurdenEffect()
    {
        Debug.Log("[Player] activated Obsidian’s Burden!");

        if (PieceMover.currentTurn != TurnType.AI)
        {
            Debug.LogWarning("Cannot use Obsidian’s Burden when it's not AI's turn.");

            yield break;
        }

        if (PieceMover.obsidianUsedThisTurn)
        {
            Debug.LogWarning("Obsidian’s Burden has already been used this AI turn.");

            yield break;
        }

        PieceMover.obsidianUsedThisTurn = true;

        // Cancel any current AI processing (if applicable)
        Debug.Log("[Obsidian’s Burden] Forcing AI rethrow...");

        yield return new WaitForSeconds(0.5f);

        AiMover.StartStickThrow(forceReroll: true); // <-- new optional param
        // Note: do NOT call HandleTurnAfterScroll here — AI will handle it post-reroll
    }
    private IEnumerator HorusRetreatEffect(bool isAI)
    {
        if (isAI)
            PieceMover.horusPenaltyPending = true; // Apply to player next turn
        else
            PieceMover.horusPenaltyPending = true; // Apply to AI next turn

        Debug.Log("[Horus Retreat] Penalty will apply to opponent's next stick throw.");
        yield return new WaitForSeconds(0.5f);

    }
    private IEnumerator AnippesGraceEffect(bool isAI)
    {
        if (isAI)
            PieceMover.anippeGraceActive_AI = true;
        else
            PieceMover.anippeGraceActive_Player = true;

        Debug.Log($"[{(isAI ? "AI" : "Player")}] activated Anippe’s Grace — will skip 1 trigger tile this turn.");

        yield return new WaitForSeconds(0.5f);

    }
    private IEnumerator VaultOfShadowsEffect(bool isAI)
    {
        ScrollManager sm = FindObjectOfType<ScrollManager>();
        if (sm == null)
        {
            Debug.LogWarning("ScrollManager not found.");

            yield break;
        }

        string lastKey = isAI ? sm.lastScrollEffectKey_AI : sm.lastScrollEffectKey_Player;

        if (string.IsNullOrEmpty(lastKey) || lastKey == "Vault of Shadows")
        {
            Debug.LogWarning("No scroll to repeat or cannot repeat Vault of Shadows itself.");
            yield return new WaitForSeconds(0.3f);

            yield break;
        }

        Debug.Log($"[{(isAI ? "AI" : "Player")}] Vault of Shadows repeating: {lastKey}");
        yield return new WaitForSeconds(0.5f); // Optional delay
        ExecuteEffect(lastKey, isAI); // Repeats the last scroll!
    }
    private IEnumerator HekasBlessingEffect(bool isAI)
    {
        Debug.Log($"[{(isAI ? "AI" : "Player")}] activating Heka’s Blessing");

        yield return new WaitForSeconds(0.5f); // Optional delay

        ScrollManager sm = FindObjectOfType<ScrollManager>();
        if (sm != null)
            sm.GrantHekasBlessingScroll(isAI);

    }
    private Coroutine messageRoutine;

    private void ShowTemporaryMessage(string message, float duration = 2f)
    {
        if (messageRoutine != null)
            StopCoroutine(messageRoutine);

        messageRoutine = StartCoroutine(ClearAfterDelay(message, duration));
        scrollFeedbackText.text = message;
    }

    private IEnumerator ClearAfterDelay(string message, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (scrollFeedbackText.text == message) // Only clear if message hasn't changed
            scrollFeedbackText.text = "";
    }

}
