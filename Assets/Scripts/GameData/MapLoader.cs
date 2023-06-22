using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapLoader : MonoBehaviour, IDataPersistence
{
    [SerializeField] private BlockTile startBlock;
    [SerializeField] private DataPersistenceManager dataPersistence;
    [SerializeField] private GameObject basePrefab;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject tilesParent;

    protected List<PlacedTile> placedTiles = new List<PlacedTile>();
    private CombineInstance[] combine;

    //https://docs.unity3d.com/ScriptReference/Mesh.CombineMeshes.html
    //https://stackoverflow.com/questions/21739791/unity-joining-multiple-mesh-colliders
    //https://stackoverflow.com/questions/59792261/simplify-collision-mesh-of-road-like-system

    private void Start() 
    {
        combine = new CombineInstance[placedTiles.Count];
        Instantiate(basePrefab);
        GameObject startTile = null;

        for (int i = 0; i < placedTiles.Count; i++) {
            GameObject prefab = null;
            prefab = dataPersistence.blockTiles.SingleOrDefault(tile => tile.id == placedTiles[i].TileID).prefab;
            if (prefab == null)
                continue;

            GameObject tile = Instantiate(prefab, placedTiles[i].TilePosition, Quaternion.Euler(placedTiles[i].TileRotation), tilesParent.transform);
            tile.transform.localScale = placedTiles[i].TileScale;

            combine[i].mesh = tile.GetComponent<MeshFilter>().sharedMesh;
            combine[i].transform = tile.transform.localToWorldMatrix;
            tile.GetComponent<MeshCollider>().enabled = false;

            if (placedTiles[i].TileID == startBlock.id)
                startTile = tile;
        }

        Mesh mesh = new Mesh();
        mesh.CombineMeshes(combine);

        //tilesParent.GetComponent<MeshFilter>().mesh = mesh;
        tilesParent.GetComponent<MeshCollider>().sharedMesh = mesh;

        if (startTile == null)
            return;

        player.transform.position = startTile.transform.position + (Vector3.up * (startTile.transform.localScale.y * 2)) + (-startTile.transform.forward * (startTile.transform.localScale.x * 0.5f));
    }

    public void LoadData(GameData data) => this.placedTiles = data.placedTiles;
    public void SaveData(ref GameData data) => data.placedTiles = this.placedTiles;
}