using System;
using System.Collections.Generic;
using Michsky.UI.ModernUIPack;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>start_auction_re.prefab — English 라이브 경매 모달.</summary>
[DisallowMultipleComponent]
public sealed class StartAuctionModalView : MonoBehaviour
{
    private const string SettingRoot = "setting";
    private const string DimOverlayName = "DimOverlay";
    private const string ConfirmOverlayPath = "confirm_overlay";
    private const string WaitingLabel = "대기중";
    private const float DimAlpha = 0.8f;

    private GateAuctionLotRuntime _lot;
    private GateDatabase _database;
    private EnglishAuctionSession _session;

    private Image _dimOverlay;
    private TextMeshProUGUI _gateInfoLabelsText;
    private TextMeshProUGUI _gateInfoText;
    private Image _gateIconImage;
    private Image _mainImage;
    private ProgressBar _energyBar;
    private TextMeshProUGUI _energyPercentText;
    private TextMeshProUGUI _gatePriceText;
    private TextMeshProUGUI _gateDeltaText;
    private TextMeshProUGUI _timeText;
    private TextMeshProUGUI _countText;
    private TextMeshProUGUI _playerGoldText;
    private TextMeshProUGUI _minBidAmountText;
    private TMP_InputField _bidInput;
    private Button _bidButton;
    private Button _exitButton;
    private Button _confirmYesButton;
    private Button _confirmNoButton;
    private Transform _tableRoot;
    private RectTransform _tableContent;
    private ScrollRect _tableScroll;
    private Transform _aiRowTemplate;
    private bool _leaderboardScrollReady;
    private readonly List<CompetingGuildRowBinding> _guildRows = new();

    private GameObject _hintBefore;
    private GameObject _hintAfter;
    private TextMeshProUGUI _hintText;
    private TextMeshProUGUI _hintAmountText;
    private Button _hintBuyButton;

    private GameObject _confirmRoot;
    private bool _uiReady;
    private string _bidFeedbackMessage;
    private float _bidFeedbackUntil;

    public event Action SessionEnded;
    public event Action Opened;
    public event Action Closed;

    private struct CompetingGuildRowBinding
    {
        public GameObject Root;
        public Image Background;
        public TextMeshProUGUI RankText;
        public TextMeshProUGUI GuildText;
        public TextMeshProUGUI BidText;
        public GameObject Arrow;
    }

    private void Awake() => EnsureUiReady();

    private void EnsureUiReady()
    {
        if (_uiReady)
            return;

        _uiReady = true;
        CacheUiRefs();
        WireControls();
        EnsureDragHandle();
        GameUIFont.ApplyToHierarchy(this);
    }

    private void OnDestroy()
    {
        UnbindSession();
        if (_exitButton != null)
            _exitButton.onClick.RemoveListener(OnExitClicked);
        if (_bidButton != null)
            _bidButton.onClick.RemoveListener(OnBidClicked);
        if (_confirmYesButton != null)
            _confirmYesButton.onClick.RemoveListener(OnConfirmWithdraw);
        if (_confirmNoButton != null)
            _confirmNoButton.onClick.RemoveListener(OnConfirmDismiss);
        if (_hintBuyButton != null)
            _hintBuyButton.onClick.RemoveListener(OnHintBuyClicked);
        if (_bidInput != null)
            _bidInput.onValueChanged.RemoveListener(OnBidInputChanged);
    }

    private void Update()
    {
        if (_session == null || !_session.IsActive)
            return;

        RefreshTimerAndCount();

        if (!string.IsNullOrEmpty(_bidFeedbackMessage) && Time.unscaledTime >= _bidFeedbackUntil)
        {
            _bidFeedbackMessage = null;
            RefreshBidControl();
        }
    }

    public void Open(GateAuctionLotRuntime lot, GateDatabase database, EnglishAuctionSession session)
    {
        EnsureUiReady();

        _lot = lot;
        _database = database;
        _session = session;
        UnbindSession();
        if (_session != null)
            _session.Changed += OnSessionChanged;

        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        Opened?.Invoke();
        ConfigureOverlayLayout(transform as RectTransform);
        transform.Find(SettingRoot)?.SetAsLastSibling();
        transform.Find(ConfirmOverlayPath)?.SetAsLastSibling();
        AuctionBidInputUtility.ClearInput(_bidInput);
        RefreshAll();
    }

    /// <summary>루트 stretch + DimOverlay 80% + setting은 prefab 배치·드래그.</summary>
    public static void ConfigureOverlayLayout(RectTransform root)
    {
        if (root == null)
            return;

        StretchFull(root);

        var rootImage = root.GetComponent<Image>();
        if (rootImage != null)
        {
            rootImage.color = new Color(0f, 0f, 0f, 0f);
            rootImage.raycastTarget = true;
        }

        EnsureDimOverlay(root);

        var confirm = root.Find(ConfirmOverlayPath) as RectTransform;
        if (confirm != null)
            StretchFull(confirm);

        var setting = root.Find(SettingRoot);
        if (setting != null)
            setting.SetAsLastSibling();
    }

    private static void EnsureDimOverlay(RectTransform root)
    {
        var dim = root.Find(DimOverlayName) as RectTransform;
        if (dim == null)
        {
            var go = new GameObject(DimOverlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dim = go.GetComponent<RectTransform>();
            dim.SetParent(root, false);
        }

        dim.SetAsFirstSibling();
        StretchFull(dim);

        var image = dim.GetComponent<Image>() ?? dim.gameObject.AddComponent<Image>();
        image.sprite = null;
        image.color = new Color(0f, 0f, 0f, DimAlpha);
        image.raycastTarget = true;
    }

    private static void StretchFull(RectTransform rect)
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

    public void Close()
    {
        UnbindSession();
        _lot = null;
        _session = null;
        _database = null;
        if (_confirmRoot != null)
            _confirmRoot.SetActive(false);
        gameObject.SetActive(false);
        Closed?.Invoke();
    }

    private void UnbindSession()
    {
        if (_session != null)
            _session.Changed -= OnSessionChanged;
    }

    private void OnSessionChanged()
    {
        if (_session != null && _session.Ended)
        {
            var lotId = _lot?.LotId ?? 0;
            GateAuctionManager.Instance?.CompleteEnglishSessionIfEnded(lotId);
            Close();
            SessionEnded?.Invoke();
            return;
        }

        RefreshAll();
    }

    private void CacheUiRefs()
    {
        _dimOverlay = GetComponent<Image>();

        var setting = transform.Find(SettingRoot) ?? transform;
        var centerTop = setting.Find("center_top") ?? setting;
        var gatePriceRoot = centerTop.Find("gate_price") ?? setting.Find("gate_price");
        var gateStatusRoot = centerTop.Find("gate_status") ?? setting.Find("gate_status");
        var right = setting.Find("right_pannel");

        var gateInfoRoot = centerTop.Find("gate_info") ?? setting.Find("gate_info");
        _gateInfoLabelsText = FindTmp(gateInfoRoot, "Text (TMP)");
        _gateInfoText = FindTmp(gateInfoRoot, "Text (TMP) (1)")
                        ?? FindTmp(setting, "left_pannel/Text (TMP) (1)");
        _gateIconImage = gateInfoRoot != null
            ? gateInfoRoot.Find("gate_icon")?.GetComponent<Image>()
              ?? gateInfoRoot.Find("icon")?.GetComponent<Image>()
            : null;
        _mainImage = gateInfoRoot?.Find("main_img")?.GetComponent<Image>();

        var energyRoot = gateInfoRoot != null ? gateInfoRoot.Find("Energy_Percent") : null;
        if (energyRoot != null)
        {
            _energyBar = energyRoot.GetComponent<ProgressBar>();
            _energyPercentText = energyRoot.Find("percent")?.GetComponent<TextMeshProUGUI>();
        }

        CacheHintRefs(gateInfoRoot?.Find("gate_hint"));

        _gatePriceText = FindTmp(gatePriceRoot, "current_price");
        _gateDeltaText = FindTmp(gatePriceRoot, "calc/Text (TMP)")
                         ?? FindTmp(gatePriceRoot, "input amount")
                         ?? FindTmp(gatePriceRoot, "text");

        _timeText = FindTmp(gateStatusRoot, "time")
                    ?? FindTmp(gateStatusRoot, "time_img/time")
                    ?? FindTmp(centerTop, "time");
        _countText = FindTmp(gateStatusRoot, "count") ?? FindTmp(centerTop, "count");

        _playerGoldText = FindTmp(right, "player_gold/gold amount")
                          ?? FindTmp(right, "player_gold");
        var inputRoot = right != null ? right.Find("input_gold") : null;
        _minBidAmountText = FindTmp(inputRoot, "min amount")
                            ?? FindTmp(right, "min_gold/min amount");

        _bidInput = inputRoot != null
            ? inputRoot.GetComponentInChildren<TMP_InputField>(true)
            : null;

        _bidButton = FindButton(right, "Bid_Button");
        _exitButton = FindButton(setting, "TitleBar/ButtonX")
                      ?? FindButton(setting, "center_top/windowname/exit_button")
                      ?? FindButton(setting, "exit_button");

        HideWithdrawButton(setting, right);

        CacheConfirmRefs();

        _tableRoot = setting.Find("left_pannel/table");
        _aiRowTemplate = _tableRoot != null ? _tableRoot.Find("row_ai") : null;
        DisableRowRaycasts();
        CacheGuildRows();

        if (_gatePriceText == null || _bidButton == null || _tableRoot == null)
        {
            Debug.LogWarning(
                "[StartAuctionModal] UI refs incomplete. " +
                $"price={_gatePriceText != null}, bid={_bidButton != null}, table={_tableRoot != null}",
                this);
        }

        if (_exitButton == null)
            Debug.LogWarning("[StartAuctionModal] TitleBar close button missing.", this);
    }

    private void EnsureDragHandle()
    {
        var setting = transform.Find(SettingRoot) as RectTransform;
        if (setting == null)
            return;

        var titleBar = setting.Find("TitleBar");
        if (titleBar == null)
        {
            foreach (Transform child in setting)
            {
                if (child.name.Contains("TitleBar", StringComparison.Ordinal))
                {
                    titleBar = child;
                    break;
                }
            }
        }

        if (titleBar == null)
            return;

        var handle = titleBar.GetComponent<StartAuctionModalDragHandle>()
                     ?? titleBar.gameObject.AddComponent<StartAuctionModalDragHandle>();
        handle.Configure(setting, transform as RectTransform);

        var titleImage = titleBar.GetComponent<UnityEngine.UI.Image>();
        if (titleImage != null)
            titleImage.raycastTarget = true;
    }

    private void CacheHintRefs(Transform hintRoot)
    {
        if (hintRoot == null)
            return;

        _hintBefore = hintRoot.Find("bf_pay")?.gameObject;
        _hintAfter = hintRoot.Find("af_pay")?.gameObject;
        _hintText = hintRoot.Find("af_pay/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
        _hintAmountText = hintRoot.Find("bf_pay/amount")?.GetComponent<TextMeshProUGUI>();
        _hintBuyButton = hintRoot.Find("bf_pay")?.GetComponent<Button>();

        if (_hintAfter != null)
        {
            var afButton = _hintAfter.GetComponent<Button>();
            if (afButton != null)
                Destroy(afButton);

            var afImage = _hintAfter.GetComponent<Image>();
            if (afImage != null)
                afImage.raycastTarget = false;
        }
    }

    private static void HideWithdrawButton(Transform setting, Transform right)
    {
        var withdraw = right != null ? right.Find("withdraw_Button") : null;
        if (withdraw == null && setting != null)
            withdraw = setting.Find("withdraw_Button");

        if (withdraw != null)
            withdraw.gameObject.SetActive(false);
    }

    private void DisableRowRaycasts()
    {
        if (_tableRoot == null)
            return;

        foreach (var image in _tableRoot.GetComponentsInChildren<Image>(true))
            image.raycastTarget = false;
    }

    private void CacheGuildRows()
    {
        _guildRows.Clear();
        var parent = GetLeaderboardRowParent();
        if (parent == null)
            return;

        var rows = new List<(Transform Transform, float Y)>();
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (!child.name.StartsWith("row", StringComparison.Ordinal))
                continue;

            var y = child is RectTransform rect ? rect.anchoredPosition.y : 0f;
            rows.Add((child, y));
        }

        rows.Sort((a, b) => b.Y.CompareTo(a.Y));

        for (var i = 0; i < rows.Count; i++)
            _guildRows.Add(BindGuildRow(rows[i].Transform));
    }

    private Transform GetLeaderboardRowParent() =>
        _tableContent != null ? _tableContent : _tableRoot;

    private static CompetingGuildRowBinding BindGuildRow(Transform row)
    {
        return new CompetingGuildRowBinding
        {
            Root = row.gameObject,
            Background = row.GetComponent<Image>(),
            RankText = row.Find("No")?.GetComponentInChildren<TextMeshProUGUI>(true),
            GuildText = row.Find("Guild_name")?.GetComponentInChildren<TextMeshProUGUI>(true)
                          ?? row.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>(),
            BidText = row.Find("Bid_price")?.GetComponentInChildren<TextMeshProUGUI>(true),
            Arrow = row.Find("arrow")?.gameObject
        };
    }

    private static void ApplyRowStyle(in CompetingGuildRowBinding row, bool isPlayer)
    {
        var background = isPlayer ? Color.black : Color.white;
        var foreground = isPlayer ? Color.white : Color.black;

        if (row.Background != null)
            row.Background.color = background;

        ApplyCellStyle(row.Root.transform, "No", background, foreground, row.RankText);
        ApplyCellStyle(row.Root.transform, "Guild_name", background, foreground, row.GuildText);
        ApplyCellStyle(row.Root.transform, "Bid_price", background, foreground, row.BidText);

        if (row.Arrow != null)
        {
            row.Arrow.SetActive(isPlayer);
            var arrowImage = row.Arrow.GetComponent<Image>();
            if (arrowImage != null)
                arrowImage.color = foreground;
        }
    }

    private static void ApplyCellStyle(
        Transform row,
        string cellName,
        Color background,
        Color foreground,
        TextMeshProUGUI text)
    {
        var cell = row.Find(cellName);
        if (cell == null)
            return;

        var image = cell.GetComponent<Image>();
        if (image != null)
            image.color = background;

        if (text != null)
            text.color = foreground;
    }

    private void WireControls()
    {
        if (_bidInput != null)
        {
            AuctionBidInputUtility.Configure(_bidInput);
            _bidInput.onValueChanged.AddListener(OnBidInputChanged);
        }

        if (_bidButton != null)
            _bidButton.onClick.AddListener(OnBidClicked);
        if (_exitButton != null)
            _exitButton.onClick.AddListener(OnExitClicked);
        if (_confirmYesButton != null)
            _confirmYesButton.onClick.AddListener(OnConfirmWithdraw);
        if (_confirmNoButton != null)
            _confirmNoButton.onClick.AddListener(OnConfirmDismiss);
        if (_hintBuyButton != null)
            _hintBuyButton.onClick.AddListener(OnHintBuyClicked);
    }

    private void CacheConfirmRefs()
    {
        _confirmRoot = transform.Find(ConfirmOverlayPath)?.gameObject;
        if (_confirmRoot == null)
        {
            Debug.LogWarning("[StartAuctionModal] confirm_overlay not found on prefab.", this);
            return;
        }

        var confirmRoot = _confirmRoot.transform;
        _confirmYesButton = FindButton(confirmRoot, "panel/yes");
        _confirmNoButton = FindButton(confirmRoot, "panel/no");

        if (_confirmYesButton == null || _confirmNoButton == null)
        {
            Debug.LogWarning(
                "[StartAuctionModal] confirm buttons missing. " +
                $"yes={_confirmYesButton != null}, no={_confirmNoButton != null}",
                this);
        }

        _confirmRoot.SetActive(false);
    }

    private void RefreshAll()
    {
        if (_lot == null || _session == null)
            return;

        EnsureLeaderboardLayout(GetRequiredAiRowCount());
        RefreshGateInfo();
        RefreshGatePrice();
        RefreshTimerAndCount();
        RefreshBidControl();
        RefreshHintState();
        RefreshLeaderboard();
    }

    private int GetRequiredAiRowCount()
    {
        if (_lot == null)
            return 3;

        return Mathf.Max(1, _lot.Lot.englishAiCount);
    }

    private void RefreshGateInfo()
    {
        if (_lot == null)
            return;

        var lot = _lot.Lot;
        var energy = _database != null
            ? _database.GetEnergyDisplayPercent(lot)
            : lot.energy * 100f / GateDatabase.EnergyPercentMax;
        energy = Mathf.Clamp(energy, 0f, 100f);

        if (_gateInfoLabelsText != null)
        {
            _gateInfoLabelsText.text = "게이트 ID\n등급\n지역\n에너지 %";
            GameUIFont.EnsureGlyphs(_gateInfoLabelsText, _gateInfoLabelsText.text);
        }

        if (_gateInfoText != null)
        {
            var grade = GateGradeUtility.GetDisplayName(lot.grade);
            var region = string.IsNullOrWhiteSpace(lot.regionDisplay)
                ? lot.regionCode
                : lot.regionDisplay;
            var values = $"{lot.gateId}\n{grade}\n{region}\n{energy:0} %";
            _gateInfoText.text = values;
            GameUIFont.EnsureGlyphs(_gateInfoText, values);
        }

        if (_gateIconImage != null)
        {
            var icon = GateGradeIconCache.GetIcon(lot.grade);
            if (icon != null)
            {
                _gateIconImage.sprite = icon;
                _gateIconImage.enabled = true;
            }
        }

        if (_mainImage != null)
        {
            var preview = GateAuctionImageCache.GetImage(lot.grade);
            if (preview != null)
            {
                _mainImage.sprite = preview;
                _mainImage.color = Color.white;
            }
        }

        if (_energyBar != null)
        {
            _energyBar.isOn = false;
            _energyBar.ChangeValue(energy);
        }

        if (_energyPercentText != null)
            _energyPercentText.text = $"{energy:0}%";
    }

    private void RefreshHintState()
    {
        if (_lot == null)
            return;

        var purchased = _lot.HintPurchased;
        var revealed = _lot.HintRevealed;
        var canPurchase = !purchased && _lot.State == GateAuctionLotState.Bidding;

        if (_hintBefore != null)
            _hintBefore.SetActive(canPurchase);

        if (_hintAfter != null)
            _hintAfter.SetActive(purchased || revealed);

        if (_hintBuyButton != null)
            _hintBuyButton.interactable = canPurchase;

        if (_hintAmountText != null)
        {
            if (canPurchase && _database != null)
            {
                _hintAmountText.gameObject.SetActive(true);
                var cost = _database.GetBrokerHintCost(_lot.Lot.grade, _lot.Lot.auction.bidBandMin);
                _hintAmountText.text = $"{cost:N0} G";
            }
            else
            {
                _hintAmountText.gameObject.SetActive(false);
            }
        }

        if (_hintText != null)
        {
            _hintText.text = revealed
                ? BuildRevealedHintText()
                : string.Empty;
        }
    }

    private string BuildRevealedHintText()
    {
        if (_lot == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(_lot.BrokerHintInfoText))
            return _lot.BrokerHintInfoText;

        return _lot.BrokerHintText;
    }

    private void OnHintBuyClicked()
    {
        if (_lot == null || GateAuctionManager.Instance == null)
            return;

        if (!GateAuctionManager.Instance.TryPurchaseHint(_lot.LotId))
            return;

        GateAuctionManager.Instance.NotifyHintRevealed(_lot.LotId);
        RefreshHintState();
    }

    private void RefreshGatePrice()
    {
        if (_gatePriceText != null)
            _gatePriceText.text = $"{_session.CurrentHighBid:N0} G";

        if (_gateDeltaText == null)
            return;

        var previous = _session.PreviousHighBid;
        var current = _session.CurrentHighBid;
        if (_session.BidCount <= 0 || current <= previous)
        {
            _gateDeltaText.text = "-";
            return;
        }

        var delta = current - previous;
        var pct = previous > 0 ? (delta * 100f / previous) : 0f;
        _gateDeltaText.text = $"+{delta:N0} G  (+{pct:0.#}%)";
    }

    private void RefreshTimerAndCount()
    {
        if (_timeText != null)
        {
            var seconds = Mathf.CeilToInt(_session.RemainingSeconds);
            var minutes = seconds / 60;
            var sec = seconds % 60;
            _timeText.text = $"{minutes:00}:{sec:00}";
        }

        if (_countText != null)
        {
            var activeBidders = CountActiveBidders();
            var label = $"{activeBidders} 명";
            _countText.text = label;
            GameUIFont.EnsureGlyphs(_countText, label);
        }
    }

    private int CountActiveBidders()
    {
        if (_session == null)
            return 0;

        var count = 0;
        foreach (var participant in _session.Participants)
        {
            if (participant.HasBid)
                count++;
        }

        return count;
    }

    private void RefreshBidControl()
    {
        var totalGold = GameManager.Instance != null ? GameManager.Instance.Gold : 0;

        if (_playerGoldText != null)
            _playerGoldText.text = $"{totalGold:N0} G";

        var minBid = _session.GetMinBid();
        if (_minBidAmountText != null)
        {
            if (!string.IsNullOrEmpty(_bidFeedbackMessage) && Time.unscaledTime < _bidFeedbackUntil)
            {
                _minBidAmountText.text = _bidFeedbackMessage;
                GameUIFont.EnsureGlyphs(_minBidAmountText, _bidFeedbackMessage);
            }
            else
            {
                _minBidAmountText.text = $"{minBid:N0} G";
            }
        }

        RefreshBidButtons();
    }

    private void OnBidInputChanged(string _) => RefreshBidButtons();

    private void RefreshBidButtons()
    {
        var sessionActive = _session != null && _session.IsActive;
        var canBid = sessionActive;

        if (canBid && _session.TryGetRequiredMinBid(out var minBid))
            canBid = _session.TryValidatePlayerBid(ResolveBidFallback(minBid), out _);

        if (_bidButton != null)
            _bidButton.interactable = canBid;
    }

    private int ResolveBidFallback(int minBid)
    {
        if (_bidInput != null && AuctionBidInputUtility.TryParseAmount(_bidInput.text, out var parsed))
            return parsed;

        var increment = _database != null
            ? _database.GetEnglishBidIncrement()
            : Mathf.Max(1, _session?.BidIncrement ?? 5);
        return minBid + Mathf.Max(1, increment);
    }

    private bool TryResolveBidAmount(out int amount, out string blockReason)
    {
        amount = 0;
        blockReason = null;

        if (_session == null || !_session.IsActive)
        {
            blockReason = "경매가 종료되었습니다.";
            return false;
        }

        var minBid = _session.GetMinBid();
        amount = ResolveBidFallback(minBid);
        return _session.TryValidatePlayerBid(amount, out blockReason);
    }

    private void EnsureLeaderboardLayout(int aiCount)
    {
        if (_tableRoot == null)
            return;

        EnsureAiRowCount(aiCount);

        if (aiCount > 3)
            EnsureTableScroll();
        else if (_tableScroll != null)
            _tableScroll.enabled = false;
    }

    private void EnsureAiRowCount(int aiCount)
    {
        if (_aiRowTemplate == null || _tableRoot == null)
            return;

        if (aiCount > 3 && !_leaderboardScrollReady)
            EnsureTableScroll();

        var parent = GetLeaderboardRowParent();
        var aiRows = CountAiRows(parent);

        while (aiRows < aiCount)
        {
            var clone = Instantiate(_aiRowTemplate, parent);
            clone.name = aiRows == 0 ? "row_ai" : $"row_ai ({aiRows})";
            clone.gameObject.SetActive(true);
            aiRows++;
        }

        CacheGuildRows();
    }

    private static int CountAiRows(Transform parent)
    {
        if (parent == null)
            return 0;

        var count = 0;
        for (var i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i).name.StartsWith("row_ai", StringComparison.Ordinal))
                count++;
        }

        return count;
    }

    private void EnsureTableScroll()
    {
        if (_tableRoot == null || _leaderboardScrollReady)
            return;

        var callum = _tableRoot.Find("callum");
        var callumHeight = callum is RectTransform callumRect ? callumRect.sizeDelta.y + 6f : 34f;

        var viewportGo = new GameObject("RowScrollViewport", typeof(RectTransform));
        var viewport = viewportGo.GetComponent<RectTransform>();
        viewport.SetParent(_tableRoot, false);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = new Vector2(0f, -callumHeight);
        viewportGo.AddComponent<RectMask2D>();

        var contentGo = new GameObject("RowScrollContent", typeof(RectTransform));
        _tableContent = contentGo.GetComponent<RectTransform>();
        _tableContent.SetParent(viewport, false);
        _tableContent.anchorMin = new Vector2(0f, 1f);
        _tableContent.anchorMax = new Vector2(1f, 1f);
        _tableContent.pivot = new Vector2(0.5f, 1f);
        _tableContent.anchoredPosition = Vector2.zero;
        _tableContent.sizeDelta = new Vector2(0f, 0f);

        var layout = contentGo.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 2f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _tableScroll = viewportGo.AddComponent<ScrollRect>();
        _tableScroll.content = _tableContent;
        _tableScroll.viewport = viewport;
        _tableScroll.horizontal = false;
        _tableScroll.vertical = true;
        _tableScroll.movementType = ScrollRect.MovementType.Clamped;

        var rowsToMove = new List<Transform>();
        for (var i = 0; i < _tableRoot.childCount; i++)
        {
            var child = _tableRoot.GetChild(i);
            if (child.name.StartsWith("row", StringComparison.Ordinal))
                rowsToMove.Add(child);
        }

        foreach (var row in rowsToMove)
            row.SetParent(_tableContent, false);

        _leaderboardScrollReady = true;
        CacheGuildRows();
    }

    private void RefreshLeaderboard()
    {
        var ranked = BuildRankedParticipants();

        for (var i = 0; i < _guildRows.Count; i++)
        {
            var row = _guildRows[i];
            if (row.Root == null)
                continue;

            if (i >= ranked.Count)
            {
                row.Root.SetActive(false);
                continue;
            }

            row.Root.SetActive(true);
            var entry = ranked[i];
            var isPlayer = entry.Participant.IsPlayer;

            ApplyRowStyle(row, isPlayer);

            if (row.RankText != null)
                row.RankText.text = entry.Rank.ToString();

            if (row.GuildText != null)
            {
                var guildName = entry.Participant.GuildName;
                row.GuildText.text = guildName;
                GameUIFont.EnsureGlyphs(row.GuildText, guildName);
            }

            if (row.BidText != null)
            {
                var bidLabel = entry.Participant.HasBid
                    ? $"{entry.Participant.BidAmount:N0} G"
                    : WaitingLabel;
                row.BidText.text = bidLabel;
                GameUIFont.EnsureGlyphs(row.BidText, bidLabel);
            }

            if (_tableContent != null && row.Root.transform.parent == _tableContent)
                row.Root.transform.SetSiblingIndex(i);
        }
    }

    private struct RankedParticipant
    {
        public EnglishAuctionParticipant Participant;
        public int Rank;
    }

    private List<RankedParticipant> BuildRankedParticipants()
    {
        var bidders = new List<EnglishAuctionParticipant>();
        var waiting = new List<EnglishAuctionParticipant>();

        foreach (var participant in _session.Participants)
        {
            if (participant.HasBid)
                bidders.Add(participant);
            else
                waiting.Add(participant);
        }

        bidders.Sort((a, b) =>
        {
            var byAmount = b.BidAmount.CompareTo(a.BidAmount);
            if (byAmount != 0)
                return byAmount;

            if (a.IsPlayer == b.IsPlayer)
                return 0;

            return a.IsPlayer ? -1 : 1;
        });

        var result = new List<RankedParticipant>(bidders.Count + waiting.Count);
        for (var i = 0; i < bidders.Count; i++)
            result.Add(new RankedParticipant { Participant = bidders[i], Rank = i + 1 });

        for (var i = 0; i < waiting.Count; i++)
            result.Add(new RankedParticipant { Participant = waiting[i], Rank = bidders.Count + i + 1 });

        return result;
    }

    private void ShowBidFeedback(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        _bidFeedbackMessage = message;
        _bidFeedbackUntil = Time.unscaledTime + 4f;
        RefreshBidControl();
    }

    private void OnBidClicked()
    {
        if (_lot == null || _session == null || GateAuctionManager.Instance == null)
            return;

        if (!TryResolveBidAmount(out var amount, out var blockReason))
        {
            ShowBidFeedback(blockReason);
            return;
        }

        if (!GateAuctionManager.Instance.TryEnglishPlayerBid(_lot.LotId, amount))
            ShowBidFeedback("입찰에 실패했습니다.");
    }

    private void OnExitClicked()
    {
        if (_confirmRoot != null)
        {
            ShowWithdrawConfirm();
            return;
        }

        WithdrawAndClose();
    }

    private void ShowWithdrawConfirm()
    {
        if (_confirmRoot == null)
            return;

        _confirmRoot.transform.SetAsLastSibling();
        _confirmRoot.SetActive(true);
    }

    private void OnConfirmWithdraw() => WithdrawAndClose();

    private void WithdrawAndClose()
    {
        if (_lot != null && GateAuctionManager.Instance != null)
            GateAuctionManager.Instance.TryWithdrawEnglishSession(_lot.LotId);

        if (_confirmRoot != null)
            _confirmRoot.SetActive(false);

        Close();
        SessionEnded?.Invoke();
    }

    private void OnConfirmDismiss()
    {
        if (_confirmRoot != null)
            _confirmRoot.SetActive(false);
    }

    private static TextMeshProUGUI FindTmp(Transform root, string path)
    {
        if (root == null)
            return null;

        var t = string.IsNullOrEmpty(path) ? root : root.Find(path);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    private Button FindButton(Transform root, string nameOrPath)
    {
        if (root == null)
            return null;

        if (nameOrPath.IndexOf('/') >= 0)
        {
            var pathMatch = root.Find(nameOrPath);
            return pathMatch != null ? pathMatch.GetComponent<Button>() : null;
        }

        var direct = root.Find(nameOrPath);
        if (direct != null)
        {
            var directButton = direct.GetComponent<Button>();
            if (directButton != null)
                return directButton;
        }

        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            if (string.Equals(button.name, nameOrPath, StringComparison.Ordinal))
                return button;
        }

        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            if (string.Equals(button.name, nameOrPath, StringComparison.Ordinal))
                return button;
        }

        return null;
    }
}
