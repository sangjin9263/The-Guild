using UnityEngine;

public class DungeonSlot : MonoBehaviour
{
    [SerializeField] private int slotIndex;
    [SerializeField] private string displayName = "Dungeon";
    [SerializeField] private Transform contentRoot;
    [SerializeField] private Transform groundRoot;
    [SerializeField] private Transform propsRoot;
    [SerializeField] private Transform portalRoot;
    [SerializeField] private Transform battleRoot;

    public int SlotIndex => slotIndex;
    public string DisplayName => displayName;
    public Transform ContentRoot => contentRoot != null ? contentRoot : transform;
    public Transform GroundRoot => groundRoot;
    public Transform PropsRoot => propsRoot;
    public Transform PortalRoot => portalRoot;
    public Transform BattleRoot => battleRoot;

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
