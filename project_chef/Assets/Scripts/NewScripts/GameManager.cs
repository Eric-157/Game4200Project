using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Room Spawning")]
    [Tooltip("Empty object in the scene used as the location to spawn rooms.")]
    public Transform roomSpawnPoint;

    [Tooltip("Tutorial room prefab that is spawned at game start (not part of random list).")]
    public GameObject tutorialRoomPrefab;

    [Tooltip("List of room prefabs used for regular room generation.")]
    public List<GameObject> roomPrefabs = new List<GameObject>();

    [Header("Player")]
    [Tooltip("Optional: drag the player transform here. If empty, the player will be found by tag 'Player'.")]
    public Transform player;

    // public state
    public int currentRoomID { get; private set; } = -1; // -1 = no room, 0 = tutorial (if used)
    public int roomsVisited { get; private set; } = 0;

    // internal
    private GameObject currentRoomInstance;

    // enemy tracking
    private int enemiesAlive = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (player == null)
        {
            var pgo = GameObject.FindGameObjectWithTag("Player");
            if (pgo != null) player = pgo.transform;
        }
    }

    private void Start()
    {
        if (tutorialRoomPrefab != null && roomSpawnPoint != null)
        {
            GenerateTutorialRoom();
        }
    }

    public void GenerateTutorialRoom()
    {
        if (currentRoomInstance != null)
        {
            Destroy(currentRoomInstance);
        }


        currentRoomInstance = Instantiate(tutorialRoomPrefab, roomSpawnPoint.position, roomSpawnPoint.rotation, null);
        currentRoomInstance.name = "Room";

        currentRoomID = 0;

        MovePlayerToSpawn();

        var camBounds = FindObjectOfType<CameraController>();
        if (camBounds != null) camBounds.RefreshBounds();
        if (camBounds != null) camBounds.SetPlayer(player);
    }

    public void GenerateRoomByIndex(int index)
    {
        if (index < 0 || index >= roomPrefabs.Count)
        {
            Debug.LogWarning("Invalid room index: " + index);
            return;
        }

        if (currentRoomInstance != null)
            Destroy(currentRoomInstance);

        currentRoomInstance = Instantiate(roomPrefabs[index], roomSpawnPoint.position, roomSpawnPoint.rotation, null);
        currentRoomInstance.name = "Room";

        currentRoomID = index;
        roomsVisited++;

        MovePlayerToSpawn();

        // notify camera bounds if present
        var camBounds = FindObjectOfType<CameraController>();
        if (camBounds != null) camBounds.RefreshBounds();
        if (camBounds != null) camBounds.SetPlayer(player);
    }

    private void MovePlayerToSpawn()
    {
        // if (player == null)
        // {
        //     var pgo = GameObject.FindGameObjectWithTag("Player");
        //     if (pgo != null) player = pgo.transform;
        // }

        if (player == null || currentRoomInstance == null)
        {
            Debug.LogWarning("Cannot move player to spawn: missing player or room instance.");
            return;
        }

        Transform spawn = currentRoomInstance.transform.Find("SpawnPoint");
        if (spawn != null)
        {
            player.position = spawn.position;
            player.rotation = spawn.rotation;
            Debug.Log("Moved player to spawn: " + player.name + " at " + player.position);
        }
        else
        {
            Debug.LogWarning("Room prefab missing a SpawnPoint object!");
        }
    }

    public void RegisterEnemy()
    {
        enemiesAlive++;
    }

    public void UnregisterEnemy()
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);

        if (enemiesAlive == 0)
        {
            UnlockAllDoorsInCurrentRoom();
        }
    }

    private void UnlockAllDoorsInCurrentRoom()
    {
        if (currentRoomInstance == null) return;

        var doors = currentRoomInstance.GetComponentsInChildren<DoorTrigger>(true);
        foreach (var d in doors)
        {
            if (d != null) d.UnlockDoor();
        }
    }

    public GameObject GetCurrentRoomRoot() => currentRoomInstance;
    public int GetEnemiesAlive() => enemiesAlive;
}
