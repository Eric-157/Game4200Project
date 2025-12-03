using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    [Header("Door Settings")]
    public bool isLocked = true;
    public int nextRoomID = 2;

    private bool playerInRange;


    private void Update()
    {
        // Check if door is locked, or if it should remain locked because enemies are still alive
        var gm = GameManager.Instance;
        bool shouldBeLocked = isLocked && (gm != null && gm.GetEnemiesAlive() > 0);

        if (Input.GetKeyDown(KeyCode.R))
        {
            // For testing: reload current room
            if (gm != null)
            {
                gm.TransitionToRoom(gm.currentRoomID);
            }
        }

        if (!playerInRange || shouldBeLocked)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            int desiredRoomID = DetermineNextRoomID();

            // Use GameManager's fade transition instead of direct room generation
            if (gm != null)
            {
                gm.TransitionToRoom(desiredRoomID);
            }
        }


    }


    private int DetermineNextRoomID()
    {
        var gm = GameManager.Instance;
        // Prefer a random next room if the GameManager has room prefabs
        if (gm != null && gm.roomPrefabs != null && gm.roomPrefabs.Count > 0)
        {
            Debug.Log("Random room selected: " + nextRoomID);
            return Random.Range(0, gm.roomPrefabs.Count);
        }
        else
        {
            Debug.Log("Random room selected: " + nextRoomID);
            return Random.Range(0, gm.roomPrefabs.Count);
        }
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
