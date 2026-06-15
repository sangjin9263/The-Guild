using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PortalVisualSetup
{
    private const string PortalPrefabPath = "Assets/Resources/Prefabs/Dimensional_Portal_2.prefab";

    public static void SetupPortalVisual()
    {
        ReplaceAllPortalVisualsInScene();
        NormalizeAllPanelPortals();
        WirePortalsInOpenScene();
        AssetDatabase.SaveAssets();
        Debug.Log("Portal visuals wired in the active scene.");
    }

    public static void NormalizePanelPortalsMenu()
    {
        ReplaceAllPortalVisualsInScene();
        NormalizeAllPanelPortals();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Panel portal references wired. Portal/Visual transforms use Dimensional_Portal_2.");
    }

    public static void ReplaceAllPortalVisualsInScene()
    {
        var replaced = 0;

        foreach (var town in Object.FindObjectsByType<TownPanelContent>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
            replaced += ReplacePortalVisual(ResolveTownPortal(town.transform, installVisual: false)) ? 1 : 0;

        foreach (var dungeon in Object.FindObjectsByType<DungeonPanelContent>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
            replaced += ReplacePortalVisual(ResolveDungeonPortal(dungeon.transform, installVisual: false)) ? 1 : 0;

        var legacyRoot = GameObject.Find("Portals");
        if (legacyRoot != null)
        {
            for (var i = 0; i < legacyRoot.transform.childCount; i++)
            {
                if (ReplacePortalVisual(legacyRoot.transform.GetChild(i)))
                    replaced++;
            }
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log($"[PortalVisual] Replaced {replaced} portal visual(s) with Dimensional_Portal_2 at scale {DesktopOverlaySettings.PortalVisualScale}.");
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

    public static Transform ResolveTownPortal(Transform contentRoot) =>
        ResolveTownPortal(contentRoot, installVisual: true);

    private static Transform ResolveTownPortal(Transform contentRoot, bool installVisual)
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
                TownPanelLayout.PortalLocalX,
                TownPanelLayout.PortalLocalY,
                0f);
        }

        if (installVisual)
            EnsurePortalVisual(portal);

        return portal;
    }

    public static Transform ResolveDungeonPortal(Transform contentRoot) =>
        ResolveDungeonPortal(contentRoot, installVisual: true);

    private static Transform ResolveDungeonPortal(Transform contentRoot, bool installVisual)
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

        if (installVisual)
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

        InstallPortalVisual(portalRoot, GetPortalVisualScale(portalRoot));
    }

    public static bool ReplacePortalVisual(Transform portalRoot)
    {
        if (portalRoot == null)
            return false;

        var existing = portalRoot.Find("Visual");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        return InstallPortalVisual(portalRoot, GetPortalVisualScale(portalRoot));
    }

    private static Vector3 GetPortalVisualScale(Transform portalRoot)
    {
        if (IsTownPortal(portalRoot))
            return TownPanelLayout.PortalVisualScale;

        return Vector3.one * DesktopOverlaySettings.PortalVisualScale;
    }

    private static bool IsTownPortal(Transform portalRoot)
    {
        if (portalRoot == null)
            return false;

        if (portalRoot.name == "VillagePortal")
            return true;

        return portalRoot.GetComponentInParent<TownPanelContent>() != null;
    }

    private static bool InstallPortalVisual(Transform portalRoot, Vector3 scale)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PortalPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"Portal prefab not found: {PortalPrefabPath}");
            return false;
        }

        var visualObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, portalRoot);
        visualObject.name = "Visual";
        var visual = visualObject.transform;
        visual.localPosition = Vector3.zero;
        visual.localRotation = Quaternion.identity;
        visual.localScale = scale;
        return true;
    }

    private static void WireTownPortalReference(TownPanelContent content, Transform portal)
    {
        var serialized = new SerializedObject(content);
        serialized.FindProperty("portalRoot").objectReferenceValue = portal;
        serialized.FindProperty("portalOffsetX").floatValue = TownPanelLayout.PortalLocalX;
        serialized.FindProperty("portalOffsetY").floatValue = TownPanelLayout.PortalLocalY;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        portal.localPosition = new Vector3(
            TownPanelLayout.PortalLocalX,
            TownPanelLayout.PortalLocalY,
            0f);
    }

    private static void WireDungeonPortalReference(DungeonPanelContent content, Transform portal)
    {
        var serialized = new SerializedObject(content);
        serialized.FindProperty("portalRoot").objectReferenceValue = portal;
        serialized.FindProperty("portalOffsetX").floatValue =
            content.SlotIndex == 2
                ? DesktopOverlaySettings.PrimaryDungeonPortalLocalX
                : DesktopOverlaySettings.BattlePortalX;
        serialized.FindProperty("portalOffsetY").floatValue =
            content.SlotIndex == 2
                ? DesktopOverlaySettings.PrimaryDungeonPortalLocalY
                : DesktopOverlaySettings.PortalY;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        portal.localPosition = new Vector3(
            content.SlotIndex == 2
                ? DesktopOverlaySettings.PrimaryDungeonPortalLocalX
                : DesktopOverlaySettings.BattlePortalX,
            content.SlotIndex == 2
                ? DesktopOverlaySettings.PrimaryDungeonPortalLocalY
                : DesktopOverlaySettings.PortalY,
            0f);
    }

    private static void WirePortalsInOpenScene()
    {
        var portalsRoot = GameObject.Find("Portals");
        if (portalsRoot == null)
            return;

        ReplacePortalVisual(portalsRoot.transform.Find("BattlePortal"));
        ReplacePortalVisual(portalsRoot.transform.Find("VillagePortal"));
        EditorSceneManager.MarkSceneDirty(portalsRoot.scene);
    }
}
