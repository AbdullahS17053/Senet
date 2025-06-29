using UnityEngine;

public class TileMarker : MonoBehaviour
{
    [SerializeField] Material defaultMaterial;
    private Renderer rend;

    void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    public void Highlight(Material highlightMat)
    {
        rend.material = highlightMat;
    }

    public void Unhighlight()
    {
        rend.material = defaultMaterial;
    }
}