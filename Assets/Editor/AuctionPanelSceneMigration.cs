#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Main.unity 경매 UI — AuctionPanel_re(shell) 또는 레거시 AuctionPanel.</summary>
public static class AuctionPanelSceneMigration
{
    private const string ScenePath = "Assets/Scenes/Main.unity";
    public const string AuctionPanelShellPrefabPath = "Assets/Resources/Prefabs/UI/AuctionPanel_re.prefab";
    private const string AuctionPanelLegacyPrefabPath = "Assets/Resources/Prefabs/UI/AuctionPanel.prefab";

    [MenuItem("The Guild/UI/Migrate Auction Panel Re (Shell)")]
    public static void MigrateShellFromMenu()
    {
        MigrateShell();
        Debug.Log("AuctionPanelSceneMigration: AuctionPanel_re shell migration complete.");
    }

    [MenuItem("Tools/Guild/Migrate Auction Panel To Prefab")]
    public static void MigrateLegacyFromMenu()
    {
        MigrateLegacy();
        Debug.Log("AuctionPanelSceneMigration: legacy AuctionPanel migration complete.");
    }

    public static void MigrateShell() => Migrate(AuctionPanelShellPrefabPath, runScrollSetup: false);

    public static void MigrateLegacy()
    {
        AuctionPanelScrollSetup.Setup();
        Migrate(AuctionPanelLegacyPrefabPath, runScrollSetup: false);
    }

    private static void Migrate(string prefabPath, bool runScrollSetup)
    {
        if (runScrollSetup)
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

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"AuctionPanelSceneMigration: prefab not found at {prefabPath}");
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, auctionZone);
        instance.name = "AuctionPanel";
        instance.SetActive(false);

        var panelRect = instance.GetComponent<RectTransform>();
        StretchFull(panelRect);

        WireAuctionZonePanelContent(panelRect);
        WireAuctionPanelUi(instance);
        EnsureAuctionPanelLayoutFit(instance);
        WireStartAuctionReModalOnInstance(instance);

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

        var closeButton = FindPanelChild(panelInstance.transform, "TitleBar/ButtonX")?.GetComponent<Button>();

        var serialized = new SerializedObject(auctionUi);
        serialized.FindProperty("panelRoot").objectReferenceValue = panelInstance;
        serialized.FindProperty("titleText").objectReferenceValue = null;
        serialized.FindProperty("bodyText").objectReferenceValue = null;
        serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void EnsureAuctionPanelLayoutFit(GameObject panelInstance)
    {
        var fit = panelInstance.GetComponent<AuctionPanelLayoutFit>();
        if (fit == null)
            fit = panelInstance.AddComponent<AuctionPanelLayoutFit>();

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

        var designRoot = panelRoot.Find(AuctionPanelLayoutFit.DesignRootName);
        return designRoot != null ? designRoot.Find(relativePath) : null;
    }

    private static void WireStartAuctionReModalOnInstance(GameObject panelInstance)
    {
        const string modalPath = "Assets/Resources/Prefabs/UI/start_auction_re.prefab";

        var legacy = panelInstance.transform.Find("start_auction");
        if (legacy != null)
            Object.DestroyImmediate(legacy.gameObject);

        var existing = panelInstance.transform.Find("start_auction_re");
        if (existing == null)
        {
            var modalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modalPath);
            if (modalPrefab == null)
            {
                Debug.LogWarning($"AuctionPanelSceneMigration: missing {modalPath}");
                return;
            }

            existing = ((GameObject)PrefabUtility.InstantiatePrefab(modalPrefab, panelInstance.transform)).transform;
            existing.name = "start_auction_re";
        }

        var rect = existing as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        existing.gameObject.SetActive(false);
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
