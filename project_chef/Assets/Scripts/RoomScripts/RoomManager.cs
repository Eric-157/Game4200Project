using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class RoomData
{
    public int roomID;
    public int roomsVisited;
}

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;
    private string filePath;

    private RoomData roomData;

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
        // Update data
        roomData.roomID = nextRoomID;
        roomData.roomsVisited++;
        SaveRoomData();

        // Create or load a new scene
        SceneManager.LoadScene("Room_" + nextRoomID);
    }

    public int GetCurrentRoomID() => roomData.roomID;
    public int GetRoomsVisited() => roomData.roomsVisited;
}
