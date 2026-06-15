using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class ZoneLayoutSetup
{
    private const string StarterTilePath =
        "Assets/Store/Cainos/Pixel Art Platformer - Village Props/Tileset Palette/TP Ground/TX Tileset Ground_35.asset";
    private const string TilemapUnlitMaterialPath =
        "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

    public static void SetupZoneLayoutInActiveScene()
    {
        ApplyZoneLayout(paintStarterGroundIfEmpty: true);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Zone layout applied to active scene.");
    }

    public static void ApplyZoneLayout(bool paintStarterGroundIfEmpty)
    {
        EnsureGrid();
        RemoveLegacyGround();
        var combatTilemap = EnsureTilemap("Combat_Ground");
        var villageTilemap = EnsureTilemap("Village_Ground");
        EnsurePropsFolders();
        EnsureZoneGuides();
        EnsurePortals();
        EnsureLayoutController();

        if (paintStarterGroundIfEmpty)
        {
            if (!HasPaintedTiles(combatTilemap))
                PaintCombatGround(combatTilemap);
            if (!HasPaintedTiles(villageTilemap))
                PaintVillageGround(villageTilemap);
        }
    }

    private static void EnsureGrid()
    {
        if (GameObject.Find("Grid") != null)
            return;

        var gridObject = new GameObject("Grid");
        var grid = gridObject.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);
    }

    private static void RemoveLegacyGround()
    {
        var grid = GameObject.Find("Grid");
        if (grid == null)
            return;

        var legacy = grid.transform.Find("Ground");
        if (legacy != null)
            Object.DestroyImmediate(legacy.gameObject);
    }

    public static Tilemap EnsureTilemap(string tilemapName)
    {
        var gridObject = GameObject.Find("Grid");
        if (gridObject == null)
            EnsureGrid();

        gridObject = GameObject.Find("Grid");
        return EnsureTilemapUnder(gridObject.transform, tilemapName);
    }

    public static Tilemap EnsureTilemapUnder(Transform gridParent, string tilemapName)
    {
        var tilemapObject = gridParent.Find(tilemapName)?.gameObject;
        if (tilemapObject == null)
        {
            tilemapObject = new GameObject(tilemapName);
            tilemapObject.transform.SetParent(gridParent, false);
            tilemapObject.AddComponent<Tilemap>();
            tilemapObject.AddComponent<TilemapRenderer>();
        }

        ConfigureGroundRenderer(tilemapObject.GetComponent<TilemapRenderer>());
        return tilemapObject.GetComponent<Tilemap>();
    }

    private static void ConfigureGroundRenderer(TilemapRenderer renderer)
    {
        if (renderer == null)
            return;

        renderer.sortingOrder = 0;
        var unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(TilemapUnlitMaterialPath);
        if (unlitMaterial != null)
            renderer.sharedMaterial = unlitMaterial;
    }

    private static void EnsurePropsFolders()
    {
        var propsRoot = GameObject.Find("Props");
        if (propsRoot == null)
            propsRoot = new GameObject("Props");

        EnsureChild(propsRoot.transform, "Combat_Props");
        EnsureChild(propsRoot.transform, "Village_Props");
    }

    private static void EnsureChild(Transform parent, string name)
    {
        if (parent.Find(name) != null)
            return;

        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
    }

    private static void EnsureZoneGuides()
    {
        var root = GameObject.Find("ZoneLayout");
        if (root == null)
            root = new GameObject("ZoneLayout");

        EnsureGuide(root.transform, "Guide_DungeonEdge", DesktopOverlaySettings.DungeonMaxX);
        EnsureGuide(root.transform, "Guide_BuildingEdge", DesktopOverlaySettings.BuildingMinX);
    }

    private static void EnsureGuide(Transform parent, string name, float x)
    {
        var guide = parent.Find(name);
        if (guide == null)
        {
            var guideObject = new GameObject(name);
            guideObject.transform.SetParent(parent, false);
            guide = guideObject.transform;
            guideObject.AddComponent<ZoneGuideMarker>();
        }

        guide.position = new Vector3(x, 0f, 0f);
    }

    private static void EnsureLayoutController()
    {
        if (GameObject.Find("GameWorkspace") == null)
            return;
    }

    private static void EnsurePortals()
    {
        var root = GameObject.Find("Portals");
        if (root == null)
            root = new GameObject("Portals");

        PlacePortal(root.transform, "BattlePortal", DesktopOverlaySettings.BattlePortalPosition);
        PlacePortal(root.transform, "VillagePortal", DesktopOverlaySettings.VillagePortalPosition);
    }

    private static void PlacePortal(Transform parent, string name, Vector2 position)
    {
        var portal = parent.Find(name);
        if (portal == null)
        {
            var portalObject = new GameObject(name);
            portalObject.transform.SetParent(parent, false);
            portal = portalObject.transform;
        }

        portal.position = new Vector3(position.x, position.y, 0f);
        PortalVisualSetup.EnsurePortalVisual(portal);
    }

    public static void PaintCombatGround(Tilemap tilemap)
    {
        PaintRect(
            tilemap,
            Mathf.CeilToInt(DesktopOverlaySettings.DungeonMinX),
            Mathf.FloorToInt(DesktopOverlaySettings.DungeonMaxX) - 1,
            DesktopOverlaySettings.GroundMinY,
            DesktopOverlaySettings.CombatGroundMaxY);
    }

    public static void PaintVillageGround(Tilemap tilemap)
    {
        PaintRect(
            tilemap,
            Mathf.CeilToInt(DesktopOverlaySettings.BuildingMinX) + 1,
            Mathf.FloorToInt(DesktopOverlaySettings.BuildingMaxX) - 1,
            DesktopOverlaySettings.GroundMinY,
            DesktopOverlaySettings.VillageGroundMaxY);
    }

    private static void PaintRect(Tilemap tilemap, int minX, int maxX, int minY, int maxY)
    {
        var tile = AssetDatabase.LoadAssetAtPath<TileBase>(StarterTilePath);
        if (tile == null)
        {
            Debug.LogWarning($"Ground tile not found: {StarterTilePath}");
            return;
        }

        tilemap.ClearAllTiles();
        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
        }
    }

    public static bool HasPaintedTiles(Tilemap tilemap)
    {
        if (tilemap == null)
            return false;

        tilemap.CompressBounds();
        var bounds = tilemap.cellBounds;
        return bounds.size.x > 0 && bounds.size.y > 0;
    }
}
