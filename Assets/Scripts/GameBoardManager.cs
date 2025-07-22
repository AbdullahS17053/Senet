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
            float deltaY = currentTouchPos.y - startTouchPos.y;

            float newXRotation = board.transform.eulerAngles.x + deltaY * rotationSpeed;
            if (newXRotation > 180) newXRotation -= 360;
            newXRotation = Mathf.Clamp(newXRotation, -60f, 30f);

            Vector3 currentRotation = board.transform.eulerAngles;
            board.transform.eulerAngles = new Vector3(newXRotation, currentRotation.y, currentRotation.z);

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
            float deltaY = touch.position.y - startTouchPos.y;

            float newXRotation = board.transform.eulerAngles.x + deltaY * rotationSpeed;
            if (newXRotation > 180) newXRotation -= 360;
            newXRotation = Mathf.Clamp(newXRotation, -60f, 30f);

            Vector3 currentRotation = board.transform.eulerAngles;
            board.transform.eulerAngles = new Vector3(newXRotation, currentRotation.y, currentRotation.z);

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