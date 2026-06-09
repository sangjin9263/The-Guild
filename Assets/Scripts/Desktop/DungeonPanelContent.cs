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
    [SerializeField] private float portalOffsetX = -3f;
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
        var center3 = RectCenterToWorld(rect, camera);
        var scale = editorPreview ? 1f : GetWorldScale(rect, scaleReferenceHeight, camera);
        ApplyContentRootLayout(
            center3,
            scale,
            groundTilemap,
            propsRoot,
            portalRoot,
            battleRoot,
            portalOffsetX,
            portalOffsetY);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
