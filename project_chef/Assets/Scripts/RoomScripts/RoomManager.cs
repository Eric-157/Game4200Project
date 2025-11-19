using System.IO;
using UnityEngine;

[System.Serializable]
public class RoomData
{
    public int roomID;
    public int roomsVisited;
}

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    [Header("Room Prefabs")]
    public GameObject[] roomPrefabs;

    private GameObject currentRoomInstance;
    private string filePath;

    private RoomData roomData;
    public static System.Action OnRoomChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        filePath = Path.Combine(Application.persistentDataPath, "RoomData.json");
        LoadRoomData();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            GenerateNextRoom(roomData.roomID);
        }
    }

    public void LoadRoomData()
    {
        if (!File.Exists(filePath))
        {
            roomData = new RoomData { roomID = 1, roomsVisited = 0 };
            SaveRoomData();
        }
        else
        {
            string json = File.ReadAllText(filePath);
            roomData = JsonUtility.FromJson<RoomData>(json);
        }
    }

    public void SaveRoomData()
    {
        string json = JsonUtility.ToJson(roomData, true);
        File.WriteAllText(filePath, json);
    }

    public void GenerateNextRoom(int nextRoomID)
    {
        roomData.roomID = nextRoomID;
        roomData.roomsVisited++;
        SaveRoomData();

        if (currentRoomInstance != null)
            Destroy(currentRoomInstance);

        GameObject prefab = roomPrefabs[nextRoomID];
        currentRoomInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity);

        MovePlayerToRoomSpawn();
        OnRoomChanged.Invoke();
    }

    private void MovePlayerToRoomSpawn()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player").transform;

        Transform spawn = currentRoomInstance.transform.Find("SpawnPoint");

        if (spawn != null)
        {
            player.position = spawn.position;
            player.rotation = spawn.rotation;
        }
        else
        {
            Debug.LogWarning("Room prefab missing a SpawnPoint object!");
        }
    }

    public int GetCurrentRoomID() => roomData.roomID;
    public int GetRoomsVisited() => roomData.roomsVisited;
}
