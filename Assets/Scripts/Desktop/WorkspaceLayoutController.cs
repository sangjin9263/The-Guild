using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

[DefaultExecutionOrder(200)]
public class WorkspaceLayoutController : MonoBehaviour
{
    public static WorkspaceLayoutController Instance { get; private set; }
    public static bool IsApplied { get; private set; }

    public static event Action LayoutApplied;

    public RectTransform WorkspaceRoot => workspaceRoot;
    public Camera TargetCamera => targetCamera;

    [SerializeField] private Camera targetCamera;
    [SerializeField] private RectTransform workspaceRoot;
    [SerializeField] private RectTransform layoutRoot;
    [SerializeField] private Canvas workspaceCanvas;
    [SerializeField] private int primaryCombatSlotIndex = 2;

    private readonly Dictionary<string, WorkspacePanel> panels = new();
    private bool layoutDirty = true;
    private bool editorPreviewMode;
    private Vector2Int lastScreenSize;

    public int PrimaryCombatSlotIndex => primaryCombatSlotIndex;
    public Tilemap CombatGround => ResolvePrimaryDungeonContent()?.GroundTilemap;

    public bool IsPrimaryCombatSlot(int slotIndex) => slotIndex == primaryCombatSlotIndex;

    private void Awake()
    {
        Instance = this;
        IsApplied = false;
        ResolveReferences();
        CachePanels();
    }

    private void OnEnable()
    {
        DesktopOverlayBootstrap.WindowReady += HandleWindowReady;
        GameManager.StateChanged += HandleGameStateChanged;
        WorkspacePanel.RectChanged += OnPanelRectSaved;
    }

    private void OnDisable()
    {
        DesktopOverlayBootstrap.WindowReady -= HandleWindowReady;
        GameManager.StateChanged -= HandleGameStateChanged;
        WorkspacePanel.RectChanged -= OnPanelRectSaved;
    }

    private void Start()
    {
        StartCoroutine(ApplyWhenReady());
    }

    private void LateUpdate()
    {
        var screenSize = new Vector2Int(Screen.width, Screen.height);
        if (screenSize.x > 0 && screenSize.y > 0 && screenSize != lastScreenSize)
        {
            lastScreenSize = screenSize;
            if (IsApplied)
                layoutDirty = true;
        }

        if (!layoutDirty)
            return;

        ApplyLayoutInternal();
    }

    public void HandlePanelRectChanged(WorkspacePanel panel)
    {
        layoutDirty = true;
    }

    public bool WouldOverlapOtherPanels(WorkspacePanel movingPanel, WorkspacePanelRect candidateRect)
    {
        foreach (var panel in panels.Values)
        {
            if (panel == null || panel == movingPanel)
                continue;

            if (!panel.gameObject.activeInHierarchy)
                continue;

            if (panel.ReferenceRect.Overlaps(candidateRect))
                return true;
        }

        return false;
    }

    public void RefreshLayout()
    {
        layoutDirty = true;
        ApplyLayoutInternal();
    }

    public void ApplyEditorPreviewLayout()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
            return;

        ResolveReferences();
        ApplySavedOrDefaultPanelRects();
        layoutDirty = true;
        ApplyLayoutInternal();
#endif
    }

    public WorkspacePanel GetPanel(string panelId)
    {
        panels.TryGetValue(panelId, out var panel);
        return panel;
    }

    public WorkspacePanelRect GetPanelRect(string panelId)
    {
        if (panels.TryGetValue(panelId, out var panel))
            return panel.ReferenceRect;

        return DesktopOverlaySettings.GetDefaultPanelRect(panelId);
    }

    public DungeonPanelContent GetDungeonContent(int slotIndex)
    {
        var panelId = WorkspacePanelId.GetDungeonSlotId(slotIndex);
        return panels.TryGetValue(panelId, out var panel) ? panel.DungeonContent : null;
    }

    public static Vector2 GetCombatAllyOrigin(Camera camera)
    {
        if (Instance != null && TryGetCombatGroundEdgeSpawn(alignLeft: false, out var position))
            return position;

        return GetFallbackCombatSpawn(camera, allySide: true);
    }

    public static Vector2 GetCombatEnemyOrigin(Camera camera)
    {
        if (Instance != null && TryGetCombatGroundEdgeSpawn(alignLeft: true, out var position))
            return position;

        return GetFallbackCombatSpawn(camera, allySide: false);
    }

    private void HandleWindowReady()
    {
        ConfigureWorkspaceCanvas();
        ApplySavedOrDefaultPanelRects();
        layoutDirty = true;
        StartCoroutine(ApplyWhenReady());
    }

    private void HandleGameStateChanged()
    {
        UpdateDungeonPanelVisibility();
        layoutDirty = true;
    }

    private void OnPanelRectSaved(WorkspacePanel panel)
    {
        layoutDirty = true;
    }

    private IEnumerator ApplyWhenReady()
    {
        for (var i = 0; i < 180; i++)
        {
            if (workspaceRoot == null)
                ResolveReferences();

            ConfigureWorkspaceCanvas();
            ApplySavedOrDefaultPanelRects();
            ApplyLayoutInternal();
            if (IsApplied)
                yield break;

            yield return null;
        }
    }

    private void ResolveReferences()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (workspaceCanvas == null)
            workspaceCanvas = GetComponent<Canvas>();

        if (workspaceRoot == null && workspaceCanvas != null)
            workspaceRoot = workspaceCanvas.GetComponent<RectTransform>();

        if (workspaceRoot == null)
            workspaceRoot = GameObject.Find("GameWorkspace")?.GetComponent<RectTransform>();

        EnsureLayoutRoot();
        CachePanels();
    }

    private void EnsureLayoutRoot()
    {
        if (workspaceRoot == null)
            return;

        if (layoutRoot == null)
        {
            var existing = workspaceRoot.Find("LayoutRoot");
            if (existing != null)
                layoutRoot = existing as RectTransform;
        }

        if (layoutRoot == null)
        {
            var layoutObject = new GameObject("LayoutRoot", typeof(RectTransform));
            layoutObject.transform.SetParent(workspaceRoot, false);
            layoutRoot = layoutObject.GetComponent<RectTransform>();
        }

        DesktopOverlaySettings.ApplyFullScreenOverlayRoot(workspaceRoot);
        DesktopOverlaySettings.ApplyLayoutRootTransform(layoutRoot);

        RemoveDuplicateWorkspacePanels(workspaceRoot, layoutRoot);

        foreach (var panel in workspaceRoot.GetComponentsInChildren<WorkspacePanel>(true))
        {
            if (panel.transform.parent == layoutRoot)
                continue;

            panel.transform.SetParent(layoutRoot, false);
        }

        RemoveDuplicateWorkspacePanels(workspaceRoot, layoutRoot);
    }

    /// <summary>
    /// Removes duplicate WorkspacePanel objects that share a panelId.
    /// Prefers the instance parented under LayoutRoot (canonical fixed-layout panels).
    /// </summary>
    public static int RemoveDuplicateWorkspacePanels(Transform workspaceRoot, Transform layoutRoot)
    {
        if (workspaceRoot == null || layoutRoot == null)
            return 0;

        var panels = workspaceRoot.GetComponentsInChildren<WorkspacePanel>(true);
        var byId = new Dictionary<string, List<WorkspacePanel>>();

        foreach (var panel in panels)
        {
            if (!byId.TryGetValue(panel.PanelId, out var list))
            {
                list = new List<WorkspacePanel>();
                byId[panel.PanelId] = list;
            }

            list.Add(panel);
        }

        var removed = 0;
        foreach (var list in byId.Values)
        {
            if (list.Count <= 1)
                continue;

            WorkspacePanel keep = null;
            foreach (var panel in list)
            {
                if (panel.transform.parent == layoutRoot)
                {
                    keep = panel;
                    break;
                }
            }

            keep ??= list[0];

            foreach (var panel in list)
            {
                if (panel == keep)
                    continue;

                if (Application.isPlaying)
                    Destroy(panel.gameObject);
                else
                    DestroyImmediate(panel.gameObject);

                removed++;
            }
        }

        return removed;
    }

    private void CachePanels()
    {
        panels.Clear();
        if (workspaceRoot == null)
            return;

        foreach (var panel in workspaceRoot.GetComponentsInChildren<WorkspacePanel>(true))
            panels[panel.PanelId] = panel;
    }

    private void ConfigureWorkspaceCanvas()
    {
        if (workspaceCanvas == null)
            return;

        workspaceCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        workspaceCanvas.sortingOrder = 50;

        var scaler = workspaceCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = workspaceCanvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = DesktopOverlaySettings.UseFixedPanelLayout
            ? CanvasScaler.ScaleMode.ConstantPixelSize
            : CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.scaleFactor = 1f;
        scaler.referenceResolution = DesktopOverlaySettings.WindowSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        if (workspaceRoot != null)
            DesktopOverlaySettings.ApplyFullScreenOverlayRoot(workspaceRoot);
    }

    private void ApplySavedOrDefaultPanelRects()
    {
        foreach (var panel in panels.Values)
            panel.ApplySavedOrDefaultRect();

        UpdateDungeonPanelVisibility();
        ResolveActivePanelOverlaps();
        BuildingPanelUI.ApplyToUiZone();
    }

    private void ResolveActivePanelOverlaps()
    {
        var placedRects = new List<(WorkspacePanel panel, WorkspacePanelRect rect)>();

        foreach (var panel in panels.Values)
        {
            if (!panel.gameObject.activeInHierarchy)
                continue;

            var rect = panel.ReferenceRect;
            if (OverlapsPlacedPanel(panel, rect, placedRects))
            {
                rect = DesktopOverlaySettings.GetDefaultPanelRect(panel.PanelId);
                panel.ApplyReferenceRect(rect);
                WorkspacePanelLayoutStore.Save(panel.PanelId, rect);
            }

            placedRects.Add((panel, panel.ReferenceRect));
        }
    }

    public void ResetAllPanelsToDefaultLayout()
    {
        ResolveReferences();
        CachePanels();

        foreach (var panel in panels.Values)
            panel.ResetToDefault();

        layoutDirty = true;
        ApplyLayoutInternal();
    }

    private static bool OverlapsPlacedPanel(
        WorkspacePanel movingPanel,
        WorkspacePanelRect candidateRect,
        List<(WorkspacePanel panel, WorkspacePanelRect rect)> placedRects)
    {
        foreach (var (panel, rect) in placedRects)
        {
            if (panel == movingPanel)
                continue;

            if (rect.Overlaps(candidateRect))
                return true;
        }

        return false;
    }

    private void UpdateDungeonPanelVisibility()
    {
        for (var slotIndex = 0; slotIndex < DesktopOverlaySettings.DungeonSlotCount; slotIndex++)
        {
            var panelId = WorkspacePanelId.GetDungeonSlotId(slotIndex);
            if (!panels.TryGetValue(panelId, out var panel))
                continue;

            var unlocked = GameManager.Instance == null
                ? GameManager.IsSlotUnlockedForCount(slotIndex, 1)
                : GameManager.Instance.IsSlotUnlocked(slotIndex);
            panel.gameObject.SetActive(unlocked);
            panel.SetContentVisible(unlocked);
        }
    }

    private void ApplyLayoutInternal()
    {
        ResolveReferences();
        if (workspaceRoot == null)
            return;

        EnsureLayoutRoot();

        var camera = targetCamera ?? Camera.main;
        SideViewCamera.Apply(camera);

        foreach (var panel in panels.Values)
        {
            if (panel != null && panel.gameObject.activeInHierarchy)
                panel.ApplyReferenceRect(panel.ReferenceRect);
        }

        ResolveActivePanelOverlaps();

        foreach (var panel in panels.Values)
            ApplyPanelLayout(panel, camera);

        BuildingPanelUI.ApplyToUiZone();

        layoutDirty = false;
        IsApplied = true;
        LayoutApplied?.Invoke();
    }

    private void ApplyPanelLayout(WorkspacePanel panel, Camera camera)
    {
        if (panel == null || !panel.gameObject.activeInHierarchy)
            return;

        panel.ApplyWorldContentLayout(camera, editorPreviewMode);
        panel.ApplyUiZoneLayout();
    }

    private DungeonPanelContent ResolvePrimaryDungeonContent()
    {
        if (panels.Count == 0)
            CachePanels();

        var primary = GetDungeonContent(primaryCombatSlotIndex);
        if (primary != null && primary.gameObject.activeInHierarchy)
            return primary;

        for (var slotIndex = DesktopOverlaySettings.DungeonSlotCount - 1; slotIndex >= 0; slotIndex--)
        {
            var content = GetDungeonContent(slotIndex);
            if (content != null && content.gameObject.activeInHierarchy)
                return content;
        }

        return null;
    }

    private static bool TryGetCombatGroundEdgeSpawn(bool alignLeft, out Vector2 position)
    {
        position = default;

        var tilemap = CombatGroundQuery.ResolveCombatGround();
        if (tilemap == null)
            return false;

        tilemap.CompressBounds();
        var bounds = tilemap.cellBounds;
        if (bounds.size.x <= 0 || bounds.size.y <= 0)
            return false;

        var cellX = alignLeft ? bounds.xMin : bounds.xMax;
        if (!CombatGroundQuery.TryGetSurfaceWorldPosition(tilemap, cellX, out position))
        {
            var cellY = bounds.yMin;
            var world = tilemap.GetCellCenterWorld(new Vector3Int(cellX, cellY, 0));
            position = new Vector2(world.x, world.y);
        }

        return true;
    }

    private static Vector2 GetFallbackCombatSpawn(Camera camera, bool allySide)
    {
        if (Instance != null)
        {
            var content = Instance.ResolvePrimaryDungeonContent();
            if (content != null)
            {
                var panel = Instance.GetPanel(WorkspacePanelId.GetDungeonSlotId(content.SlotIndex));
                if (panel != null)
                {
                    var center = DesktopOverlaySettings.ReferenceRectCenterToWorld(panel.ReferenceRect, camera);
                    var x = allySide ? center.x + 4f : center.x - 4f;
                    return new Vector2(x, center.y);
                }
            }
        }

        var viewHalfWidth = DesktopOverlaySettings.GetViewHalfWidth(camera);
        var xFallback = allySide ? -viewHalfWidth * 0.25f : -viewHalfWidth * 0.4f;
        return new Vector2(xFallback, DesktopOverlaySettings.GetDefaultGroundSurfaceY());
    }
}
