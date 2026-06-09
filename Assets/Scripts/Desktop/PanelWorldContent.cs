using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class PanelWorldContent : MonoBehaviour
{
    private const float FitInset = 0.995f;

    [SerializeField] private Transform contentRoot;

    public Transform ContentRoot => contentRoot != null ? contentRoot : transform;

    public abstract void ApplyLayout(
        WorkspacePanelRect rect,
        float scaleReferenceHeight,
        Camera camera,
        bool editorPreview = false);

    protected void ApplyPanelWorldLayout(
        WorkspacePanelRect rect,
        Camera camera,
        Vector2 designPanelSizeFallback,
        bool editorPreview,
        Tilemap groundTilemap,
        Transform propsRoot,
        Transform portalRoot,
        Transform battleRoot = null)
    {
        if (editorPreview)
        {
            ApplyContentRootLayout(
                RectCenterToWorld(rect, camera),
                Vector3.one,
                groundTilemap,
                propsRoot,
                portalRoot,
                battleRoot);
            return;
        }

        if (TryComputeTilemapPanelFit(
                rect,
                groundTilemap,
                camera,
                designPanelSizeFallback,
                out var worldPosition,
                out var scale))
        {
            ApplyContentRootLayout(
                worldPosition,
                scale,
                groundTilemap,
                propsRoot,
                portalRoot,
                battleRoot);
            return;
        }

        ApplyContentRootLayout(
            RectCenterToWorld(rect, camera),
            GetPanelContentScale(rect, groundTilemap, camera, designPanelSizeFallback),
            groundTilemap,
            propsRoot,
            portalRoot,
            battleRoot);
    }

    protected void ApplyContentRootLayout(
        Vector3 center,
        Vector3 scale,
        Tilemap groundTilemap,
        Transform propsRoot,
        Transform portalRoot,
        Transform battleRoot)
    {
        var root = ContentRoot;
        root.position = center;
        root.localScale = scale;

        if (groundTilemap != null)
        {
            if (groundTilemap.transform.parent != null && groundTilemap.transform.parent != root)
                ResetChildTransform(groundTilemap.transform.parent);

            ResetChildTransform(groundTilemap.transform);
        }

        ResetChildTransform(propsRoot);
        ResetChildTransform(battleRoot);

        if (portalRoot != null && portalRoot.parent != null && portalRoot.parent != root)
            ResetChildTransform(portalRoot.parent);
    }

    protected static void ResetChildTransform(Transform child)
    {
        if (child == null)
            return;

        child.localPosition = Vector3.zero;
        child.localScale = Vector3.one;
    }

    protected static Vector3 RectCenterToWorld(WorkspacePanelRect rect, Camera camera)
    {
        var center = DesktopOverlaySettings.ReferenceRectCenterToWorld(rect, camera);
        return new Vector3(center.x, center.y, 0f);
    }

    private bool TryComputeTilemapPanelFit(
        WorkspacePanelRect rect,
        Tilemap groundTilemap,
        Camera camera,
        Vector2 designPanelSizeFallback,
        out Vector3 worldPosition,
        out Vector3 scale)
    {
        worldPosition = default;
        scale = Vector3.one;

        var root = ContentRoot;
        if (groundTilemap == null || root == null)
            return false;

        if (!TryMeasureTilemapLocalBounds(groundTilemap, root, out var localMin, out var localMax, out var localSize))
            return false;

        DesktopOverlaySettings.ReferenceRectToWorldBounds(rect, camera, out var panelBottomLeft, out var panelTopRight);
        var panelWorldW = panelTopRight.x - panelBottomLeft.x;
        var panelWorldH = panelTopRight.y - panelBottomLeft.y;
        if (panelWorldW <= 0f || panelWorldH <= 0f)
            return false;

        var scaleX = panelWorldW / localSize.x;
        var scaleY = panelWorldH / localSize.y;
        var uniform = Mathf.Min(scaleX, scaleY) * FitInset;
        scale = new Vector3(uniform, uniform, 1f);

        var localCenterX = (localMin.x + localMax.x) * 0.5f;
        var panelCenterX = (panelBottomLeft.x + panelTopRight.x) * 0.5f;
        worldPosition = new Vector3(
            panelCenterX - uniform * localCenterX,
            panelBottomLeft.y - uniform * localMin.y,
            0f);

        return true;
    }

    private static bool TryMeasureTilemapLocalBounds(
        Tilemap tilemap,
        Transform contentRoot,
        out Vector2 localMin,
        out Vector2 localMax,
        out Vector2 localSize)
    {
        localMin = default;
        localMax = default;
        localSize = default;

        tilemap.CompressBounds();
        var bounds = tilemap.cellBounds;
        var hasTiles = false;
        var minX = float.PositiveInfinity;
        var minY = float.PositiveInfinity;
        var maxX = float.NegativeInfinity;
        var maxY = float.NegativeInfinity;

        foreach (var cell in bounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(cell))
                continue;

            var local = contentRoot.InverseTransformPoint(tilemap.GetCellCenterWorld(cell));
            minX = Mathf.Min(minX, local.x);
            minY = Mathf.Min(minY, local.y);
            maxX = Mathf.Max(maxX, local.x);
            maxY = Mathf.Max(maxY, local.y);
            hasTiles = true;
        }

        if (!hasTiles)
            return false;

        var halfCell = tilemap.layoutGrid.cellSize * 0.5f;
        localMin = new Vector2(minX - halfCell.x, minY - halfCell.y);
        localMax = new Vector2(maxX + halfCell.x, maxY + halfCell.y);
        localSize = localMax - localMin;
        return localSize.x > 0f && localSize.y > 0f;
    }

    /// <summary>
    /// 타일맵 bounds가 UI 패널(색 박스)의 월드 투영 영역 안에 들어가도록 스케일합니다.
    /// </summary>
    protected static Vector3 GetPanelContentScale(
        WorkspacePanelRect rect,
        Tilemap groundTilemap,
        Camera camera,
        Vector2 designPanelSizeFallback)
    {
        if (groundTilemap != null)
        {
            groundTilemap.CompressBounds();
            var cellBounds = groundTilemap.cellBounds;
            if (cellBounds.size.x > 0 && cellBounds.size.y > 0)
            {
                var cellSize = groundTilemap.layoutGrid.cellSize;
                var tileWorldW = cellBounds.size.x * cellSize.x;
                var tileWorldH = cellBounds.size.y * cellSize.y;

                DesktopOverlaySettings.ReferenceRectToWorldBounds(rect, camera, out var bottomLeft, out var topRight);
                var panelWorldW = topRight.x - bottomLeft.x;
                var panelWorldH = topRight.y - bottomLeft.y;

                if (tileWorldW > 0f && tileWorldH > 0f && panelWorldW > 0f && panelWorldH > 0f)
                {
                    var scaleX = panelWorldW / tileWorldW;
                    var scaleY = panelWorldH / tileWorldH;
                    var uniform = Mathf.Min(scaleX, scaleY) * FitInset;
                    return new Vector3(uniform, uniform, 1f);
                }
            }
        }

        if (designPanelSizeFallback.x <= 0f || designPanelSizeFallback.y <= 0f)
            return Vector3.one;

        return new Vector3(
            rect.width / designPanelSizeFallback.x,
            rect.height / designPanelSizeFallback.y,
            1f);
    }
}
