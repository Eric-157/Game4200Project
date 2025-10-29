using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Enemy prefab to spawn.")]
    public GameObject enemyPrefab;

    [Tooltip("Number of enemies to spawn.")]
    [Min(1)] public int spawnCount = 1;

    [Tooltip("Distance between spawned enemies.")]
    public float spacing = 1f;

    [Tooltip("If true, spawns enemies horizontally (left-right). If false, vertically (up-down).")]
    public bool horizontal = true;

    [Tooltip("If true, spawn on Start.")]
    public bool spawnOnStart = true;

    [Header("Optional Timing")]
    [Tooltip("Delay between spawns in seconds. Set 0 for instant spawn.")]
    public float spawnDelay = 0f;

    private void Start()
    {
        if (spawnOnStart)
            StartCoroutine(SpawnEnemies());
    }

    public void TriggerSpawn()
    {
        StartCoroutine(SpawnEnemies());
    }

    private IEnumerator SpawnEnemies()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning($"{name}: No enemy prefab assigned!");
            yield break;
        }

        Vector3 direction = horizontal ? Vector3.right : Vector3.forward;
        Vector3 center = transform.position;

        for (int i = 0; i < spawnCount; i++)
        {
            // Alternate sides relative to the center
            int side = (i % 2 == 0) ? 1 : -1;
            int index = (i + 1) / 2;

            Vector3 offset = (Vector3)(direction * spacing * index * side);
            Vector3 spawnPos = center + offset;

            Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

            if (spawnDelay > 0f)
                yield return new WaitForSeconds(spawnDelay);
        }
    }
}
