using UnityEngine;

[CreateAssetMenu(fileName = "ScrollData", menuName = "GameData/ScrollData")]
public class ScrollData : ScriptableObject
{
    public Sprite[] scrollSprites;
    public Sprite[] scrollBacks;
    public string[] scrollNames;
    public string[] scrollEffectKeys; // New: maps each scroll to its effect
}