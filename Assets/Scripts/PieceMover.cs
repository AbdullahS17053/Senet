using System.Collections;
using UnityEngine;

public enum TurnType { Player, AI }

public class PieceMover : MonoBehaviour
{
    public GameObject throwButton;
    public GameObject skipButton;
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

    private ScrollManager scrollManager;
    [HideInInspector] public bool isProtected = false;
    [HideInInspector] public Material originalMaterial;
    
    [SerializeField] private UnityEngine.UI.Text turnFeedbackText;




    

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
        
        if(skipButton==null)
            skipButton = GameObject.FindWithTag("SkipButton");
        
        
        scrollManager = GameObject.FindObjectOfType<ScrollManager>();
        turnFeedbackText.text = "Player Turn";


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
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                GameObject tapped = hit.collider.gameObject;

                if (tapped.CompareTag("Piece"))
                {
                    PieceMover tappedPiece = tapped.GetComponent<PieceMover>();
                    if (!tappedPiece.isAI) // Only allow selecting player piece
                        HandlePieceTap(tappedPiece);
                }
                else if (highlightedTile != null && tapped.transform == highlightedTile)
                {
                    selectedPiece?.StartCoroutine(selectedPiece.MoveToTile(highlightedTile));
                }
            }
        }
    }

    void HandlePieceTap(PieceMover piece)
    {
        int currentIndex = piece.GetCurrentTileIndex();
        if (currentIndex == -1) return;

        int targetIndex = currentIndex + lastStickValue;
        if (targetIndex >= totalTiles) return;

        Transform targetTile;
        if (!IsValidMove(piece, targetIndex, out targetTile))
            return;

        // Unhighlight previous
        if (highlightedTile != null)
            highlightedTile.GetComponent<TileMarker>()?.Unhighlight();

        // Highlight new tile
        targetTile.GetComponent<TileMarker>()?.Highlight(highlightMaterial);

        selectedPiece = piece;
        highlightedTile = targetTile;
    }
    public static bool HasAnyValidMove(bool isAI)
    {
        PieceMover[] allPieces = GameObject.FindObjectsOfType<PieceMover>();

        foreach (PieceMover piece in allPieces)
        {
            if (piece.isAI != isAI) continue;

            int currentIndex = piece.GetCurrentTileIndex();
            if (currentIndex == -1) continue;

            int targetIndex = currentIndex + lastStickValue;
            if (targetIndex >= totalTiles) continue;

            Transform dummy;
            if (IsValidMove(piece, targetIndex, out dummy))
                return true;
        }

        return false;
    }
    public static void PassTurnImmediately()
    {
        if (currentTurn == TurnType.AI)
        {
            Debug.Log("Passing turn from AI → Player");
            lastStickValue = 0;
            moveInProgress = false;
            currentTurn = TurnType.Player;

            StickThrower stickThrower = GameObject.FindObjectOfType<StickThrower>();
            if (stickThrower != null)
            {
                Debug.Log(1);
                stickThrower.UpdateThrowButtonState();
                Debug.Log(2);
            }
            else
            {
                Debug.LogError("StickThrower not found when trying to enable throw button.");
            }

            // ✅ Update turn text
            Instance?.ShowTemporaryTurnMessage("Player Turn");
        }
        else if (currentTurn == TurnType.Player)
        {
            Debug.Log("Passing turn from Player → AI");
            lastStickValue = 0;
            moveInProgress = false;
            currentTurn = TurnType.AI;

            // ✅ Update turn text
            Instance?.ShowTemporaryTurnMessage("AI Turn");

            AiMover.StartStickThrow();
        }
    }

    public static bool IsValidMove(PieceMover piece, int targetIndex, out Transform targetTile)
    {
        targetTile = null;

        if (targetIndex >= totalTiles) return false;

        int currentIndex = piece.GetCurrentTileIndex();
        if (currentIndex == -1) return false;
        //Prevent leaving Skyspark unless stick value is exactly 4
        Transform currentTile = boardTransform.GetChild(currentIndex);
        TileMarker marker = currentTile.GetComponent<TileMarker>();

        if (marker != null && marker.isSkysparkTile && lastStickValue != 4)
        {
            Debug.Log($"{piece.name} is on Skyspark Swap and cannot move unless stick value is exactly 4.");
            return false;
        }

        
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

        float moveSpeed = 5f;
        float yPos = transform.position.y;

        for (int i = currentIndex + 1; i <= targetIndex; i++)
        {
            Transform nextTile = boardTransform.GetChild(i);
            Vector3 end = new Vector3(nextTile.position.x, yPos, nextTile.position.z);

            // Move to the next tile position step-by-step
            while (Vector3.Distance(transform.position, end) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, end, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = end;
        }


        // Check if there's an opponent piece to send back
        Transform previousTile = boardTransform.GetChild(currentIndex);
        PieceMover opponent = targetTile.GetComponentInChildren<PieceMover>();

        if (opponent != null && opponent.isAI != isAI)
        {
            if (opponent.isProtected)
            {
                Debug.Log($"{opponent.name} is protected by Sylvan Shield. Cannot swap.");
                // Cancel move (optional: allow occupying same tile if game logic allows)
                moveInProgress = false;
                yield break;
            }

            opponent.transform.SetParent(previousTile);
            opponent.transform.localPosition = new Vector3(0, opponent.transform.localPosition.y, 0);
        }


        // Final placement
        transform.SetParent(targetTile);
        transform.localPosition = new Vector3(0, transform.localPosition.y, 0);

        // Unhighlight and reset selection
        highlightedTile?.GetComponent<TileMarker>()?.Unhighlight();
        highlightedTile = null;
        selectedPiece = null;
        // 🚩 Senusret Path trigger check
        if (ScrollEffectExecutor.Instance.senusretMarkedTile != null &&
            targetTile == ScrollEffectExecutor.Instance.senusretMarkedTile)
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

            if (fallbackTile == null)
            {
                Debug.LogWarning("No valid fallback tile found behind 15!");
            }
            else
            {
                transform.SetParent(fallbackTile);
                transform.localPosition = new Vector3(0, transform.localPosition.y, 0);
                Debug.Log($"{name} relocated to tile: {fallbackTile.name}");

                // Optional: trigger tile logic again if fallback tile is a trigger
                TileMarker marker = fallbackTile.GetComponent<TileMarker>();
                if (marker != null && marker.isTriggerTile)
                {
                    if (isAI)
                        AiMover.Instance.UseRandomAiScroll();
                    else
                        GameObject.FindObjectOfType<ScrollManager>()?.SetScrollsInteractable(true);
                }
            }
        }

        // Rethrow logic
        lastMoveWasRethrow = (lastStickValue == 1 || lastStickValue == 4);
        lastStickValue = 0;

        moveInProgress = false;

        // Win condition
        if (targetIndex == totalTiles - 1)
        {
            GameManager.Instance.CheckForWinCondition();
            yield return new WaitForSeconds(0.1f); // short delay
            Destroy(gameObject);
            yield break;
        }


        // ---- Scroll Trigger Logic with Anippe’s Grace ----
        TileMarker tileMarker = targetTile.GetComponent<TileMarker>();
        if (tileMarker != null && tileMarker.isTriggerTile)
        {
            bool skipTrigger = false;

            if (isAI && anippeGraceActive_AI && !anippeGraceUsed_AI)
            {
                anippeGraceUsed_AI = true;
                skipTrigger = true;
                Debug.Log("[Anippe’s Grace] AI skipped trigger tile effect.");
            }
            else if (!isAI && anippeGraceActive_Player && !anippeGraceUsed_Player)
            {
                anippeGraceUsed_Player = true;
                skipTrigger = true;
                Debug.Log("[Anippe’s Grace] Player skipped trigger tile effect.");
            }

            if (!skipTrigger)
            {
                if (isAI)
                {
                    AiMover.Instance.UseRandomAiScroll();
                }
                else
                {
                    ScrollManager scrollManager = GameObject.FindObjectOfType<ScrollManager>();
                    if (scrollManager != null)
                        scrollManager.SetScrollsInteractable(true);

                    yield break; // Wait for player to use or cancel scroll
                }
            }
        }


        // ---- Turn Switching ----
        if (!lastMoveWasRethrow)
        {
            currentTurn = (currentTurn == TurnType.Player) ? TurnType.AI : TurnType.Player;
        }
        
        ShowTemporaryTurnMessage(currentTurn == TurnType.Player ? "Player Turn" : "AI Turn");

        UpdateThrowButtonState();
        if (isAI && anippeGraceUsed_AI)
            anippeGraceActive_AI = false;
        else if (!isAI && anippeGraceUsed_Player)
            anippeGraceActive_Player = false;


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
            throwButton.SetActive(currentTurn == TurnType.Player);
            skipButton.SetActive(currentTurn != TurnType.Player);
        }

        // Show stick visuals again regardless of whose turn it is
        StickThrower stickThrower = GameObject.FindObjectOfType<StickThrower>();
        if (stickThrower != null)
        {
            stickThrower.ShowStickVisuals();
        }
    }
    private Coroutine turnMessageRoutine;

    private void ShowTemporaryTurnMessage(string message, float duration = 2f)
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
