using UnityEngine;
using UnityEngine.EventSystems;

public class SpumTopDownController : MonoBehaviour
{
    public float speed = 3f;
    public SPUM_Prefabs spumPrefabs;

    [Header("Mouse Aim")]
    [SerializeField] private Camera aimCamera;
    [SerializeField] private float aimMinDistance = 0.15f;

    private Rigidbody2D rb;
    private float facingScaleX = 1f;
    private Vector2 moveDirection;
    private Vector2 aimDirection = Vector2.left;

    public Vector2 LastMoveDirection { get; private set; } = Vector2.down;
    public Vector2 AimDirection => aimDirection;

    public bool IsMoving(float minSpeed = 0.05f)
    {
        if (rb != null && rb.linearVelocity.sqrMagnitude > minSpeed * minSpeed)
            return true;

        return moveDirection.sqrMagnitude > 0.01f;
    }

    public Vector2 GetAttackForwardDirection()
    {
        UpdateAimDirection();
        return aimDirection;
    }

    public float GetAimAngleDegrees()
    {
        return Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
    }

    private Vector2 GetFacingDirection()
    {
        if (spumPrefabs == null)
            return Vector2.left;

        return spumPrefabs.transform.localScale.x < 0f ? Vector2.right : Vector2.left;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (aimCamera == null)
            aimCamera = Camera.main;

        if (spumPrefabs == null)
            spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();

        if (spumPrefabs == null)
            return;

        facingScaleX = Mathf.Abs(spumPrefabs.transform.localScale.x);
        if (facingScaleX < 0.01f)
            facingScaleX = 1f;

        aimDirection = GetFacingDirection();

        if (!spumPrefabs.allListsHaveItemsExist())
            spumPrefabs.PopulateAnimationLists();
        spumPrefabs.OverrideControllerInit();
        spumPrefabs.PlayAnimation(PlayerState.IDLE, 0);
    }

    private void Update()
    {
        if (spumPrefabs == null || rb == null)
            return;

        UpdateAimDirection();

        Vector2 dir = Vector2.zero;
        if (Input.GetKey(KeyCode.A))
            dir.x = -1;
        else if (Input.GetKey(KeyCode.D))
            dir.x = 1;

        if (Input.GetKey(KeyCode.W))
            dir.y = 1;
        else if (Input.GetKey(KeyCode.S))
            dir.y = -1;

        dir.Normalize();
        moveDirection = dir;
        if (dir.sqrMagnitude > 0.01f)
            LastMoveDirection = dir;

        rb.linearVelocity = speed * dir;

        UpdateFacingFromAim();

        spumPrefabs.PlayAnimation(dir.sqrMagnitude > 0f ? PlayerState.MOVE : PlayerState.IDLE, 0);
    }

    private void UpdateAimDirection()
    {
        if (!Application.isPlaying || !Application.isFocused)
            return;

        if (IsPointerOverUi())
            return;

        if (aimCamera == null)
            aimCamera = Camera.main;

        if (aimCamera == null || !aimCamera.isActiveAndEnabled)
            return;

        var mouseScreen = Input.mousePosition;
        if (!IsFinite(mouseScreen))
            return;

        if (!IsMouseInsideCameraRect(mouseScreen))
            return;

        if (!TryGetMouseWorldOnPlayerPlane(mouseScreen, out var mouseWorld))
            return;

        var toMouse = (Vector2)(mouseWorld - transform.position);
        if (toMouse.sqrMagnitude < aimMinDistance * aimMinDistance)
            return;

        aimDirection = toMouse.normalized;
    }

    private bool IsMouseInsideCameraRect(Vector3 mouseScreen)
    {
        var pixelRect = aimCamera.pixelRect;
        return mouseScreen.x >= pixelRect.xMin
               && mouseScreen.x <= pixelRect.xMax
               && mouseScreen.y >= pixelRect.yMin
               && mouseScreen.y <= pixelRect.yMax;
    }

    private bool TryGetMouseWorldOnPlayerPlane(Vector3 mouseScreen, out Vector3 worldPoint)
    {
        worldPoint = default;

        var depth = aimCamera.WorldToScreenPoint(transform.position).z;
        if (!float.IsFinite(depth) || depth <= 0.0001f)
            return false;

        mouseScreen.z = depth;
        worldPoint = aimCamera.ScreenToWorldPoint(mouseScreen);

        return IsFinite(worldPoint);
    }

    private static bool IsPointerOverUi()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        return eventSystem.IsPointerOverGameObject();
    }

    private static bool IsFinite(Vector3 value)
    {
        return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    private void UpdateFacingFromAim()
    {
        if (Mathf.Abs(aimDirection.x) < 0.01f)
            return;

        if (aimDirection.x > 0f)
            SetFacing(-facingScaleX);
        else
            SetFacing(facingScaleX);
    }

    private void SetFacing(float scaleX)
    {
        var scale = spumPrefabs.transform.localScale;
        spumPrefabs.transform.localScale = new Vector3(scaleX, scale.y, scale.z);
    }
}
