using System.Collections;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Melee hitbox behaviour. Attach to a hitbox prefab to give it a lifetime
/// and damage enemies on trigger collisions. Designed to behave similarly
/// to ProjectileBehavior but for melee hitboxes.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Attack_Melee : MonoBehaviour
{
    [Header("Damage / Lifetime")]
    public float damage = 1f;
    public float lifetime = 0.5f;
    public LayerMask enemyLayer;
    [Tooltip("If true, the hitbox will be destroyed after hitting an enemy once.")]
    public bool destroyOnHit = true;

    private void Awake()
    {
        // Ensure collider exists and is setup as a trigger for OnTriggerEnter to fire
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }

        // Ensure a kinematic Rigidbody exists so trigger callbacks are reliable
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        else
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Track which enemies have already been hit by this hitbox instance
        hitEnemies = new HashSet<int>();
    }

    private void Start()
    {
        // Auto-destroy after lifetime
        if (lifetime > 0f)
            Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only interact with enemies
        if (((1 << other.gameObject.layer) & enemyLayer) == 0) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            // Ensure each unique hitbox only damages a given enemy once
            int eid = enemy.GetInstanceID();
            if (hitEnemies.Contains(eid)) return;
            hitEnemies.Add(eid);

            enemy.TakeDamage(damage);
            if (destroyOnHit)
                Destroy(gameObject);
        }
    }

    // remembers enemies already hit by this hitbox instance
    private HashSet<int> hitEnemies;

    // Helper to set damage at runtime (spawners can call this)
    public void SetDamage(float d) { damage = d; }
    public void SetLifetime(float t)
    {
        lifetime = t;
        if (t > 0f)
        {
            // reset destroy timer
            Destroy(gameObject, t);
        }
    }
    public void SetEnemyLayer(LayerMask mask) { enemyLayer = mask; }
}
