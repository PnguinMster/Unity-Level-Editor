using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor.Events;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;

[Serializable]
public struct PlacedTile
{
    public int PlacedID;
    public string TileID;
    public Vector3 TilePosition;
    public Vector3 TileRotation;
    public Vector3 TileScale;

    public PlacedTile(int placedID, string tileID, Transform tileTransform)
    {
        PlacedID = placedID;
        TileID = tileID;
        TilePosition = tileTransform.position;
        TileRotation = tileTransform.eulerAngles;
        TileScale = tileTransform.localScale;
    }

    public void SetPlacedID(int placedID)
    {
        PlacedID = placedID;
    }
}

public class EditorUI : MonoBehaviour, IDataPersistence
{
    [SerializeField] private Button baseButton;
    [SerializeField] private DataPersistenceManager DataPersistence;
    [SerializeField] private GridLayoutGroup gridPanel;
    public static event Action<BlockTile> onBlockSelected;

    protected List<PlacedTile> placedTiles;

    private void OnEnable()
    {
        EditorController.onPlacedTile += AddPlacedTile;
        EditorController.onTileDeleted += DeletePlacedTile;
        EditorController.onUpdatePlacedTile += UpdatePlacedTile;
    }

    private void OnDisable()
    {
        EditorController.onPlacedTile -= AddPlacedTile;
        EditorController.onTileDeleted -= DeletePlacedTile;
        EditorController.onUpdatePlacedTile -= UpdatePlacedTile;
    }

    private void Start()
    {
        LoadLevelTiles();

        if(placedTiles == null)
            placedTiles = new List<PlacedTile>();

        for (int i = 0; i < DataPersistence.blockTiles.Length; i++) {
            Button button = Instantiate(baseButton, gridPanel.transform);
            button.image.sprite = DataPersistence.blockTiles[i].icon;

            UnityAction<int> action = new UnityAction<int>(SetSelectedTile);
            UnityEventTools.AddIntPersistentListener(button.onClick, action, i);
        }
    }

    private void LoadLevelTiles()
    {
        if (placedTiles == null) return;

        int tileID = 0;
        foreach (PlacedTile placedTile in placedTiles) {
            BlockTile blockTile = Array.Find(DataPersistence.blockTiles, block => block.id == placedTile.TileID);
            GameObject tile = Instantiate(blockTile.prefab, placedTile.TilePosition, Quaternion.Euler(placedTile.TileRotation));
            tile.transform.localScale = placedTile.TileScale;
            placedTile.SetPlacedID(tileID);
            tile.AddComponent<TileInfo>().runtimeID = tileID;
            tileID++;
        }

        EditorController.nextTileID = tileID + 1;
    }

    private void SetSelectedTile(int index) => onBlockSelected?.Invoke(DataPersistence.blockTiles[index]);
    private void AddPlacedTile(PlacedTile tileAdded)
    {
        if (placedTiles.Any(x => x.PlacedID == tileAdded.PlacedID)) return;
        placedTiles.Add(tileAdded);
    }
    private void DeletePlacedTile(int tilePlacedID)
    {
        for (int i = 0; i < placedTiles.Count;i++) {
            if (placedTiles[i].PlacedID == tilePlacedID) {
                placedTiles.RemoveAt(i);
                return;
            }
        }
    }
    private void UpdatePlacedTile(int runtimeID, Transform transform)
    {
        for (int i = 0; i < placedTiles.Count; i++) {
            if (placedTiles[i].PlacedID == runtimeID) {
                placedTiles[i] = new PlacedTile(runtimeID, placedTiles[i].TileID, transform);
                return;
            }
        }
    }

    public void LoadData(GameData data) => placedTiles = data.placedTiles;
    public void SaveData(ref GameData data) => data.placedTiles = placedTiles;
}