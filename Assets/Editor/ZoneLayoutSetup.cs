using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class ZoneLayoutSetup
{
    private const string StarterTilePath =
        "Assets/Store/Cainos/Pixel Art Platformer - Village Props/Tileset Palette/TP Ground/TX Tileset Ground_35.asset";
    private const string TilemapUnlitMaterialPath =
        "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

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
