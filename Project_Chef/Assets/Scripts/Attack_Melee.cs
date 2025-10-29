using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Attack_Melee : MonoBehaviour
{
    public Transform projectileMeleeSpawnPoint;
    public GameObject projectileMeleePrefab;

    public InputActionReference attack;

    private bool isAttacking;

    void Update()
    {
        if (Input.GetMouseButtonDown(1) && isAttacking == false) // Right Mouse Button
        {
            StartCoroutine(ExecuteAttack());
            isAttacking = true;
        }
    }

    private IEnumerator ExecuteAttack()
    {
        var projectile = Instantiate(projectileMeleePrefab, projectileMeleeSpawnPoint.position, projectileMeleeSpawnPoint.rotation);

        yield return new WaitForSeconds(1);
        isAttacking = false;
    }

}
