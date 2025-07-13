using UnityEngine;

public class GameBoardManager : MonoBehaviour
{
    [SerializeField] private GameObject board; // Assign your Board GameObject in Inspector

    void Start()
    {
        TileMarker.InitializeFromBoard(board);
    }
}

