using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PortalVisualSetup
{
    private const string PortalPrefabPath = "Assets/Resources/Dimensional_Portal_0.prefab";

    [MenuItem("The Guild/Map/Setup Portal Visual")]
    public static void SetupPortalVisual()
    {
        WirePortalsInOpenScene();
        AssetDatabase.SaveAssets();
        Debug.Log("Portal visuals wired in the active scene.");
    }

    public static void EnsurePortalVisual(Transform portalRoot)
    {
        if (portalRoot == null)
            return;

        if (portalRoot.Find("Visual") != null)
            return;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PortalPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"Portal prefab not found: {PortalPrefabPath}");
            return;
        }

        var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, portalRoot);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * DesktopOverlaySettings.PortalVisualScale;
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
