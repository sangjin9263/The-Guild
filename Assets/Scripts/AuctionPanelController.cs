using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>AuctionPanel_re — slot 목록 + 상세 카드 master-detail.</summary>
[DisallowMultipleComponent]
public sealed class AuctionPanelController : MonoBehaviour
{
    private const string StartAuctionPrefabPath = "Prefabs/UI/start_auction_re";
    private const int SlotCount = 8;

    private static readonly string[] LotListPaths =
    {
        "Content/Lot_List",
        "Lot_List"
    };

    private readonly List<AuctionLotSlotView> _slots = new();
    private readonly List<GateAuctionLotRuntime> _visibleLots = new();

    private GateDatabase _database;
    private AuctionPanelFilterView _filterView;
    private AuctionGateDetailView _detailView;
    private AuctionHelpPopupView _helpPopup;
    private StartAuctionModalView _englishModal;
    private GameObject _englishBlockOverlay;
    private TextMeshProUGUI _emptyLabel;

    private int _selectedIndex = -1;
    private bool _modalEventsWired;

    public int SelectedLotId =>
        _selectedIndex >= 0 && _selectedIndex < _visibleLots.Count
            ? _visibleLots[_selectedIndex].LotId
            : 0;

    private void Awake()
    {
        EnsureFilterView();
        EnsureEnglishModal();
        EnsureViews();
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

        SetEnglishBlockVisible(false);
    }

    public void PrepareForPanelOpen()
    {
        EnsureFilterView();
        EnsureDatabase();
        EnsureEnglishModal();
        EnsureViews();

        var buildingLevel = GateAuctionManager.Instance != null
            ? GateAuctionManager.TestBuildingLevel
            : 1;

        _filterView?.RefreshGradeOptions(_database, buildingLevel);
        _filterView?.ResetToDefaults();
        RefreshGoldDisplay();
    }

    public void RefreshFromManager()
    {
        EnsureDatabase();
        EnsureFilterView();
        EnsureViews();
        RefreshGoldDisplay();

        var manager = GateAuctionManager.Instance;
        if (manager == null)
        {
            BindLots(System.Array.Empty<GateAuctionLotRuntime>());
            return;
        }

        var filter = _filterView != null
            ? _filterView.AppliedCriteria
            : AuctionPanelFilterCriteria.Empty;

        _visibleLots.Clear();
        var lots = manager.ActiveLots;
        for (var i = 0; i < lots.Count; i++)
        {
            if (AuctionPanelFilterCriteria.Matches(lots[i], filter, _database))
                _visibleLots.Add(lots[i]);
        }

        BindLots(_visibleLots);
    }

    public void NotifyRowsUpdated()
    {
        RefreshSlotStatuses();
        RefreshSelectedDetail();
        RefreshGoldDisplay();
        RefreshEnglishBlock();
    }

    private void BindLots(IReadOnlyList<GateAuctionLotRuntime> lots)
    {
        EnsureViews();

        for (var i = 0; i < _slots.Count; i++)
        {
            if (i < lots.Count)
                _slots[i].Bind(lots[i], _database);
            else
                _slots[i].Clear();
        }

        if (lots.Count == 0)
        {
            _selectedIndex = -1;
            _detailView?.Clear();
            SetEmptyLabelVisible(true);
            return;
        }

        SetEmptyLabelVisible(false);

        if (_selectedIndex < 0 || _selectedIndex >= lots.Count)
            _selectedIndex = 0;

        ApplySelection();
    }

    private void ApplySelection()
    {
        for (var i = 0; i < _slots.Count; i++)
            _slots[i].SetSelected(i == _selectedIndex);

        RefreshSelectedDetail();
    }

    private void RefreshSelectedDetail()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _visibleLots.Count)
        {
            _detailView?.Clear();
            return;
        }

        _detailView?.Bind(_visibleLots[_selectedIndex], _database);
        _slots[_selectedIndex].RefreshStatus();
    }

    private void RefreshSlotStatuses()
    {
        for (var i = 0; i < _slots.Count; i++)
            _slots[i].RefreshStatus();
    }

    private void OnSlotClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _visibleLots.Count)
            return;

        _selectedIndex = slotIndex;
        ApplySelection();
        _helpPopup?.Hide();
    }

    private void OnFilterApplied() => RefreshFromManager();

    private void OnAuctionsChanged() => NotifyRowsUpdated();

    private void OnEnglishSessionChanged(int lotId)
    {
        if (lotId == 0)
            DismissEnglishModal();

        NotifyRowsUpdated();
    }

    /// <summary>English 세션 종료·취소 후 모달 UI 정리.</summary>
    public void DismissEnglishModal()
    {
        if (_englishModal == null || !_englishModal.gameObject.activeSelf)
            return;

        _englishModal.Close();
    }

    private void OnGameStateChanged() => RefreshGoldDisplay();

    private void OnEnglishSessionEnded()
    {
        SetEnglishBlockVisible(false);
        NotifyRowsUpdated();
    }

    private void EnsureViews()
    {
        if (_slots.Count > 0)
            return;

        var lotList = FindPanelTransform(LotListPaths);
        if (lotList == null)
        {
            Debug.LogError("[AuctionPanel] Content/Lot_List not found.", this);
            return;
        }

        var viewport = lotList.Find("LotViewport");
        if (viewport == null)
        {
            Debug.LogError("[AuctionPanel] LotViewport not found.", this);
            return;
        }

        _helpPopup = GetComponent<AuctionHelpPopupView>() ?? gameObject.AddComponent<AuctionHelpPopupView>();

        var lotInfo = lotList.Find("Lotinfo");
        _detailView = GetComponent<AuctionGateDetailView>() ?? gameObject.AddComponent<AuctionGateDetailView>();
        _detailView.Initialize(lotInfo, _englishModal, _helpPopup);

        EnsureEmptyLabel(lotList);
        EnsureEnglishBlockOverlay();

        var slotCount = Mathf.Min(SlotCount, viewport.childCount);
        for (var i = 0; i < slotCount; i++)
        {
            var child = viewport.GetChild(i);
            var slot = child.GetComponent<AuctionLotSlotView>() ?? child.gameObject.AddComponent<AuctionLotSlotView>();
            slot.Initialize(i);
            slot.WireClick(OnSlotClicked);
            _slots.Add(slot);
        }

        HideLegacyEmbeddedModal();
        HideButtonInfo();
    }

    private void HideButtonInfo()
    {
        var infoButton = AuctionPanelLayoutFit.FindPanelTransform(transform, "TitleBar/ButtonInfo");
        if (infoButton != null)
            infoButton.gameObject.SetActive(false);
    }

    private void HideLegacyEmbeddedModal()
    {
        CleanupLegacyEnglishModals();
        EnsureEnglishModal();
    }

    private void EnsureEmptyLabel(Transform lotList)
    {
        if (_emptyLabel != null)
            return;

        var existing = lotList.Find("EmptyLabel");
        if (existing != null)
        {
            _emptyLabel = existing.GetComponent<TextMeshProUGUI>();
            return;
        }

        var go = new GameObject("EmptyLabel", typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(lotList, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(400f, 60f);
        _emptyLabel = go.AddComponent<TextMeshProUGUI>();
        _emptyLabel.alignment = TextAlignmentOptions.Center;
        _emptyLabel.fontSize = 28f;
        _emptyLabel.text = "경매 없음";
        _emptyLabel.color = Color.black;
    }

    private void SetEmptyLabelVisible(bool visible)
    {
        if (_emptyLabel != null)
            _emptyLabel.gameObject.SetActive(visible);
    }

    private void EnsureEnglishBlockOverlay()
    {
        if (_englishBlockOverlay != null)
            return;

        _englishBlockOverlay = new GameObject("EnglishBlockOverlay", typeof(RectTransform));
        var rect = _englishBlockOverlay.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.SetAsLastSibling();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = _englishBlockOverlay.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.01f);
        image.raycastTarget = true;
        _englishBlockOverlay.SetActive(false);
    }

    private void RefreshEnglishBlock()
    {
        var manager = GateAuctionManager.Instance;
        var active = manager != null && manager.HasActiveEnglishSession;
        SetEnglishBlockVisible(active);
    }

    private void SetEnglishBlockVisible(bool visible)
    {
        EnsureEnglishBlockOverlay();
        if (_englishBlockOverlay != null)
        {
            _englishBlockOverlay.SetActive(visible);
            if (visible)
                _englishBlockOverlay.transform.SetAsLastSibling();
        }

        if (visible && _englishModal != null && _englishModal.gameObject.activeSelf)
            _englishModal.transform.SetAsLastSibling();
    }

    private void EnsureEnglishModal()
    {
        CleanupLegacyEnglishModals();

        _englishModal = transform.Find("start_auction_re")?.GetComponent<StartAuctionModalView>();

        if (_englishModal == null)
        {
            var prefab = Resources.Load<GameObject>(StartAuctionPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[AuctionPanel] missing Resources/{StartAuctionPrefabPath}", this);
                return;
            }

            var instance = Instantiate(prefab, transform);
            instance.name = "start_auction_re";
            _englishModal = instance.GetComponent<StartAuctionModalView>();
            if (_englishModal == null)
                _englishModal = instance.AddComponent<StartAuctionModalView>();
        }

        StartAuctionModalView.ConfigureOverlayLayout(_englishModal.GetComponent<RectTransform>());
        _englishModal.gameObject.SetActive(false);

        if (_englishModal != null && !_modalEventsWired)
        {
            _englishModal.SessionEnded += OnEnglishSessionEnded;
            _englishModal.Opened += OnEnglishModalOpened;
            _englishModal.Closed += OnEnglishModalClosed;
            _modalEventsWired = true;
        }

        _detailView?.SetEnglishModal(_englishModal);
    }

    private void CleanupLegacyEnglishModals()
    {
        var legacy = transform.Find("start_auction");
        if (legacy != null)
        {
            if (_englishModal != null && _englishModal.transform == legacy)
                UnwireEnglishModalEvents();

            Destroy(legacy.gameObject);
            _englishModal = null;
        }

        var modals = GetComponentsInChildren<StartAuctionModalView>(true);
        for (var i = 0; i < modals.Length; i++)
        {
            var modal = modals[i];
            if (modal == null || modal.gameObject.name == "start_auction_re")
                continue;

            if (_englishModal == modal)
                UnwireEnglishModalEvents();

            Destroy(modal.gameObject);
            _englishModal = null;
        }
    }

    private void UnwireEnglishModalEvents()
    {
        if (_englishModal == null || !_modalEventsWired)
            return;

        _englishModal.SessionEnded -= OnEnglishSessionEnded;
        _englishModal.Opened -= OnEnglishModalOpened;
        _englishModal.Closed -= OnEnglishModalClosed;
        _modalEventsWired = false;
    }

    private void OnEnglishModalOpened() => SetEnglishBlockVisible(true);

    private void OnEnglishModalClosed() => SetEnglishBlockVisible(false);

    private Transform FindPanelTransform(params string[] relativePaths)
    {
        foreach (var path in relativePaths)
        {
            var found = AuctionPanelLayoutFit.FindPanelTransform(transform, path);
            if (found != null)
                return found;
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
        var coinText = AuctionPanelLayoutFit
            .FindPanelTransform(transform, "TitleBar/Coin_UI/Coin_Count")
            ?.GetComponent<TextMeshProUGUI>();
        if (coinText == null)
            return;

        if (GameManager.Instance != null)
            coinText.text = $"{GameManager.Instance.Gold:N0}";

        var unit = AuctionPanelLayoutFit
            .FindPanelTransform(transform, "TitleBar/Coin_UI/Coin_Unit")
            ?.GetComponent<TextMeshProUGUI>();
        if (unit != null && string.IsNullOrWhiteSpace(unit.text))
            unit.text = "G";
    }

    private void OnDestroy()
    {
        UnwireEnglishModalEvents();
    }
}
