using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class PanelWorldContent : MonoBehaviour
{
    private const float FitInset = 0.995f;

    [SerializeField] private Transform contentRoot;

    [Header("Layout Tuning")]
    [SerializeField] private bool overrideWorldY;
    [SerializeField] private float worldYOverride;

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
        Vector3 worldAnchorPosition,
        Vector3 scale,
        Tilemap groundTilemap,
        Transform propsRoot,
        Transform portalRoot,
        Transform battleRoot)
    {
        var root = ContentRoot;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.Undo.RecordObject(root, "Apply Panel World Layout");
#endif
        root.position = worldAnchorPosition;
        root.localScale = scale;

        if (overrideWorldY)
            root.position = new Vector3(root.position.x, worldYOverride, root.position.z);

        if (groundTilemap != null)
        {
            var gridTransform = groundTilemap.layoutGrid != null
                ? groundTilemap.layoutGrid.transform
                : groundTilemap.transform.parent;

            if (gridTransform != null && gridTransform != root)
                ResetChildTransform(gridTransform);

            if (groundTilemap.transform != gridTransform)
                ResetChildTransform(groundTilemap.transform);
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.EditorUtility.SetDirty(root);
#endif
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

        if (localSize.x <= 0f || localSize.y <= 0f)
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
        var groundSurfaceLocalY = TryGetGroundSurfaceLocalY(groundTilemap, root, out var surfaceLocalY)
            ? surfaceLocalY
            : localMax.y;
        var targetSurfaceWorldY = DesktopOverlaySettings.ReferenceYToWorld(rect.y, camera);

        worldPosition = new Vector3(
            panelCenterX - uniform * localCenterX,
            targetSurfaceWorldY - uniform * groundSurfaceLocalY,
            0f);

        return true;
    }

    private static bool TryGetGroundSurfaceLocalY(
        Tilemap tilemap,
        Transform contentRoot,
        out float surfaceLocalY)
    {
        surfaceLocalY = 0f;
        if (tilemap == null || contentRoot == null)
            return false;

        tilemap.CompressBounds();
        var bounds = tilemap.cellBounds;
        if (bounds.size.x <= 0 || bounds.size.y <= 0)
            return false;

        var minY = bounds.yMin;
        var maxY = bounds.yMax;
        for (var y = maxY; y >= minY; y--)
        {
            var hasTile = false;
            for (var x = bounds.xMin; x <= bounds.xMax; x++)
            {
                if (!tilemap.HasTile(new Vector3Int(x, y, 0)))
                    continue;

                hasTile = true;
                break;
            }

            if (!hasTile)
                continue;

            var cellCenter = CellCenterToContentRootLocal(tilemap, contentRoot, new Vector3Int(bounds.xMin, y, 0));
            var halfCell = tilemap.layoutGrid != null
                ? tilemap.layoutGrid.cellSize.y * 0.5f
                : 0.5f;
            surfaceLocalY = cellCenter.y + halfCell;
            return true;
        }

        return false;
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

        if (tilemap == null || contentRoot == null)
            return false;

        tilemap.CompressBounds();

        var rendererBounds = tilemap.localBounds;
        if (rendererBounds.size.x > 0f && rendererBounds.size.y > 0f)
        {
            localMin = LocalPointInContentRoot(tilemap, contentRoot, rendererBounds.min);
            localMax = LocalPointInContentRoot(tilemap, contentRoot, rendererBounds.max);
            localSize = localMax - localMin;
            if (localSize.x > 0f && localSize.y > 0f)
                return true;
        }

        var cellBounds = tilemap.cellBounds;
        var hasTiles = false;
        var minX = float.PositiveInfinity;
        var minY = float.PositiveInfinity;
        var maxX = float.NegativeInfinity;
        var maxY = float.NegativeInfinity;

        foreach (var cell in cellBounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(cell))
                continue;

            var local = CellCenterToContentRootLocal(tilemap, contentRoot, cell);
            minX = Mathf.Min(minX, local.x);
            minY = Mathf.Min(minY, local.y);
            maxX = Mathf.Max(maxX, local.x);
            maxY = Mathf.Max(maxY, local.y);
            hasTiles = true;
        }

        if (!hasTiles)
            return false;

        var halfCell = tilemap.layoutGrid != null
            ? tilemap.layoutGrid.cellSize * 0.5f
            : Vector3.one * 0.5f;
        localMin = new Vector2(minX - halfCell.x, minY - halfCell.y);
        localMax = new Vector2(maxX + halfCell.x, maxY + halfCell.y);
        localSize = localMax - localMin;
        return localSize.x > 0f && localSize.y > 0f;
    }

    private static Vector2 LocalPointInContentRoot(
        Tilemap tilemap,
        Transform contentRoot,
        Vector3 pointInTilemapLocal)
    {
        var local = pointInTilemapLocal;
        var current = tilemap.transform;
        while (current != null && current != contentRoot)
        {
            local = current.localPosition + Vector3.Scale(current.localRotation * local, current.localScale);
            current = current.parent;
        }

        return (Vector2)local;
    }

    /// <summary>
    /// Cell center in content-root local space, independent of the root's current world transform.
    /// </summary>
    private static Vector2 CellCenterToContentRootLocal(Tilemap tilemap, Transform contentRoot, Vector3Int cell)
    {
        var local = (Vector3)tilemap.GetCellCenterLocal(cell);
        var current = tilemap.transform;
        while (current != null && current != contentRoot)
        {
            local = current.localPosition + Vector3.Scale(current.localRotation * local, current.localScale);
            current = current.parent;
        }

        return (Vector2)local;
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
