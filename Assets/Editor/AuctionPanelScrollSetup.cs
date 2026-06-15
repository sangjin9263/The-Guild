#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>경매 Lot_List 스크롤 구성 및 Lot_prefab 정리 (gate_hint, input_gold, 10칸).</summary>
public static class AuctionPanelScrollSetup
{
    private const string LotPrefabPath = "Assets/Resources/Prefabs/UI/Lot_prefab.prefab";
    private const string AuctionPanelPath = "Assets/Resources/Prefabs/UI/AuctionPanel.prefab";
    private const int LotCount = 10;
    private const float LotHeight = 170f;
    private const float LotSpacing = 5f;
    private const float LotWidth = 910f;

    [MenuItem("Tools/Guild/Setup Auction Panel Scroll")]
    public static void SetupFromMenu()
    {
        Setup();
        Debug.Log("AuctionPanelScrollSetup: complete.");
    }

    public static void Setup()
    {
        FixLotPrefab();
        FixAuctionPanelScroll();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void FixLotPrefab()
    {
        var lotRoot = PrefabUtility.LoadPrefabContents(LotPrefabPath);
        try
        {
            var userGold = lotRoot.transform.Find("Bid/start_bid/user_gold");
            if (userGold == null)
                userGold = lotRoot.transform.Find("Bid/user_gold");
            if (userGold != null)
                userGold.name = "input_gold";

            var misnamedTime = lotRoot.transform.Find("Bid/input_gold");
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
                EnsureGateHint(energy);

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

            PrefabUtility.SaveAsPrefabAsset(lotRoot, LotPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(lotRoot);
        }
    }

    private static void EnsureGateHint(Transform energyParent)
    {
        var existing = energyParent.Find("gate_hint");
        if (existing != null)
            return;

        var hintGo = new GameObject("gate_hint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        hintGo.transform.SetParent(energyParent, false);
        hintGo.layer = energyParent.gameObject.layer;

        var rect = hintGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(-150f, -12.5f);
        rect.sizeDelta = new Vector2(300f, 25f);

        var text = hintGo.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.text = "으드득 거리는 소리가 들린다.";
        text.fontSize = 18;
        text.color = new Color(0.9372549f, 0.5058824f, 1f, 1f);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Store/HONETi/FlatBlackUniversalGUI/Fonts/Galmuri-v2.40.3/Galmuri11 SDF.asset");
        if (font != null)
            text.font = font;
    }

    private static void FixAuctionPanelScroll()
    {
        var panelRoot = PrefabUtility.LoadPrefabContents(AuctionPanelPath);
        try
        {
            var lotList = panelRoot.transform.Find("Content/Lot_List") as RectTransform;
            if (lotList == null)
            {
                Debug.LogError("AuctionPanelScrollSetup: Content/Lot_List not found.");
                return;
            }

            var lotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LotPrefabPath);
            if (lotPrefab == null)
            {
                Debug.LogError("AuctionPanelScrollSetup: Lot_prefab not found.");
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

            for (var i = 0; i < LotCount; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(lotPrefab, content);
                instance.name = $"Lot_{i + 1}";
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
}
#endif
