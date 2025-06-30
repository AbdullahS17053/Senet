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
    public bool isAI = false;

    void Start()
    {
        if (boardTransform == null)
            boardTransform = GameObject.Find("Board").transform;

        if (mainCamera == null)
            mainCamera = Camera.main;
        
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

        Transform targetTile = boardTransform.GetChild(targetIndex);

        // Allow landing if tile is empty or has an opponent piece
        PieceMover occupyingPiece = targetTile.GetComponentInChildren<PieceMover>();
        bool canReplace = occupyingPiece != null && occupyingPiece.isAI != piece.isAI;
        if (!canReplace && targetTile.childCount > 0) return;

        // Unhighlight previous
        if (highlightedTile != null)
            highlightedTile.GetComponent<TileMarker>()?.Unhighlight();

        // Highlight new tile
        targetTile.GetComponent<TileMarker>()?.Highlight(highlightMaterial);

        selectedPiece = piece;
        highlightedTile = targetTile;
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

// Reset visuals and state
        highlightedTile?.GetComponent<TileMarker>()?.Unhighlight();
        highlightedTile = null;
        selectedPiece = null;

// Store last stick value and reset it
        int lastUsedStick = lastStickValue;
        lastStickValue = 0;
        moveInProgress = false;

// Determine if same player gets another turn
        bool getsAnotherTurn = (lastUsedStick == 1 || lastUsedStick == 4);

        if (!getsAnotherTurn)
        {
            currentTurn = (currentTurn == TurnType.Player) ? TurnType.AI : TurnType.Player;
        }

        UpdateThrowButtonState();

// Start next turn
        if (currentTurn == TurnType.AI)
        {
            if (getsAnotherTurn)
                AiMover.StartStickThrow();
            else
                AiMover.StartAITurn();
        }


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
