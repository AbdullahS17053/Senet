using System.Collections;
using UnityEngine;

public class PieceMover : MonoBehaviour
{
    private static Transform boardTransform;
    private static Camera mainCamera;
    private static int totalTiles = 30;

    public static int lastStickValue = 0;
    public static bool moveInProgress = false;

    public static PieceMover selectedPiece = null;
    public static Transform highlightedTile = null;

    public static Material highlightMaterial;

    void Start()
    {
        if (boardTransform == null)
            boardTransform = GameObject.Find("Board").transform;

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began && lastStickValue > 0 && !moveInProgress)
        {
            Touch touch = Input.touches[0];
            Ray ray = mainCamera.ScreenPointToRay(touch.position);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                GameObject tapped = hit.collider.gameObject;

                if (tapped.CompareTag("Piece"))
                {
                    HandlePieceTap(tapped.GetComponent<PieceMover>());
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
        if (targetTile.childCount > 0) return; // Tile occupied

        // Unhighlight previous tile if any
        if (highlightedTile != null)
        {
            TileMarker tile = highlightedTile.GetComponent<TileMarker>();
            tile?.Unhighlight();
        }

        // Highlight new tile
        TileMarker newTile = targetTile.GetComponent<TileMarker>();
        newTile?.Highlight(highlightMaterial);

        selectedPiece = piece;
        highlightedTile = targetTile;
    }

    public IEnumerator MoveToTile(Transform targetTile)
    {
        moveInProgress = true;

        float yPos = transform.position.y;
        Vector3 start = transform.position;
        Vector3 end = new Vector3(targetTile.position.x, yPos, targetTile.position.z);

        float moveSpeed = 5f;
        while (Vector3.Distance(transform.position, end) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, end, moveSpeed * Time.deltaTime);
            yield return null;
        }

        // Snap and reparent
        transform.position = end;
        transform.SetParent(targetTile);
        transform.localPosition = new Vector3(0, transform.localPosition.y, 0);

        // Reset selection and highlight
        highlightedTile.GetComponent<TileMarker>()?.Unhighlight();
        highlightedTile = null;
        selectedPiece = null;

        lastStickValue = 0;
        moveInProgress = false;
    }

    int GetCurrentTileIndex()
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
}
