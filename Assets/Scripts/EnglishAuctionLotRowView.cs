using Michsky.UI.ModernUIPack;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>English Lot 카드 — 경매시작 시 start_auction 모달 오픈.</summary>
[DisallowMultipleComponent]
public sealed class EnglishAuctionLotRowView : MonoBehaviour, IAuctionLotRowView
{
    private const string BidRootPath = "english/Bid";
    private const string FinishPath = "english/Finish";
    private const string FinishResultSuccess = "경매 종료\n결과: 성공";
    private const string FinishResultFailure = "경매 종료\n결과: 실패";
    private const string BidStartLabel = "경매\n시작";
    private const string BidInProgressLabel = "경매 중";
    private const string BidAckLabel = "확인";

    private GateAuctionLotRuntime _lot;
    private GateDatabase _database;
    private GateHintRowView _hintRowView;

    private TextMeshProUGUI _minBidText;
    private TextMeshProUGUI _aiCountText;
    private TextMeshProUGUI _rewardMinText;
    private TextMeshProUGUI _rewardMaxText;
    private Button _bidButton;
    private TextMeshProUGUI _bidButtonLabel;
    private ProgressBar _energyBar;
    private Image _gateIconImage;
    private GameObject _englishIcon;
    private GameObject _finishOverlay;
    private TextMeshProUGUI _finishResultText;
    private StartAuctionModalView _modal;

    private void Awake()
    {
        _hintRowView = GetComponent<GateHintRowView>() ?? gameObject.AddComponent<GateHintRowView>();
        CacheUiRefs();
        WireControls();
        GameUIFont.ApplyToHierarchy(this);
    }

    private void WireControls()
    {
        if (_bidButton != null)
        {
            _bidButton.onClick.RemoveListener(OnStartBidClicked);
            _bidButton.onClick.AddListener(OnStartBidClicked);
        }
    }

    private void OnDestroy()
    {
        if (_bidButton != null)
            _bidButton.onClick.RemoveListener(OnStartBidClicked);
    }

    public void Bind(GateAuctionLotRuntime lot, GateDatabase database)
    {
        _lot = lot;
        _database = database;
        gameObject.SetActive(true);
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

    public void SetModal(StartAuctionModalView modal) => _modal = modal;

    private StartAuctionModalView ResolveModal()
    {
        if (_modal != null)
            return _modal;

        _modal = GetComponentInParent<AuctionPanelController>()?.GetComponentInChildren<StartAuctionModalView>(true);
        if (_modal == null)
            _modal = FindFirstObjectByType<StartAuctionModalView>(FindObjectsInactive.Include);

        return _modal;
    }

    public void RefreshInteractiveState()
    {
        if (_lot == null)
            return;

        _hintRowView?.Refresh();

        if (_bidButton != null)
        {
            var canStart = _lot.State == GateAuctionLotState.Bidding && !_lot.IsEnglishSessionActive;
            var canReopen = _lot.State == GateAuctionLotState.Bidding && _lot.IsEnglishSessionActive;
            var canAck = _lot.State == GateAuctionLotState.Won || _lot.State == GateAuctionLotState.Lost;

            _bidButton.interactable = canStart || canReopen || canAck;

            if (_bidButtonLabel != null)
            {
                var buttonLabel = _lot.State switch
                {
                    GateAuctionLotState.Won => BidAckLabel,
                    GateAuctionLotState.Lost => BidAckLabel,
                    _ when _lot.IsEnglishSessionActive => BidInProgressLabel,
                    _ => BidStartLabel
                };
                _bidButtonLabel.text = buttonLabel;
                GameUIFont.EnsureGlyphs(_bidButtonLabel, buttonLabel);
            }
        }

        if (_lot.State == GateAuctionLotState.Acknowledged)
        {
            ShowFinishOverlay(true);
            return;
        }

        ShowFinishOverlay(_lot.State == GateAuctionLotState.Won || _lot.State == GateAuctionLotState.Lost);
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

    private void CacheUiRefs()
    {
        _minBidText = FindTmp($"{BidRootPath}/price/amount_start");
        _aiCountText = FindTmp($"{BidRootPath}/AI_number/ppl");
        _rewardMinText = FindTmp("Gate_Info/Reward/Gold_Min/Amount_min");
        _rewardMaxText = FindTmp("Gate_Info/Reward/Gold_Max/Amount_max");

        var bidGo = FindTransform($"{BidRootPath}/start_bid");
        if (bidGo != null)
        {
            _bidButton = bidGo.GetComponent<Button>();
            _bidButtonLabel = bidGo.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        var energyGo = FindTransform("Gate_Info/Energy_Percent");
        if (energyGo != null)
            _energyBar = energyGo.GetComponent<ProgressBar>();

        var gateIconGo = FindTransform("Gate_Icon");
        if (gateIconGo != null)
            _gateIconImage = gateIconGo.GetComponent<Image>();

        var english = FindTransform($"{BidRootPath}/start_bid/english_Icon");
        if (english != null)
            _englishIcon = english.gameObject;

        _finishOverlay = FindTransform(FinishPath)?.gameObject;
        _finishResultText = FindTmp($"{FinishPath}/result");
        if (_finishOverlay != null)
            _finishOverlay.SetActive(false);
    }

    private void EnsureUiRefs()
    {
        if (_bidButton != null && _minBidText != null)
            return;

        CacheUiRefs();
        WireControls();
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
        if (_aiCountText != null)
            _aiCountText.text = $"{Mathf.Max(1, gateLot.englishAiCount)}명";

        if (_gateIconImage != null)
        {
            var icon = _database != null
                ? _database.GetGradeIcon(gateLot.grade)
                : GateGradeIconCache.GetIcon(gateLot.grade);
            if (icon != null)
                _gateIconImage.sprite = icon;
        }

        if (_englishIcon != null)
            _englishIcon.SetActive(true);

        if (_energyBar != null)
        {
            _energyBar.isOn = false;
            var percent = _database != null
                ? _database.GetEnergyDisplayPercent(gateLot)
                : gateLot.energy * 100f / GateDatabase.EnergyPercentMax;
            _energyBar.ChangeValue(Mathf.Clamp(percent, 0f, 100f));
        }
    }

    private void OnStartBidClicked()
    {
        if (_lot == null || GateAuctionManager.Instance == null)
            return;

        if (_lot.State == GateAuctionLotState.Won || _lot.State == GateAuctionLotState.Lost)
        {
            GateAuctionManager.Instance.TryAcknowledge(_lot.LotId);
            return;
        }

        if (_lot.IsEnglishSessionActive)
        {
            var modal = ResolveModal();
            if (modal != null && _lot.EnglishSession != null)
                modal.Open(_lot, _database, _lot.EnglishSession);
            return;
        }

        if (_modal == null)
            _modal = ResolveModal();

        if (_modal == null)
        {
            Debug.LogWarning("[EnglishAuctionLotRowView] StartAuctionModalView not assigned.", this);
            return;
        }

        if (!GateAuctionManager.Instance.TryStartEnglishSession(_lot, out var session))
        {
            Debug.LogWarning(
                $"[EnglishAuctionLotRowView] Cannot start English session lot={_lot.LotId} " +
                $"state={_lot.State} english={_lot.IsEnglish} active={GateAuctionManager.Instance.ActiveEnglishLotId}.",
                this);
            return;
        }

        _modal.Open(_lot, _database, session);
        RefreshInteractiveState();
    }

    private static string FormatGold(int amount) => $"{amount:N0} G";

    private Transform FindTransform(string path)
    {
        var found = transform.Find(path);
        if (found != null)
            return found;

        if (path.StartsWith("english/"))
            return transform.Find(path["english/".Length..]);

        return transform.Find($"english/{path}");
    }

    private TextMeshProUGUI FindTmp(string path)
    {
        var t = FindTransform(path);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }
}
