using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class WorkspacePanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static event Action<WorkspacePanel> RectChanged;

    [Header("Identity")]
    [SerializeField] private string panelId = WorkspacePanelId.Town;
    [SerializeField] private string label = "Panel";

    [Header("Layout")]
    [SerializeField] private WorkspacePanelRect defaultRect;
    [SerializeField] private bool useSavedLayout = true;
    [SerializeField] private bool draggable = true;
    [SerializeField] private float worldScaleReferenceHeight = 480f;

    [Header("Content")]
    [SerializeField] private Transform worldContentRoot;
    [SerializeField] private UiZonePanelContent uiZoneContent;
    [SerializeField] private AuctionZonePanelContent auctionZoneContent;

    [Header("Frame")]
    [SerializeField] private bool showFrame = true;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Text labelText;
    [SerializeField] private Color frameColor = new(1f, 1f, 1f, 0.25f);

    private RectTransform rectTransform;
    private WorkspacePanelRect referenceRect;
    private bool isDragging;

    public string PanelId => panelId;
    public string Label => label;
    public bool IsDragging => isDragging;
    public WorkspacePanelRect ReferenceRect => referenceRect;
    public WorkspacePanelRect DefaultRect => GetDefaultRect();
    public Transform WorldContentRoot => worldContentRoot;
    public UiZonePanelContent UiZoneContent => uiZoneContent;
    public AuctionZonePanelContent AuctionZoneContent => auctionZoneContent;
    public float WorldScaleReferenceHeight => worldScaleReferenceHeight;

    public TownPanelContent TownContent =>
        worldContentRoot != null ? worldContentRoot.GetComponent<TownPanelContent>() : null;

    public DungeonPanelContent DungeonContent =>
        worldContentRoot != null ? worldContentRoot.GetComponent<DungeonPanelContent>() : null;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        EnsureVisuals();
        EnsureDragHandle();
    }

    public void Configure(
        string id,
        WorkspacePanelRect rect,
        string panelLabel,
        Color tint,
        float scaleReferenceHeight)
    {
        panelId = id;
        defaultRect = rect;
        label = panelLabel;
        worldScaleReferenceHeight = scaleReferenceHeight;
        frameColor = tint;
        EnsureVisuals();

        if (labelText != null)
            labelText.text = panelLabel;

        if (backgroundImage != null && showFrame)
            backgroundImage.color = tint;
    }

    public WorkspacePanelRect GetDefaultRect()
    {
        if (defaultRect.width > 0f && defaultRect.height > 0f)
            return defaultRect;

        return DesktopOverlaySettings.GetDefaultPanelRect(panelId);
    }

    /// <summary>
    /// Resolves the panel rect the same way runtime layout does (saved drag, anchors, or defaults).
    /// Used by Scene guides so they match Game view panel positions.
    /// </summary>
    public WorkspacePanelRect GetLayoutRect()
    {
        if (Application.isPlaying && referenceRect.width > 0f && referenceRect.height > 0f)
            return referenceRect;

        if (useSavedLayout && WorkspacePanelLayoutStore.TryLoad(panelId, out var saved))
        {
            return saved.ClampInside(
                DesktopOverlaySettings.GetLayoutBoundsWidth(),
                DesktopOverlaySettings.GetLayoutBoundsHeight());
        }

        var fromTransform = ReadRectFromTransform();
        if (fromTransform.width > 1f && fromTransform.height > 1f)
        {
            return fromTransform.ClampInside(
                DesktopOverlaySettings.GetLayoutBoundsWidth(),
                DesktopOverlaySettings.GetLayoutBoundsHeight());
        }

        return GetDefaultRect();
    }

    public void ApplySavedOrDefaultRect()
    {
        var rect = useSavedLayout && WorkspacePanelLayoutStore.TryLoad(panelId, out var saved)
            ? saved
            : GetDefaultRect();
        ApplyReferenceRect(rect);
    }

    public void ApplyReferenceRect(WorkspacePanelRect rect)
    {
        referenceRect = rect.ClampInside(
            DesktopOverlaySettings.GetLayoutBoundsWidth(),
            DesktopOverlaySettings.GetLayoutBoundsHeight());
        ApplyRectToTransform(referenceRect);
    }

    public void ApplyWorldContentLayout(Camera camera = null, bool editorPreview = false)
    {
        if (worldContentRoot == null)
            return;

        camera ??= DesktopOverlaySettings.ResolveLayoutCamera();

        var layoutRect = GetLayoutRect();

        var townContent = TownContent;
        if (townContent != null)
        {
            townContent.ApplyLayout(layoutRect, worldScaleReferenceHeight, camera, editorPreview);
            return;
        }

        var dungeonContent = DungeonContent;
        if (dungeonContent != null)
            dungeonContent.ApplyLayout(layoutRect, worldScaleReferenceHeight, camera, editorPreview);
    }

    public void ApplyUiZoneLayout()
    {
        if (uiZoneContent != null)
            uiZoneContent.ApplyLayout(referenceRect);
    }

    public void ApplyAuctionZoneLayout()
    {
        if (auctionZoneContent != null)
            auctionZoneContent.ApplyLayout(referenceRect);
    }

    public void SetContentVisible(bool visible)
    {
        if (worldContentRoot != null)
            worldContentRoot.gameObject.SetActive(visible);

        if (uiZoneContent != null)
            uiZoneContent.gameObject.SetActive(visible);

        if (auctionZoneContent != null)
            auctionZoneContent.gameObject.SetActive(visible);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!draggable)
            return;

        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!draggable || !isDragging)
            return;

        if (WorkspaceLayoutController.Instance == null)
            return;

        var controller = WorkspaceLayoutController.Instance;
        var scale = DesktopOverlaySettings.GetReferenceToScreenScale();
        var deltaReference = eventData.delta / scale;
        var refWidth = DesktopOverlaySettings.GetLayoutBoundsWidth();
        var refHeight = DesktopOverlaySettings.GetLayoutBoundsHeight();

        if (!TryResolveDragRect(deltaReference, refWidth, refHeight, controller, out var nextRect))
            return;

        referenceRect = nextRect;
        ApplyRectToTransform(referenceRect);
        controller.HandlePanelRectChanged(this);
    }

    private bool TryResolveDragRect(
        Vector2 deltaReference,
        float refWidth,
        float refHeight,
        WorkspaceLayoutController controller,
        out WorkspacePanelRect resolvedRect)
    {
        resolvedRect = referenceRect;

        var full = referenceRect.Offset(deltaReference.x, deltaReference.y)
            .ClampInside(refWidth, refHeight);
        if (!controller.WouldOverlapOtherPanels(this, full))
        {
            resolvedRect = full;
            return true;
        }

        var xOnly = referenceRect.Offset(deltaReference.x, 0f).ClampInside(refWidth, refHeight);
        if (!controller.WouldOverlapOtherPanels(this, xOnly))
        {
            resolvedRect = xOnly;
            return true;
        }

        var yOnly = referenceRect.Offset(0f, deltaReference.y).ClampInside(refWidth, refHeight);
        if (!controller.WouldOverlapOtherPanels(this, yOnly))
        {
            resolvedRect = yOnly;
            return true;
        }

        return false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!draggable || !isDragging)
            return;

        isDragging = false;
        WorkspacePanelLayoutStore.Save(panelId, referenceRect);
        RectChanged?.Invoke(this);
    }

    public void ResetToDefault()
    {
        ApplyReferenceRect(DesktopOverlaySettings.GetDefaultPanelRect(panelId));
        WorkspacePanelLayoutStore.Save(panelId, referenceRect);
    }

    private void EnsureVisuals()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (backgroundImage == null && showFrame)
        {
            backgroundImage = gameObject.AddComponent<Image>();
            backgroundImage.raycastTarget = true;
        }

        if (backgroundImage != null)
            backgroundImage.enabled = showFrame;

        if (labelText == null)
            labelText = GetComponentInChildren<Text>(true);
    }

    private void EnsureDragHandle()
    {
        var labelTransform = transform.Find("Label");
        if (labelTransform == null)
            return;

        if (labelTransform.GetComponent<WorkspacePanelDragHandle>() == null)
            labelTransform.gameObject.AddComponent<WorkspacePanelDragHandle>();

        var text = labelTransform.GetComponent<Text>();
        if (text != null)
            text.raycastTarget = true;
    }

    private WorkspacePanelRect ReadRectFromTransform()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (DesktopOverlaySettings.UseFixedPanelLayout)
            return DesktopOverlaySettings.ReadFixedReferenceRect(rectTransform);

        var refWidth = DesktopOverlaySettings.ReferenceWidth;
        var refHeight = DesktopOverlaySettings.ReferenceHeight;
        return new WorkspacePanelRect(
            rectTransform.anchorMin.x * refWidth,
            rectTransform.anchorMin.y * refHeight,
            (rectTransform.anchorMax.x - rectTransform.anchorMin.x) * refWidth,
            (rectTransform.anchorMax.y - rectTransform.anchorMin.y) * refHeight);
    }

    private void ApplyRectToTransform(WorkspacePanelRect rect)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (DesktopOverlaySettings.UseFixedPanelLayout)
        {
            DesktopOverlaySettings.ApplyFixedReferenceRect(rectTransform, rect);
            return;
        }

        var refWidth = DesktopOverlaySettings.ReferenceWidth;
        var refHeight = DesktopOverlaySettings.ReferenceHeight;
        rectTransform.anchorMin = new Vector2(rect.x / refWidth, rect.y / refHeight);
        rectTransform.anchorMax = new Vector2(
            (rect.x + rect.width) / refWidth,
            (rect.y + rect.height) / refHeight);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
