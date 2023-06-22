using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DataPersistenceManager : MonoBehaviour
{
    [Header("File Storage Config")]
    [SerializeField] private string fileName;

    public BlockTile[] blockTiles;

    private FileDataHandler dataHandler;
    private GameData gameData;
    private List<IDataPersistence> dataPersistenceObjects;

    public static DataPersistenceManager instance { get; private set; }

    private void Awake()
    {
        if (instance != null) 
            Debug.LogError("Found more than one Data persistence Manager in the scene.");
        
        instance = this;
    }

    private void OnEnable()
    {
        this.dataHandler = new FileDataHandler(Application.persistentDataPath, fileName);
        this.dataPersistenceObjects = FindAllDataPersistenceObjects();
        LoadData();
    }

    public void NewData() => this.gameData = new GameData();
    

    public void LoadData()
    {
        this.gameData = dataHandler.Load();

        if (this.gameData == null) {
            Debug.Log("No data was found.");
            NewData();
        }

        foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects) 
            dataPersistenceObj.LoadData(gameData);   
    }

    public void SaveData()
    {
        foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects)
            dataPersistenceObj.SaveData(ref gameData);

        dataHandler.Save(gameData);
    }

    private List<IDataPersistence> FindAllDataPersistenceObjects()
    {
        IEnumerable<IDataPersistence> dataPersistenceObjects = FindObjectsOfType<MonoBehaviour>()
            .OfType<IDataPersistence>();
        return new List<IDataPersistence>(dataPersistenceObjects);
    }
}