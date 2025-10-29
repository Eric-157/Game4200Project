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

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (combat == null) combat = GetComponent<PlayerCombat>();
    }

    private void Update()
    {
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
}
