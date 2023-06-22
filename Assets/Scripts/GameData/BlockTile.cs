using UnityEngine;

[CreateAssetMenu(menuName = "Block Tile Data")]
public class BlockTile : ScriptableObject
{
    public string id;
    public Sprite icon;
    public GameObject prefab;
}