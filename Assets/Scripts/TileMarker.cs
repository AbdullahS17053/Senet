using UnityEngine;
using System.Collections.Generic;

public class TileMarker : MonoBehaviour
{
    [SerializeField] Material defaultMaterial;
    [SerializeField] Material triggerMaterial;

    private Renderer rend;

    public bool isTriggerTile = false;

    private static List<TileMarker> allTiles = new List<TileMarker>();
    public static List<TileMarker> triggerTiles = new List<TileMarker>();

    void Awake()
    {
        rend = GetComponent<Renderer>();
        allTiles.Add(this);
    }

    void Start()
    {
        if (triggerTiles.Count == 0 && allTiles.Count >= 30)
        {
            SelectTriggerTiles();
        }
    }

    private void SelectTriggerTiles()
    {
        List<int> candidateIndices = new List<int>();

        // Only allow indices from 10 to 29 (last 20 tiles, excluding the first 10)
        for (int i = 10; i < allTiles.Count; i++)
        {
            candidateIndices.Add(i);
        }

        int tries = 0;
        while (triggerTiles.Count < 5 && tries < 100)
        {
            tries++;
            int index = candidateIndices[Random.Range(0, candidateIndices.Count)];
            bool isConsecutive = false;

            foreach (TileMarker tile in triggerTiles)
            {
                int existingIndex = allTiles.IndexOf(tile);
                if (Mathf.Abs(existingIndex - index) == 1)
                {
                    isConsecutive = true;
                    break;
                }
            }

            if (!isConsecutive && !triggerTiles.Contains(allTiles[index]))
            {
                TileMarker tile = allTiles[index];
                tile.isTriggerTile = true;
                tile.defaultMaterial = triggerMaterial;
                tile.Highlight(triggerMaterial);
                triggerTiles.Add(tile);
            }
        }
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
