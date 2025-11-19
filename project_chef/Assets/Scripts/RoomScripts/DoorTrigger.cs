using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    [Header("Door Settings")]
    public bool isLocked = true;
    public int nextRoomID;

    private bool playerInRange;


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            int desiredRoomID = DetermineNextRoomID();
            GameManager.Instance.GenerateRoomByIndex(desiredRoomID);
        }

        if (!playerInRange || isLocked)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            int desiredRoomID = DetermineNextRoomID();
            GameManager.Instance.GenerateRoomByIndex(desiredRoomID);
        }
    }


    private int DetermineNextRoomID()
    {
        int roomsVisited = GameManager.Instance.roomsVisited;
        nextRoomID = Random.Range(0, GameManager.Instance.roomPrefabs.Count);

        // Insert your custom logic here
        // Example: every 5 rooms → special room
        if (roomsVisited > 0 && roomsVisited % 5 == 0)
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("DoorTrigger: GameManager.Instance is null — cannot pick random room. Using configured nextRoomID.");
                return nextRoomID;
            }

            if (gm.roomPrefabs == null || gm.roomPrefabs.Count == 0)
            {
                Debug.LogWarning("DoorTrigger: GameManager.roomPrefabs is not set or empty — cannot pick random room. Using configured nextRoomID.");
                return nextRoomID;
            }

            // Pick a random valid index from available prefabs
            //int randomIndex = Random.Range(0, gm.roomPrefabs.Count);
            Debug.Log("Random room index selected: " + nextRoomID);
            return nextRoomID;
        }

        return nextRoomID;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }

    public void UnlockDoor()
    {
        isLocked = false;
    }
}
