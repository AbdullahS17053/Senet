using UnityEngine;
using UnityEngine.UI;

public class PieceSelector : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] Button selectPlayerButton;
    [SerializeField] Button selectAIButton;

    [SerializeField] private GameObject selectionPanel;
    
    private GameManager gameManager;
    

    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        
        selectPlayerButton.onClick.AddListener(() => 
            StartCoroutine(gameManager.PlayTapEffect(() => SelectPiecesByName("Player"))));

        selectAIButton.onClick.AddListener(() => 
            StartCoroutine(gameManager.PlayTapEffect(() => SelectPiecesByName("AI"))));
        
        selectionPanel.SetActive(true);
    }

    void SelectPiecesByName(string selectedNamePrefix)
    {
        PieceMover[] allPieces = FindObjectsOfType<PieceMover>();

        foreach (var piece in allPieces)
        {
            if (piece.name.StartsWith(selectedNamePrefix))
                piece.isAI = false;
            else
                piece.isAI = true;
        }
        selectionPanel.SetActive(false);
    }
}