#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ebay/English 결과 패널 + 슬롯 StatusOverlay를 prefab으로 생성하고 AuctionPanel_re에 연결.
/// </summary>
public static class AuctionResultPanelSetup
{
    private const string AuctionFolder = "Assets/Resources/Prefabs/UI/Auction";
    private const string EbayResultPrefabPath = AuctionFolder + "/EbayResultPanel.prefab";
    private const string EnglishResultPrefabPath = AuctionFolder + "/EnglishResultPanel.prefab";
    private const string SlotStatusOverlayPrefabPath = AuctionFolder + "/SlotStatusOverlay.prefab";

    private static readonly string[] AiLabels = { "AI1", "AI2", "AI3" };

    [MenuItem("The Guild/UI/Wire Auction Result Panels")]
    public static void WireFromMenu()
    {
        WireAuctionResultPanels(AuctionPanelSceneMigration.AuctionPanelShellPrefabPath);
        Debug.Log("[AuctionPanel] Auction result panels wired into AuctionPanel_re prefab.");
    }

    public static void WireAuctionResultPanels(string panelPrefabPath)
    {
        EnsureResultPrefabsExist();
        EnsureEnglishResultPanelCount();

        var ebayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EbayResultPrefabPath);
        var engPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnglishResultPrefabPath);
        var slotOverlayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlotStatusOverlayPrefabPath);
        if (ebayPrefab == null || engPrefab == null || slotOverlayPrefab == null)
        {
            Debug.LogError("[AuctionPanel] Failed to load auction result prefabs.");
            return;
        }

        var prefabRoot = PrefabUtility.LoadPrefabContents(panelPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"[AuctionPanel] Prefab not found at {panelPrefabPath}");
            return;
        }

        var lotInfo = FindDeepChild(prefabRoot.transform, "Lotinfo");
        if (lotInfo != null)
        {
            WireCardResultPanel(lotInfo.Find("gate_card_ebay"), ebayPrefab, "EbayResultPanel");
            WireCardResultPanel(lotInfo.Find("gate_card_eng"), engPrefab, "EnglishResultPanel");
        }
        else
        {
            Debug.LogWarning("[AuctionPanel] Lotinfo not found in AuctionPanel_re.");
        }

        var lotViewport = FindDeepChild(prefabRoot.transform, "LotViewport");
        if (lotViewport != null)
        {
            for (var i = 0; i < lotViewport.childCount; i++)
                WireSlotStatusOverlay(lotViewport.GetChild(i), slotOverlayPrefab);
        }
        else
        {
            Debug.LogWarning("[AuctionPanel] LotViewport not found in AuctionPanel_re.");
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, panelPrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        AssetDatabase.SaveAssets();
    }

    public static void EnsureResultPrefabsExist()
    {
        EnsureDirectory(AuctionFolder);
        EnsurePrefab(EbayResultPrefabPath, BuildEbayResultPanel);
        EnsurePrefab(EnglishResultPrefabPath, BuildEnglishResultPanel);
        EnsurePrefab(SlotStatusOverlayPrefabPath, BuildSlotStatusOverlay);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureDirectory(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        var parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        var name = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureDirectory(parent);

        AssetDatabase.CreateFolder(parent, name);
    }

    private static GameObject EnsurePrefab(string path, System.Func<GameObject> builder)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing;

        var root = builder();
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void WireCardResultPanel(Transform card, GameObject prefab, string panelName)
    {
        if (card == null || prefab == null)
            return;

        var existing = card.Find(panelName);
        if (existing != null)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(existing.gameObject);
            if (source == prefab)
            {
                existing.gameObject.SetActive(false);
                return;
            }

            Object.DestroyImmediate(existing.gameObject);
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, card);
        instance.name = panelName;
        instance.SetActive(false);
    }

    private static void WireSlotStatusOverlay(Transform slot, GameObject prefab)
    {
        if (slot == null || prefab == null)
            return;

        var existing = slot.Find("StatusOverlay");
        if (existing != null)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(existing.gameObject);
            if (source == prefab)
            {
                existing.gameObject.SetActive(false);
                return;
            }

            Object.DestroyImmediate(existing.gameObject);
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, slot);
        instance.name = "StatusOverlay";
        instance.transform.SetAsLastSibling();
        instance.SetActive(false);
    }

    private static GameObject BuildEbayResultPanel()
    {
        var root = CreatePanelRoot("EbayResultPanel", new Vector2(520f, 180f));

        CreateAnchoredText(root.transform, "Countdown", new Vector2(0f, 120f), new Vector2(480f, 40f), 28f);
        CreateAnchoredText(root.transform, "Status", new Vector2(0f, 110f), new Vector2(480f, 40f), 22f);

        var bidsRoot = new GameObject("Bids", typeof(RectTransform)).GetComponent<RectTransform>();
        bidsRoot.SetParent(root.transform, false);
        bidsRoot.anchorMin = new Vector2(0f, 0f);
        bidsRoot.anchorMax = new Vector2(1f, 1f);
        bidsRoot.offsetMin = new Vector2(12f, 48f);
        bidsRoot.offsetMax = new Vector2(-12f, -70f);

        CreateBidText(bidsRoot, "player", new Vector2(0f, -4f));
        for (var i = 0; i < AiLabels.Length; i++)
            CreateBidText(bidsRoot, AiLabels[i], new Vector2(0f, -28f - (i * 22f)));

        return root;
    }

    public static void EnsureEnglishResultPanelCount()
    {
        var path = EnglishResultPrefabPath;
        var root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
            return;

        var count = root.transform.Find("count");
        if (count == null)
        {
            var countGo = new GameObject("count", typeof(RectTransform));
            var rect = countGo.GetComponent<RectTransform>();
            rect.SetParent(root.transform, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = countGo.AddComponent<TextMeshProUGUI>();
            text.text = "입찰 진행 중";
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 50f;
            text.color = Color.white;
            countGo.SetActive(false);
        }

        var ack = root.transform.Find("AckButton");
        if (ack != null)
            Object.DestroyImmediate(ack.gameObject);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static GameObject BuildEnglishResultPanel()
    {
        var root = CreatePanelRoot("EnglishResultPanel", new Vector2(520f, 140f));

        var countGo = new GameObject("count", typeof(RectTransform));
        var countRect = countGo.GetComponent<RectTransform>();
        countRect.SetParent(root.transform, false);
        countRect.anchorMin = Vector2.zero;
        countRect.anchorMax = Vector2.one;
        countRect.offsetMin = Vector2.zero;
        countRect.offsetMax = Vector2.zero;
        var countText = countGo.AddComponent<TextMeshProUGUI>();
        countText.text = "입찰 진행 중";
        countText.alignment = TextAlignmentOptions.Center;
        countText.fontSize = 50f;
        countText.color = Color.white;
        countGo.SetActive(false);

        CreateAnchoredText(root.transform, "Status", new Vector2(0f, 70f), new Vector2(480f, 40f), 22f);
        return root;
    }

    private static GameObject BuildSlotStatusOverlay()
    {
        var root = new GameObject("SlotStatusOverlay", typeof(RectTransform));
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);
        bg.raycastTarget = false;

        var textGo = new GameObject("Text", typeof(RectTransform));
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 22f;
        text.color = Color.white;
        return root;
    }

    private static GameObject CreatePanelRoot(string name, Vector2 size)
    {
        var root = new GameObject(name, typeof(RectTransform));
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 20f);
        rect.sizeDelta = size;

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);
        return root;
    }

    private static TextMeshProUGUI CreateAnchoredText(
        Transform parent,
        string name,
        Vector2 anchoredPos,
        Vector2 size,
        float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        return text;
    }

    private static TextMeshProUGUI CreateBidText(RectTransform parent, string name, Vector2 anchoredPos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(480f, 20f);

        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = 16f;
        text.color = Color.white;
        return text;
    }

    private static void CreateAckButton(Transform parent)
    {
        var ackGo = new GameObject("AckButton", typeof(RectTransform));
        var ackRect = ackGo.GetComponent<RectTransform>();
        ackRect.SetParent(parent, false);
        ackRect.anchorMin = new Vector2(0.5f, 0f);
        ackRect.anchorMax = new Vector2(0.5f, 0f);
        ackRect.pivot = new Vector2(0.5f, 0f);
        ackRect.anchoredPosition = new Vector2(0f, 8f);
        ackRect.sizeDelta = new Vector2(160f, 36f);

        ackGo.AddComponent<Button>();
        ackGo.AddComponent<Image>().color = new Color(0.2f, 0.45f, 0.95f, 1f);

        var label = CreateAnchoredText(ackRect, "Label", Vector2.zero, Vector2.zero, 18f);
        var labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        label.text = "확인";
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null)
            return null;

        if (root.name == name)
            return root;

        for (var i = 0; i < root.childCount; i++)
        {
            var found = FindDeepChild(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }
}
#endif
