#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class AuctionPanelLayoutSetup
{
    private const string AuctionPanelShellPrefabPath = AuctionPanelSceneMigration.AuctionPanelShellPrefabPath;
    private const string AuctionPanelLegacyPrefabPath = "Assets/Resources/Prefabs/UI/AuctionPanel.prefab";
    private const string StartAuctionRePrefabPath = "Assets/Resources/Prefabs/UI/start_auction_re.prefab";

    [MenuItem("The Guild/UI/Wire Start Auction Re Modal")]
    public static void WireStartAuctionReModalFromMenu()
    {
        WireStartAuctionReModal(AuctionPanelShellPrefabPath);
        Debug.Log("[AuctionPanel] start_auction_re wired into AuctionPanel_re prefab.");
    }

    [MenuItem("The Guild/UI/Fix Auction Panel Re Layout Scale")]
    public static void FixAuctionPanelShellLayoutFromMenu()
    {
        FixAuctionPanelPrefab(AuctionPanelShellPrefabPath);
        Debug.Log("[AuctionPanel] AuctionPanel_re layout scale fix applied.");
    }

    [MenuItem("The Guild/UI/Fix Auction Panel Layout Scale")]
    public static void FixAuctionPanelLegacyLayoutFromMenu()
    {
        FixAuctionPanelPrefab(AuctionPanelLegacyPrefabPath);
        Debug.Log("[AuctionPanel] Legacy AuctionPanel layout scale fix applied.");
    }

    public static void FixAuctionPanelShellPrefab()
    {
        FixAuctionPanelPrefab(AuctionPanelShellPrefabPath);
        WireStartAuctionReModal(AuctionPanelShellPrefabPath);
        AuctionResultPanelSetup.WireAuctionResultPanels(AuctionPanelShellPrefabPath);
    }

    private static void FixAuctionPanelPrefab(string prefabPath)
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"[AuctionPanel] Prefab not found at {prefabPath}");
            return;
        }

        var fit = prefabRoot.GetComponent<AuctionPanelLayoutFit>();
        if (fit == null)
            fit = prefabRoot.AddComponent<AuctionPanelLayoutFit>();

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

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    private static void WireStartAuctionReModal(string panelPrefabPath)
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(panelPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"[AuctionPanel] Prefab not found at {panelPrefabPath}");
            return;
        }

        var legacy = prefabRoot.transform.Find("start_auction");
        if (legacy != null)
            Object.DestroyImmediate(legacy.gameObject);

        var existing = prefabRoot.transform.Find("start_auction_re");
        if (existing == null)
        {
            var modalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StartAuctionRePrefabPath);
            if (modalPrefab == null)
            {
                Debug.LogError($"[AuctionPanel] Missing modal prefab at {StartAuctionRePrefabPath}");
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(modalPrefab, prefabRoot.transform);
            instance.name = "start_auction_re";
            StretchModalOverlay(instance.GetComponent<RectTransform>());
            instance.SetActive(false);
        }
        else
        {
            existing.name = "start_auction_re";
            StretchModalOverlay(existing as RectTransform);
            existing.gameObject.SetActive(false);
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, panelPrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    private static void StretchModalOverlay(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
#endif
