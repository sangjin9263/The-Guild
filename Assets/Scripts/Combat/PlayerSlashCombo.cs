using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public struct SlashSpawnLocal
{
    [Tooltip("Along attack direction. Positive = toward cursor.")]
    public float forward;

    [Tooltip("Perpendicular to attack. Positive = 90 degrees counter-clockwise from aim.")]
    public float side;

    [Tooltip("World-space height from character pivot. Not rotated with aim.")]
    public float height;
}

public class PlayerSlashCombo : MonoBehaviour
{
    private static readonly Collider2D[] OverlapBuffer = new Collider2D[16];
    private static readonly ContactFilter2D HitboxFilter = new()
    {
        useTriggers = true,
        useLayerMask = false
    };

    [SerializeField] private GameObject slashA;
    [SerializeField] private GameObject slashB;

    [Header("Slash A (1st click)")]
    [SerializeField] private SlashSpawnLocal slashASpawn = new() { height = 0.3f };
    [SerializeField] private Vector3 slashARotation = new(0f, 180f, 330f);
    [SerializeField] private Vector3 slashAScale = Vector3.one;
    [Tooltip("Left-tuned slash A arc direction in degrees. Left=180, Right=0, Up=90, Down=-90")]
    [SerializeField] private float slashAForwardAngle = 180f;

    [Header("Slash A Hitbox")]
    [SerializeField] private SlashHitboxGroup slashAHitbox = new()
    {
        sizeMultiplier = 1.1f,
        parts = new[]
        {
            new SlashHitboxPart
            {
                length = 0.5f,
                width = 0.42f,
                forward = 0f,
                angleOffset = -25f
            },
            new SlashHitboxPart
            {
                length = 0.55f,
                width = 0.48f,
                forward = 0.28f,
                angleOffset = 0f
            },
            new SlashHitboxPart
            {
                length = 0.5f,
                width = 0.42f,
                forward = 0.58f,
                angleOffset = 25f
            }
        }
    };
    [SerializeField] private float slashADamage = 1f;

    [Header("Slash B (2nd click)")]
    [SerializeField] private SlashSpawnLocal slashBSpawn = new() { height = 0.3f };
    [SerializeField] private Vector3 slashBRotation = new(0f, 0f, 150f);
    [SerializeField] private Vector3 slashBScale = Vector3.one;
    [Tooltip("Left-tuned slash B arc direction in degrees. Left=180, Right=0, Up=90, Down=-90")]
    [SerializeField] private float slashBForwardAngle = 180f;

    [Header("Slash B Hitbox")]
    [SerializeField] private SlashHitboxGroup slashBHitbox = new()
    {
        sizeMultiplier = 1.1f,
        parts = new[]
        {
            new SlashHitboxPart
            {
                length = 0.52f,
                width = 0.44f,
                forward = 0.1f,
                angleOffset = -18f
            },
            new SlashHitboxPart
            {
                length = 0.52f,
                width = 0.44f,
                forward = 0.48f,
                angleOffset = 18f
            }
        }
    };
    [SerializeField] private float slashBDamage = 1f;

    [Header("Forward Spawn")]
    [SerializeField] private float forwardDistanceMoving = 1f;
    [SerializeField] private float forwardDistanceIdle = 0.45f;

    [Header("Hitbox Debug")]
    [SerializeField] private bool showHitboxDebug;
    [SerializeField] private KeyCode toggleHitboxDebugKey = KeyCode.H;
    [SerializeField] private float hitboxDebugDuration = 0.35f;
    [SerializeField] private Color hitboxFillColor = new(1f, 0.15f, 1f, 0.35f);
    [SerializeField] private string hitboxSortingLayer = "Layer 1";
    [SerializeField] private int hitboxSortingOrder = 50;

    [Header("Hit Impact")]
    [SerializeField] private GameObject hitImpactPrefab;
    [SerializeField] private float hitImpactLifetime = 1f;
    [SerializeField] private Vector2 hitImpactOffset = Vector2.zero;

    [Header("Common")]
    [Tooltip("World-space pivot offset from character. Not rotated with aim.")]
    [SerializeField] private Vector3 pivotOffset = Vector3.zero;
    [SerializeField] private float comboResetTime = 1f;
    [SerializeField] private float vfxLifetime = 2f;
    [SerializeField] private SPUM_Prefabs spumPrefabs;

    private SpumTopDownController movement;
    private SortingGroup sortingGroup;
    private int comboStep;
    private float lastAttackTime = float.MinValue;

    public bool ShowHitboxDebug
    {
        get => showHitboxDebug;
        set => showHitboxDebug = value;
    }

    private void Awake()
    {
        movement = GetComponent<SpumTopDownController>();
        sortingGroup = GetComponent<SortingGroup>();
        if (spumPrefabs == null)
            spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
    }

    private void Update()
    {
        if (toggleHitboxDebugKey != KeyCode.None && Input.GetKeyDown(toggleHitboxDebugKey))
            showHitboxDebug = !showHitboxDebug;

        if (!Input.GetMouseButtonDown(0))
            return;

        if (Time.time - lastAttackTime > comboResetTime)
            comboStep = 0;

        var isSlashA = comboStep == 0;
        var prefab = isSlashA ? slashA : slashB;
        var spawn = isSlashA ? slashASpawn : slashBSpawn;
        var rotation = isSlashA ? slashARotation : slashBRotation;
        var scale = isSlashA ? slashAScale : slashBScale;
        var forwardAngle = isSlashA ? slashAForwardAngle : slashBForwardAngle;
        var hitbox = isSlashA ? slashAHitbox : slashBHitbox;
        var damage = isSlashA ? slashADamage : slashBDamage;
        var attackIndex = comboStep;
        comboStep = (comboStep + 1) % 2;
        lastAttackTime = Time.time;

        if (prefab == null)
            return;

        PerformAttack(prefab, spawn, rotation, scale, forwardAngle, hitbox, damage, attackIndex);
    }

    private void PerformAttack(
        GameObject prefab,
        SlashSpawnLocal spawn,
        Vector3 eulerAngles,
        Vector3 scale,
        float slashForwardAngle,
        SlashHitboxGroup hitbox,
        float damage,
        int attackIndex)
    {
        var aimDir = movement != null
            ? movement.GetAttackForwardDirection()
            : GetFacingFallback();
        aimDir = SlashAimUtility.NormalizeAim(aimDir, Vector2.left);

        var aimAngle = SlashAimUtility.GetAimAngleDegrees(aimDir);
        var aimDeltaRotation = SlashAimUtility.GetAimDeltaRotation(aimAngle, slashForwardAngle);

        var forwardDistance = movement != null && movement.IsMoving()
            ? forwardDistanceMoving
            : forwardDistanceIdle;

        var anchor = SlashAimUtility.GetAttackAnchor(transform.position, pivotOffset, spawn, aimDir, forwardDistance);
        var finalRotation = aimDeltaRotation * Quaternion.Euler(eulerAngles);

        SpawnSlashVfx(prefab, anchor, finalRotation, scale);
        ApplyHitboxGroup(hitbox, anchor, aimAngle, slashForwardAngle, damage, attackIndex);
    }

    private void SpawnSlashVfx(GameObject prefab, Vector3 anchor, Quaternion rotation, Vector3 scale)
    {
        var instance = Instantiate(prefab, transform);
        instance.transform.localPosition = anchor - transform.position;
        instance.transform.rotation = rotation;
        instance.transform.localScale = scale;
        instance.SetActive(true);

        var sorting = instance.GetComponent<SyncParticleSorting>();
        if (sorting == null)
            sorting = instance.AddComponent<SyncParticleSorting>();
        sorting.Configure(GetComponent<SortingGroup>());

        foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Clear(true);
            ps.Play(true);
        }

        Destroy(instance, vfxLifetime);
    }

    private void ApplyHitboxGroup(
        SlashHitboxGroup hitboxGroup,
        Vector3 anchor,
        float aimAngle,
        float authoredForwardAngle,
        float damage,
        int attackIndex)
    {
        if (hitboxGroup.parts == null || hitboxGroup.parts.Length == 0)
            return;

        var processed = new HashSet<Hurtbox>();
        var multiplier = Mathf.Max(1f, hitboxGroup.sizeMultiplier);
        var aimDir = SlashAimUtility.DirectionFromAngle(aimAngle);

        foreach (var part in hitboxGroup.parts)
        {
            if (part.length <= 0f || part.width <= 0f)
                continue;

            var partAngle = SlashAimUtility.GetAuthoredPartAngle(aimAngle, part.angleOffset);
            var center = SlashAimUtility.GetAuthoredPartCenter(anchor, part, aimAngle, authoredForwardAngle);
            var size = new Vector2(part.length * multiplier, part.width * multiplier);

            if (showHitboxDebug)
            {
                HitboxDebugVisual.Show(
                    center,
                    size,
                    partAngle,
                    hitboxDebugDuration,
                    hitboxFillColor,
                    hitboxSortingLayer,
                    hitboxSortingOrder);
            }

            int hitCount = Physics2D.OverlapBox(center, size, partAngle, HitboxFilter, OverlapBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                var collider = OverlapBuffer[i];
                if (collider == null || IsAttackerCollider(collider))
                    continue;

                var hurtbox = collider.GetComponentInParent<Hurtbox>();
                if (hurtbox == null || processed.Contains(hurtbox))
                    continue;

                processed.Add(hurtbox);

                var hitPoint = GetHitPoint(collider, center) + hitImpactOffset;
                hurtbox.ReceiveHit(new HitInfo(gameObject, aimDir, hitPoint, attackIndex, damage));
                HitImpactSpawner.Spawn(hitImpactPrefab, hitPoint, aimDir, sortingGroup, hitImpactLifetime);
            }
        }
    }

    private static Vector2 GetHitPoint(Collider2D hurtboxCollider, Vector2 hitboxCenter)
    {
        return hurtboxCollider.ClosestPoint(hitboxCenter);
    }

    private bool IsAttackerCollider(Collider2D collider)
    {
        return collider.transform == transform || collider.transform.IsChildOf(transform);
    }

    private Vector2 GetFacingFallback()
    {
        if (spumPrefabs == null)
            return Vector2.left;

        return spumPrefabs.transform.localScale.x < 0f ? Vector2.right : Vector2.left;
    }
}
