using System.Collections;
using UnityEngine;

/// <summary>
/// Simple one-shot hitbox that applies damage to enemies within a radius when spawned.
/// Attach to a prefab used as a visual hitbox, or let PlayerCombat attach it at runtime.
/// </summary>
public class Hitbox : MonoBehaviour
{
    [Tooltip("Damage applied to each hit target")]
    public float damage = 1f;
    [Tooltip("Radius used for physics overlap if no collider present")]
    public float radius = 1f;
    [Tooltip("Layers to consider as targets")]
    public LayerMask targetLayers;
    [Tooltip("How long the hitbox lives before destroying (visual lifespan)")]
    public float duration = 0.25f;

    private void Start()
    {
        // Always perform an immediate overlap to hit enemies already inside the area
        DoOverlapDamage();
        // If the prefab has trigger colliders we still ignore further OnTriggerEnter
        // so Smash is a single instant effect.
        if (duration > 0f)
            Destroy(gameObject, duration);
    }

    private void DoOverlapDamage()
    {
        Vector3 pos = transform.position;
        Collider[] hits = Physics.OverlapSphere(pos, radius, targetLayers);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Intentionally no-op: damage is applied immediately on spawn for one-shot hitboxes.
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
