using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public enum AttackType { Slice, Slap, Smash }

    [Header("Combat Settings")]
    public AttackType CurrentAttack = AttackType.Slice;
    public float AttackRange = 1.5f;      // Slice/Slap
    public float SmashRadius = 3f;        // Smash
    public Transform AttackPoint;         // child object in front of player
    public LayerMask EnemyLayer;

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
        Collider[] hits = Physics.OverlapSphere(AttackPoint.position, SmashRadius, EnemyLayer);
        foreach (Collider hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null) enemy.TakeDamage(dmg);
        }
    }

    private IEnumerator AttackCooldownRoutine()
    {
        canAttack = false;
        float cooldownTime = Mathf.Max(0.1f, 1f - stats.attackSpeed); // adjust by player attack speed
        yield return new WaitForSeconds(cooldownTime);
        canAttack = true;
    }
}
