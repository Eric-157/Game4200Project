using UnityEngine;

public class ProjectileBehavior : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damage = 10f;            // Default damage value (override per prefab)
    public float lifetime = 3f;           // Destroy after X seconds
    public LayerMask enemyLayer;          // Assign in Inspector for clarity

    private void Start()
    {
        // Auto-destroy so projectiles don't linger forever
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only interact with enemies
        if (((1 << other.gameObject.layer) & enemyLayer) == 0) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject); // destroy projectile after hit
        }
    }
}
