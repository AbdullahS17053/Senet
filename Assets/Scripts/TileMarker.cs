using UnityEngine;
using System.Collections.Generic;

public class TileMarker : MonoBehaviour
{
    [SerializeField] private Material defaultMaterial;
    [SerializeField] public Material triggerMaterial;
    public bool isSkysparkTile = false;
    public bool senusretTile = false;


    private Renderer rend;

    public bool isTriggerTile = false;

    private static List<TileMarker> allTiles = new List<TileMarker>();
    public static List<TileMarker> triggerTiles = new List<TileMarker>();

    private void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    public static void InitializeFromBoard(GameObject board)
    {
        allTiles.Clear();
        triggerTiles.Clear();

        foreach (Transform child in board.transform)
        {
            TileMarker tile = child.GetComponent<TileMarker>();
            if (tile != null)
            {
                allTiles.Add(tile);
                tile.isTriggerTile = false;
                tile.Unhighlight(); // Reset material
            }
        }

        if (allTiles.Count >= 30)
        {
            SelectTriggerTiles();
        }
        else
        {
            Debug.LogWarning("Not enough tiles (need at least 30). Found: " + allTiles.Count);
        }
    }

    private static void SelectTriggerTiles()
    {
        List<int> candidateIndices = new List<int>();

        // Only from last 20 tiles
        int startIndex = allTiles.Count - 20;
        for (int i = startIndex; i < allTiles.Count; i++)
        {
            candidateIndices.Add(i);
        }

        int tries = 0;
        while (triggerTiles.Count < 5 && tries < 100)
        {
            tries++;
            int randIdx = candidateIndices[Random.Range(0, candidateIndices.Count)];

            bool isConsecutive = false;
            foreach (TileMarker t in triggerTiles)
            {
                int existingIndex = allTiles.IndexOf(t);
                if (Mathf.Abs(existingIndex - randIdx) == 1)
                {
                    isConsecutive = true;
                    break;
                }
            }

            if (!isConsecutive)
            {
                TileMarker tile = allTiles[randIdx];
                if (!triggerTiles.Contains(tile))
                {
                    tile.isTriggerTile = true;
                    tile.Highlight(tile.triggerMaterial);
                    triggerTiles.Add(tile);
                }
            }
        }

        Debug.Log($"Trigger tiles selected: {triggerTiles.Count}");
    }

    public void Highlight(Material highlightMat)
    {
        rend.material = highlightMat;
    }

    public void Unhighlight()
    {
        if (isSkysparkTile)
        {
            rend.material = defaultMaterial;
            rend.material.color = Color.red;
            return;
        }
        
        else if (senusretTile)
        {
            rend.material.color = Color.magenta;
            return;
        }

        rend.material = isTriggerTile ? triggerMaterial : defaultMaterial;
    }
}
