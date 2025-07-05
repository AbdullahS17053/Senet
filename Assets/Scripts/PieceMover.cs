using System.Collections;
using UnityEngine;

public enum TurnType { Player, AI }

public class PieceMover : MonoBehaviour
{
    public GameObject throwButton;
    private static Transform boardTransform;
    private static Camera mainCamera;
    private static int totalTiles = 30;

    public static int lastStickValue = 0;
    public static bool moveInProgress = false;

    public static PieceMover selectedPiece = null;
    public static Transform highlightedTile = null;

    public static Material highlightMaterial;

    public static TurnType currentTurn = TurnType.Player;
    public static PieceMover Instance;
    public bool isAI = false;

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
    public static void CheckPlayerCanMove()
    {
        if (!HasAnyValidMove(false))
        {
            Debug.Log("Player has no valid moves. Passing turn...");
            PassTurnImmediately();
        }
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
        }
        else if (currentTurn == TurnType.Player)
        {
            Debug.Log("Passing turn from Player → AI");
            lastStickValue = 0;
            moveInProgress = false;
            currentTurn = TurnType.AI;
            AiMover.StartStickThrow();
        }
    }
    public static bool IsValidMove(PieceMover piece, int targetIndex, out Transform targetTile)
    {
        targetTile = null;

        if (targetIndex >= totalTiles) return false;

        int currentIndex = piece.GetCurrentTileIndex();
        if (currentIndex == -1) return false;

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

    int lastUsedStick = lastStickValue;
    lastStickValue = 0;
    moveInProgress = false;

    // If piece reached the last tile (index 29)
    if (targetIndex == totalTiles - 1)
    {
        Destroy(gameObject); // Remove this piece

        // Check win condition
        if (CheckWinCondition())
            yield break; // Stop further turn switching if game ended
    }

    bool getsAnotherTurn = (lastUsedStick == 1 || lastUsedStick == 4);
    if (!getsAnotherTurn)
    {
        currentTurn = (currentTurn == TurnType.Player) ? TurnType.AI : TurnType.Player;
    }

    UpdateThrowButtonState();

    if (currentTurn == TurnType.AI)
    {
        if (getsAnotherTurn)
            AiMover.StartStickThrow();
        else
            AiMover.StartAITurn();
    }
}
    private bool CheckWinCondition()
    {
        PieceMover[] allPieces = GameObject.FindObjectsOfType<PieceMover>();
        int playerCount = 0;
        int aiCount = 0;

        foreach (var piece in allPieces)
        {
            if (piece.isAI) aiCount++;
            else playerCount++;
        }

        if (playerCount == 0)
        {
            Debug.Log("AI Wins!");
            // Add UI or game over logic here
            return true;
        }

        if (aiCount == 0)
        {
            Debug.Log("Player Wins!");
            // Add UI or game over logic here
            return true;
        }

        return false;
    }



    public void MoveToStart()
    {
        Transform startTile = boardTransform.GetChild(0);
        transform.SetParent(startTile);
        transform.localPosition = new Vector3(0, transform.localPosition.y, 0);
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
        if (highlightedTile != null)
        {
            highlightedTile.GetComponent<TileMarker>()?.Unhighlight();
            highlightedTile = null;
        }
        selectedPiece = null;
    }
    void UpdateThrowButtonState()
    {
        if (throwButton != null)
            throwButton.SetActive(currentTurn == TurnType.Player);
    }
}
