#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class WorkspacePanelLayoutGizmo
{
    private static bool showGuides = true;

    static WorkspacePanelLayoutGizmo()
    {
        SceneView.duringSceneGui += OnSceneGui;
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

        var controller = Object.FindFirstObjectByType<WorkspaceLayoutController>();
        if (controller == null)
        {
            Debug.LogWarning("WorkspaceLayoutController not found in scene.");
            return;
        }

        controller.ApplyEditorPreviewLayout();

        var focusTilemap = FindGroundTilemapForPanel(WorkspacePanelId.DungeonSlot3)
            ?? FindGroundTilemapForPanel(WorkspacePanelId.Town);
        if (focusTilemap != null)
            PrepareTilemapForPainting(focusTilemap);

        SceneView.RepaintAll();
        Debug.Log(
            "Panel layout preview applied. Paint tiles in the Scene view (not Game view). " +
            "If the brush still does nothing, select your Ground tilemap and set Tile Palette Focus On.");
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

    private static void OnSceneGui(SceneView sceneView)
    {
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
            Handles.Label(labelPosition, $"{panel.Label}\n(Play 위치 가이드)");

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
