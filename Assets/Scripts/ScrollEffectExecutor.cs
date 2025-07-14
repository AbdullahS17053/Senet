using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ScrollEffectExecutor : MonoBehaviour
{
    public static ScrollEffectExecutor Instance;
    public Transform senusretMarkedTile; // The tile marked by Senusret Path
    [SerializeField] private Material selectedTileMaterial;



    private void Awake() => Instance = this;

    public void ExecuteEffect(string effectKey, bool isAI)
    {
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
            default:
                Debug.LogWarning("No effect found for key: " + effectKey);
                HandleTurnAfterScroll(); // fallback
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
            HandleTurnAfterScroll();
            yield break;
        }

        if (isAI)
        {
            yield return new WaitForSeconds(0.5f); // AI "thinking"
            PieceMover aiPiece = movable[Random.Range(0, movable.Count)];
            int nextIndex = aiPiece.GetCurrentTileIndex() + 1;
            Transform targetTile = GameObject.Find("Board").transform.GetChild(nextIndex);
            yield return aiPiece.StartCoroutine(aiPiece.MoveToTile(targetTile));
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

        //HandleTurnAfterScroll();
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
            HandleTurnAfterScroll();
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
            shieldMat.color = Color.cyan;
            rend.material = shieldMat;

            StartCoroutine(RemoveProtectionNextTurn(!isAI, selected));
        }

        //HandleTurnAfterScroll();
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
        HandleTurnAfterScroll();
    }


    private IEnumerator SelectTileByTouch(List<Transform> candidates, System.Action<Transform> callback)
{
    Transform selectedTile = null;
    Dictionary<Transform, Material> originalMaterials = new Dictionary<Transform, Material>();

    // Highlight candidate tiles and store original materials
    foreach (Transform t in candidates)
    {
        Renderer r = t.GetComponent<Renderer>();
        if (r != null)
        {
            originalMaterials[t] = r.material;
            r.material.color = Color.yellow;
        }
    }

    // Wait for user touch selection
    while (selectedTile == null)
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (candidates.Contains(hit.transform))
                    selectedTile = hit.transform;
            }
        }

        yield return null;
    }

    // Restore materials after selection
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
            HandleTurnAfterScroll(); // ✅ OK here
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

        // ✅ Set and visually mark the tile AFTER selection
        senusretMarkedTile = chosenTile;
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

        // ✅ ✅ ✅ Only now switch the turn
        HandleTurnAfterScroll();
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
        HandleTurnAfterScroll();
    }
    private void HandleTurnAfterScroll()
    {
        if (PieceMover.lastMoveWasRethrow)
        {
            if (PieceMover.currentTurn == TurnType.Player)
                PieceMover.Instance.Invoke("UpdateThrowButtonState", 0.1f);
            else
                AiMover.StartStickThrow();
        }
        else
        {
            PieceMover.currentTurn = (PieceMover.currentTurn == TurnType.Player) ? TurnType.AI : TurnType.Player;
            PieceMover.ResetTurn();

            if (PieceMover.currentTurn == TurnType.Player)
                PieceMover.Instance.Invoke("UpdateThrowButtonState", 0.1f);
            else
                AiMover.StartStickThrow();
        }

        PieceMover.lastMoveWasRethrow = false;
    }
}
