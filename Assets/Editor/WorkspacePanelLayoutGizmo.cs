#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class WorkspacePanelLayoutGizmo
{
    private static bool showGuides = true;
    private static double lastSceneLayoutApplyTime;
    private static int lastSceneLayoutSignature = int.MinValue;
    private static bool sceneLayoutPending = true;

    static WorkspacePanelLayoutGizmo()
    {
        SceneView.duringSceneGui += OnSceneGui;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += OnEditorDelayCall;
    }

    private static void OnEditorDelayCall()
    {
        if (Application.isPlaying)
            return;

        sceneLayoutPending = true;
        ApplyEditorSceneLayout(force: true);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode)
            return;

        lastSceneLayoutSignature = 0;
        sceneLayoutPending = true;
    }

    [MenuItem("The Guild/Layout/Toggle Panel Layout Guides")]
    private static void ToggleGuides()
    {
        showGuides = !showGuides;
        SceneView.RepaintAll();
        Debug.Log(showGuides
            ? "Workspace panel layout guides enabled in Scene view."
            : "Workspace panel layout guides disabled.");
    }

    [MenuItem("The Guild/Layout/Toggle Panel Layout Guides", true)]
    private static bool ToggleGuidesValidate() => true;

    [MenuItem("The Guild/Layout/Preview Panel Layout In Scene")]
    public static void PreviewPanelLayoutInScene()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("Stop Play mode first, then run preview for tile painting.");
            return;
        }

        ApplyEditorSceneLayout(force: true);

        var focusTilemap = FindGroundTilemapForPanel(WorkspacePanelId.DungeonSlot3)
            ?? FindGroundTilemapForPanel(WorkspacePanelId.Town);
        if (focusTilemap != null)
            PrepareTilemapForPainting(focusTilemap);

        SceneView.RepaintAll();
        Debug.Log(
            "Panel layout preview applied. Scene tiles now match the Play layout guides. " +
            "Use Prepare Town/Dungeon Ground For Painting to focus the tile brush.");
    }

    [MenuItem("The Guild/Layout/Prepare Dungeon 1 Ground For Painting")]
    public static void PrepareDungeon3GroundForPainting()
    {
        var tilemap = FindGroundTilemapForPanel(WorkspacePanelId.DungeonSlot3);
        if (tilemap == null)
        {
            Debug.LogWarning("Dungeon 1 ground tilemap not found.");
            return;
        }

        PrepareTilemapForPainting(tilemap);
    }

    [MenuItem("The Guild/Layout/Prepare Town Ground For Painting")]
    public static void PrepareTownGroundForPainting()
    {
        var tilemap = FindGroundTilemapForPanel(WorkspacePanelId.Town);
        if (tilemap == null)
        {
            Debug.LogWarning("Town ground tilemap not found.");
            return;
        }

        PrepareTilemapForPainting(tilemap);
    }

    private static Tilemap FindGroundTilemapForPanel(string panelId)
    {
        foreach (var panel in Object.FindObjectsByType<WorkspacePanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (panel.PanelId != panelId)
                continue;

            var dungeonContent = panel.DungeonContent;
            if (dungeonContent != null && dungeonContent.GroundTilemap != null)
                return dungeonContent.GroundTilemap;

            var townContent = panel.TownContent;
            if (townContent != null && townContent.GroundTilemap != null)
                return townContent.GroundTilemap;
        }

        return null;
    }

    private static void PrepareTilemapForPainting(Tilemap tilemap)
    {
        if (tilemap == null)
            return;

        Selection.activeGameObject = tilemap.gameObject;
        GridPaintingState.scenePaintTarget = tilemap.gameObject;

        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();

        Debug.Log($"Ready to paint on: {GetHierarchyPath(tilemap.transform)}");
    }

    private static string GetHierarchyPath(Transform transform)
    {
        var path = transform.name;
        var parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    private static void ApplyEditorSceneLayout(bool force = false)
    {
        if (Application.isPlaying)
            return;

        var signature = ComputeSceneLayoutSignature();
        if (!force && signature == lastSceneLayoutSignature)
            return;

        lastSceneLayoutSignature = signature;
        lastSceneLayoutApplyTime = EditorApplication.timeSinceStartup;
        sceneLayoutPending = false;

        var camera = DesktopOverlaySettings.ResolveLayoutCamera();
        if (camera == null)
            return;

        SideViewCamera.Apply(camera);

        foreach (var panel in Object.FindObjectsByType<WorkspacePanel>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (!panel.gameObject.activeInHierarchy)
                continue;

            panel.ApplyReferenceRect(panel.GetLayoutRect());
            panel.ApplyWorldContentLayout(camera, editorPreview: false);
        }
    }

    private static int ComputeSceneLayoutSignature()
    {
        var hash = 17;
        hash = hash * 31 + DesktopOverlaySettings.GetLayoutBoundsWidth().GetHashCode();
        hash = hash * 31 + DesktopOverlaySettings.GetLayoutBoundsHeight().GetHashCode();

        foreach (var panel in Object.FindObjectsByType<WorkspacePanel>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (!panel.gameObject.activeInHierarchy)
                continue;

            var rect = panel.GetLayoutRect();
            hash = hash * 31 + rect.x.GetHashCode();
            hash = hash * 31 + rect.y.GetHashCode();
            hash = hash * 31 + rect.width.GetHashCode();
            hash = hash * 31 + rect.height.GetHashCode();
        }

        return hash;
    }

    private static void TryRefreshEditorSceneLayout()
    {
        if (Application.isPlaying)
            return;

        var signature = ComputeSceneLayoutSignature();
        if (signature != lastSceneLayoutSignature)
            sceneLayoutPending = true;

        if (!sceneLayoutPending)
            return;

        var elapsed = EditorApplication.timeSinceStartup - lastSceneLayoutApplyTime;
        if (lastSceneLayoutApplyTime > 0d && elapsed < 0.5d)
            return;

        ApplyEditorSceneLayout();
    }

    private static void OnSceneGui(SceneView sceneView)
    {
        TryRefreshEditorSceneLayout();

        if (!showGuides)
            return;

        var camera = DesktopOverlaySettings.ResolveLayoutCamera();
        if (camera == null)
            return;

        SideViewCamera.Apply(camera);

        var panels = Object.FindObjectsByType<WorkspacePanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var panel in panels)
        {
            if (!panel.gameObject.activeInHierarchy)
                continue;

            var rect = panel.GetLayoutRect();
            DesktopOverlaySettings.ReferenceRectToWorldBounds(rect, camera, out var bottomLeft, out var topRight);
            var color = GetPanelColor(panel.PanelId);
            var previousColor = Handles.color;
            Handles.color = color;

            var corners = new[]
            {
                new Vector3(bottomLeft.x, bottomLeft.y, 0f),
                new Vector3(topRight.x, bottomLeft.y, 0f),
                new Vector3(topRight.x, topRight.y, 0f),
                new Vector3(bottomLeft.x, topRight.y, 0f)
            };

            for (var i = 0; i < corners.Length; i++)
                Handles.DrawLine(corners[i], corners[(i + 1) % corners.Length], 3f);

            var labelPosition = new Vector3(
                (bottomLeft.x + topRight.x) * 0.5f,
                (bottomLeft.y + topRight.y) * 0.5f,
                0f);
            Handles.Label(labelPosition, $"{panel.Label}\n(패널 가이드)");

            Handles.color = previousColor;
        }
    }

    private static Color GetPanelColor(string panelId) => panelId switch
    {
        WorkspacePanelId.Town => new Color(1f, 0.45f, 0.1f, 0.9f),
        WorkspacePanelId.UiZone => new Color(0.65f, 0.65f, 0.65f, 0.9f),
        WorkspacePanelId.DungeonSlot1 or WorkspacePanelId.DungeonSlot2 or WorkspacePanelId.DungeonSlot3
            => new Color(0.35f, 0.55f, 0.95f, 0.9f),
        _ => Color.white
    };
}
#endif
