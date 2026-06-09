using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonPanelContent : PanelWorldContent
{
    [SerializeField] private int slotIndex;
    [SerializeField] private string displayName = "Dungeon";
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Transform propsRoot;
    [SerializeField] private Transform portalRoot;
    [SerializeField] private Transform battleRoot;
    [Tooltip("Default for new portals in editor setup. Move Portal in the Scene to place it.")]
    [SerializeField] private float portalOffsetX = -3f;
    [Tooltip("Default for new portals in editor setup. Move Portal in the Scene to place it.")]
    [SerializeField] private float portalOffsetY = -3f;

    public int SlotIndex => slotIndex;
    public string DisplayName => displayName;
    public Tilemap GroundTilemap => groundTilemap;
    public Transform BattleRoot => battleRoot;

    public override void ApplyLayout(
        WorkspacePanelRect rect,
        float scaleReferenceHeight,
        Camera camera,
        bool editorPreview = false)
    {
        var designSize = new Vector2(
            DesktopOverlaySettings.DungeonSlotWidth,
            scaleReferenceHeight);

        ApplyPanelWorldLayout(
            rect,
            camera,
            designSize,
            editorPreview,
            groundTilemap,
            propsRoot,
            portalRoot,
            battleRoot);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
