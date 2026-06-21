#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Main.unity 길드 UI를 BuildingPanel.prefab 기준으로 연결합니다.</summary>
public static class BuildingPanelSceneMigration
{
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private const string BuildingPanelPrefabPath = "Assets/Resources/Prefabs/UI/BuildingPanel.prefab";

    [MenuItem("The Guild/UI/Migrate Building Panel To Prefab")]
    public static void MigrateFromMenu()
    {
        Migrate();
        Debug.Log("[BuildingPanel] Scene migration complete.");
    }

    public static void Migrate()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var uiZonePanel = GameObject.Find("UiZonePanel")?.transform;
        if (uiZonePanel == null)
        {
            Debug.LogError("[BuildingPanel] UiZonePanel not found.");
            return;
        }

        RemoveExistingBuildingPanels(uiZonePanel);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildingPanelPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[BuildingPanel] Prefab not found at {BuildingPanelPrefabPath}");
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, uiZonePanel);
        instance.name = "BuildingPanel";
        instance.SetActive(false);
        StretchFull(instance.GetComponent<RectTransform>());

        WireUiZonePanelContent(instance);
        WireBuildingPanelUi(instance);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void RemoveExistingBuildingPanels(Transform uiZonePanel)
    {
        for (var i = uiZonePanel.childCount - 1; i >= 0; i--)
        {
            var child = uiZonePanel.GetChild(i);
            if (child.name == "BuildingPanel")
                Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void WireUiZonePanelContent(GameObject panelInstance)
    {
        var zoneContent = Object.FindFirstObjectByType<UiZonePanelContent>();
        if (zoneContent == null)
        {
            Debug.LogWarning("[BuildingPanel] UiZonePanelContent not found.");
            return;
        }

        var panelRect = panelInstance.GetComponent<RectTransform>();
        var serialized = new SerializedObject(zoneContent);
        serialized.FindProperty("panelRoot").objectReferenceValue = panelRect;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void WireBuildingPanelUi(GameObject panelInstance)
    {
        var buildingUi = Object.FindFirstObjectByType<BuildingPanelUI>();
        if (buildingUi == null)
        {
            Debug.LogWarning("[BuildingPanel] BuildingPanelUI not found on GameWorkspace.");
            return;
        }

        EnsureGuildPanelController(panelInstance);
        EnsureBuildingPanelLayoutFit(panelInstance);

        var guildNameText = FindPanelChild(panelInstance.transform, "TitleBar/Name")?.GetComponent<TextMeshProUGUI>();
        var closeButton = FindPanelChild(panelInstance.transform, "TitleBar/exit")?.GetComponent<Button>();

        var serialized = new SerializedObject(buildingUi);
        serialized.FindProperty("panelRoot").objectReferenceValue = panelInstance;
        serialized.FindProperty("guildNameText").objectReferenceValue = guildNameText;
        serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void EnsureGuildPanelController(GameObject panelInstance)
    {
        if (panelInstance.GetComponent<GuildPanelController>() == null)
            panelInstance.AddComponent<GuildPanelController>();
    }

    public static void EnsureBuildingPanelLayoutFit(GameObject panelInstance)
    {
        var fit = panelInstance.GetComponent<BuildingPanelLayoutFit>();
        if (fit == null)
            fit = panelInstance.AddComponent<BuildingPanelLayoutFit>();

        fit.EnsureDesignRoot();
        fit.ApplyFit();
    }

    private static Transform FindPanelChild(Transform panelRoot, string relativePath)
    {
        if (panelRoot == null)
            return null;

        var direct = panelRoot.Find(relativePath);
        if (direct != null)
            return direct;

        var designRoot = panelRoot.Find(BuildingPanelLayoutFit.DesignRootName);
        return designRoot != null ? designRoot.Find(relativePath) : null;
    }

    private static void StretchFull(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
#endif
