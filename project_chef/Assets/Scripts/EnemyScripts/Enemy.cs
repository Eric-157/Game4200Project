using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
    [Header("General Settings")]
    public string EnemyName = "DefaultEnemy";
    public SpriteRenderer SpriteRenderer;

    [Header("Stats")]
    public int HP = 3;
    public float MoveSpeed = 3f;
    public float AttackSpeed = 1f;
    public float Damage = 1f;

    [Header("AI Settings")]
    [Tooltip("If true, enemy chases the player. If false, keeps distance.")]
    public bool IsMelee = true;
    public float PreferredDistance = 5f;

    [Header("Combat")]
    [Tooltip("Drag and drop a combat ScriptableObject asset here.")]
    public EnemyCombatBase Combat;

    [HideInInspector] public Transform Player;
    private Rigidbody rb;
    private float frozenUntil = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (Player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) Player = p.transform;
        }
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterEnemy();
    }

    private void FixedUpdate()
    {
        if (Player == null || HP <= 0) return;

        if (Time.time < frozenUntil)
        {
            // Frozen: zero velocity and skip movement
            rb.velocity = Vector3.zero;
            return;
        }

        Vector3 direction = (Player.position - transform.position).normalized;

        if (IsMelee)
        {
            rb.velocity = direction * MoveSpeed;
        }
        else
        {
            float distance = Vector3.Distance(transform.position, Player.position);
            if (distance < PreferredDistance)
                rb.velocity = -direction * MoveSpeed; // move away
            else if (distance > PreferredDistance + 0.5f)
                rb.velocity = direction * MoveSpeed;  // move closer
            else
                rb.velocity = Vector3.zero;
        }

        // Face player (Y-axis only)
        Vector3 lookPos = new Vector3(Player.position.x, transform.position.y, Player.position.z);
        transform.LookAt(lookPos);
    }

    public void TakeDamage(float amount)
    {
        HP -= Mathf.RoundToInt(amount);
        if (HP <= 0) Die();
    }

    private void Die()
    {
        rb.velocity = Vector3.zero;
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterEnemy();
        Destroy(gameObject);
    }

    public void Attack()
    {
        Combat?.ExecuteAttack(this);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterEnemy();
    }

    /// <summary>
    /// Freeze this enemy's movement for a duration (seconds).
    /// Used during room transitions to prevent movement until fade-in is complete.
    /// </summary>
    public void FreezeMovement(float seconds)
    {
        frozenUntil = Time.time + seconds;
        rb.velocity = Vector3.zero;
    }
}
