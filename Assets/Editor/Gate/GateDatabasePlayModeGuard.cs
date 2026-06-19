using UnityEditor;
using UnityEngine;

/// <summary>
/// Re-imports GateDatabase when entering Play Mode if auction rows were never populated
/// (e.g. after renaming ebayAuctions → auctions without running Import Gateinfo).
/// </summary>
[InitializeOnLoad]
internal static class GateDatabasePlayModeGuard
{
    static GateDatabasePlayModeGuard()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        var db = AssetDatabase.LoadAssetAtPath<GateDatabase>(GateDatabase.DefaultAssetPath);
        if (db == null || (db.auctions != null && db.auctions.Count > 0))
            return;

        Debug.LogWarning("[GateImport] GateDatabase.auctions is empty — auto-importing Gateinfo before Play Mode.");
        GateDatabaseImporter.Import(writeAsset: true);
    }
}
