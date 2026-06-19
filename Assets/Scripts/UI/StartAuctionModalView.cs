using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>start_auction.prefab — English 라이브 경매 모달.</summary>
[DisallowMultipleComponent]
public sealed class StartAuctionModalView : MonoBehaviour
{
    private const string SettingRoot = "setting";
    private const string ConfirmOverlayPath = "confirm_overlay";
    private const string WaitingLabel = "대기중";

    private GateAuctionLotRuntime _lot;
    private GateDatabase _database;
    private EnglishAuctionSession _session;

    private TextMeshProUGUI _gateInfoLabelsText;
    private TextMeshProUGUI _gateInfoText;
    private Image _gateIconImage;
    private TextMeshProUGUI _gatePriceText;
    private TextMeshProUGUI _gateDeltaText;
    private TextMeshProUGUI _timeText;
    private TextMeshProUGUI _countText;
    private TextMeshProUGUI _playerGoldText;
    private TextMeshProUGUI _minBidAmountText;
    private TMP_InputField _bidInput;
    private Button _bidButton;
    private Button _exitButton;
    private Button _withdrawButton;
    private Button _confirmYesButton;
    private Button _confirmNoButton;
    private Transform _tableRoot;
    private readonly List<CompetingGuildRowBinding> _guildRows = new();

    private GameObject _confirmRoot;
    private bool _uiReady;
    private string _bidFeedbackMessage;
    private float _bidFeedbackUntil;

    public event Action SessionEnded;

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
        GameUIFont.ApplyToHierarchy(this);
    }

    private void OnDestroy()
    {
        UnbindSession();
        if (_exitButton != null)
            _exitButton.onClick.RemoveListener(OnExitClicked);
        if (_withdrawButton != null)
            _withdrawButton.onClick.RemoveListener(OnWithdrawClicked);
        if (_bidButton != null)
            _bidButton.onClick.RemoveListener(OnBidClicked);
        if (_confirmYesButton != null)
            _confirmYesButton.onClick.RemoveListener(OnConfirmWithdraw);
        if (_confirmNoButton != null)
            _confirmNoButton.onClick.RemoveListener(OnConfirmDismiss);
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
        ConfigureOverlayLayout(transform as RectTransform);
        AuctionBidInputUtility.ClearInput(_bidInput);
        RefreshAll();
    }

    /// <summary>오버레이 stretch + setting 중앙 고정. 크기·Scale은 prefab 값 유지.</summary>
    public static void ConfigureOverlayLayout(RectTransform root)
    {
        if (root == null)
            return;

        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = Vector2.zero;
        root.localScale = Vector3.one;

        var setting = root.Find(SettingRoot) as RectTransform;
        if (setting == null)
            return;

        setting.anchorMin = new Vector2(0.5f, 0.5f);
        setting.anchorMax = new Vector2(0.5f, 0.5f);
        setting.pivot = new Vector2(0.5f, 0.5f);
        setting.anchoredPosition = Vector2.zero;
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
        var setting = transform.Find(SettingRoot) ?? transform;
        var centerTop = setting.Find("center_top");
        var gatePriceRoot = centerTop != null ? centerTop.Find("gate_price") : null;
        var gateStatusRoot = centerTop != null ? centerTop.Find("gate_status") : null;
        var right = setting.Find("right_pannel");

        var gateInfoRoot = centerTop != null ? centerTop.Find("gate_info") : null;
        _gateInfoLabelsText = FindTmp(gateInfoRoot, "Text (TMP)");
        _gateInfoText = FindTmp(gateInfoRoot, "Text (TMP) (1)")
                        ?? FindTmp(setting, "left_pannel/Text (TMP) (1)");
        _gateIconImage = gateInfoRoot != null ? gateInfoRoot.Find("icon")?.GetComponent<Image>() : null;

        _gatePriceText = FindTmp(gatePriceRoot, "current_price");
        _gateDeltaText = FindTmp(gatePriceRoot, "calc/Text (TMP)")
                         ?? FindTmp(gatePriceRoot, "input amount")
                         ?? FindTmp(gatePriceRoot, "text");

        _timeText = FindTmp(gateStatusRoot, "time") ?? FindTmp(centerTop, "time");
        _countText = FindTmp(gateStatusRoot, "count") ?? FindTmp(centerTop, "count");

        _playerGoldText = FindTmp(right, "player_gold/gold amount")
                          ?? FindTmp(right, "player_gold");
        var inputRoot = right != null ? right.Find("input_gold") : null;
        _minBidAmountText = FindTmp(inputRoot, "min amount");
        _bidInput = inputRoot != null
            ? inputRoot.GetComponentInChildren<TMP_InputField>(true)
            : null;

        _bidButton = FindButton(right, "Bid_Button");
        _exitButton = FindButton(setting, "center_top/windowname/exit_button")
                        ?? FindButton(setting, "exit_button");
        _withdrawButton = FindButton(right, "withdraw_Button")
                          ?? FindButton(setting, "withdraw_Button");

        CacheConfirmRefs();

        _tableRoot = setting.Find("left_pannel/table");
        DisableRowRaycasts();
        CacheGuildRows();

        if (_gatePriceText == null || _bidButton == null || _tableRoot == null)
        {
            Debug.LogWarning(
                "[StartAuctionModal] UI refs incomplete. " +
                $"price={_gatePriceText != null}, bid={_bidButton != null}, table={_tableRoot != null}",
                this);
        }

        if (_exitButton == null || _withdrawButton == null)
        {
            Debug.LogWarning(
                "[StartAuctionModal] Close buttons missing. " +
                $"exit={_exitButton != null}, withdraw={_withdrawButton != null}",
                this);
        }
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
        if (_tableRoot == null)
            return;

        for (var i = 0; i < _tableRoot.childCount; i++)
        {
            var child = _tableRoot.GetChild(i);
            if (!child.name.StartsWith("row", StringComparison.Ordinal))
                continue;

            _guildRows.Add(BindGuildRow(child));
        }
    }

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
        if (_withdrawButton != null)
            _withdrawButton.onClick.AddListener(OnWithdrawClicked);
        if (_confirmYesButton != null)
            _confirmYesButton.onClick.AddListener(OnConfirmWithdraw);
        if (_confirmNoButton != null)
            _confirmNoButton.onClick.AddListener(OnConfirmDismiss);
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

        RefreshGateInfo();
        RefreshGatePrice();
        RefreshTimerAndCount();
        RefreshBidControl();
        RefreshLeaderboard();
    }

    private void RefreshGateInfo()
    {
        if (_gateInfoLabelsText != null)
        {
            _gateInfoLabelsText.text = "게이트 ID\n등급\n지역\n에너지 %";
            GameUIFont.EnsureGlyphs(_gateInfoLabelsText, _gateInfoLabelsText.text);
        }

        if (_gateInfoText == null)
            return;

        var lot = _lot.Lot;
        var grade = GateGradeUtility.GetDisplayName(lot.grade);
        var energy = _database != null
            ? _database.GetEnergyDisplayPercent(lot)
            : lot.energy * 100f / GateDatabase.EnergyPercentMax;
        var region = string.IsNullOrWhiteSpace(lot.regionDisplay)
            ? lot.regionCode
            : lot.regionDisplay;
        var values =
            $"{lot.gateId}\n{grade}\n{region}\n{energy:0} %";

        _gateInfoText.text = values;
        GameUIFont.EnsureGlyphs(_gateInfoText, values);

        if (_gateIconImage != null)
        {
            var icon = _database != null
                ? _database.GetGradeIcon(lot.grade)
                : GateGradeIconCache.GetIcon(lot.grade);
            if (icon != null)
            {
                _gateIconImage.sprite = icon;
                _gateIconImage.enabled = true;
            }
        }
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
            _countText.text = _session.BidCount.ToString("N0");
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
        var canBid = sessionActive && _session.TryGetRequiredMinBid(out var minBid)
                     && _session.TryValidatePlayerBid(minBid, out _);

        if (_bidButton != null)
            _bidButton.interactable = canBid;
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
        if (_bidInput != null && AuctionBidInputUtility.TryParseAmount(_bidInput.text, out var parsed))
            amount = parsed;
        else
            amount = minBid;

        return _session.TryValidatePlayerBid(amount, out blockReason);
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

        bidders.Sort((a, b) => b.BidAmount.CompareTo(a.BidAmount));

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
        Debug.LogWarning($"[StartAuctionModal] {message}");
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

    private void OnExitClicked() => ShowWithdrawConfirm();

    private void OnWithdrawClicked() => ShowWithdrawConfirm();

    private void ShowWithdrawConfirm()
    {
        if (_confirmRoot == null)
            return;

        _confirmRoot.transform.SetAsLastSibling();
        _confirmRoot.SetActive(true);
    }

    private void OnConfirmWithdraw()
    {
        if (_lot == null || GateAuctionManager.Instance == null)
            return;

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
