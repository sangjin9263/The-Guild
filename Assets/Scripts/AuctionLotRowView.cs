using Michsky.UI.ModernUIPack;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>경매 Lot 카드 1장 — GateAuctionLotRuntime 바인딩.</summary>
[DisallowMultipleComponent]
public sealed class AuctionLotRowView : MonoBehaviour, IAuctionLotRowView
{
    private const int BidStepGoldFallback = 5;
    private const string ResultPanelPath = "ebay/AuctionResultPanel";
    private const string ResultCardPath = "ebay/AuctionResultPanel/ResultCard";
    private const string ResultStatusPath = "ebay/AuctionResultPanel/ResultCard/StatusText";
    private const string ResultAckPath = "ebay/AuctionResultPanel/ResultCard/AckButton";
    private const string ResultBidsPath = "ebay/AuctionResultPanel/ResultCard/result";
    private const string FinishPath = "ebay/Finish";
    private const string FinishResultSuccess = "경매 종료\n결과: 성공";
    private const string FinishResultFailure = "경매 종료\n결과: 실패";
    private const string BidRootPath = "ebay/Bid";
    private const string BidInputRootPath = "ebay/Bid/input_gold";
    private static readonly string[] AiBidderLabels = { "AI1", "AI2", "AI3" };

    private GateAuctionLotRuntime _lot;
    private GateDatabase _database;

    private TMP_InputField _bidInput;
    private TextMeshProUGUI _minBidText;
    private TextMeshProUGUI _rewardMinText;
    private TextMeshProUGUI _rewardMaxText;
    private TextMeshProUGUI _statusText;
    private GateHintRowView _hintRowView;
    private Button _bidButton;
    private Button _upButton;
    private Button _downButton;
    private Button _ackButton;
    private ProgressBar _energyBar;
    private Image _gateIconImage;
    private GameObject _ebayIcon;
    private GameObject _englishIcon;
    private GameObject _resultPanel;
    private GameObject _finishOverlay;
    private TextMeshProUGUI _finishResultText;
    private GameObject _bidInaction;
    private GameObject _resultCard;
    private TextMeshProUGUI _bidInactionCountdownText;
    private TextMeshProUGUI _playerBidText;
    private readonly TextMeshProUGUI[] _aiBidTexts = new TextMeshProUGUI[GateAuctionLotRuntime.AiBidderCount];

    private int _selectedBid;
    private int _minBidGold;

    private void Awake()
    {
        _hintRowView = GetComponent<GateHintRowView>() ?? gameObject.AddComponent<GateHintRowView>();
        CacheUiRefs();
        WireBidControls();
        GameUIFont.ApplyToHierarchy(this);
    }

    private void WireBidControls()
    {
        if (_bidButton != null)
        {
            _bidButton.onClick.RemoveListener(OnBidClicked);
            _bidButton.onClick.AddListener(OnBidClicked);
        }

        if (_upButton != null)
        {
            _upButton.onClick.RemoveListener(OnBidUp);
            _upButton.onClick.AddListener(OnBidUp);
        }

        if (_downButton != null)
        {
            _downButton.onClick.RemoveListener(OnBidDown);
            _downButton.onClick.AddListener(OnBidDown);
        }

        if (_ackButton != null)
        {
            _ackButton.onClick.RemoveListener(OnAckClicked);
            _ackButton.onClick.AddListener(OnAckClicked);
        }

        if (_bidInput != null)
        {
            _bidInput.onEndEdit.RemoveListener(OnBidInputEndEdit);
            _bidInput.onEndEdit.AddListener(OnBidInputEndEdit);
        }
    }

    private void OnDestroy()
    {
        if (_bidButton != null)
            _bidButton.onClick.RemoveListener(OnBidClicked);
        if (_upButton != null)
            _upButton.onClick.RemoveListener(OnBidUp);
        if (_downButton != null)
            _downButton.onClick.RemoveListener(OnBidDown);
        if (_ackButton != null)
            _ackButton.onClick.RemoveListener(OnAckClicked);
        if (_bidInput != null)
            _bidInput.onEndEdit.RemoveListener(OnBidInputEndEdit);
    }

    private void Update()
    {
        if (_lot == null || _lot.State != GateAuctionLotState.PendingResult)
            return;

        RefreshPendingCountdown();
    }

    public void Bind(GateAuctionLotRuntime lot, GateDatabase database)
    {
        _lot = lot;
        _database = database;
        gameObject.SetActive(true);

        _minBidGold = lot.UserMinBid;
        _selectedBid = Mathf.Max(lot.OpeningBid, _minBidGold);
        EnsureUiRefs();
        RefreshStaticFields();
        _hintRowView?.Bind(lot, database);
        RefreshInteractiveState();
    }

    public void Clear()
    {
        _lot = null;
        _hintRowView?.Clear();
        gameObject.SetActive(false);
    }

    private void CacheUiRefs()
    {
        _bidInput = FindBidInput();
        _minBidText = FindTmp($"{BidRootPath}/price/amount_start");
        _rewardMinText = FindTmp("Gate_Info/Reward/Gold_Min/Amount_min");
        _rewardMaxText = FindTmp("Gate_Info/Reward/Gold_Max/Amount_max");

        var bidGo = FindTransform($"{BidRootPath}/start_bid");
        if (bidGo != null)
            _bidButton = bidGo.GetComponent<Button>();

        var upGo = FindTransform($"{BidRootPath}/input_gold/Up");
        if (upGo != null)
            _upButton = upGo.GetComponent<Button>();

        var downGo = FindTransform($"{BidRootPath}/input_gold/Down");
        if (downGo != null)
            _downButton = downGo.GetComponent<Button>();

        var energyGo = FindTransform("Gate_Info/Energy_Percent");
        if (energyGo != null)
            _energyBar = energyGo.GetComponent<ProgressBar>();

        var gateIconGo = FindTransform("Gate_Icon");
        if (gateIconGo != null)
            _gateIconImage = gateIconGo.GetComponent<Image>();

        var ebay = FindTransform($"{BidRootPath}/start_bid/ebay_Icon");
        if (ebay != null)
            _ebayIcon = ebay.gameObject;

        var english = FindTransform($"{BidRootPath}/start_bid/english_Icon");
        if (english != null)
            _englishIcon = english.gameObject;

        _finishOverlay = FindTransform(FinishPath)?.gameObject;
        _finishResultText = FindTmp($"{FinishPath}/result");
        if (_finishOverlay != null)
            _finishOverlay.SetActive(false);

        if (_bidInput != null)
            AuctionBidInputUtility.Configure(_bidInput);

        CacheResultPanelRefs();
        LogMissingBidRefs();
    }

    private TMP_InputField FindBidInput()
    {
        var inputRoot = FindTransform(BidInputRootPath);
        if (inputRoot == null)
            return null;

        return inputRoot.GetComponentInChildren<TMP_InputField>(true);
    }

    private void EnsureUiRefs()
    {
        var rebinding = _bidInput == null || _minBidText == null || _bidButton == null;
        if (!rebinding)
            return;

        CacheUiRefs();
        WireBidControls();
    }

    private void LogMissingBidRefs()
    {
        if (_bidInput == null)
            Debug.LogWarning("[AuctionLotRowView] Bid/input_gold TMP_InputField not found.", this);
        if (_minBidText == null)
            Debug.LogWarning("[AuctionLotRowView] Bid/price/amount_start not found.", this);
        if (_bidButton == null)
            Debug.LogWarning("[AuctionLotRowView] Bid/start_bid button not found.", this);
    }

    private Transform FindTransform(string path)
    {
        var found = transform.Find(path);
        if (found != null)
            return found;

        if (path.StartsWith("ebay/"))
            return transform.Find(path["ebay/".Length..]);

        return transform.Find($"ebay/{path}");
    }

    private void CacheResultPanelRefs()
    {
        var panel = FindTransform(ResultPanelPath);
        if (panel == null)
        {
            Debug.LogWarning("[AuctionLotRowView] AuctionResultPanel not found on lot prefab.", this);
            return;
        }

        _resultPanel = panel.gameObject;
        _bidInaction = panel.Find("bid_inaction")?.gameObject;
        _resultCard = panel.Find("ResultCard")?.gameObject
                      ?? FindTransform(ResultCardPath)?.gameObject;

        _bidInactionCountdownText = _bidInaction != null
            ? _bidInaction.transform.Find("bid_inaction (1)")?.GetComponent<TextMeshProUGUI>()
            : null;

        _statusText = FindTransform(ResultStatusPath)?.GetComponent<TextMeshProUGUI>()
                      ?? panel.Find("ResultCard/StatusText")?.GetComponent<TextMeshProUGUI>()
                      ?? panel.Find("StatusText")?.GetComponent<TextMeshProUGUI>();

        _ackButton = FindTransform(ResultAckPath)?.GetComponent<Button>()
                     ?? panel.Find("ResultCard/AckButton")?.GetComponent<Button>()
                     ?? panel.Find("AckButton")?.GetComponent<Button>();

        var resultRoot = panel.Find("ResultCard/result") ?? FindTransform(ResultBidsPath);
        if (resultRoot != null)
        {
            _playerBidText = resultRoot.Find("player")?.GetComponent<TextMeshProUGUI>();
            for (var i = 0; i < AiBidderLabels.Length; i++)
                _aiBidTexts[i] = resultRoot.Find(AiBidderLabels[i])?.GetComponent<TextMeshProUGUI>();
        }

        _resultPanel.SetActive(false);
    }

    private TextMeshProUGUI FindTmp(string path)
    {
        var t = FindTransform(path);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    private void RefreshStaticFields()
    {
        if (_lot == null)
            return;

        var gateLot = _lot.Lot;
        var band = gateLot.auction;

        if (_rewardMinText != null)
            _rewardMinText.text = "-";

        if (_rewardMaxText != null)
            _rewardMaxText.text = "-";

        if (_minBidText != null)
            _minBidText.text = FormatGold(band.bidBandMin);

        if (_gateIconImage != null)
        {
            Sprite icon = null;
            if (_database != null)
                icon = _database.GetGradeIcon(gateLot.grade);
            else
                icon = GateGradeIconCache.GetIcon(gateLot.grade);

            if (icon != null)
                _gateIconImage.sprite = icon;
        }

        if (_ebayIcon != null)
            _ebayIcon.SetActive(gateLot.auctionType == AuctionType.Ebay);
        if (_englishIcon != null)
            _englishIcon.SetActive(gateLot.auctionType == AuctionType.English);

        if (_energyBar != null)
        {
            _energyBar.isOn = false;
            var percent = _database != null
                ? _database.GetEnergyDisplayPercent(gateLot)
                : gateLot.energy * 100f / GateDatabase.EnergyPercentMax;
            _energyBar.ChangeValue(Mathf.Clamp(percent, 0f, 100f));
        }

        RefreshBidAmountDisplay();
    }

    private static string FormatGold(int amount) => $"{amount:N0} G";
    private static string FormatBidGold(int amount) => $"{amount:N0}G";

    private void RefreshPendingCountdown()
    {
        if (_bidInactionCountdownText == null)
            return;

        var seconds = Mathf.CeilToInt(_lot.RemainingSeconds);
        _bidInactionCountdownText.text = $"...{seconds}초";
    }

    private void RefreshBidResultTexts()
    {
        var userBid = _lot.UserBid.GetValueOrDefault();

        if (_playerBidText != null)
            _playerBidText.text = $"내 입찰: {FormatBidGold(userBid)}";

        var aiBids = _lot.AiBids;
        for (var i = 0; i < _aiBidTexts.Length; i++)
        {
            if (_aiBidTexts[i] == null)
                continue;

            var label = i < AiBidderLabels.Length ? AiBidderLabels[i] : $"AI{i + 1}";
            var amount = i < aiBids.Count ? aiBids[i] : 0;
            _aiBidTexts[i].text = $"{label} : {FormatBidGold(amount)}";
        }
    }

    private void ShowFinishOverlay(bool visible)
    {
        if (_finishOverlay == null)
            return;

        if (visible)
            RefreshFinishResult();

        _finishOverlay.SetActive(visible);
    }

    private void RefreshFinishResult()
    {
        if (_finishResultText == null || _lot == null)
            return;

        var text = _lot.PlayerWonAuction ? FinishResultSuccess : FinishResultFailure;
        _finishResultText.text = text;
        GameUIFont.EnsureGlyphs(_finishResultText, text);
    }

    private void SetResultPhase(bool pending)
    {
        if (_bidInaction != null)
            _bidInaction.SetActive(pending);

        if (_resultCard != null)
            _resultCard.SetActive(!pending);
    }

    private void RefreshBidAmountDisplay()
    {
        AuctionBidInputUtility.ClearInput(_bidInput);
    }

    private int CommitBidFromInput()
    {
        _selectedBid = AuctionBidInputUtility.ResolveAmount(
            _bidInput, _selectedBid, _minBidGold, BidStep());

        if (AuctionBidInputUtility.HasUserInput(_bidInput))
            AuctionBidInputUtility.SetAmount(_bidInput, _selectedBid);

        return _selectedBid;
    }

    public void RefreshInteractiveState()
    {
        if (_lot == null)
            return;

        _hintRowView?.Refresh();

        var canEditBid = _lot.State == GateAuctionLotState.Bidding;
        var showResult = _lot.State == GateAuctionLotState.PendingResult
                         || _lot.State == GateAuctionLotState.Won
                         || _lot.State == GateAuctionLotState.Lost;

        if (_bidButton != null)
            _bidButton.interactable = canEditBid;
        if (_upButton != null)
            _upButton.interactable = canEditBid;
        if (_downButton != null)
            _downButton.interactable = canEditBid;
        if (_bidInput != null)
            _bidInput.interactable = canEditBid;

        if (_lot.State == GateAuctionLotState.Acknowledged)
        {
            if (_resultPanel != null)
                _resultPanel.SetActive(false);

            ShowFinishOverlay(true);
            return;
        }

        if (_finishOverlay != null)
            _finishOverlay.SetActive(false);

        if (_resultPanel != null)
            _resultPanel.SetActive(showResult);

        if (!showResult)
            return;

        switch (_lot.State)
        {
            case GateAuctionLotState.PendingResult:
                SetResultPhase(pending: true);
                RefreshPendingCountdown();
                break;

            case GateAuctionLotState.Won:
                SetResultPhase(pending: false);
                if (_statusText != null)
                    _statusText.text = "낙찰 성공!\n확인 후 입장권을 받습니다.";
                RefreshBidResultTexts();
                if (_ackButton != null)
                    _ackButton.gameObject.SetActive(true);
                break;

            case GateAuctionLotState.Lost:
                SetResultPhase(pending: false);
                if (_statusText != null)
                    _statusText.text = "낙찰 실패\n입찰금이 환불되었습니다.";
                RefreshBidResultTexts();
                if (_ackButton != null)
                    _ackButton.gameObject.SetActive(true);
                break;
        }
    }

    private int BidStep()
    {
        if (_lot != null && _lot.BidIncrement > 0)
            return _lot.BidIncrement;

        if (_database != null)
            return _database.GetEbayBidIncrement();

        return BidStepGoldFallback;
    }

    private void OnBidInputEndEdit(string _)
    {
        if (_lot == null || _lot.State != GateAuctionLotState.Bidding)
            return;

        if (!AuctionBidInputUtility.HasUserInput(_bidInput))
            return;

        _selectedBid = AuctionBidInputUtility.ResolveAmount(
            _bidInput, _selectedBid, _minBidGold, BidStep());
        AuctionBidInputUtility.SetAmount(_bidInput, _selectedBid);
    }

    private void OnBidUp()
    {
        if (_lot == null || _lot.State != GateAuctionLotState.Bidding)
            return;

        _selectedBid = Mathf.Max(_selectedBid + BidStep(), _minBidGold);
    }

    private void OnBidDown()
    {
        if (_lot == null || _lot.State != GateAuctionLotState.Bidding)
            return;

        _selectedBid = Mathf.Max(_selectedBid - BidStep(), _minBidGold);
    }

    private void OnBidClicked()
    {
        if (_lot == null || GateAuctionManager.Instance == null)
            return;

        var bidAmount = CommitBidFromInput();
        GateAuctionManager.Instance.TryBid(_lot.LotId, bidAmount);
    }

    private void OnAckClicked()
    {
        if (_lot == null || GateAuctionManager.Instance == null)
            return;

        GateAuctionManager.Instance.TryAcknowledge(_lot.LotId);
    }
}
