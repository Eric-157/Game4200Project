using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerCombat : MonoBehaviour
{
    public enum AttackType { Slice, Slap, Smash }

    [Header("Combat Settings")]
    public AttackType CurrentAttack = AttackType.Slice;
    public float AttackRange = 1.5f;      // Slice/Slap
    public float SmashRadius = 3f;        // Smash
    public Transform AttackPoint;         // child object in front of player
    public LayerMask EnemyLayer;
    [Header("Hitbox Prefab")]
    [Tooltip("Optional prefab used to visually represent a smash hitbox. If assigned, it will be instantiated at the AttackPoint.")]
    public GameObject smashHitboxPrefab;
    [Tooltip("Scale multiplier applied to the hitbox prefab (visual). Radius used for damage is taken from SmashRadius unless the prefab's Hitbox overrides it.")]
    public float smashHitboxScale = 1f;
    [Tooltip("Duration (seconds) for the visual hitbox prefab to exist before destroying")]
    public float smashHitboxDuration = 0.25f;

    [Header("Player Stats")]
    public PlayerStats stats;             // reference to player stats

    private bool canAttack = true;

    public void ExecuteAttack()
    {
        if (!canAttack || stats == null) return;

        float dmg = stats.CalculateAttackDamage();

        switch (CurrentAttack)
        {
            case AttackType.Slice: SliceAttack(dmg); break;
            case AttackType.Slap: SlapAttack(dmg); break;
            case AttackType.Smash: SmashAttack(dmg); break;
        }

        StartCoroutine(AttackCooldownRoutine());
    }

    private void SliceAttack(float dmg)
    {
        Collider[] hits = Physics.OverlapSphere(AttackPoint.position, AttackRange, EnemyLayer);
        if (hits.Length > 0)
        {
            Enemy enemy = hits[0].GetComponent<Enemy>();
            if (enemy != null) enemy.TakeDamage(dmg);
        }
    }

    private void SlapAttack(float dmg)
    {
        Collider[] hits = Physics.OverlapSphere(AttackPoint.position, AttackRange, EnemyLayer);
        foreach (Collider hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null) enemy.TakeDamage(dmg);
        }
    }

    private void SmashAttack(float dmg)
    {
        // Spawn visual hitbox prefab if provided
        if (smashHitboxPrefab != null)
        {
            var go = Instantiate(smashHitboxPrefab, AttackPoint.position, AttackPoint.rotation);
            // apply scale
            go.transform.localScale = go.transform.localScale * smashHitboxScale;

            // Ensure a Hitbox component exists so damage is applied
            var hb = go.GetComponent<Hitbox>();
            if (hb == null)
            {
                hb = go.AddComponent<Hitbox>();
            }
            hb.damage = dmg;
            hb.radius = SmashRadius;
            hb.targetLayers = EnemyLayer;
            hb.duration = smashHitboxDuration;
        }
        else
        {
            // Fallback: immediate overlap sphere damage (no visual)
            Collider[] hits = Physics.OverlapSphere(AttackPoint.position, SmashRadius, EnemyLayer);
            foreach (Collider hit in hits)
            {
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null) enemy.TakeDamage(dmg);
            }
        }
    }

    private IEnumerator AttackCooldownRoutine()
    {
        canAttack = false;
        float cooldownTime = Mathf.Max(0.1f, 1f - stats.attackSpeed); // adjust by player attack speed
        yield return new WaitForSeconds(cooldownTime);
        canAttack = true;
    }

    private void Update()
    {
        // Left mouse button triggers the current attack.
        // Ignore clicks over UI so UI buttons/menus work without firing attacks.
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            ExecuteAttack();
        }
    }
}
