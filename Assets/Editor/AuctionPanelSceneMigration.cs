#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Main.unity 경매 UI를 Resources/AuctionPanel.prefab + Lot_prefab 기준으로 통일.</summary>
public static class AuctionPanelSceneMigration
{
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private const string AuctionPanelPrefabPath = "Assets/Resources/Prefabs/UI/AuctionPanel.prefab";

    [MenuItem("Tools/Guild/Migrate Auction Panel To Prefab")]
    public static void MigrateFromMenu()
    {
        Migrate();
        Debug.Log("AuctionPanelSceneMigration: complete.");
    }

    public static void Migrate()
    {
        AuctionPanelScrollSetup.Setup();

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var auctionZone = GameObject.Find("AuctionZonePanel")?.transform;
        if (auctionZone == null)
        {
            Debug.LogError("AuctionPanelSceneMigration: AuctionZonePanel not found.");
            return;
        }

        RemoveExistingAuctionPanels(auctionZone);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AuctionPanelPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"AuctionPanelSceneMigration: prefab not found at {AuctionPanelPrefabPath}");
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, auctionZone);
        instance.name = "AuctionPanel";
        instance.SetActive(false);

        var panelRect = instance.GetComponent<RectTransform>();
        StretchFull(panelRect);

        WireAuctionZonePanelContent(panelRect);
        WireAuctionPanelUi(instance);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void RemoveExistingAuctionPanels(Transform auctionZone)
    {
        for (var i = auctionZone.childCount - 1; i >= 0; i--)
        {
            var child = auctionZone.GetChild(i);
            if (child.name != "AuctionPanel")
                continue;

            Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void WireAuctionZonePanelContent(RectTransform panelRect)
    {
        var zoneContent = Object.FindFirstObjectByType<AuctionZonePanelContent>();
        if (zoneContent == null)
        {
            Debug.LogWarning("AuctionPanelSceneMigration: AuctionZonePanelContent not found.");
            return;
        }

        var serialized = new SerializedObject(zoneContent);
        serialized.FindProperty("panelRoot").objectReferenceValue = panelRect;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireAuctionPanelUi(GameObject panelInstance)
    {
        var auctionUi = Object.FindFirstObjectByType<AuctionPanelUI>();
        if (auctionUi == null)
        {
            Debug.LogWarning("AuctionPanelSceneMigration: AuctionPanelUI not found.");
            return;
        }

        var closeButton = panelInstance.transform.Find("TitleBar/ButtonX")?.GetComponent<Button>();

        var serialized = new SerializedObject(auctionUi);
        serialized.FindProperty("panelRoot").objectReferenceValue = panelInstance;
        serialized.FindProperty("titleText").objectReferenceValue = null;
        serialized.FindProperty("bodyText").objectReferenceValue = null;
        serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
        serialized.ApplyModifiedPropertiesWithoutUndo();
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
