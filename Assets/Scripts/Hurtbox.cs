using UnityEngine;

public readonly struct HitInfo
{
    public GameObject Attacker { get; }
    public Vector2 AimDirection { get; }
    public Vector2 HitPoint { get; }
    public int ComboIndex { get; }
    public float Damage { get; }

    public HitInfo(
        GameObject attacker,
        Vector2 aimDirection,
        Vector2 hitPoint,
        int comboIndex,
        float damage)
    {
        Attacker = attacker;
        AimDirection = aimDirection;
        HitPoint = hitPoint;
        ComboIndex = comboIndex;
        Damage = damage;
    }
}

public class Hurtbox : MonoBehaviour
{
    [SerializeField] private float maxHealth = 3f;

    private float health;

    public float Health => health;
    public float MaxHealth => maxHealth;

    private void Awake()
    {
        health = maxHealth;
    }

    public void ReceiveHit(HitInfo hit)
    {
        health -= hit.Damage;
        Debug.Log($"{name} took {hit.Damage} damage. HP {health}/{maxHealth}", this);

        if (health <= 0f)
            Debug.Log($"{name} defeated.", this);
    }
}
