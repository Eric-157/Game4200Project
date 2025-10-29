using UnityEngine;

[CreateAssetMenu(fileName = "MeleeAttack", menuName = "Enemy Combat/Melee Attack")]
public class MeleeAttack : EnemyCombatBase
{
    [Tooltip("Range within which the enemy can hit the player.")]
    public float range = 1.5f;

    private bool canAttack = true;

    public override void ExecuteAttack(Enemy enemy)
    {
        if (!canAttack || enemy.Player == null) return;

        float distance = Vector3.Distance(enemy.transform.position, enemy.Player.position);
        if (distance <= range)
        {
            Debug.Log($"{enemy.EnemyName} attacks player for {enemy.Damage} damage!");
            // TODO: Apply damage to player here
            enemy.StartCoroutine(AttackCooldown());
        }
    }

    private System.Collections.IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(cooldown);
        canAttack = true;
    }
}
