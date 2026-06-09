using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class PanelWorldContent : MonoBehaviour
{
    [SerializeField] private Transform contentRoot;

    public Transform ContentRoot => contentRoot != null ? contentRoot : transform;

    public abstract void ApplyLayout(
        WorkspacePanelRect rect,
        float scaleReferenceHeight,
        Camera camera,
        bool editorPreview = false);

    protected void ApplyContentRootLayout(
        Vector3 center,
        float scale,
        Tilemap groundTilemap,
        Transform propsRoot,
        Transform portalRoot,
        Transform battleRoot,
        float portalOffsetX,
        float portalOffsetY)
    {
        var root = ContentRoot;
        root.position = center;
        root.localScale = Vector3.one * scale;

        if (groundTilemap != null)
        {
            if (groundTilemap.transform.parent != null && groundTilemap.transform.parent != root)
                ResetChildTransform(groundTilemap.transform.parent);

            ResetChildTransform(groundTilemap.transform);
        }

        ResetChildTransform(propsRoot);
        ResetChildTransform(battleRoot);

        if (portalRoot != null)
        {
            if (portalRoot.parent != null && portalRoot.parent != root)
                ResetChildTransform(portalRoot.parent);

            portalRoot.localPosition = new Vector3(portalOffsetX, portalOffsetY, 0f);
        }
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

    protected static float GetWorldScale(
        WorkspacePanelRect rect,
        float scaleReferenceHeight,
        Camera camera)
    {
        if (scaleReferenceHeight <= 0f)
            return 1f;

        var panelWorldHeight = DesktopOverlaySettings.GetPanelWorldHeight(rect, camera);
        var referenceWorldHeight = DesktopOverlaySettings.GetPanelWorldHeight(
            new WorkspacePanelRect(0f, 0f, rect.width, scaleReferenceHeight),
            camera);
        if (referenceWorldHeight <= 0f)
            return DesktopOverlaySettings.GetPanelWorldScale(rect, scaleReferenceHeight);

        return panelWorldHeight / referenceWorldHeight;
    }
}
