using UnityEngine;

public class GameBoardManager : MonoBehaviour
{
    [SerializeField] private GameObject board;
    [SerializeField] private float rotationSpeed = 0.2f; // Adjust sensitivity

    private Vector2 startTouchPos;
    private bool isSwiping = false;

    void Start()
    {
        TileMarker.InitializeFromBoard(board);
    }

    void Update()
    {
        HandleSwipeRotation();
    }

    void HandleSwipeRotation()
    {
#if UNITY_EDITOR
        // For mouse (Editor)
        if (Input.GetMouseButtonDown(0))
        {
            startTouchPos = Input.mousePosition;
            isSwiping = true;
        }
        else if (Input.GetMouseButton(0) && isSwiping)
        {
            Vector2 currentTouchPos = Input.mousePosition;
            float deltaX = currentTouchPos.x - startTouchPos.x;

            // Rotate around Y axis
            board.transform.Rotate(0f, deltaX * rotationSpeed, 0f);

            startTouchPos = currentTouchPos;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isSwiping = false;
        }
#else
        // For touch (Mobile)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                startTouchPos = touch.position;
                isSwiping = true;
            }
            else if (touch.phase == TouchPhase.Moved && isSwiping)
            {
                float deltaX = touch.position.x - startTouchPos.x;

                board.transform.Rotate(0f, deltaX * rotationSpeed, 0f);

                startTouchPos = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isSwiping = false;
            }
        }
#endif
    }
}