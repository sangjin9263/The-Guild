using System;
using UnityEngine;

[Serializable]
public struct AutoCombatStats
{
    public float maxHealth;
    public float attackDamage;
    public float attackRange;
    public float attackInterval;
    public float moveSpeed;

    public static AutoCombatStats Default => new()
    {
        maxHealth = 100f,
        attackDamage = 12f,
        attackRange = 1.25f,
        attackInterval = 1f,
        moveSpeed = 2f
    };
}
