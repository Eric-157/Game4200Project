using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Script in reference to this video: https://www.youtube.com/watch?v=EwiUomzehKU

public class ProjectileBehavior : MonoBehaviour
{
    public float life = 3;
    void Awake()
    {
        Destroy(gameObject, life);
    }

    // Update is called once per frame
    void OnCollisionEnter(Collision collision) // Need help making exceptions
    {
        Destroy(collision.gameObject); // Destroys whatever the projectile hit (can include the player, currently)
        Destroy(gameObject); // Destroys the projectile
    }
}
