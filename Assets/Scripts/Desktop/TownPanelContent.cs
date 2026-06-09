using UnityEngine;
using UnityEngine.Tilemaps;

public class TownPanelContent : PanelWorldContent
{
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Transform propsRoot;
    [SerializeField] private Transform portalRoot;
    [SerializeField] private float portalOffsetX = 26f;
    [SerializeField] private float portalOffsetY = -3f;

    public Tilemap GroundTilemap => groundTilemap;

    public override void ApplyLayout(
        WorkspacePanelRect rect,
        float scaleReferenceHeight,
        Camera camera,
        bool editorPreview = false)
    {
        var center3 = RectCenterToWorld(rect, camera);
        var scale = editorPreview ? 1f : GetWorldScale(rect, scaleReferenceHeight, camera);
        ApplyContentRootLayout(
            center3,
            scale,
            groundTilemap,
            propsRoot,
            portalRoot,
            null,
            portalOffsetX,
            portalOffsetY);
    }
}
