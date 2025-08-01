using UnityEngine;
using UnityEngine.UI;

public class PieceSelector : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] Button selectPlayerButton;
    [SerializeField] Button selectAIButton;

    [Header("UI References")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private Image bgImage;
    [SerializeField] private Image puppetImage;
    [SerializeField] private GameObject scrolls;

    [Header("Image Arrays")]
    [SerializeField] private Sprite[] bgImages;
    [SerializeField] private Sprite[] puppetImages;

    private GameManager gameManager;

    private void Awake()
    {
        if (bgImages.Length > 0)
            bgImage.sprite = bgImages[Random.Range(0, bgImages.Length)];

        if (puppetImages.Length > 0)
            puppetImage.sprite = puppetImages[Random.Range(0, puppetImages.Length)];
    }

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
            piece.isAI = !piece.name.StartsWith(selectedNamePrefix);
        }

        selectionPanel.SetActive(false);
        scrolls.SetActive(true);
    }
}