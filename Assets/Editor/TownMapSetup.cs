#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TownMapSetup
{
    private const string StarterTilePath =
        "Assets/Store/Cainos/Pixel Art Platformer - Village Props/Tileset Palette/TP Ground/TX Tileset Ground_35.asset";

    public static void SetupTownPanelMap()
    {
        var town = Object.FindFirstObjectByType<TownPanelContent>(FindObjectsInactive.Include);
        if (town == null)
        {
            Debug.LogWarning("TownPanelContent not found. Run The Guild/Layout/Setup Workspace Layout first.");
            return;
        }

        NormalizeTownHierarchy(town);
        var ground = ResolveGroundTilemap(town);
        if (ground == null)
        {
            Debug.LogWarning("Town ground tilemap not found under TownContent/Grid.");
            return;
        }

        PaintTownGround(ground);
        var portal = PortalVisualSetup.ResolveTownPortal(town.transform);
        PlaceTownPortal(portal);
        WireTownContentReferences(town, ground, portal);

        WorkspacePanelLayoutGizmo.PreviewPanelLayoutInScene();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log(
            "Town panel map ready. Ground painted in TownContent local space and VillagePortal placed on the left edge.");
    }

    public static void PaintTownGroundMenu()
    {
        var town = Object.FindFirstObjectByType<TownPanelContent>(FindObjectsInactive.Include);
        if (town == null || town.GroundTilemap == null)
        {
            Debug.LogWarning("Town ground tilemap not found.");
            return;
        }

        PaintTownGround(town.GroundTilemap);
        WorkspacePanelLayoutGizmo.PreviewPanelLayoutInScene();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    public static void PlaceTownPortalMenu()
    {
        var town = Object.FindFirstObjectByType<TownPanelContent>(FindObjectsInactive.Include);
        if (town == null)
        {
            Debug.LogWarning("TownPanelContent not found.");
            return;
        }

        var portal = PortalVisualSetup.ResolveTownPortal(town.transform);
        PlaceTownPortal(portal);
        WireTownContentReferences(town, town.GroundTilemap, portal);
        WorkspacePanelLayoutGizmo.PreviewPanelLayoutInScene();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    public static void PaintTownGround(Tilemap tilemap)
    {
        var tile = AssetDatabase.LoadAssetAtPath<TileBase>(StarterTilePath);
        if (tile == null)
        {
            Debug.LogWarning($"Ground tile not found: {StarterTilePath}");
            return;
        }

        tilemap.ClearAllTiles();
        for (var x = TownPanelLayout.GroundMinX; x <= TownPanelLayout.GroundMaxX; x++)
        {
            for (var y = TownPanelLayout.GroundMinY; y <= TownPanelLayout.GroundMaxY; y++)
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
        }

        EditorUtility.SetDirty(tilemap);
    }

    public static void PlaceTownPortal(Transform portalRoot)
    {
        if (portalRoot == null)
            return;

        portalRoot.localPosition = new Vector3(
            TownPanelLayout.PortalLocalX,
            TownPanelLayout.PortalLocalY,
            0f);
        PortalVisualSetup.EnsurePortalVisual(portalRoot);
        EditorUtility.SetDirty(portalRoot);
    }

    private static void NormalizeTownHierarchy(TownPanelContent town)
    {
        var contentRoot = town.transform;
        var grid = contentRoot.Find("Grid");
        if (grid == null)
        {
            var gridObject = new GameObject("Grid");
            grid = gridObject.transform;
            grid.SetParent(contentRoot, false);
            var gridComponent = gridObject.AddComponent<Grid>();
            gridComponent.cellSize = new Vector3(1f, 1f, 0f);
        }

        RemoveDuplicateTilemaps(grid, keepName: "Ground");
        EnsureChild(contentRoot, "Props");
    }

    private static Tilemap ResolveGroundTilemap(TownPanelContent town)
    {
        if (town.GroundTilemap != null)
            return town.GroundTilemap;

        var grid = town.transform.Find("Grid");
        if (grid == null)
            return null;

        return ZoneLayoutSetup.EnsureTilemapUnder(grid, "Ground");
    }

    private static void RemoveDuplicateTilemaps(Transform grid, string keepName)
    {
        for (var i = grid.childCount - 1; i >= 0; i--)
        {
            var child = grid.GetChild(i);
            if (child.GetComponent<Tilemap>() == null)
                continue;

            if (child.name == keepName)
                continue;

            if (!ZoneLayoutSetup.HasPaintedTiles(child.GetComponent<Tilemap>()))
                Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void EnsureChild(Transform parent, string name)
    {
        if (parent.Find(name) != null)
            return;

        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
    }

    private static void WireTownContentReferences(
        TownPanelContent town,
        Tilemap ground,
        Transform portal)
    {
        var props = town.transform.Find("Props");
        var serialized = new SerializedObject(town);
        serialized.FindProperty("groundTilemap").objectReferenceValue = ground;
        serialized.FindProperty("propsRoot").objectReferenceValue = props;
        serialized.FindProperty("portalRoot").objectReferenceValue = portal;
        serialized.FindProperty("portalOffsetX").floatValue = TownPanelLayout.PortalLocalX;
        serialized.FindProperty("portalOffsetY").floatValue = TownPanelLayout.PortalLocalY;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
