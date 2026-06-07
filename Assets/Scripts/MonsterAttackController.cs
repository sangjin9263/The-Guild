using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MonsterAttackController : MonoBehaviour
{
    private enum State
    {
        Chase,
        Telegraph,
        Dash,
        Recover
    }

    private static readonly Collider2D[] OverlapBuffer = new Collider2D[8];

    [Header("Target")]
    [SerializeField] private string playerTag = "Player";

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 1.6f;
    [SerializeField] private float attackTriggerRange = 3.5f;
    [SerializeField] private float loseInterestRange = 9f;

    [Header("Telegraph")]
    [SerializeField] private float telegraphDuration = 0.85f;
    [SerializeField] private float minAttackLength = 2f;
    [SerializeField] private float maxAttackLength = 5.5f;
    [SerializeField] private float lengthPadding = 0.35f;
    [SerializeField] private float attackWidth = 0.55f;
    [SerializeField] private float innerLineWidthRatio = 0.72f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 9f;
    [SerializeField] private float dashDamage = 1f;
    [SerializeField] private float dashHitboxForward = 0.35f;
    [SerializeField] private float dashHitboxLength = 0.7f;

    [Header("Recover")]
    [SerializeField] private float recoverDuration = 0.55f;

    [Header("References")]
    [SerializeField] private AttackTelegraphVisual telegraphVisual;
    [SerializeField] private SPUM_Prefabs spumPrefabs;

    private Rigidbody2D rb;
    private Transform playerTransform;
    private State state = State.Chase;
    private Vector2 attackDirection = Vector2.left;
    private float attackLength;
    private float stateTimer;
    private float dashTraveled;
    private float facingScaleX = 1f;
    private readonly HashSet<Hurtbox> dashHits = new();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (spumPrefabs == null)
            spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();

        if (telegraphVisual == null)
            telegraphVisual = GetComponentInChildren<AttackTelegraphVisual>(true);

        if (telegraphVisual == null)
        {
            var telegraphObject = new GameObject("AttackTelegraph");
            telegraphObject.transform.SetParent(transform, false);
            telegraphVisual = telegraphObject.AddComponent<AttackTelegraphVisual>();
        }
    }

    private void Start()
    {
        if (spumPrefabs != null)
        {
            if (!spumPrefabs.allListsHaveItemsExist())
                spumPrefabs.PopulateAnimationLists();

            spumPrefabs.OverrideControllerInit();
            facingScaleX = Mathf.Max(0.01f, Mathf.Abs(spumPrefabs.transform.localScale.x));
            PlayAnimation(PlayerState.IDLE);
        }

        RefreshPlayerTarget();
    }

    private void FixedUpdate()
    {
        RefreshPlayerTarget();

        switch (state)
        {
            case State.Chase:
                TickChase();
                break;
            case State.Telegraph:
                TickTelegraph();
                break;
            case State.Dash:
                TickDash();
                break;
            case State.Recover:
                TickRecover();
                break;
        }
    }

    private void RefreshPlayerTarget()
    {
        if (playerTransform != null)
            return;

        var playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
            playerTransform = playerObject.transform;
    }

    private void TickChase()
    {
        if (playerTransform == null)
        {
            StopMovement();
            PlayAnimation(PlayerState.IDLE);
            return;
        }

        var toPlayer = (Vector2)playerTransform.position - (Vector2)transform.position;
        var distance = toPlayer.magnitude;

        if (distance > loseInterestRange)
        {
            StopMovement();
            PlayAnimation(PlayerState.IDLE);
            return;
        }

        if (distance <= attackTriggerRange)
        {
            BeginTelegraph(toPlayer, distance);
            return;
        }

        var moveDir = toPlayer / distance;
        rb.MovePosition(rb.position + moveDir * (chaseSpeed * Time.fixedDeltaTime));
        UpdateFacing(moveDir);
        PlayAnimation(PlayerState.MOVE);
    }

    private void BeginTelegraph(Vector2 toPlayer, float distanceToPlayer)
    {
        state = State.Telegraph;
        stateTimer = 0f;
        StopMovement();

        attackDirection = toPlayer.normalized;
        attackLength = Mathf.Clamp(distanceToPlayer + lengthPadding, minAttackLength, maxAttackLength);

        telegraphVisual.Configure(attackLength, attackWidth, innerLineWidthRatio);
        telegraphVisual.ShowAt(transform.position, attackDirection, 0f);
        UpdateFacing(attackDirection);
        PlayAnimation(PlayerState.IDLE);
    }

    private void TickTelegraph()
    {
        StopMovement();
        stateTimer += Time.fixedDeltaTime;

        var fill = telegraphDuration <= 0f ? 1f : stateTimer / telegraphDuration;
        telegraphVisual.SetFill(fill);

        if (stateTimer >= telegraphDuration)
            BeginDash();
    }

    private void BeginDash()
    {
        state = State.Dash;
        stateTimer = 0f;
        dashTraveled = 0f;
        dashHits.Clear();
        telegraphVisual.Hide();
        PlayAnimation(PlayerState.MOVE);
    }

    private void TickDash()
    {
        var step = dashSpeed * Time.fixedDeltaTime;
        var remaining = attackLength - dashTraveled;
        if (remaining <= 0f)
        {
            BeginRecover();
            return;
        }

        step = Mathf.Min(step, remaining);
        var delta = attackDirection * step;
        rb.MovePosition(rb.position + delta);
        dashTraveled += step;

        ApplyDashHitbox();
        UpdateFacing(attackDirection);

        if (dashTraveled >= attackLength)
            BeginRecover();
    }

    private void ApplyDashHitbox()
    {
        var angle = SlashAimUtility.GetAimAngleDegrees(attackDirection);
        var center = (Vector2)transform.position
                     + attackDirection * dashHitboxForward;
        var size = new Vector2(dashHitboxLength, attackWidth * 0.85f);
        var hitCount = Physics2D.OverlapBoxNonAlloc(center, size, angle, OverlapBuffer);

        for (var i = 0; i < hitCount; i++)
        {
            var collider = OverlapBuffer[i];
            if (collider == null || IsOwnerCollider(collider))
                continue;

            var hurtbox = collider.GetComponentInParent<Hurtbox>();
            if (hurtbox == null)
                continue;

            if (!dashHits.Add(hurtbox))
                continue;

            var hitPoint = collider.ClosestPoint(center);
            hurtbox.ReceiveHit(new HitInfo(
                gameObject,
                attackDirection,
                hitPoint,
                0,
                dashDamage));
        }
    }

    private void BeginRecover()
    {
        state = State.Recover;
        stateTimer = 0f;
        StopMovement();
        telegraphVisual.Hide();
        PlayAnimation(PlayerState.IDLE);
    }

    private void TickRecover()
    {
        StopMovement();
        stateTimer += Time.fixedDeltaTime;

        if (stateTimer >= recoverDuration)
            state = State.Chase;
    }

    private void StopMovement()
    {
        rb.linearVelocity = Vector2.zero;
    }

    private void UpdateFacing(Vector2 direction)
    {
        if (spumPrefabs == null || Mathf.Abs(direction.x) < 0.05f)
            return;

        var scale = spumPrefabs.transform.localScale;
        scale.x = direction.x < 0f ? facingScaleX : -facingScaleX;
        spumPrefabs.transform.localScale = scale;
    }

    private void PlayAnimation(PlayerState playerState)
    {
        if (spumPrefabs == null)
            return;

        spumPrefabs.PlayAnimation(playerState, 0);
    }

    private bool IsOwnerCollider(Collider2D collider)
    {
        return collider.transform == transform || collider.transform.IsChildOf(transform);
    }
}
