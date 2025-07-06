using UnityEngine;
using System.Collections.Generic;

public class TileMarker : MonoBehaviour
{
    [SerializeField] Material defaultMaterial;
    [SerializeField] Material triggerMaterial;

    private Renderer rend;

    // Indicates whether this tile is a trigger tile
    public bool isTriggerTile = false;

    // Static list to store all tile markers
    private static List<TileMarker> allTiles = new List<TileMarker>();
    public static List<TileMarker> triggerTiles = new List<TileMarker>();

    void Awake()
    {
        rend = GetComponent<Renderer>();
        allTiles.Add(this);
    }

    void Start()
    {
        // Ensure we only run the trigger tile selection once
        if (triggerTiles.Count == 0 && allTiles.Count >= 20)
        {
            SelectTriggerTiles();
        }
    }

    private void SelectTriggerTiles()
    {
        List<int> candidateIndices = new List<int>();
        int total = allTiles.Count;

        // Consider only the last 20 tiles
        for (int i = total - 20; i < total; i++)
        {
            candidateIndices.Add(i);
        }

        int tries = 0;
        while (triggerTiles.Count < 3 && tries < 100)
        {
            tries++;
            int index = candidateIndices[Random.Range(0, candidateIndices.Count)];
            bool isConsecutive = false;

            // Prevent selection of adjacent tiles
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
                tile.isTriggerTile = true; //Set the flag
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
