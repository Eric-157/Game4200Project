using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [Header("Linked Doors")]
    [Tooltip("All doors in this room that should unlock once all enemies are defeated.")]
    public List<DoorTrigger> linkedDoors = new List<DoorTrigger>();

    private int enemiesRemaining = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (enemiesRemaining == 0)
            UnlockAllDoors();
    }

    public void RegisterEnemy()
    {
        enemiesRemaining++;
    }

    public void UnregisterEnemy()
    {
        enemiesRemaining--;

        if (enemiesRemaining <= 0)
        {
            UnlockAllDoors();
        }
    }

    private void UnlockAllDoors()
    {
        foreach (DoorTrigger door in linkedDoors)
        {
            if (door != null)
                door.UnlockDoor();
        }
    }
}
