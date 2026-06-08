using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class DashTrailVisual : MonoBehaviour
{
    public const string DefaultBurstPrefabPath =
        "Assets/Prefabs/VFX/Wind_Ground_Alpha_Left_0.5_Burst.prefab";

    [SerializeField] private GameObject burstPrefab;
    [SerializeField] private Transform burstRoot;
    [SerializeField] private float spawnInterval = 0.12f;
    [SerializeField] private int sortingOrderOffset = -1;

    [Header("Direction Anchors")]
    [Tooltip("Horizontal dash right, or vertical dash while the sprite faces right.")]
    [SerializeField] private Transform rightAnchor;
    [Tooltip("Horizontal dash left, or vertical dash while the sprite faces left.")]
    [SerializeField] private Transform leftAnchor;

    private SortingGroup sortingReference;
    private bool isActive;
    private float spawnTimer;
    private bool lastFacesRight;

    public bool IsActive => isActive;
    public Transform BurstRoot => burstRoot;

    private void Awake()
    {
        EnsureBurstRoot();
        SetBurstVisible(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            EditorEnsureDirectionAnchors();
    }

    private void OnDrawGizmosSelected()
    {
        DrawAnchorGizmo(rightAnchor, Color.red);
        DrawAnchorGizmo(leftAnchor, Color.blue);
    }

    private static void DrawAnchorGizmo(Transform anchor, Color color)
    {
        if (anchor == null)
            return;

        var worldPos = anchor.position;
        Gizmos.color = color;
        Gizmos.DrawWireSphere(worldPos, 0.08f);

        var forward = anchor.TransformDirection(Vector3.right);
        Gizmos.DrawLine(worldPos, worldPos + forward * 0.35f);
    }
#endif

    public void SetBurstPrefab(GameObject prefab)
    {
        if (prefab != null)
            burstPrefab = prefab;
    }

    public void Configure(SortingGroup sortingGroup)
    {
        sortingReference = sortingGroup;
        ApplySorting();
    }

    public void Begin(Vector2 direction, bool facesRight)
    {
        EnsureBurstRoot();
        if (burstRoot == null)
            return;

        lastFacesRight = facesRight;
        ApplyAnchorForDirection(direction, facesRight);

        isActive = true;
        spawnTimer = spawnInterval;
        SetBurstVisible(true);
        ReplayParticles();
    }

    public void Tick(float deltaTime, Vector2 direction, bool facesRight)
    {
        if (!isActive || burstRoot == null)
            return;

        if (facesRight != lastFacesRight)
        {
            lastFacesRight = facesRight;
            ApplyAnchorForDirection(direction, facesRight);
        }

        spawnTimer += deltaTime;
        if (spawnTimer < spawnInterval)
            return;

        spawnTimer = 0f;
        ReplayParticles();
    }

    public void End()
    {
        isActive = false;
        spawnTimer = 0f;
        SetBurstVisible(false);
    }

#if UNITY_EDITOR
    public void EditorEnsureBurstFromPrefab()
    {
        EnsureBurstPrefabReference();
        EditorEnsureDirectionAnchors();

        if (burstPrefab == null || burstRoot != null)
            return;

        var instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(
            burstPrefab, transform);
        if (instance == null)
            return;

        instance.name = burstPrefab.name;
        burstRoot = instance.transform;

        if (rightAnchor != null)
            CopyAnchorTransform(rightAnchor, burstRoot);

        UnityEditor.Undo.RegisterCreatedObjectUndo(instance, "Create Dash Trail Burst");
    }

    public void EditorEnsureDirectionAnchors()
    {
        rightAnchor = EnsureAnchorTransform(rightAnchor, "Anchor_Right", new Vector3(-0.5f, 0.4f, 0f));
        leftAnchor = EnsureAnchorTransform(leftAnchor, "Anchor_Left", new Vector3(0.5f, 0.4f, 0f));
    }

    private Transform EnsureAnchorTransform(Transform existing, string name, Vector3 defaultLocalPosition)
    {
        if (existing != null)
            return existing;

        Transform found = transform.Find(name);
        if (found != null)
            return found;

        var anchorObject = new GameObject(name);
        anchorObject.transform.SetParent(transform, false);
        anchorObject.transform.localPosition = defaultLocalPosition;
        anchorObject.transform.localRotation = Quaternion.identity;
        anchorObject.transform.localScale = Vector3.one;
        UnityEditor.Undo.RegisterCreatedObjectUndo(anchorObject, "Create Dash Trail Anchor");
        return anchorObject.transform;
    }
#endif

    private void ApplyAnchorForDirection(Vector2 direction, bool facesRight)
    {
        var anchor = ResolveAnchor(direction, facesRight);
        if (anchor == null || burstRoot == null)
            return;

        CopyAnchorTransform(anchor, burstRoot);
    }

    private static void CopyAnchorTransform(Transform source, Transform target)
    {
        target.localPosition = source.localPosition;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
    }

    private Transform ResolveAnchor(Vector2 direction, bool facesRight)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return facesRight ? rightAnchor : leftAnchor;

        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            return direction.x >= 0f ? rightAnchor : leftAnchor;

        // Vertical dash: use the anchor on the same side as sprite facing.
        return facesRight ? rightAnchor : leftAnchor;
    }

    private void ReplayParticles()
    {
        if (burstRoot == null)
            return;

        foreach (var ps in burstRoot.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Clear(true);
            ps.Play(true);
        }
    }

    private void SetBurstVisible(bool visible)
    {
        if (burstRoot != null)
            burstRoot.gameObject.SetActive(visible);
    }

    private void EnsureBurstRoot()
    {
        if (burstRoot != null)
        {
            ApplySorting();
            return;
        }

        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (IsDirectionAnchor(child))
                continue;

            burstRoot = child;
            ApplySorting();
            return;
        }

        EnsureBurstPrefabReference();
        if (burstPrefab == null)
            return;

        var instance = Instantiate(burstPrefab, transform);
        instance.name = burstPrefab.name;
        burstRoot = instance.transform;

        if (rightAnchor != null)
            CopyAnchorTransform(rightAnchor, burstRoot);

        instance.SetActive(false);
        ApplySorting();
    }

    private bool IsDirectionAnchor(Transform child)
    {
        return child == rightAnchor
               || child == leftAnchor
               || child.name is "Anchor_Right" or "Anchor_Left" or "Anchor_Up" or "Anchor_Down";
    }

    private void EnsureBurstPrefabReference()
    {
        if (burstPrefab != null)
            return;

#if UNITY_EDITOR
        burstPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DefaultBurstPrefabPath);
#endif
    }

    private void ApplySorting()
    {
        if (burstRoot == null)
            return;

        string layerName = "Layer 1";
        var order = sortingOrderOffset;

        if (sortingReference != null)
        {
            layerName = sortingReference.sortingLayerName;
            order = sortingReference.sortingOrder + sortingOrderOffset;
        }

        foreach (var renderer in burstRoot.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            renderer.sortingLayerName = layerName;
            renderer.sortingOrder = order;
        }
    }
}
