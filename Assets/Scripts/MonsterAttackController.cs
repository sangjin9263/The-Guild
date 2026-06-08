using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
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
    private static readonly RaycastHit2D[] BlockerCastBuffer = new RaycastHit2D[16];

    [Header("Target")]
    [SerializeField] private string playerTag = "Player";

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 1.6f;
    [SerializeField] private float attackTriggerRange = 3.5f;
    [SerializeField] private float loseInterestRange = 9f;
    [Tooltip("How far ahead to probe for walls while chasing.")]
    [SerializeField] private float chaseProbeDistance = 1.25f;
    [Tooltip("Direct chase is used when this much forward space is clear.")]
    [SerializeField] private float chaseMinForwardClear = 0.35f;
    [Tooltip("How strongly to slide sideways when forward path is blocked (0 = straight, 1 = pure sideways).")]
    [SerializeField] private float chaseSteerBlend = 0.82f;
    [Tooltip("Seconds with low speed against a wall before sideways steering kicks in.")]
    [SerializeField] private float chaseStuckTime = 0.35f;
    [Tooltip("Chase speed fraction below which the monster counts as stuck against a blocker.")]
    [SerializeField] private float chaseStuckSpeedRatio = 0.45f;

    [Header("Telegraph")]
    [SerializeField] private float telegraphDuration = 0.85f;
    [Tooltip("Dash always travels this distance. Telegraph line uses the same length.")]
    [SerializeField] private float maxAttackLength = 5.5f;
    [SerializeField] private float attackWidth = 0.55f;
    [SerializeField] private float innerLineWidthRatio = 0.72f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 9f;
    [SerializeField] private float dashDamage = 1f;
    [SerializeField] private float dashHitboxForward = 0.35f;
    [SerializeField] private float dashHitboxLength = 0.7f;

    [Header("Recover")]
    [SerializeField] private float recoverDuration = 0.55f;

    [Header("Dash Path")]
    [Tooltip("Static wall colliders block dash. Chase uses physics like the player.")]
    [SerializeField] private float blockerSkin = 0.02f;

    [Header("Dash Trail")]
    [SerializeField] private GameObject dashTrailBurstPrefab;
    [Tooltip("Child burst is positioned in the Scene view under DashTrail. Prefab is only used to create that child once.")]
    [SerializeField] private DashTrailVisual dashTrailVisual;

    [Header("References")]
    [SerializeField] private AttackTelegraphVisual telegraphVisual;
    [SerializeField] private SPUM_Prefabs spumPrefabs;

    private Rigidbody2D rb;
    private BoxCollider2D hurtboxCollider;
    private BoxCollider2D physicsCollider;
    private SortingGroup sortingGroup;
    private Transform playerTransform;
    private bool playerCollisionsIgnored;
    private State state = State.Chase;
    private Vector2 attackDirection = Vector2.left;
    private float attackLength;
    private float stateTimer;
    private float dashTraveled;
    private float facingScaleX = 1f;
    private bool telegraphAimLocked;
    private Vector2 telegraphLockedAimPoint;
    private float chaseBlockedTimer;
    private int chaseSteerSide;
    private float lastChaseDistanceToPlayer = float.MaxValue;
    private readonly HashSet<Hurtbox> dashHits = new();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sortingGroup = GetComponent<SortingGroup>();
        ConfigureBodyColliders();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (spumPrefabs == null)
            spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();

        EnsureDashTrailComponents();
        SyncDashTrailSettings();
    }

    private void ConfigureBodyColliders()
    {
        hurtboxCollider = null;
        physicsCollider = null;

        foreach (var collider in GetComponents<BoxCollider2D>())
        {
            if (collider.isTrigger)
                hurtboxCollider = collider;
            else
                physicsCollider = collider;
        }

        if (physicsCollider == null)
        {
            physicsCollider = gameObject.AddComponent<BoxCollider2D>();
            physicsCollider.isTrigger = false;
            if (hurtboxCollider != null)
            {
                physicsCollider.offset = hurtboxCollider.offset;
                physicsCollider.size = hurtboxCollider.size;
            }
            else
            {
                physicsCollider.offset = new Vector2(0f, 0.2f);
                physicsCollider.size = new Vector2(0.55f, 0.55f);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureDashTrailComponents();
        SyncDashTrailSettings();
    }
#endif

    private void EnsureDashTrailComponents()
    {
        if (telegraphVisual == null)
            telegraphVisual = GetComponentInChildren<AttackTelegraphVisual>(true);

        if (telegraphVisual == null && Application.isPlaying)
        {
            var telegraphObject = new GameObject("AttackTelegraph");
            telegraphObject.transform.SetParent(transform, false);
            telegraphVisual = telegraphObject.AddComponent<AttackTelegraphVisual>();
        }

        if (dashTrailVisual == null)
            dashTrailVisual = GetComponentInChildren<DashTrailVisual>(true);

        if (dashTrailVisual == null)
        {
            var trailObject = new GameObject("DashTrail");
            trailObject.transform.SetParent(transform, false);
            dashTrailVisual = trailObject.AddComponent<DashTrailVisual>();
        }
        else if (dashTrailVisual.transform.parent != transform)
        {
            dashTrailVisual.transform.SetParent(transform, false);
        }

        if (dashTrailBurstPrefab == null)
            dashTrailBurstPrefab = LoadDefaultDashBurstPrefab();

        if (dashTrailBurstPrefab != null)
            dashTrailVisual.SetBurstPrefab(dashTrailBurstPrefab);

#if UNITY_EDITOR
        if (dashTrailVisual.BurstRoot == null && dashTrailBurstPrefab != null)
            dashTrailVisual.EditorEnsureBurstFromPrefab();
        else
            dashTrailVisual.EditorEnsureDirectionAnchors();
#endif
    }

    private void SyncDashTrailSettings()
    {
        if (dashTrailVisual == null)
            return;

        if (sortingGroup == null)
            sortingGroup = GetComponent<SortingGroup>();

        dashTrailVisual.Configure(sortingGroup);
    }

    private static GameObject LoadDefaultDashBurstPrefab()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/VFX/Wind_Ground_Alpha_Left_0.5_Burst.prefab");
#else
        return null;
#endif
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
        if (playerTransform == null)
        {
            var playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject != null)
                playerTransform = playerObject.transform;
        }

        EnsurePlayerCollisionIgnored();
    }

    private void EnsurePlayerCollisionIgnored()
    {
        if (playerCollisionsIgnored || physicsCollider == null || playerTransform == null)
            return;

        foreach (var playerCollider in playerTransform.GetComponentsInChildren<Collider2D>())
        {
            if (playerCollider.isTrigger)
                continue;

            Physics2D.IgnoreCollision(physicsCollider, playerCollider, true);
        }

        playerCollisionsIgnored = true;
    }

    private void TickChase()
    {
        EnsureDynamicMovement();

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

        if (distance <= attackTriggerRange && HasClearDashPathToPlayer(distance, toPlayer))
        {
            BeginTelegraph(toPlayer, distance);
            return;
        }

        var moveDir = ChooseChaseMoveDirection(toPlayer, distance);
        rb.linearVelocity = moveDir * chaseSpeed;
        UpdateFacing(moveDir);
        PlayAnimation(PlayerState.MOVE);
    }

    private void ResetChaseSteering()
    {
        chaseBlockedTimer = 0f;
        chaseSteerSide = 0;
        lastChaseDistanceToPlayer = float.MaxValue;
    }

    private Vector2 ChooseChaseMoveDirection(Vector2 toPlayer, float distanceToPlayer)
    {
        if (distanceToPlayer <= 0.0001f)
            return Vector2.zero;

        var desired = toPlayer / distanceToPlayer;
        var forwardClear = GetBlockerLimitedDistance(desired, chaseProbeDistance);
        var blockedAhead = forwardClear < chaseMinForwardClear;
        var movingSlowly = rb.linearVelocity.sqrMagnitude
            < (chaseSpeed * chaseStuckSpeedRatio) * (chaseSpeed * chaseStuckSpeedRatio);
        var makingProgress = distanceToPlayer < lastChaseDistanceToPlayer - 0.015f;
        lastChaseDistanceToPlayer = distanceToPlayer;

        if (blockedAhead && movingSlowly && !makingProgress)
            chaseBlockedTimer += Time.fixedDeltaTime;
        else
        {
            chaseBlockedTimer = Mathf.Max(0f, chaseBlockedTimer - Time.fixedDeltaTime * 2f);
            if (!blockedAhead)
                chaseSteerSide = 0;
        }

        if (chaseBlockedTimer < chaseStuckTime)
            return desired;

        var left = new Vector2(-desired.y, desired.x);
        var right = -left;
        var leftClear = GetBlockerLimitedDistance(left, chaseProbeDistance);
        var rightClear = GetBlockerLimitedDistance(right, chaseProbeDistance);
        var leftScore = leftClear + Vector2.Dot(left, desired) * 0.3f;
        var rightScore = rightClear + Vector2.Dot(right, desired) * 0.3f;

        if (chaseSteerSide == 0 || Mathf.Abs(leftScore - rightScore) >= 0.05f)
            chaseSteerSide = leftScore >= rightScore ? -1 : 1;

        var steerDir = chaseSteerSide < 0 ? left : right;
        var blend = Mathf.Clamp01(chaseSteerBlend + 0.18f);
        return Vector2.Lerp(desired, steerDir, blend).normalized;
    }

    private void BeginTelegraph(Vector2 toPlayer, float distanceToPlayer)
    {
        ResetChaseSteering();
        SetKinematicMovement();
        state = State.Telegraph;
        stateTimer = 0f;
        telegraphAimLocked = false;

        attackDirection = toPlayer / distanceToPlayer;
        attackLength = maxAttackLength;

        if (playerTransform != null)
            telegraphLockedAimPoint = playerTransform.position;

        telegraphVisual.Configure(attackLength, attackWidth, innerLineWidthRatio);
        telegraphVisual.ShowAt(transform.position, attackDirection, 0f);
        UpdateFacing(attackDirection);
        PlayAnimation(PlayerState.IDLE);
    }

    private void TickTelegraph()
    {
        StopMovement();
        UpdateTelegraphAim();

        telegraphVisual.Configure(attackLength, attackWidth, innerLineWidthRatio);
        telegraphVisual.ShowAt(transform.position, attackDirection, 0f);
        UpdateFacing(attackDirection);

        stateTimer += Time.fixedDeltaTime;
        var fill = telegraphDuration <= 0f ? 1f : stateTimer / telegraphDuration;
        telegraphVisual.SetFill(fill);

        if (stateTimer >= telegraphDuration)
            BeginDash();
    }

    private void UpdateTelegraphAim()
    {
        attackLength = maxAttackLength;

        if (playerTransform == null)
            return;

        var toPlayer = (Vector2)playerTransform.position - rb.position;
        var distanceToPlayer = toPlayer.magnitude;
        var inRange = distanceToPlayer <= attackTriggerRange;
        var pathClear = HasClearDashPathToPlayer(distanceToPlayer, toPlayer);

        if (!telegraphAimLocked)
        {
            if (inRange && pathClear)
            {
                telegraphLockedAimPoint = playerTransform.position;
                attackDirection = toPlayer / distanceToPlayer;
                return;
            }

            telegraphAimLocked = true;
            telegraphLockedAimPoint = playerTransform.position;
        }
        else if (inRange && pathClear)
        {
            telegraphAimLocked = false;
            telegraphLockedAimPoint = playerTransform.position;
            attackDirection = toPlayer / distanceToPlayer;
            return;
        }

        var toLocked = telegraphLockedAimPoint - rb.position;
        if (toLocked.sqrMagnitude > 0.0001f)
            attackDirection = toLocked.normalized;
    }

    private void BeginDash()
    {
        SetKinematicMovement();
        state = State.Dash;
        stateTimer = 0f;
        dashTraveled = 0f;
        dashHits.Clear();
        telegraphVisual.Hide();
        attackLength = maxAttackLength;

        dashTrailVisual.Begin(attackDirection, IsFacingRight());
        PlayAnimation(PlayerState.MOVE);
    }

    private void TickDash()
    {
        dashTrailVisual.Tick(Time.fixedDeltaTime, attackDirection, IsFacingRight());

        var step = dashSpeed * Time.fixedDeltaTime;
        var remaining = attackLength - dashTraveled;
        if (remaining <= 0f)
        {
            BeginRecover();
            return;
        }

        step = Mathf.Min(step, remaining);
        var allowedStep = GetBlockerLimitedDistance(attackDirection, step);
        if (allowedStep <= 0.0001f)
        {
            BeginRecover();
            return;
        }

        rb.MovePosition(rb.position + attackDirection * allowedStep);
        dashTraveled += allowedStep;

        ApplyDashHitbox();
        UpdateFacing(attackDirection);

        if (dashTraveled >= attackLength)
            BeginRecover();
    }

    private void ApplyDashHitbox()
    {
        var angle = SlashAimUtility.GetAimAngleDegrees(attackDirection);
        var center = (Vector2)transform.position + attackDirection * dashHitboxForward;
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
        SetKinematicMovement();
        state = State.Recover;
        stateTimer = 0f;
        telegraphVisual.Hide();
        dashTrailVisual.End();
        PlayAnimation(PlayerState.IDLE);
    }

    private void TickRecover()
    {
        StopMovement();
        stateTimer += Time.fixedDeltaTime;

        if (stateTimer >= recoverDuration)
        {
            ResetChaseSteering();
            state = State.Chase;
        }
    }

    private void EnsureDynamicMovement()
    {
        if (rb.bodyType == RigidbodyType2D.Dynamic)
            return;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
    }

    private void SetKinematicMovement()
    {
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
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

    private bool IsFacingRight()
    {
        return spumPrefabs != null && spumPrefabs.transform.localScale.x < 0f;
    }

    private bool IsOwnerCollider(Collider2D collider)
    {
        return collider.transform == transform || collider.transform.IsChildOf(transform);
    }

    private bool IsPlayerCollider(Collider2D collider)
    {
        if (collider == null)
            return false;

        if (collider.CompareTag(playerTag))
            return true;

        if (playerTransform != null
            && (collider.transform == playerTransform || collider.transform.IsChildOf(playerTransform)))
            return true;

        return collider.GetComponentInParent<SpumTopDownController>() != null;
    }

    private bool HasClearDashPathToPlayer(float distanceToPlayer, Vector2 toPlayer)
    {
        if (distanceToPlayer <= 0.15f)
            return true;

        var direction = toPlayer / distanceToPlayer;
        var allowedDistance = GetBlockerLimitedDistance(direction, distanceToPlayer);
        return allowedDistance >= distanceToPlayer - 0.12f;
    }

    private float GetBlockerLimitedDistance(Vector2 direction, float maxDistance)
    {
        if (maxDistance <= 0f || physicsCollider == null)
            return maxDistance;

        direction.Normalize();

        var castCenter = (Vector2)transform.TransformPoint(physicsCollider.offset);
        var castSize = physicsCollider.size * 0.9f;
        var angle = transform.eulerAngles.z;
        var closest = maxDistance;

        var hitCount = Physics2D.BoxCastNonAlloc(
            castCenter,
            castSize,
            angle,
            direction,
            BlockerCastBuffer,
            maxDistance);

        for (var i = 0; i < hitCount; i++)
        {
            var hit = BlockerCastBuffer[i];
            if (!IsDashObstacleHit(hit.collider))
                continue;

            closest = Mathf.Min(closest, hit.distance);
        }

        if (closest >= maxDistance)
            return maxDistance;

        return Mathf.Max(0f, closest - blockerSkin);
    }

    private bool IsDashObstacleHit(Collider2D collider)
    {
        if (collider == null || collider.isTrigger)
            return false;

        if (IsOwnerCollider(collider) || IsPlayerCollider(collider))
            return false;

        var attached = collider.attachedRigidbody;
        return attached == null || attached.bodyType == RigidbodyType2D.Static;
    }
}
