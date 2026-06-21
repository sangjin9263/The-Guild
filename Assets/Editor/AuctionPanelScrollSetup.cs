#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>경매 Lot_List 스크롤 구성 — Lot_prefab_ebay / Lot_prefab_eng 슬롯 배치.</summary>
public static class AuctionPanelScrollSetup
{
    private const string EbayLotPrefabPath = "Assets/Resources/Prefabs/UI/Lot_prefab_ebay.prefab";
    private const string EngLotPrefabPath = "Assets/Resources/Prefabs/UI/Lot_prefab_eng.prefab";
    private const string AuctionPanelPath = "Assets/Resources/Prefabs/UI/AuctionPanel.prefab";
    private const int LotCount = 10;
    private const float LotHeight = 170f;
    private const float LotSpacing = 5f;

    /// <summary>현재 풀 — Ebay/English 혼합. 슬롯 5+5 (Restore 메뉴).</summary>
    private const int EnglishLotSlotCount = 5;

    [MenuItem("The Guild/Data/Restore Auction Panel Lot Slots")]
    public static void RestoreLotSlotsFromMenu()
    {
        FixLotPrefabs();
        FixAuctionPanelScroll();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AuctionPanel] Restored {LotCount} lot slots ({LotCount - EnglishLotSlotCount} ebay, {EnglishLotSlotCount} eng).");
    }

    [MenuItem("Tools/Guild/Setup Auction Panel Scroll")]
    public static void SetupFromMenu()
    {
        Setup();
        Debug.Log("AuctionPanelScrollSetup: complete.");
    }

    public static void Setup()
    {
        FixLotPrefabs();
        FixAuctionPanelScroll();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void FixLotPrefabs()
    {
        FixEbayLotPrefab();
        FixEngLotPrefabLayout();
    }

    private static void FixEbayLotPrefab()
    {
        var lotRoot = PrefabUtility.LoadPrefabContents(EbayLotPrefabPath);
        if (lotRoot == null)
        {
            Debug.LogError($"AuctionPanelScrollSetup: missing {EbayLotPrefabPath}");
            return;
        }

        try
        {
            var userGold = lotRoot.transform.Find("ebay/Bid/start_bid/user_gold");
            if (userGold == null)
                userGold = lotRoot.transform.Find("Bid/start_bid/user_gold");
            if (userGold == null)
                userGold = lotRoot.transform.Find("ebay/Bid/user_gold");
            if (userGold == null)
                userGold = lotRoot.transform.Find("Bid/user_gold");
            if (userGold != null)
                userGold.name = "input_gold";

            var misnamedTime = lotRoot.transform.Find("ebay/Bid/input_gold");
            if (misnamedTime == null)
                misnamedTime = lotRoot.transform.Find("Bid/input_gold");
            if (misnamedTime != null && misnamedTime.GetComponentInChildren<TextMeshProUGUI>(true) != null)
            {
                foreach (var label in misnamedTime.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (label.text.Contains("남은") || label.text.Contains("00:00"))
                    {
                        misnamedTime.name = "time";
                        break;
                    }
                }
            }

            var energy = lotRoot.transform.Find("Gate_Info/Energy_Percent");
            if (energy != null)
                RemoveMisplacedGateHint(energy);

            ApplyLotCardLayout(lotRoot);
            PrefabUtility.SaveAsPrefabAsset(lotRoot, EbayLotPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(lotRoot);
        }
    }

    private static void FixEngLotPrefabLayout()
    {
        var lotRoot = PrefabUtility.LoadPrefabContents(EngLotPrefabPath);
        if (lotRoot == null)
        {
            Debug.LogError($"AuctionPanelScrollSetup: missing {EngLotPrefabPath}");
            return;
        }

        try
        {
            if (lotRoot.GetComponent<EnglishAuctionLotRowView>() == null)
                lotRoot.AddComponent<EnglishAuctionLotRowView>();

            ApplyLotCardLayout(lotRoot);
            PrefabUtility.SaveAsPrefabAsset(lotRoot, EngLotPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(lotRoot);
        }
    }

    private static void ApplyLotCardLayout(GameObject lotRoot)
    {
        var rootRect = lotRoot.GetComponent<RectTransform>();
        if (rootRect != null)
        {
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.sizeDelta = new Vector2(0f, LotHeight);
            rootRect.anchoredPosition = Vector2.zero;
        }

        var layoutElement = lotRoot.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = lotRoot.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = LotHeight;
        layoutElement.minHeight = LotHeight;
        layoutElement.flexibleWidth = 1f;
    }

    private static void RemoveMisplacedGateHint(Transform energyParent)
    {
        var misplaced = energyParent.Find("gate_hint");
        if (misplaced != null)
            Object.DestroyImmediate(misplaced.gameObject);
    }

    private static void FixAuctionPanelScroll()
    {
        var panelRoot = PrefabUtility.LoadPrefabContents(AuctionPanelPath);
        if (panelRoot == null)
        {
            Debug.LogError($"AuctionPanelScrollSetup: missing {AuctionPanelPath}");
            return;
        }

        try
        {
            var lotList = panelRoot.transform.Find("Content/Lot_List") as RectTransform;
            if (lotList == null)
            {
                Debug.LogError("AuctionPanelScrollSetup: Content/Lot_List not found.");
                return;
            }

            var ebayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EbayLotPrefabPath);
            var engPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EngLotPrefabPath);
            if (ebayPrefab == null)
            {
                Debug.LogError($"AuctionPanelScrollSetup: missing {EbayLotPrefabPath}");
                return;
            }

            if (EnglishLotSlotCount > 0 && engPrefab == null)
            {
                Debug.LogError($"AuctionPanelScrollSetup: missing {EngLotPrefabPath}");
                return;
            }

            var scrollbar = lotList.Find("Scrollbar") as RectTransform;
            if (scrollbar == null)
                scrollbar = CreateVerticalScrollbar(lotList);

            ConfigureScrollbar(scrollbar, lotList);

            var viewport = lotList.Find("LotViewport") as RectTransform;
            if (viewport == null)
            {
                var viewportGo = new GameObject("LotViewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
                viewportGo.transform.SetParent(lotList, false);
                viewportGo.layer = lotList.gameObject.layer;
                viewport = viewportGo.GetComponent<RectTransform>();

                var viewportImage = viewportGo.GetComponent<Image>();
                viewportImage.color = new Color(1f, 1f, 1f, 0f);
                viewportImage.raycastTarget = false;
            }

            StretchToParent(viewport, lotList, rightPadding: 32f);

            var content = viewport.Find("LotContent") as RectTransform;
            if (content == null)
            {
                var contentGo = new GameObject("LotContent", typeof(RectTransform));
                contentGo.transform.SetParent(viewport, false);
                contentGo.layer = lotList.gameObject.layer;
                content = contentGo.GetComponent<RectTransform>();
            }

            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            var layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = LotSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(0, 0, 0, 0);

            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ClearLotListChildren(lotList, viewport, scrollbar, content);

            var engSlots = Mathf.Clamp(EnglishLotSlotCount, 0, LotCount);
            var ebaySlots = LotCount - engSlots;

            for (var i = 0; i < LotCount; i++)
            {
                var useEng = i >= ebaySlots;
                var prefab = useEng ? engPrefab : ebayPrefab;
                var suffix = useEng ? "eng" : "ebay";
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, content);
                instance.name = $"Lot_{i + 1}_{suffix}";
            }

            var scrollRect = lotList.GetComponent<ScrollRect>();
            if (scrollRect == null)
                scrollRect = lotList.gameObject.AddComponent<ScrollRect>();
            scrollRect.content = content;
            scrollRect.viewport = viewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;
            scrollRect.verticalScrollbar = scrollbar.GetComponent<Scrollbar>();
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            EnsureStartAuctionModalChild(panelRoot);

            var lotListImage = lotList.GetComponent<Image>();
            if (lotListImage != null)
                lotListImage.raycastTarget = false;

            PrefabUtility.SaveAsPrefabAsset(panelRoot, AuctionPanelPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(panelRoot);
        }
    }

    private static void ClearLotListChildren(RectTransform lotList, RectTransform viewport, RectTransform scrollbar, RectTransform content)
    {
        for (var i = lotList.childCount - 1; i >= 0; i--)
        {
            var child = lotList.GetChild(i);
            if (child == viewport || child == scrollbar)
                continue;
            Object.DestroyImmediate(child.gameObject);
        }

        for (var i = content.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(content.GetChild(i).gameObject);
    }

    private static RectTransform CreateVerticalScrollbar(RectTransform lotList)
    {
        var scrollbarGo = new GameObject("Scrollbar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
        scrollbarGo.transform.SetParent(lotList, false);
        scrollbarGo.layer = lotList.gameObject.layer;

        var scrollbarRect = scrollbarGo.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 0.5f);
        scrollbarRect.sizeDelta = new Vector2(30f, 0f);
        scrollbarRect.anchoredPosition = new Vector2(-5f, 0f);

        var bg = scrollbarGo.GetComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);

        var slidingAreaGo = new GameObject("Sliding Area", typeof(RectTransform));
        slidingAreaGo.transform.SetParent(scrollbarGo.transform, false);
        var slidingRect = slidingAreaGo.GetComponent<RectTransform>();
        StretchFill(slidingRect, 10f, 10f);

        var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handleGo.transform.SetParent(slidingAreaGo.transform, false);
        var handleRect = handleGo.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20f, 20f);
        var handleImage = handleGo.GetComponent<Image>();
        handleImage.color = Color.white;

        var scrollbar = scrollbarGo.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;

        return scrollbarRect;
    }

    private static void ConfigureScrollbar(RectTransform scrollbar, RectTransform lotList)
    {
        scrollbar.SetParent(lotList, false);
        scrollbar.anchorMin = new Vector2(1f, 0f);
        scrollbar.anchorMax = new Vector2(1f, 1f);
        scrollbar.pivot = new Vector2(1f, 0.5f);
        scrollbar.sizeDelta = new Vector2(30f, 0f);
        scrollbar.anchoredPosition = new Vector2(-5f, 0f);
    }

    private static void StretchToParent(RectTransform target, RectTransform parent, float rightPadding)
    {
        target.SetParent(parent, false);
        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.one;
        target.pivot = new Vector2(0.5f, 0.5f);
        target.offsetMin = Vector2.zero;
        target.offsetMax = new Vector2(-rightPadding, 0f);
    }

    private static void StretchFill(RectTransform target, float horizontalPadding, float verticalPadding)
    {
        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.one;
        target.offsetMin = new Vector2(horizontalPadding, verticalPadding);
        target.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
    }

    private static void EnsureStartAuctionModalChild(GameObject panelRoot)
    {
        const string modalPath = "Assets/Resources/Prefabs/UI/start_auction_re.prefab";
        const string modalName = "start_auction_re";

        var legacy = panelRoot.transform.Find("start_auction");
        if (legacy != null)
            Object.DestroyImmediate(legacy.gameObject);

        if (panelRoot.transform.Find(modalName) != null)
            return;

        var modalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modalPath);
        if (modalPrefab == null)
        {
            Debug.LogWarning($"AuctionPanelScrollSetup: missing {modalPath}");
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(modalPrefab, panelRoot.transform);
        instance.name = modalName;
        var rect = instance.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        instance.SetActive(false);
    }
}
#endif
