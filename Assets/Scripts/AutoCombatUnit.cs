using UnityEngine;

public enum AutoCombatTeam
{
    Ally,
    Enemy
}

[DisallowMultipleComponent]
public class AutoCombatUnit : MonoBehaviour
{
    [SerializeField] private AutoCombatTeam team;
    [SerializeField] private AutoCombatStats stats = AutoCombatStats.Default;

    private float health;
    private float attackTimer;
    private float groundY;
    private AutoCombatUnit currentTarget;
    private SpriteRenderer spriteRenderer;

    public AutoCombatTeam Team => team;
    public bool IsAlive => health > 0f;
    public float Health => health;
    public float MaxHealth => stats.maxHealth;

    public void Configure(AutoCombatTeam unitTeam, AutoCombatStats unitStats)
    {
        team = unitTeam;
        stats = unitStats;
        health = stats.maxHealth;
        attackTimer = Random.Range(0f, stats.attackInterval * 0.5f);
        groundY = transform.position.y;
        ApplyTeamColor();
    }

    private void Awake()
    {
        if (health <= 0f)
            health = stats.maxHealth;

        groundY = transform.position.y;
        EnsureVisual();
        ApplyTeamColor();
    }

    private void Update()
    {
        if (!IsAlive)
            return;

        attackTimer -= Time.deltaTime;
        SnapToGround();

        if (currentTarget == null || !currentTarget.IsAlive)
            currentTarget = FindNearestEnemy();

        if (currentTarget == null)
            return;

        var deltaX = currentTarget.transform.position.x - transform.position.x;
        var distanceX = Mathf.Abs(deltaX);

        if (distanceX > stats.attackRange)
        {
            var step = Mathf.Sign(deltaX) * stats.moveSpeed * Time.deltaTime;
            if (Mathf.Abs(step) > distanceX)
                step = deltaX;

            transform.position = new Vector3(transform.position.x + step, groundY, 0f);
            UpdateFacing(Mathf.Sign(step));
            return;
        }

        UpdateFacing(Mathf.Sign(deltaX));

        if (attackTimer > 0f)
            return;

        currentTarget.TakeDamage(stats.attackDamage);
        attackTimer = stats.attackInterval;
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive)
            return;

        health = Mathf.Max(0f, health - amount);

        if (health <= 0f)
            Die();
    }

    private AutoCombatUnit FindNearestEnemy()
    {
        AutoCombatUnit nearest = null;
        var bestDistance = float.MaxValue;

        foreach (var unit in FindObjectsByType<AutoCombatUnit>(FindObjectsSortMode.None))
        {
            if (unit == this || !unit.IsAlive || unit.team == team)
                continue;

            var distance = Mathf.Abs(transform.position.x - unit.transform.position.x);
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            nearest = unit;
        }

        return nearest;
    }

    private void SnapToGround()
    {
        var tilemap = CombatGroundQuery.ResolveCombatGround();
        if (tilemap != null &&
            CombatGroundQuery.TryGetSurfaceYAtWorldX(tilemap, transform.position.x, out var surfaceY))
        {
            groundY = surfaceY;
        }

        transform.position = new Vector3(transform.position.x, groundY, 0f);
    }

    private void UpdateFacing(float directionX)
    {
        if (spriteRenderer == null || directionX == 0f)
            return;

        spriteRenderer.flipX = directionX < 0f;
    }

    private void Die()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0.35f);
    }

    private void EnsureVisual()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            return;

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CombatVisualFactory.GetCharacterSprite();
        spriteRenderer.sortingOrder = 10;
    }

    private void ApplyTeamColor()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.color = team == AutoCombatTeam.Ally
            ? new Color(0.35f, 0.75f, 1f)
            : new Color(1f, 0.35f, 0.35f);
    }
}
