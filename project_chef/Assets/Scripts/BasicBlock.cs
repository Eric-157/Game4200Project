using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicBlock : ScriptableObject
{
    [Header("Visuals")]
    public Sprite enemySprite;
    public RuntimeAnimatorController enemyAnimator;

    [Header("Stats")]
    public string enemyName;
    public int health = 100;
    public int dmg = 10;
    public int defense = 5;
    public float moveSpeed = 2.0f;
    public float attackSpeed = 1.0f;

    [Header("Other")]
    public AudioClip deathSound;
}
