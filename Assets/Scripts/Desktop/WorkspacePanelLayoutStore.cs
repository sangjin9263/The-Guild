using UnityEngine;

public static class WorkspacePanelLayoutStore
{
    private const string Prefix = "WorkspacePanel_";

    public static bool TryLoad(string panelId, out WorkspacePanelRect rect)
    {
        rect = default;
        if (!PlayerPrefs.HasKey(Key(panelId, "x")))
            return false;

        rect = new WorkspacePanelRect(
            PlayerPrefs.GetFloat(Key(panelId, "x")),
            PlayerPrefs.GetFloat(Key(panelId, "y")),
            PlayerPrefs.GetFloat(Key(panelId, "w")),
            PlayerPrefs.GetFloat(Key(panelId, "h")));
        return true;
    }

    public static void Save(string panelId, WorkspacePanelRect rect)
    {
        PlayerPrefs.SetFloat(Key(panelId, "x"), rect.x);
        PlayerPrefs.SetFloat(Key(panelId, "y"), rect.y);
        PlayerPrefs.SetFloat(Key(panelId, "w"), rect.width);
        PlayerPrefs.SetFloat(Key(panelId, "h"), rect.height);
        PlayerPrefs.Save();
    }

    public static void ClearAll()
    {
        foreach (var panelId in new[]
                 {
                     WorkspacePanelId.Town,
                     WorkspacePanelId.UiZone,
                     WorkspacePanelId.DungeonSlot1,
                     WorkspacePanelId.DungeonSlot2,
                     WorkspacePanelId.DungeonSlot3
                 })
        {
            PlayerPrefs.DeleteKey(Key(panelId, "x"));
            PlayerPrefs.DeleteKey(Key(panelId, "y"));
            PlayerPrefs.DeleteKey(Key(panelId, "w"));
            PlayerPrefs.DeleteKey(Key(panelId, "h"));
        }

        PlayerPrefs.Save();
    }

    private static string Key(string panelId, string suffix) => Prefix + panelId + "_" + suffix;
}
