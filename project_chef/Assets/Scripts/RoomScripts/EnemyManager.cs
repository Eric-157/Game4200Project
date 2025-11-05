using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [Header("Linked Doors")]
    [Tooltip("Assign all doors that should unlock when all enemies are defeated.")]
    public List<DoorTrigger> linkedDoors = new List<DoorTrigger>();

    private int enemiesRemaining;

    private void Awake()
    {
        Instance = this;
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
