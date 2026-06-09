public static class WorkspacePanelId
{
    public const string Town = "Town";
    public const string UiZone = "UiZone";
    public const string DungeonSlot1 = "DungeonSlot_1";
    public const string DungeonSlot2 = "DungeonSlot_2";
    public const string DungeonSlot3 = "DungeonSlot_3";

    public static string GetDungeonSlotId(int slotIndex) => slotIndex switch
    {
        0 => DungeonSlot1,
        1 => DungeonSlot2,
        2 => DungeonSlot3,
        _ => DungeonSlot1
    };
}
