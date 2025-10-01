using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;


//script taken from this tutorial on YouTube: https://www.youtube.com/watch?v=ONlMEZs9Rgw
public class PlayerMovement : MonoBehaviour
{
    public Rigidbody rb;

    public float moveSpeed;

    private Vector3 _moveDirection;

    public InputActionReference move;
    public InputActionReference attack;


    private void Update()
    {
        _moveDirection = move.action.ReadValue<Vector3>();
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector3(_moveDirection.x * moveSpeed, 0,  _moveDirection.z * moveSpeed);
    }

    private void OnEnable()
    {
        attack.action.started += Attack;
    }

    private void OnDisable()
    {
        attack.action.started -= Attack;
    }

    private void Attack(InputAction.CallbackContext obj)
    {
        Debug.Log("You executed an attack!");
    }

}
