using UnityEngine;

/// <summary>
/// BuildingPanel을 1920×1080 디자인 좌표로 유지한 채 부모 RectTransform(UiZone) 크기에 딱 맞게 stretch scale 합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[ExecuteAlways]
public sealed class BuildingPanelLayoutFit : MonoBehaviour
{
    public const string DesignRootName = "DesignRoot";

    private static readonly string[] DesignChildNames = { "TitleBar", "Content", "Outline" };

    [SerializeField] private RectTransform designRoot;
    [SerializeField] private Vector2 designSize = new(
        DesktopOverlaySettings.ReferenceWidth,
        DesktopOverlaySettings.ReferenceHeight);

    private RectTransform _panelRect;

    public Vector2 DesignSize => designSize;

    private void Awake()
    {
        _panelRect = (RectTransform)transform;
        EnsureDesignRoot();
        ApplyFit();
    }

    private void OnEnable()
    {
        _panelRect = (RectTransform)transform;
        ApplyFit();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (_panelRect == null)
            _panelRect = (RectTransform)transform;

        ApplyFit();
    }

    public void ApplyFit()
    {
        if (_panelRect == null)
            _panelRect = (RectTransform)transform;

        EnsureDesignRoot();
        if (designRoot == null || _panelRect == null)
            return;

        var width = _panelRect.rect.width;
        var height = _panelRect.rect.height;
        if (width <= 1f || height <= 1f)
            return;

        var scaleX = width / designSize.x;
        var scaleY = height / designSize.y;
        designRoot.localScale = new Vector3(scaleX, scaleY, 1f);
        designRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, designSize.x);
        designRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, designSize.y);
        designRoot.anchoredPosition = Vector2.zero;
    }

    public void EnsureDesignRoot()
    {
        if (designRoot != null)
            return;

        var existing = transform.Find(DesignRootName) as RectTransform;
        if (existing != null)
        {
            designRoot = existing;
            return;
        }

        var hasDirectChildren = false;
        foreach (var childName in DesignChildNames)
        {
            if (transform.Find(childName) != null)
            {
                hasDirectChildren = true;
                break;
            }
        }

        if (!hasDirectChildren)
            return;

        var rootObject = new GameObject(DesignRootName, typeof(RectTransform));
        designRoot = rootObject.GetComponent<RectTransform>();
        designRoot.SetParent(transform, false);
        designRoot.SetAsFirstSibling();
        designRoot.anchorMin = new Vector2(0.5f, 0.5f);
        designRoot.anchorMax = new Vector2(0.5f, 0.5f);
        designRoot.pivot = new Vector2(0.5f, 0.5f);
        designRoot.sizeDelta = designSize;
        designRoot.anchoredPosition = Vector2.zero;
        designRoot.localScale = Vector3.one;

        foreach (var childName in DesignChildNames)
        {
            var child = transform.Find(childName);
            if (child == null)
                continue;

            child.SetParent(designRoot, false);
        }
    }

    public static Transform FindPanelTransform(Transform panelRoot, string relativePath)
    {
        if (panelRoot == null || string.IsNullOrEmpty(relativePath))
            return null;

        var direct = panelRoot.Find(relativePath);
        if (direct != null)
            return direct;

        var designRoot = panelRoot.Find(DesignRootName);
        return designRoot != null ? designRoot.Find(relativePath) : null;
    }
}
