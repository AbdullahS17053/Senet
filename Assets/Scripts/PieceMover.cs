using System.Collections;
using UnityEngine;
using System.Linq;


public enum TurnType { Player, AI }

public class PieceMover : MonoBehaviour
{
    public GameObject throwButton;
    public GameObject skipButton;
    [HideInInspector] public bool hasPermanentGrace = false;

    private static Transform boardTransform;
    private static Camera mainCamera;
    private static int totalTiles = 30;

    public static int lastStickValue = 0;
    public static bool moveInProgress = false;
    public static bool obsidianUsedThisTurn = false;
    public static int horusPenaltyAmount = 0;
    public static bool horusPenaltyPending = false;

    public static bool anippeGraceActive_Player = false;
    public static bool anippeGraceUsed_Player = false;

    public static bool anippeGraceActive_AI = false;
    public static bool anippeGraceUsed_AI = false;



    public static PieceMover selectedPiece = null;
    public static Transform highlightedTile = null;

    public static Material highlightMaterial;

    public static TurnType currentTurn = TurnType.Player;
    public static PieceMover Instance;
    public bool isAI = false;
    public static bool lastMoveWasRethrow = false;

    public static bool pathOfAaruActive_Player = false;
    public static bool pathOfAaruActive_AI = false;

    public static bool playerScrollsDisabled = false;
    public static bool aiScrollsDisabled = false;
    
    public static bool sandsOfEsnaPlayer = false;
    public static bool sandsOfEsnaAI = false;

    private ScrollManager scrollManager;
    [HideInInspector] public bool isProtected = false;
    [HideInInspector] public Material originalMaterial;

    [SerializeField] private UnityEngine.UI.Text turnFeedbackText;

    [HideInInspector] public int frozenTurnsRemaining = 0;
    public bool IsFrozen() => frozenTurnsRemaining > 0;







    void Start()
    {
        if (!isAI && Instance == null)
            Instance = this;

        if (boardTransform == null)
            boardTransform = GameObject.Find("Board").transform;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (throwButton == null)
            throwButton = GameObject.FindWithTag("ThrowButton"); // Optional fallback

        if (originalMaterial == null)
            originalMaterial = GetComponent<Renderer>().material;

        if (skipButton == null)
            skipButton = GameObject.FindWithTag("SkipButton");
        scrollManager = GameObject.FindObjectOfType<ScrollManager>();
        turnFeedbackText.text = "Player Turn";
        sandsOfEsnaPlayer = false;
        sandsOfEsnaAI = false;
        skipButton.SetActive(false);
        UpdateThrowButtonState();
    }


    void Update()
    {
        if (currentTurn != TurnType.Player || moveInProgress || lastStickValue <= 0)
            return;

        if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
        {
            Touch touch = Input.touches[0];
            Ray ray = mainCamera.ScreenPointToRay(touch.position);
            RaycastHit[] hits = Physics.RaycastAll(ray);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                GameObject tapped = hit.collider.gameObject;
                PieceMover tappedPiece = tapped.GetComponent<PieceMover>();

                if (tappedPiece != null && tappedPiece.isAI)
                    continue;

                if (tapped.CompareTag("Piece"))
                {
                    HandlePieceTap(tappedPiece);
                    break;
                }
                else if (highlightedTile != null && tapped.transform == highlightedTile)
                {
                    selectedPiece?.StartCoroutine(selectedPiece.MoveToTile(highlightedTile));
                    break;
                }
            }

        }
    }


    void HandlePieceTap(PieceMover piece)
    {
        if (piece.IsFrozen())
        {
            Debug.Log($"{piece.name} is frozen and cannot move.");
            return;
        }
        AudioController.Instance.PlaySound("PieceTap");
        int currentIndex = piece.GetCurrentTileIndex();
        if (currentIndex == -1) return;

        int targetIndex = currentIndex + lastStickValue;

        //Prevent anything beyond 31
        if (targetIndex > totalTiles) return;

        //Special case: move directly to tile 30 (last tile), no highlight
        if (targetIndex == totalTiles)
        {
            selectedPiece = piece;
            Transform finalTile = boardTransform.GetChild(totalTiles - 1); // Tile 30
            piece.StartCoroutine(piece.MoveToTile(finalTile));
            return;
        }

        Transform targetTile;
        if (!IsValidMove(piece, targetIndex, out targetTile))
            return;

        if (highlightedTile != null)
            highlightedTile.GetComponent<TileMarker>()?.Unhighlight();

        targetTile.GetComponent<TileMarker>()?.Highlight(highlightMaterial);

        selectedPiece = piece;
        highlightedTile = targetTile;
    }

    public static bool HasAnyValidMove(bool isAI)
    {
        PieceMover[] allPieces = GameObject.FindObjectsOfType<PieceMover>();

        foreach (PieceMover piece in allPieces)
        {
            if (piece.isAI != isAI)
                continue;

            if (piece.IsFrozen()) continue;

            int currentIndex = piece.GetCurrentTileIndex();

            if (currentIndex == -1)
                continue;

            int targetIndex = currentIndex + lastStickValue;
            if (targetIndex > totalTiles)
                continue;

            if (IsValidMove(piece, targetIndex, out _))
                return true;
        }

        return false;
    }

    public static bool HasValidMoveForAI()
    {
        bool has = HasAnyValidMove(true);
        Debug.Log($"[Check] AI has valid move? {has}");
        return has;
    }

    public static bool HasValidMoveForPlayer()
    {
        bool has = HasAnyValidMove(false);
        Debug.Log($"[Check] Player has valid move? {has}");
        return has;
    }



    public static void PassTurnImmediately()
    {
        if (currentTurn == TurnType.AI)
        {
            if (HasValidMoveForAI())
            {
                Debug.Log("[AI] Still has valid moves. Not passing turn.");
                return;
            }

            Debug.Log("Passing turn from AI → Player");
            lastStickValue = 0;
            moveInProgress = false;
            currentTurn = TurnType.Player;
            if (Instance != null && Instance.gameObject != null)
            {
                Instance.ShowTemporaryTurnMessage("Player Turn");
                StickThrower.Instance.ShowStickVisuals();
            }
            else
            {
                Debug.LogWarning("No valid PieceMover.Instance found for Player.");
            }

            StickThrower stickThrower = GameObject.FindObjectOfType<StickThrower>();
            if (sandsOfEsnaPlayer)
            {
                stickThrower.throwButton.gameObject.SetActive(false);
                stickThrower.AutoThrowWithOptions();
            }
            else
            {
                if (stickThrower != null)
                {
                    stickThrower.UpdateThrowButtonState();
                    stickThrower.ShowStickVisuals();
                }
                else
                {
                    Debug.LogError("StickThrower not found when trying to enable throw button.");
                }
            }

            // Decrease freeze counters
            foreach (var piece in GameObject.FindObjectsOfType<PieceMover>())
            {
                if (piece.frozenTurnsRemaining > 0)
                {
                    piece.frozenTurnsRemaining--;

                    if (piece.frozenTurnsRemaining == 0)
                    {
                        Renderer rend = piece.GetComponent<Renderer>();
                        if (piece.originalMaterial != null)
                            rend.material = piece.originalMaterial;

                        Debug.Log($"[Binding of Aegis] {piece.name} is no longer frozen.");
                    }
                }
            }
        }
        else if (currentTurn == TurnType.Player)
        {
            if (HasValidMoveForPlayer())
            {
                Debug.Log("[Player] Still has valid moves. Not passing turn.");
                return;
            }

            Debug.Log("Passing turn from Player → AI");
            lastStickValue = 0;
            moveInProgress = false;
            currentTurn = TurnType.AI;

            if (Instance != null && Instance.gameObject != null)
            {
                Instance.ShowTemporaryTurnMessage("AI Turn");
                AiMover.StartAITurn();
                StickThrower.Instance.ShowStickVisuals();
            }
            else
            {
                Debug.LogWarning("No valid PieceMover.Instance found for AI.");
            }

            

            // Decrease freeze counters
            foreach (var piece in GameObject.FindObjectsOfType<PieceMover>())
            {
                if (piece.frozenTurnsRemaining > 0)
                {
                    piece.frozenTurnsRemaining--;

                    if (piece.frozenTurnsRemaining == 0)
                    {
                        Renderer rend = piece.GetComponent<Renderer>();
                        if (piece.originalMaterial != null)
                            rend.material = piece.originalMaterial;

                        Debug.Log($"[Binding of Aegis] {piece.name} is no longer frozen.");
                    }
                }
            }
        }
    }


    public static bool IsValidMove(PieceMover piece, int targetIndex, out Transform targetTile)
    {
        targetTile = null;

        //Reject if targetIndex is greater than 30 (virtual max)
        if (targetIndex > totalTiles) return false;

        int currentIndex = piece.GetCurrentTileIndex();
        if (currentIndex == -1) return false;

        //Prevent leaving Skyspark unless stick value is exactly 5 now
        Transform currentTile = boardTransform.GetChild(currentIndex);
        TileMarker marker = currentTile.GetComponent<TileMarker>();

        if (marker != null && marker.isSkysparkTile && lastStickValue != 5)
        {
            bool skipSkyspark = piece.hasPermanentGrace
                                || (piece.isAI && anippeGraceActive_AI && !anippeGraceUsed_AI)
                                || (!piece.isAI && anippeGraceActive_Player && !anippeGraceUsed_Player);


            if (skipSkyspark)
            {
                if (piece.isAI) anippeGraceUsed_AI = true;
                else anippeGraceUsed_Player = true;

                Debug.Log($"[Anippe’s Grace] {piece.name} ignored Skyspark restriction.");
            }
            else
            {
                Debug.Log($"{piece.name} is on Skyspark Swap and cannot move unless stick value is exactly 5.");
                return false;
            }
        }


        //Handle virtual tile 31 logic — redirect to last real tile
        if (targetIndex == totalTiles)
        {
            targetTile = boardTransform.GetChild(totalTiles - 1); // Tile 30 (index 29)
            return true;
        }

        // Scan for consecutive opponents from currentIndex+1 to targetIndex+3
        bool pathOfAaruActive = (piece.isAI && pathOfAaruActive_AI) || (!piece.isAI && pathOfAaruActive_Player);

        if (!pathOfAaruActive)
        {
            // Scan for consecutive opponents from currentIndex+1 to targetIndex+3
            int consecutiveOpponents = 0;
            bool blockSwap = false;
            bool blockMove = false;
            int checkLimit = Mathf.Min(targetIndex + 3, totalTiles);

            for (int i = currentIndex + 1; i < checkLimit; i++)
            {
                Transform tile = boardTransform.GetChild(i);
                PieceMover occupying = tile.GetComponentInChildren<PieceMover>();

                if (occupying != null && occupying.isAI != piece.isAI)
                {
                    consecutiveOpponents++;

                    if (consecutiveOpponents == 2)
                    {
                        if (i == targetIndex)
                        {
                            blockSwap = true;
                        }
                        else if (i > targetIndex)
                        {
                            Transform targetTileCheck = boardTransform.GetChild(targetIndex);
                            PieceMover targetOccupying = targetTileCheck.GetComponentInChildren<PieceMover>();

                            if (targetOccupying != null && targetOccupying.isAI != piece.isAI)
                                blockSwap = true;
                        }
                    }

                    if (consecutiveOpponents >= 3)
                    {
                        blockMove = true;
                        break;
                    }
                }
                else
                {
                    consecutiveOpponents = 0;
                }
            }

            if (blockMove || blockSwap)
                return false;
        }


        targetTile = boardTransform.GetChild(targetIndex);

        PieceMover occupyingFinal = targetTile.GetComponentInChildren<PieceMover>();

        // Protected piece cannot be replaced
        if (occupyingFinal != null && occupyingFinal.isProtected)
            return false;

        bool canReplace = occupyingFinal != null && occupyingFinal.isAI != piece.isAI;

        if (!canReplace && targetTile.childCount > 0)
            return false;

        return true;
    }




    public IEnumerator MoveToTile(Transform targetTile)
    {
        moveInProgress = true;

        int currentIndex = GetCurrentTileIndex();
        int targetIndex = -1;

        for (int i = 0; i < totalTiles; i++)
        {
            if (boardTransform.GetChild(i) == targetTile)
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex == -1 || currentIndex == -1)
        {
            moveInProgress = false;
            yield break;
        }

        if (currentIndex + lastStickValue == totalTiles)
        {
            Transform finalTile = boardTransform.GetChild(totalTiles - 1);
            float VmoveSpeed = 5f;
            float VlocalY = transform.localPosition.y;
            float fixedY = finalTile.position.y + VlocalY;
            Vector3 end = new Vector3(finalTile.position.x, fixedY, finalTile.position.z);

            while (Vector3.Distance(transform.position, end) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, end, VmoveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.SetParent(finalTile);
            transform.localPosition = new Vector3(0, VlocalY, 0);
            yield return new WaitForSeconds(0.3f);

            GameManager.Instance.CheckForWinCondition();

            lastMoveWasRethrow = (lastStickValue == 1 || lastStickValue == 3);
            lastStickValue = 0;
            moveInProgress = false;

            if (!lastMoveWasRethrow)
                currentTurn = (currentTurn == TurnType.Player) ? TurnType.AI : TurnType.Player;

            ShowTemporaryTurnMessage(currentTurn == TurnType.Player ? "Player Turn" : "AI Turn");
            if (currentTurn == TurnType.Player && sandsOfEsnaPlayer)
            {
                StickThrower.Instance.throwButton.gameObject.SetActive(false);
                StickThrower.Instance.AutoThrowWithOptions();
            }
            UpdateThrowButtonState();

            if (isAI && anippeGraceUsed_AI) anippeGraceActive_AI = false;
            else if (!isAI && anippeGraceUsed_Player) anippeGraceActive_Player = false;

            if (currentTurn == TurnType.AI)
            {
                if (lastMoveWasRethrow)
                    AiMover.StartStickThrow();
                else
                    AiMover.StartAITurn();
            }

            Destroy(gameObject);
            yield break;
        }
        float moveSpeed = 5f;
        float localY = transform.localPosition.y;

        for (int i = currentIndex + 1; i <= targetIndex; i++)
        {
            Transform nextTile = boardTransform.GetChild(i);
            Vector3 end = new Vector3(nextTile.position.x, nextTile.position.y + localY, nextTile.position.z);

            while (Vector3.Distance(transform.position, end) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, end, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.SetParent(nextTile);
            transform.localPosition = new Vector3(0, localY, 0);
        }

        // Check for opponent to send back
        Transform previousTile = boardTransform.GetChild(currentIndex);
        PieceMover opponent = targetTile.GetComponentInChildren<PieceMover>();

        if (opponent != null && opponent.isAI != isAI)
        {
            if (opponent.isProtected)
            {
                Debug.Log($"{opponent.name} is protected by Sylvan Shield. Cannot swap.");
                moveInProgress = false;
                yield break;
            }
            AudioController.Instance.PlaySound("PieceSwap");
            opponent.transform.SetParent(previousTile);
            opponent.transform.localPosition = new Vector3(0, opponent.transform.localPosition.y, 0);
        }

        // Final placement
        transform.SetParent(targetTile);
        transform.localPosition = new Vector3(0, transform.localPosition.y, 0);

        highlightedTile?.GetComponent<TileMarker>()?.Unhighlight();
        highlightedTile = null;
        selectedPiece = null;

        // Check Senusret logic
        if (targetTile == ScrollEffectExecutor.Instance.senusretMarkedTile1 ||
            targetTile == ScrollEffectExecutor.Instance.senusretMarkedTile2)
        {
            bool skipSenusret = hasPermanentGrace
                                || (isAI && anippeGraceActive_AI && !anippeGraceUsed_AI)
                                || (!isAI && anippeGraceActive_Player && !anippeGraceUsed_Player);


            if (skipSenusret)
            {
                if (isAI) anippeGraceUsed_AI = true;
                else anippeGraceUsed_Player = true;

                Debug.Log($"[Anippe’s Grace] {(isAI ? "AI" : "Player")} skipped Senusret Path effect.");
            }
            else
            {
                Debug.Log($"{name} landed on Senusret Path! Sending back...");

                Transform board = targetTile.parent;
                Transform fallbackTile = null;

                for (int i = 15; i >= 0; i--)
                {
                    Transform candidate = board.GetChild(i);
                    if (candidate.childCount == 0)
                    {
                        fallbackTile = candidate;
                        break;
                    }
                }

                if (fallbackTile != null)
                {
                    transform.SetParent(fallbackTile);
                    transform.localPosition = new Vector3(0, transform.localPosition.y, 0);
                    Debug.Log($"{name} relocated to tile: {fallbackTile.name}");

                    TileMarker marker = fallbackTile.GetComponent<TileMarker>();
                    if (marker != null && marker.isTriggerTile)
                    {
                        if (isAI)
                            AiMover.Instance.UseRandomAiScroll();
                        else
                            GameObject.FindObjectOfType<ScrollManager>()?.SetScrollsInteractable(true);
                    }
                }
                else
                {
                    Debug.LogWarning("No valid fallback tile found behind 15!");
                }
            }
        }


        lastMoveWasRethrow = (lastStickValue == 1 || lastStickValue == 3);
        lastStickValue = 0;
        moveInProgress = false;

        // ---- Trigger Tile Logic ----
        TileMarker tileMarker = targetTile.GetComponent<TileMarker>();
        if (tileMarker != null && tileMarker.isTriggerTile)
        {
            bool skipEffect = false;

            // Skip only Skyspark or Senusret with Grace
            if ((tileMarker.isSkysparkTile || targetTile == ScrollEffectExecutor.Instance.senusretMarkedTile1 ||
                 targetTile == ScrollEffectExecutor.Instance.senusretMarkedTile2) &&
                ((isAI && anippeGraceActive_AI && !anippeGraceUsed_AI) ||
                 (!isAI && anippeGraceActive_Player && !anippeGraceUsed_Player)))
            {
                if (isAI) anippeGraceUsed_AI = true;
                else anippeGraceUsed_Player = true;

                skipEffect = true;
                Debug.Log(
                    $"[Anippe’s Grace] Skipped effect on {(tileMarker.isSkysparkTile ? "Skyspark" : "Senusret")} tile.");
            }

            if (!skipEffect)
            {
                if (isAI)
                {
                    AiMover.Instance.UseRandomAiScroll();
                    yield return new WaitForSeconds(1f);
                }
                else
                {
                    ScrollManager scrollManager = GameObject.FindObjectOfType<ScrollManager>();
                    if (scrollManager != null)
                        scrollManager.SetScrollsInteractable(true);

                    yield break;
                }
            }
        }


        // ---- Turn Switching ----
        if (!lastMoveWasRethrow)
        {
            // Switch turn between Player and AI
            currentTurn = (currentTurn == TurnType.Player) ? TurnType.AI : TurnType.Player;

            // Reset Path of Aaru flags after turn ends
            pathOfAaruActive_Player = false;
            pathOfAaruActive_AI = false;

            // NEW: Decrease frozen turns
            foreach (var piece in GameObject.FindObjectsOfType<PieceMover>())
            {
                if (piece.frozenTurnsRemaining > 0)
                {
                    piece.frozenTurnsRemaining--;

                    if (piece.frozenTurnsRemaining == 0)
                    {
                        Renderer rend = piece.GetComponent<Renderer>();
                        if (piece.originalMaterial != null)
                            rend.material = piece.originalMaterial;

                        Debug.Log($"[Binding of Aegis] {piece.name} is no longer frozen.");
                    }
                }
            }
        }

        ShowTemporaryTurnMessage(currentTurn == TurnType.Player ? "Player Turn" : "AI Turn");
        if (currentTurn == TurnType.Player && sandsOfEsnaPlayer)
        {
            StickThrower.Instance.throwButton.gameObject.SetActive(false);
            StickThrower.Instance.AutoThrowWithOptions();
        }
        UpdateThrowButtonState();

        if (isAI && anippeGraceUsed_AI) anippeGraceActive_AI = false;
        else if (!isAI && anippeGraceUsed_Player) anippeGraceActive_Player = false;

        if (currentTurn == TurnType.AI)
        {
            if (lastMoveWasRethrow)
                AiMover.StartStickThrow();
            else
                AiMover.StartAITurn();
        }
    }

    public int GetCurrentTileIndex()
    {
        if (transform.parent == null) return -1;

        for (int i = 0; i < totalTiles; i++)
        {
            if (transform.parent == boardTransform.GetChild(i))
                return i;
        }

        return -1;
    }

    public static void ResetTurn()
    {
        moveInProgress = false;
        obsidianUsedThisTurn = false;
        if (highlightedTile != null)
        {
            highlightedTile.GetComponent<TileMarker>()?.Unhighlight();
            highlightedTile = null;
        }

        selectedPiece = null;
        if (currentTurn == TurnType.Player)
        {
            anippeGraceUsed_Player = false;
        }
        else
        {
            anippeGraceUsed_AI = false;
        }

    }

    void UpdateThrowButtonState()
    {
        if (throwButton != null)
        {
            if (sandsOfEsnaPlayer == false)
            {
                throwButton.SetActive(currentTurn == TurnType.Player);
            }
            else
            {
                throwButton.SetActive(false);
            }

            skipButton.SetActive(currentTurn != TurnType.Player);
        }

        // Show stick visuals again regardless of whose turn it is
        StickThrower stickThrower = GameObject.FindObjectOfType<StickThrower>();
        if (stickThrower != null)
        {
            stickThrower.ShowStickVisuals();
        }
    }

    public Transform GetCurrentTile()
    {
        int index = GetCurrentTileIndex();
        if (index >= 0 && index < boardTransform.childCount)
            return boardTransform.GetChild(index);
        return null;
    }

    public static PieceMover GetPieceOnTile(Transform tile)
    {
        return FindObjectsOfType<PieceMover>().FirstOrDefault(p => p.GetCurrentTile() == tile);
    }
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }


    private Coroutine turnMessageRoutine;

    public void ShowTemporaryTurnMessage(string message, float duration = 5f)
    {
        if (turnFeedbackText == null) return;

        if (turnMessageRoutine != null)
            StopCoroutine(turnMessageRoutine);

        turnFeedbackText.text = message;
        turnMessageRoutine = StartCoroutine(ClearTurnMessageAfterDelay(message, duration));
    }

    private IEnumerator ClearTurnMessageAfterDelay(string message, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (turnFeedbackText.text == message)
            turnFeedbackText.text = "";
    }

}
