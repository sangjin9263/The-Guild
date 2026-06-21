#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class BuildingPanelLayoutSetup
{
    private const string BuildingPanelPrefabPath = "Assets/Resources/Prefabs/UI/BuildingPanel.prefab";

    [MenuItem("The Guild/UI/Fix Building Panel Layout Scale")]
    public static void FixBuildingPanelLayoutFromMenu()
    {
        FixBuildingPanelPrefab();
        Debug.Log("[BuildingPanel] Layout scale fix applied to prefab.");
    }

    public static void FixBuildingPanelPrefab()
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(BuildingPanelPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"[BuildingPanel] Prefab not found at {BuildingPanelPrefabPath}");
            return;
        }

        var fit = prefabRoot.GetComponent<BuildingPanelLayoutFit>();
        if (fit == null)
            fit = prefabRoot.AddComponent<BuildingPanelLayoutFit>();

        fit.EnsureDesignRoot();

        var panelRect = prefabRoot.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.localScale = Vector3.one;
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, BuildingPanelPrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }
}
#endif
