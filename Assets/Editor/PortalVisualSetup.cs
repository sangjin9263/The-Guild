using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PortalVisualSetup
{
    private const string PortalPrefabPath = "Assets/Resources/Dimensional_Portal_0.prefab";

    [MenuItem("The Guild/Map/Setup Portal Visual")]
    public static void SetupPortalVisual()
    {
        NormalizeAllPanelPortals();
        WirePortalsInOpenScene();
        AssetDatabase.SaveAssets();
        Debug.Log("Portal visuals wired in the active scene.");
    }

    [MenuItem("The Guild/Map/Normalize Panel Portals")]
    public static void NormalizePanelPortalsMenu()
    {
        NormalizeAllPanelPortals();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Panel portal references wired. Portal/Visual transforms are left as authored in the Scene.");
    }

    public static void NormalizeAllPanelPortals()
    {
        var scene = EditorSceneManager.GetActiveScene();

        foreach (var town in Object.FindObjectsByType<TownPanelContent>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            var portal = ResolveTownPortal(town.transform);
            WireTownPortalReference(town, portal);
        }

        foreach (var dungeon in Object.FindObjectsByType<DungeonPanelContent>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            var portal = ResolveDungeonPortal(dungeon.transform);
            WireDungeonPortalReference(dungeon, portal);
        }

        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);
    }

    public static Transform ResolveTownPortal(Transform contentRoot)
    {
        var portals = contentRoot.Find("Portals");
        if (portals == null)
            portals = new GameObject("Portals").transform;
        portals.SetParent(contentRoot, false);

        var portal = portals.Find("VillagePortal");
        if (portal == null)
        {
            var portalObject = new GameObject("VillagePortal");
            portal = portalObject.transform;
            portal.SetParent(portals, false);
            portal.localPosition = new Vector3(
                DesktopOverlaySettings.VillagePortalX,
                DesktopOverlaySettings.PortalY,
                0f);
        }

        EnsurePortalVisual(portal);
        return portal;
    }

    public static Transform ResolveDungeonPortal(Transform contentRoot)
    {
        var portals = contentRoot.Find("Portals");
        if (portals == null)
        {
            var portalsObject = new GameObject("Portals");
            portals = portalsObject.transform;
            portals.SetParent(contentRoot, false);
        }

        var loosePortal = FindLoosePortalChild(contentRoot);
        var portal = portals.Find("Portal");

        if (portal == null && loosePortal != null)
        {
            loosePortal.SetParent(portals, false);
            loosePortal.name = "Portal";
            portal = loosePortal;
        }
        else if (portal == null)
        {
            var portalObject = new GameObject("Portal");
            portal = portalObject.transform;
            portal.SetParent(portals, false);
            portal.localPosition = new Vector3(
                DesktopOverlaySettings.BattlePortalX,
                DesktopOverlaySettings.PortalY,
                0f);
        }
        else if (loosePortal != null && loosePortal != portal)
        {
            Object.DestroyImmediate(loosePortal.gameObject);
        }

        CleanupDuplicateLoosePortals(contentRoot, portal);
        EnsurePortalVisual(portal);
        return portal;
    }

    private static Transform FindLoosePortalChild(Transform contentRoot)
    {
        for (var i = 0; i < contentRoot.childCount; i++)
        {
            var child = contentRoot.GetChild(i);
            if (child.name == "Portal")
                return child;
        }

        return null;
    }

    private static void CleanupDuplicateLoosePortals(Transform contentRoot, Transform canonicalPortal)
    {
        for (var i = contentRoot.childCount - 1; i >= 0; i--)
        {
            var child = contentRoot.GetChild(i);
            if (child.name != "Portal" || child == canonicalPortal)
                continue;

            Object.DestroyImmediate(child.gameObject);
        }
    }

    public static void EnsurePortalVisual(Transform portalRoot)
    {
        if (portalRoot == null || portalRoot.Find("Visual") != null)
            return;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PortalPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"Portal prefab not found: {PortalPrefabPath}");
            return;
        }

        var visualObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, portalRoot);
        visualObject.name = "Visual";
        var visual = visualObject.transform;
        visual.localPosition = Vector3.zero;
        visual.localRotation = Quaternion.identity;
        visual.localScale = Vector3.one * DesktopOverlaySettings.PortalVisualScale;
    }

    private static void WireTownPortalReference(TownPanelContent content, Transform portal)
    {
        var serialized = new SerializedObject(content);
        serialized.FindProperty("portalRoot").objectReferenceValue = portal;
        serialized.FindProperty("portalOffsetX").floatValue = DesktopOverlaySettings.VillagePortalX;
        serialized.FindProperty("portalOffsetY").floatValue = DesktopOverlaySettings.PortalY;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireDungeonPortalReference(DungeonPanelContent content, Transform portal)
    {
        var serialized = new SerializedObject(content);
        serialized.FindProperty("portalRoot").objectReferenceValue = portal;
        serialized.FindProperty("portalOffsetX").floatValue = DesktopOverlaySettings.BattlePortalX;
        serialized.FindProperty("portalOffsetY").floatValue = DesktopOverlaySettings.PortalY;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WirePortalsInOpenScene()
    {
        var portalsRoot = GameObject.Find("Portals");
        if (portalsRoot == null)
            return;

        EnsurePortalVisual(portalsRoot.transform.Find("BattlePortal"));
        EnsurePortalVisual(portalsRoot.transform.Find("VillagePortal"));
        EditorSceneManager.MarkSceneDirty(portalsRoot.scene);
    }
}
