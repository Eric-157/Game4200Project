using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
    [Header("General Settings")]
    public string EnemyName = "DefaultEnemy";
    public SpriteRenderer SpriteRenderer;

    private GameManager gameManager;

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
    // store base stats so scaling can be applied idempotently
    private int baseHP;
    private float baseDamage;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        gameManager = GameManager.Instance;
        baseHP = HP;
        baseDamage = Damage;
    }

    /// <summary>
    /// Scale enemy stats based on which room we're in. Every 10 rooms increases multiplier.
    /// roomsVisited: total rooms visited counter from GameManager (1-based count of rooms generated)
    /// </summary>
    public void ScaleForRoom(int roomsVisited)
    {
        if (roomsVisited <= 0)
        {
            Debug.Log($"[Enemy {EnemyName}] ScaleForRoom: skipping (roomsVisited={roomsVisited})");
            return;
        }
        int multiplier = 1 + ((roomsVisited - 1) / 10); // 1.., rooms 1-10 =>1, 11-20=>2, etc.
        int newHP = Mathf.Max(1, Mathf.RoundToInt(baseHP * multiplier));
        float newDamage = Mathf.Max(0.1f, baseDamage * multiplier);
        Debug.Log($"[Enemy {EnemyName}] ScaleForRoom: roomsVisited={roomsVisited}, multiplier={multiplier}, HP: {baseHP}->{newHP}, Damage: {baseDamage}->{newDamage}");
        HP = newHP;
        Damage = newDamage;
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
        // Let the enemy scale itself based on the current roomsVisited value
        if (GameManager.Instance != null)
        {
            int rooms = GameManager.Instance.roomsVisited;
            ScaleForRoom(rooms);
        }
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
        // Calculate ingredient drop scaled by difficulty multiplier (every 10 rooms increases multiplier)
        int baseDrop = Random.Range(0, 3); // keep existing base randomness (0..2)
        int multiplier = 1;
        if (GameManager.Instance != null)
        {
            int rooms = GameManager.Instance.roomsVisited;
            if (rooms > 0) multiplier = 1 + ((rooms - 1) / 10);
        }
        int totalDrop = baseDrop * multiplier;
        if (gameManager != null)
            gameManager.ingredients += totalDrop;
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterEnemy();
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            var ps = collision.collider.GetComponent<PlayerStats>();
            if (ps != null)
            {
                // Apply damage; PlayerStats handles invulnerability
                ps.TakeDamage(Damage);
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // While colliding with the player, attempt to apply damage each physics step.
        // PlayerStats will ignore damage if the player is currently invulnerable,
        // so this allows damage to apply again once the invulnerability window ends
        // without requiring the player to exit and re-enter collision.
        if (collision.collider.CompareTag("Player"))
        {
            var ps = collision.collider.GetComponent<PlayerStats>();
            if (ps != null)
            {
                ps.TakeDamage(Damage);
            }
        }
    }

    public void Attack()
    {
        Combat?.ExecuteAttack(this);
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
