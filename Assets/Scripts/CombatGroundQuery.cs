using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 전투 바닥 타일맵을 찾고, 월드 X 좌표에 맞는 지면 Y를 계산합니다.
/// </summary>
public static class CombatGroundQuery
{
    public static Tilemap ResolveCombatGround()
    {
        // WorkspaceLayoutController가 지정한 주 던전(아래 슬롯, 표시 이름 「던전 1」) 바닥을 우선 사용합니다.
        if (WorkspaceLayoutController.Instance != null)
        {
            var tilemap = WorkspaceLayoutController.Instance.CombatGround;
            if (tilemap != null)
                return tilemap;
        }

        return null;
    }

    public static bool TryGetSurfaceWorldPosition(Tilemap tilemap, int cellX, out Vector2 position)
    {
        position = default;
        if (tilemap == null)
            return false;

        tilemap.CompressBounds();
        var bounds = tilemap.cellBounds;
        if (bounds.size.x <= 0 || bounds.size.y <= 0)
            return false;

        var minY = Mathf.Max(bounds.yMin, DesktopOverlaySettings.GroundMinY);
        var maxY = Mathf.Min(bounds.yMax, DesktopOverlaySettings.CombatGroundMaxY);

        for (var y = maxY; y >= minY; y--)
        {
            if (!tilemap.HasTile(new Vector3Int(cellX, y, 0)))
                continue;

            var center = tilemap.GetCellCenterWorld(new Vector3Int(cellX, y, 0));
            position = new Vector2(
                center.x,
                center.y + tilemap.cellSize.y * 0.5f);

            return true;
        }

        return false;
    }

    public static bool TryGetSurfaceYAtWorldX(Tilemap tilemap, float worldX, out float surfaceY)
    {
        surfaceY = 0f;
        if (tilemap == null)
            return false;

        var cellX = tilemap.WorldToCell(new Vector3(worldX, 0f, 0f)).x;
        if (!TryGetSurfaceWorldPosition(tilemap, cellX, out var position))
            return false;

        surfaceY = position.y;
        return true;
    }

    public static bool TryGetAverageSurfaceY(Tilemap tilemap, out float surfaceY)
    {
        surfaceY = 0f;
        if (tilemap == null)
            return false;

        tilemap.CompressBounds();
        var bounds = tilemap.cellBounds;
        if (bounds.size.x <= 0)
            return false;

        var sum = 0f;
        var count = 0;

        for (var x = bounds.xMin; x <= bounds.xMax; x++)
        {
            if (!TryGetSurfaceWorldPosition(tilemap, x, out var position))
                continue;

            sum += position.y;
            count++;
        }

        if (count == 0)
            return false;

        surfaceY = sum / count;
        return true;
    }
}
