using UnityEngine;

public abstract class EnemyCombatBase : ScriptableObject
{
    [Tooltip("Time between attacks in seconds.")]
    public float cooldown = 1f;

    public abstract void ExecuteAttack(Enemy enemy);
}
