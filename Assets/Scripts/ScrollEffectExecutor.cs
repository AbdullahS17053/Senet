using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

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
                StartCoroutine(ObsidianBurdenEffect(isAI));
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
            case "Oath of Isfet":
                StartCoroutine(OathOfIsfetEffect(isAI));
                break;
            case "Grasp of the Scarab":
                StartCoroutine(GraspOfTheScarabEffect(isAI));
                break;
            case "Path of Aaru":
                StartCoroutine(PathOfAaruEffect(isAI));
                break;
            case "Apep’s Trick":
                StartCoroutine(ApepsTrickEffect(isAI));
                break;
            case "Dominion of Kamo":
                DominionOfKamoEffect(isAI);
                break;
            case "Mirror of Merneith":
                StartCoroutine(MirrorOfMerneithEffect(isAI));
                break;
            case "Mena’s Grasp":
                StartCoroutine(MenasGraspEffect(isAI));
                break;
            case "Binding of Aegis":
                StartCoroutine(BindingOfAegisEffect(isAI));
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

        yield return new WaitForSeconds(0.5f);
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

    private IEnumerator ObsidianBurdenEffect(bool isAI)
    {
        if (isAI)
        {
            if (StickThrower.obsidianCapNextMove_Player)
            {
                Debug.LogWarning("[AI] Already applied Obsidian’s Burden on Player.");
                yield break;
            }

            Debug.Log("[AI] activated Obsidian’s Burden! Player’s next movement will be capped to 2.");
            StickThrower.obsidianCapNextMove_Player = true;
        }
        else
        {
            if (StickThrower.obsidianCapNextMove_AI)
            {
                Debug.LogWarning("[Player] Already applied Obsidian’s Burden on AI.");
                yield break;
            }

            Debug.Log("[Player] activated Obsidian’s Burden! AI’s next movement will be capped to 2.");
            StickThrower.obsidianCapNextMove_AI = true;
        }

        yield return new WaitForSeconds(0.5f);
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
        Debug.Log($"[{(isAI ? "AI" : "Player")}] activating Anippe’s Grace");

        List<PieceMover> candidates = new List<PieceMover>();
        PieceMover[] allPieces = GameObject.FindObjectsOfType<PieceMover>();

        foreach (var piece in allPieces)
        {
            if (piece.isAI == isAI && !piece.hasPermanentGrace)
            {
                candidates.Add(piece);
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("No valid pieces to protect.");
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
            selected.hasPermanentGrace = true;

            // Optional visual effect: greenish glow
            Renderer rend = selected.GetComponent<Renderer>();
            if (rend != null && selected.originalMaterial != null)
            {
                Material permanentMat = new Material(selected.originalMaterial);
                permanentMat.color = Color.black;
                rend.material = permanentMat;
            }

            Debug.Log($"[{(isAI ? "AI" : "Player")}] permanently protected: {selected.name}");
            ShowTemporaryMessage($"{(isAI ? "AI" : "You")} protected {selected.name} permanently!");
        }

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

    private IEnumerator OathOfIsfetEffect(bool isAI)
    {
        Debug.Log($"[{(isAI ? "AI" : "Player")}] activating Oath of Isfet — Swapping all pieces!");

        // Find all pieces
        List<PieceMover> aiPieces = new List<PieceMover>();
        List<PieceMover> playerPieces = new List<PieceMover>();
        Transform board = GameObject.Find("Board").transform;

        foreach (var piece in GameObject.FindObjectsOfType<PieceMover>())
        {
            if (piece.GetCurrentTileIndex() == -1) continue;

            if (piece.isAI)
                aiPieces.Add(piece);
            else
                playerPieces.Add(piece);
        }

        int swapCount = Mathf.Min(aiPieces.Count, playerPieces.Count);

        if (swapCount == 0)
        {
            Debug.Log("Oath of Isfet: No pieces to swap.");
            yield break;
        }

        for (int i = 0; i < swapCount; i++)
        {
            PieceMover aiPiece = aiPieces[i];
            PieceMover playerPiece = playerPieces[i];

            Transform aiTile = aiPiece.transform.parent;
            Transform playerTile = playerPiece.transform.parent;

            // Swap positions
            Vector3 aiLocalY = aiPiece.transform.localPosition;
            Vector3 playerLocalY = playerPiece.transform.localPosition;

            aiPiece.transform.SetParent(playerTile);
            aiPiece.transform.localPosition = new Vector3(0, aiLocalY.y, 0);

            playerPiece.transform.SetParent(aiTile);
            playerPiece.transform.localPosition = new Vector3(0, playerLocalY.y, 0);
        }

        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator GraspOfTheScarabEffect(bool isAI)
    {
        Debug.Log($"[{(isAI ? "AI" : "Player")}] used Grasp of the Scarab to send opponent's leading piece back!");

        List<PieceMover> opponentPieces = new List<PieceMover>();
        PieceMover[] allPieces = GameObject.FindObjectsOfType<PieceMover>();

        foreach (var piece in allPieces)
        {
            if (piece.isAI != isAI)
            {
                int tileIndex = piece.GetCurrentTileIndex();
                if (tileIndex != -1)
                    opponentPieces.Add(piece);
            }
        }

        if (opponentPieces.Count == 0)
        {
            Debug.LogWarning("No opponent pieces found on board.");
            yield break;
        }

        // Sort by tile index descending → leading piece first
        opponentPieces.Sort((a, b) => b.GetCurrentTileIndex().CompareTo(a.GetCurrentTileIndex()));
        PieceMover target = opponentPieces[0];

        Transform board = GameObject.Find("Board").transform;

        // Find the first empty tile starting from index 0
        Transform destination = null;
        for (int i = 0; i < board.childCount; i++)
        {
            if (board.GetChild(i).childCount == 0)
            {
                destination = board.GetChild(i);
                break;
            }
        }

        if (destination == null)
        {
            Debug.LogWarning("Grasp of the Scarab: No empty tile found near start.");
            yield break;
        }

        yield return new WaitForSeconds(0.5f);

        // Smooth move to new tile
        float moveSpeed = 6f;
        float localY = target.transform.localPosition.y;
        Vector3 targetPos =
            new Vector3(destination.position.x, destination.position.y + localY, destination.position.z);

        while (Vector3.Distance(target.transform.position, targetPos) > 0.01f)
        {
            target.transform.position =
                Vector3.MoveTowards(target.transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        target.transform.SetParent(destination);
        target.transform.localPosition = new Vector3(0, localY, 0);

        Debug.Log($"{target.name} was sent to tile: {destination.name} by Grasp of the Scarab.");
    }

    private IEnumerator PathOfAaruEffect(bool isAI)
    {
        Debug.Log($"[{(isAI ? "AI" : "Player")}] activated Path of Aaru — ignoring movement restrictions this turn!");

        if (isAI)
            PieceMover.pathOfAaruActive_AI = true;
        else
            PieceMover.pathOfAaruActive_Player = true;

        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator ApepsTrickEffect(bool isAI)
    {
        Debug.Log($"[{(isAI ? "AI" : "Player")}] activated Apep’s Trick — summons opponent’s piece!");

        // Count opponent's existing pieces
        PieceMover[] allPieces = FindObjectsOfType<PieceMover>();
        int opponentCount = allPieces.Count(p => p.isAI != isAI);

        if (opponentCount >= 5)
        {
            Debug.Log("Opponent already has 5 or more pieces. No new piece summoned.");
            yield break;
        }

        // Find an existing piece to clone
        PieceMover pieceToClone = allPieces.FirstOrDefault(p => p.isAI != isAI);
        if (pieceToClone == null)
        {
            Debug.LogWarning("No opponent piece found to clone.");
            yield break;
        }

        // Find first empty tile
        Transform board = GameObject.Find("Board").transform;
        Transform spawnTile = null;

        for (int i = 0; i < board.childCount; i++)
        {
            if (board.GetChild(i).childCount == 0)
            {
                spawnTile = board.GetChild(i);
                break;
            }
        }

        if (spawnTile == null)
        {
            Debug.LogWarning("No empty tile found for new piece.");
            yield break;
        }

        // Clone the piece and set it up
        GameObject newPiece = Instantiate(pieceToClone.gameObject, spawnTile.position, Quaternion.identity);
        newPiece.name = pieceToClone.name.Replace("(Clone)", "") + "_Reinforced";
        newPiece.transform.SetParent(spawnTile);
        newPiece.transform.localPosition = new Vector3(0, pieceToClone.transform.localPosition.y, 0);
        newPiece.transform.localScale = pieceToClone.transform.localScale;

        PieceMover newPieceMover = newPiece.GetComponent<PieceMover>();
        newPieceMover.isAI = !isAI;
        newPieceMover.originalMaterial = pieceToClone.originalMaterial;
        newPieceMover.isProtected = false;

        Debug.Log($"[Apep’s Trick] Summoned a new {(isAI ? "Player" : "AI")} piece at tile: {spawnTile.name}");

        yield return new WaitForSeconds(0.5f);
    }

    private void DominionOfKamoEffect(bool isAI)
    {
        if (isAI)
        {
            PieceMover.playerScrollsDisabled = true;
            Debug.Log("[ScrollEffectExecutor] AI used Dominion of Kamo — Player scrolls disabled!");
        }
        else
        {
            PieceMover.aiScrollsDisabled = true;
            Debug.Log("[ScrollEffectExecutor] Player used Dominion of Kamo — AI scrolls disabled!");
        }
    }

    private IEnumerator MirrorOfMerneithEffect(bool isAI)
    {
        Debug.Log($"[{(isAI ? "AI" : "Player")}] activating Mirror of Merneith");

        List<PieceMover> candidates = new List<PieceMover>();
        PieceMover[] allPieces = GameObject.FindObjectsOfType<PieceMover>();

        foreach (var piece in allPieces)
        {
            if (piece.isAI == isAI)
                candidates.Add(piece);
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("No piece available to remove.");
            yield return new WaitForSeconds(0.3f);
            yield break;
        }

        PieceMover selected = null;
        GameManager.Instance?.CheckForWinCondition();

        if (isAI)
        {
            yield return new WaitForSeconds(0.5f); // Simulate AI thinking
            selected = candidates[Random.Range(0, candidates.Count)];
        }
        else
        {
            yield return StartCoroutine(SelectPieceByTouch(candidates, result => selected = result));
        }

        if (selected != null)
        {
            // Optional visual/sound effect before destroy
            yield return new WaitForSeconds(0.2f);
            Destroy(selected.gameObject);
            Debug.Log($"[{(isAI ? "AI" : "Player")}] piece removed by Mirror of Merneith.");
        }

        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator MenasGraspEffect(bool isAI)
    {
        Debug.Log($"[{(isAI ? "AI" : "Player")}] activating Mena’s Grasp");

        List<PieceMover> targets = new List<PieceMover>();
        PieceMover[] allPieces = GameObject.FindObjectsOfType<PieceMover>();

        foreach (var piece in allPieces)
        {
            if (piece.isAI != isAI)
                targets.Add(piece);
        }

        if (targets.Count == 0)
        {
            Debug.LogWarning("No opponent pieces to affect.");
            yield break;
        }

        PieceMover selected = null;

        if (isAI)
        {
            selected = targets.OrderByDescending(p => p.GetCurrentTileIndex()).FirstOrDefault();
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            yield return StartCoroutine(SelectPieceByTouch(targets, result => selected = result));
        }

        if (selected != null)
        {
            int currentIndex = selected.GetCurrentTileIndex();
            int targetIndex = currentIndex - 3;

            if (targetIndex < 0)
            {
                Debug.Log("Mena’s Grasp has no effect — too close to start.");
                yield break;
            }

            Transform board = GameObject.Find("Board").transform;
            Transform destinationTile = board.GetChild(targetIndex);

            bool occupied = allPieces.Any(p => p != selected && p.GetCurrentTileIndex() == targetIndex);

            if (occupied)
            {
                Debug.Log("Original backward tile occupied. Searching next free tile...");
                int boardSize = board.childCount;
                bool found = false;

                for (int i = targetIndex + 1; i < boardSize; i++)
                {
                    bool tileTaken = allPieces.Any(p => p != selected && p.GetCurrentTileIndex() == i);
                    if (!tileTaken)
                    {
                        destinationTile = board.GetChild(i);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Debug.Log("No empty tile found after target. Cannot move.");
                    yield break;
                }
            }

            // Move the piece instantly
            selected.transform.SetParent(destinationTile);
            Vector3 localPos = selected.transform.localPosition;
            selected.transform.localPosition = new Vector3(0f, localPos.y, 0f);

            Debug.Log($"Moved {selected.name} back to tile {destinationTile.name}");

            yield return new WaitForSeconds(0.3f);
        }
    }
    private IEnumerator BindingOfAegisEffect(bool isAI)
    {
        Debug.Log($"[{(isAI ? "AI" : "Player")}] activating Binding of Aegis");

        List<PieceMover> targets = GameObject.FindObjectsOfType<PieceMover>()
            .Where(p => p.isAI != isAI && !p.IsFrozen())
            .ToList();

        if (targets.Count == 0)
        {
            Debug.Log("No valid opponent to freeze.");
            yield break;
        }

        PieceMover selected = null;

        if (isAI)
        {
            selected = targets.OrderByDescending(p => p.GetCurrentTileIndex()).FirstOrDefault();
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            yield return StartCoroutine(SelectPieceByTouch(targets, result => selected = result));
        }

        if (selected != null)
        {
            selected.frozenTurnsRemaining = 4;

            // Visual cue (cyan color)
            Renderer rend = selected.GetComponent<Renderer>();
            if (selected.originalMaterial != null)
            {
                Material frozenMat = new Material(selected.originalMaterial);
                frozenMat.color = Color.magenta;
                rend.material = frozenMat;
            }

            Debug.Log($"[Binding of Aegis] {selected.name} is frozen for 2 turns.");
        }

        yield return new WaitForSeconds(0.5f);
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
