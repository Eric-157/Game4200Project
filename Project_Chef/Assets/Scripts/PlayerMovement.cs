using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public Rigidbody rb;
    private Vector3 moveDirection;

    [Header("Input")]
    public InputActionReference move;
    public InputActionReference attack;

    [Header("Player Systems")]
    public PlayerStats stats;
    public PlayerCombat combat;

    // Teleport freeze — blocks input for a short duration after spawn
    private float frozenUntil = 0f;

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (combat == null) combat = GetComponent<PlayerCombat>();
    }

    private void Update()
    {
        // If frozen, don't read input
        if (Time.time < frozenUntil)
        {
            moveDirection = Vector3.zero;
            return;
        }

        Vector2 input = move.action.ReadValue<Vector2>();
        moveDirection = new Vector3(input.x, 0f, input.y).normalized;
    }

    private void FixedUpdate()
    {
        rb.velocity = moveDirection * stats.moveSpeed;
    }

    private void OnEnable() => attack.action.started += Attack;
    private void OnDisable() => attack.action.started -= Attack;

    private void Attack(InputAction.CallbackContext ctx)
    {
        combat?.ExecuteAttack();
    }

    /// <summary>
    /// Freeze player movement input for the specified duration (in seconds).
    /// Call this after teleporting the player to prevent immediate drift from residual input.
    /// </summary>
    public void FreezeMovement(float seconds)
    {
        frozenUntil = Time.time + seconds;
        moveDirection = Vector3.zero;
        rb.velocity = Vector3.zero;
    }
}
