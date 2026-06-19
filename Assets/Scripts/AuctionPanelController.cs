using System.Collections.Generic;
using UnityEngine;

/// <summary>경매 패널 Lot_List ↔ GateAuctionManager 연결.</summary>
[DisallowMultipleComponent]
public sealed class AuctionPanelController : MonoBehaviour
{
    private const string StartAuctionPrefabPath = "Prefabs/UI/start_auction";

    private static readonly string[] LotContentPaths =
    {
        "Content/Lot_List/LotViewport/LotContent",
        "Lot_List/LotViewport/LotContent"
    };

    private readonly List<IAuctionLotRowView> _ebayRows = new();
    private readonly List<IAuctionLotRowView> _englishRows = new();
    private GateDatabase _database;
    private AuctionPanelFilterView _filterView;
    private StartAuctionModalView _startAuctionModal;
    private bool _modalEventsWired;

    private void Awake()
    {
        EnsureFilterView();
        EnsureStartAuctionModal();
        EnsureRows();
    }

    private void OnEnable()
    {
        GateAuctionManager.AuctionsChanged += OnAuctionsChanged;
        GateAuctionManager.EnglishSessionChanged += OnEnglishSessionChanged;
        GameManager.StateChanged += OnGameStateChanged;

        EnsureFilterView();
        if (_filterView != null)
            _filterView.Applied += OnFilterApplied;
    }

    private void OnDisable()
    {
        GateAuctionManager.AuctionsChanged -= OnAuctionsChanged;
        GateAuctionManager.EnglishSessionChanged -= OnEnglishSessionChanged;
        GameManager.StateChanged -= OnGameStateChanged;

        if (_filterView != null)
            _filterView.Applied -= OnFilterApplied;
    }

    public void PrepareForPanelOpen()
    {
        EnsureFilterView();
        EnsureDatabase();
        EnsureStartAuctionModal();
        EnsureRows();

        var buildingLevel = GateAuctionManager.Instance != null
            ? GateAuctionManager.TestBuildingLevel
            : 1;

        _filterView?.RefreshGradeOptions(_database, buildingLevel);
        _filterView?.ResetToDefaults();
    }

    public void RefreshFromManager()
    {
        if (!EnsureRows())
            return;

        EnsureDatabase();
        EnsureFilterView();

        var manager = GateAuctionManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("AuctionPanelController: GateAuctionManager not found.", this);
            return;
        }

        var filter = _filterView != null
            ? _filterView.AppliedCriteria
            : AuctionPanelFilterCriteria.Empty;

        BindFilteredLots(manager.ActiveLots, filter);
        RefreshGoldDisplay();
    }

    private void BindFilteredLots(IReadOnlyList<GateAuctionLotRuntime> lots, AuctionPanelFilterCriteria filter)
    {
        var ebayLots = new List<GateAuctionLotRuntime>();
        var englishLots = new List<GateAuctionLotRuntime>();

        for (var i = 0; i < lots.Count; i++)
        {
            if (!AuctionPanelFilterCriteria.Matches(lots[i], filter, _database))
                continue;

            if (lots[i].IsEnglish)
                englishLots.Add(lots[i]);
            else
                ebayLots.Add(lots[i]);
        }

        BindRowList(_ebayRows, ebayLots, _database);
        BindRowList(_englishRows, englishLots, _database);
        WireEnglishRowModals();

        Debug.Log(
            $"[AuctionPanel] Bound ebay={ebayLots.Count}/{_ebayRows.Count}, english={englishLots.Count}/{_englishRows.Count}.",
            this);
    }

    private static void BindRowList(
        List<IAuctionLotRowView> rows,
        List<GateAuctionLotRuntime> lots,
        GateDatabase database)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            if (i < lots.Count)
                rows[i].Bind(lots[i], database);
            else
                rows[i].Clear();
        }
    }

    private void OnFilterApplied() => RefreshFromManager();

    public void NotifyRowsUpdated()
    {
        if (!EnsureRows())
            return;

        RefreshAllRows();
        RefreshGoldDisplay();
    }

    private void RefreshAllRows()
    {
        foreach (var row in _ebayRows)
            row.RefreshInteractiveState();
        foreach (var row in _englishRows)
            row.RefreshInteractiveState();
    }

    private void OnAuctionsChanged() => NotifyRowsUpdated();

    private void OnEnglishSessionChanged(int _) => NotifyRowsUpdated();

    private void OnGameStateChanged() => RefreshGoldDisplay();

    private bool EnsureRows()
    {
        var content = FindLotContent();
        if (content == null)
        {
            Debug.LogError(
                "AuctionPanelController: LotContent not found. Expected Content/Lot_List/LotViewport/LotContent.",
                this);
            return false;
        }

        if (!RowCacheMatchesContent(content))
            RebuildRowCache(content);

        if (_ebayRows.Count + _englishRows.Count == 0)
        {
            Debug.LogError(
                "AuctionPanelController: LotContent has no ebay/eng lot slots. " +
                "Run The Guild → Data → Restore Auction Panel Lot Slots.",
                this);
            return false;
        }

        WireEnglishRowModals();
        return true;
    }

    private bool RowCacheMatchesContent(Transform content)
    {
        var expectedEbay = 0;
        var expectedEng = 0;

        for (var i = 0; i < content.childCount; i++)
        {
            var name = content.GetChild(i).name;
            if (name.EndsWith("_eng"))
                expectedEng++;
            else if (name.EndsWith("_ebay"))
                expectedEbay++;
        }

        return _ebayRows.Count == expectedEbay && _englishRows.Count == expectedEng;
    }

    private void RebuildRowCache(Transform content)
    {
        _ebayRows.Clear();
        _englishRows.Clear();

        for (var i = 0; i < content.childCount; i++)
        {
            var child = content.GetChild(i);
            var name = child.name;

            if (name.EndsWith("_eng"))
            {
                var engRow = child.GetComponent<EnglishAuctionLotRowView>();
                if (engRow == null)
                    engRow = child.gameObject.AddComponent<EnglishAuctionLotRowView>();
                engRow.SetModal(_startAuctionModal);
                _englishRows.Add(engRow);
                continue;
            }

            if (!name.EndsWith("_ebay"))
            {
                Debug.LogWarning(
                    $"AuctionPanelController: skipping unrecognized lot slot '{name}'. " +
                    "Expected suffix _ebay or _eng.",
                    child);
                continue;
            }

            var ebayRow = child.GetComponent<AuctionLotRowView>();
            if (ebayRow == null)
                ebayRow = child.gameObject.AddComponent<AuctionLotRowView>();
            _ebayRows.Add(ebayRow);
        }
    }

    private void EnsureStartAuctionModal()
    {
        if (_startAuctionModal == null)
            _startAuctionModal = GetComponentInChildren<StartAuctionModalView>(true);

        if (_startAuctionModal == null)
        {
            var prefab = Resources.Load<GameObject>(StartAuctionPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"AuctionPanelController: missing Resources/{StartAuctionPrefabPath}", this);
                return;
            }

            var instance = Instantiate(prefab, transform);
            instance.name = "start_auction";
            _startAuctionModal = instance.GetComponent<StartAuctionModalView>();
            if (_startAuctionModal == null)
                _startAuctionModal = instance.AddComponent<StartAuctionModalView>();
        }

        ConfigureModalOverlay(_startAuctionModal.GetComponent<RectTransform>());
        _startAuctionModal.gameObject.SetActive(false);

        if (_startAuctionModal != null && !_modalEventsWired)
        {
            _startAuctionModal.SessionEnded += OnStartAuctionSessionEnded;
            _modalEventsWired = true;
        }

        WireEnglishRowModals();
    }

    private void WireEnglishRowModals()
    {
        if (_startAuctionModal == null)
            return;

        foreach (var row in _englishRows)
        {
            if (row is EnglishAuctionLotRowView engRow)
                engRow.SetModal(_startAuctionModal);
        }
    }

    private static void ConfigureModalOverlay(RectTransform rect) =>
        StartAuctionModalView.ConfigureOverlayLayout(rect);

    private void OnStartAuctionSessionEnded() => NotifyRowsUpdated();

    private Transform FindLotContent()
    {
        foreach (var path in LotContentPaths)
        {
            var found = transform.Find(path);
            if (found != null)
                return found;
        }

        foreach (var rect in GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.name == "LotContent")
                return rect;
        }

        return null;
    }

    private void EnsureDatabase()
    {
        if (_database != null)
            return;

        var manager = GateAuctionManager.Instance;
        if (manager != null)
            _database = manager.Database;
    }

    private void EnsureFilterView()
    {
        if (_filterView != null)
            return;

        _filterView = GetComponent<AuctionPanelFilterView>();
        if (_filterView == null)
            _filterView = gameObject.AddComponent<AuctionPanelFilterView>();
    }

    private void RefreshGoldDisplay()
    {
        var coinText = transform.Find("TitleBar/Coin_UI/Coin_Count")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (coinText == null)
            coinText = transform.Find("Coin_UI/Coin_Count")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (coinText == null)
            coinText = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);

        if (coinText != null && coinText.name == "Coin_Count" && GameManager.Instance != null)
            coinText.text = $"{GameManager.Instance.Gold:N0}";
    }
}
