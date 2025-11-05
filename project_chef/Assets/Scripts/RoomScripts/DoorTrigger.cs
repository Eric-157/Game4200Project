using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorTrigger : MonoBehaviour
{
    public bool isLocked = true;
    public int nextRoomID = 2; // You’ll define this via your custom logic later
    private bool playerInRange;

    private void Update()
    {
        if (playerInRange && !isLocked && Input.GetKeyDown(KeyCode.E))
        {
            int newRoomID = nextRoomID;

            // Optional: Custom logic example
            int roomsVisited = RoomManager.Instance.GetRoomsVisited();
            if (roomsVisited % 5 == 0)
                newRoomID = 99; // special room example

            RoomManager.Instance.GenerateNextRoom(newRoomID);
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
        // Optional: door open animation, light color change, etc.
    }
}
