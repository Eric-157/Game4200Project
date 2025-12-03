using System.Collections.Generic;
using System.Collections;
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

    [Header("Transition")]
    [Tooltip("How close the player must be to the SpawnPoint before fading back in")]
    public float spawnDeadzoneDistance = 5f;

    [Tooltip("Timeout (seconds) to wait for the player to be within the deadzone before forcing a re-lock")]
    public float spawnValidationTimeout = 2f;

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

        Debug.Log("[GameManager] Spawning tutorial room at " + roomSpawnPoint.position);
        currentRoomInstance = Instantiate(tutorialRoomPrefab, roomSpawnPoint.position, roomSpawnPoint.rotation, null);
        currentRoomInstance.name = "Room";

        currentRoomID = 0;

        // Use coroutine to delay spawn move by 1 frame so room hierarchy is fully initialized
        StartCoroutine(MovePlayerToSpawnDelayed());

        var camBounds = FindObjectOfType<CameraController>();
        if (camBounds != null) camBounds.RefreshBounds();
        if (camBounds != null) camBounds.SetPlayer(player);
        // If there are no enemies in this room, unlock doors immediately
        if (enemiesAlive == 0)
        {
            UnlockAllDoorsInCurrentRoom();
        }
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

        Debug.Log("[GameManager] Spawning room " + index + " at " + roomSpawnPoint.position);
        currentRoomInstance = Instantiate(roomPrefabs[index], roomSpawnPoint.position, roomSpawnPoint.rotation, null);
        currentRoomInstance.name = "Room";

        currentRoomID = index;
        roomsVisited++;

        // Use coroutine to delay spawn move by 1 frame so room hierarchy is fully initialized
        StartCoroutine(MovePlayerToSpawnDelayed());

        // notify camera bounds if present
        var camBounds = FindObjectOfType<CameraController>();
        if (camBounds != null) camBounds.RefreshBounds();
        if (camBounds != null) camBounds.SetPlayer(player);
        // If there are no enemies in this room, unlock doors immediately
        if (enemiesAlive == 0)
        {
            UnlockAllDoorsInCurrentRoom();
        }
    }

    private System.Collections.IEnumerator MovePlayerToSpawnDelayed()
    {
        // Wait one frame to ensure room hierarchy is fully initialized
        yield return null;

        if (player == null)
        {
            Debug.LogError("[GameManager] Player reference is null in MovePlayerToSpawnDelayed!");
            yield break;
        }

        if (currentRoomInstance == null)
        {
            Debug.LogError("[GameManager] Current room instance is null in MovePlayerToSpawnDelayed!");
            yield break;
        }

        Transform spawn = currentRoomInstance.transform.Find("SpawnPoint");
        Debug.Log("[GameManager] Looking for SpawnPoint in room '" + currentRoomInstance.name + "' - found: " + (spawn != null));

        if (spawn != null)
        {
            // First teleport to spawn
            player.position = spawn.position;
            player.rotation = spawn.rotation;
            Debug.Log("[GameManager] Successfully moved player '" + player.name + "' to spawn at " + player.position);

            // Freeze player movement for 0.3 seconds to prevent input-driven drift after spawn
            var pm = player.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                pm.FreezeMovement(0.3f);
                Debug.Log("[GameManager] Froze player movement for 0.3 seconds.");
            }

            // Wait for freeze duration and then re-lock player to spawn to catch any physics/system overrides
            yield return new WaitForSeconds(0.3f);

            // Second teleport to ensure player stayed at spawn despite any intervening systems
            if (player != null)
            {
                player.position = spawn.position;
                player.rotation = spawn.rotation;

                // Also clear rigidbody velocities if present
                var rb = player.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                Debug.Log("[GameManager] Re-locked player to spawn at end of freeze: " + player.position);
            }
        }
        else
        {
            Debug.LogError("[GameManager] Room '" + currentRoomInstance.name + "' missing a 'SpawnPoint' child object!");
        }
    }

    /// <summary>
    /// Public entry to perform a fade transition and load the requested room index.
    /// </summary>
    public void TransitionToRoom(int index)
    {
        StartCoroutine(RoomTransitionWithFade(index));
    }

    private IEnumerator RoomTransitionWithFade(int index)
    {
        // Fade out if a fader is present
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeToBlack();
            yield return new WaitForSeconds(ScreenFader.Instance.fadeDuration + 0.05f);
        }

        // Validate index
        if (index < 0 || index >= roomPrefabs.Count)
        {
            Debug.LogWarning("TransitionToRoom: invalid room index " + index);
            // Fade back in immediately if possible
            if (ScreenFader.Instance != null)
            {
                ScreenFader.Instance.FadeFromBlack();
            }
            yield break;
        }

        // Destroy existing room and instantiate the new one
        if (currentRoomInstance != null)
            Destroy(currentRoomInstance);

        Debug.Log("[GameManager] Transition: spawning room " + index + " at " + roomSpawnPoint.position);
        currentRoomInstance = Instantiate(roomPrefabs[index], roomSpawnPoint.position, roomSpawnPoint.rotation, null);
        currentRoomInstance.name = "Room";

        currentRoomID = index;
        roomsVisited++;

        // Refresh camera bounds now that room exists
        var camBounds = FindObjectOfType<CameraController>();
        if (camBounds != null) camBounds.RefreshBounds();
        if (camBounds != null) camBounds.SetPlayer(player);

        // Move player to spawn (this coroutine waits one frame first)
        yield return StartCoroutine(MovePlayerToSpawnDelayed());

        // Validate player is within the configured deadzone of the spawn point
        if (currentRoomInstance != null)
        {
            Transform spawn = currentRoomInstance.transform.Find("SpawnPoint");
            if (spawn != null && player != null)
            {
                float elapsed = 0f;
                bool within = false;
                while (elapsed < spawnValidationTimeout)
                {
                    if (Vector3.Distance(player.position, spawn.position) <= spawnDeadzoneDistance)
                    {
                        within = true;
                        break;
                    }
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (!within)
                {
                    Debug.LogWarning("Player not within spawn deadzone after timeout — re-locking to spawn.");
                    player.position = spawn.position;
                    player.rotation = spawn.rotation;
                    var rb = player.GetComponent<Rigidbody>();
                    if (rb != null) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                }
            }
        }

        // Fade back in
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeFromBlack();
            yield return new WaitForSeconds(ScreenFader.Instance.fadeDuration + 0.05f);
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
