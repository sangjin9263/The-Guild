using UnityEngine;
using UnityEngine.Tilemaps;

public class TownPanelContent : PanelWorldContent
{
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Transform propsRoot;
    [SerializeField] private Transform portalRoot;
    [Tooltip("Default for new portals in editor setup. Move Portal in the Scene to place it.")]
    [SerializeField] private float portalOffsetX = -10f;
    [Tooltip("Default for new portals in editor setup. Move Portal in the Scene to place it.")]
    [SerializeField] private float portalOffsetY = -3.36f;

    public Tilemap GroundTilemap => groundTilemap;

    public override void ApplyLayout(
        WorkspacePanelRect rect,
        float scaleReferenceHeight,
        Camera camera,
        bool editorPreview = false)
    {
        var designSize = new Vector2(
            DesktopOverlaySettings.TownPanelWidth,
            DesktopOverlaySettings.TownPanelHeight);

        ApplyPanelWorldLayout(
            rect,
            camera,
            designSize,
            editorPreview,
            groundTilemap,
            propsRoot,
            portalRoot);
    }
}
