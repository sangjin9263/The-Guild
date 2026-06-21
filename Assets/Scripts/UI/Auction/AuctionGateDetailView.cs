using Michsky.UI.ModernUIPack;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>AuctionPanel_re Lotinfo — gate_card_ebay / gate_card_eng 상세 바인딩.</summary>
[DisallowMultipleComponent]
public sealed class AuctionGateDetailView : MonoBehaviour
{
    private Transform _ebayCard;
    private Transform _engCard;
    private AuctionHelpPopupView _helpPopup;

    private GateAuctionLotRuntime _lot;
    private GateDatabase _database;
    private StartAuctionModalView _englishModal;

    private Image _ebayGateIcon;
    private Image _ebayMainImage;
    private Image _engGateIcon;
    private Image _engMainImage;
    private TextMeshProUGUI _ebayTitle;
    private TextMeshProUGUI _engTitle;
    private TextMeshProUGUI _ebayMinBid;
    private TextMeshProUGUI _engMinBid;
    private TextMeshProUGUI _ebayAuctionType;
    private TextMeshProUGUI _engAuctionType;
    private TextMeshProUGUI _engAiCount;
    private ProgressBar _ebayEnergyBar;
    private ProgressBar _engEnergyBar;
    private TextMeshProUGUI _ebayEnergyPercent;
    private TextMeshProUGUI _engEnergyPercent;

    private TMP_InputField _ebayBidInput;
    private Button _ebayBidButton;
    private Button _engStartButton;

    private Button _ebayBuyInfoButton;
    private Button _engBuyInfoButton;
    private Button _ebayHelpButton;
    private Button _engHelpButton;
    private TextMeshProUGUI _ebayBuyInfoLabelText;
    private TextMeshProUGUI _engBuyInfoLabelText;
    private TextMeshProUGUI _ebayHintAmount;
    private TextMeshProUGUI _engHintAmount;
    private GameObject _ebayHintBefore;
    private GameObject _engHintBefore;
    private GameObject _ebayHintAfter;
    private GameObject _engHintAfter;
    private TextMeshProUGUI _ebayHintText;
    private TextMeshProUGUI _engHintText;

    private const string BuyInfoDefaultText = "정보 구매";
    private const string BuyInfoPurchasedText = "구매 완료";
    private const string InProgressLabel = "입찰 진행 중";

    private GameObject _ebayResultRoot;
    private TextMeshProUGUI _ebayResultStatus;
    private TextMeshProUGUI _ebayResultCountdown;
    private GameObject _ebayStatusSucc;
    private GameObject _ebayStatusFail;
    private GameObject _ebayBidsRoot;

    private GameObject _engResultRoot;
    private TextMeshProUGUI _engResultStatus;
    private GameObject _engCountRoot;
    private GameObject _engStatusSucc;
    private GameObject _engStatusFail;
    private TextMeshProUGUI _engBidText;
    private TextMeshProUGUI _engWinnerNameText;
    private TextMeshProUGUI _ebayPlayerBidText;
    private readonly TextMeshProUGUI[] _ebayAiBidTexts = new TextMeshProUGUI[3];
    private static readonly string[] AiLabels = { "AI1", "AI2", "AI3" };

    private readonly struct EbayBidRankEntry
    {
        public readonly string Label;
        public readonly int Amount;
        public readonly int CombatPower;
        public readonly bool IsPlayer;

        public EbayBidRankEntry(string label, int amount, int combatPower, bool isPlayer)
        {
            Label = label;
            Amount = amount;
            CombatPower = combatPower;
            IsPlayer = isPlayer;
        }
    }

    public void Initialize(Transform lotInfoRoot, StartAuctionModalView englishModal, AuctionHelpPopupView helpPopup)
    {
        SetEnglishModal(englishModal);
        _helpPopup = helpPopup;

        _ebayCard = lotInfoRoot.Find("gate_card_ebay");
        _engCard = lotInfoRoot.Find("gate_card_eng");

        CacheCardRefs(_ebayCard, isEbay: true);
        CacheCardRefs(_engCard, isEbay: false);
        CacheEbayResultRefs(_ebayCard);
        CacheEnglishResultRefs(_engCard);
        WireControls();
        HideAll();
    }

    public void SetEnglishModal(StartAuctionModalView englishModal) => _englishModal = englishModal;

    public void Bind(GateAuctionLotRuntime lot, GateDatabase database)
    {
        _lot = lot;
        _database = database;
        _helpPopup?.Hide();

        if (lot == null)
        {
            HideAll();
            return;
        }

        var isEbay = !lot.IsEnglish;
        if (_ebayCard != null)
            _ebayCard.gameObject.SetActive(isEbay);
        if (_engCard != null)
            _engCard.gameObject.SetActive(!isEbay);

        if (isEbay)
            ApplyLotToCard(lot, database, isEbay: true);
        else
            ApplyLotToCard(lot, database, isEbay: false);

        RefreshInteractiveState();
    }

    public void Clear()
    {
        _lot = null;
        _helpPopup?.Hide();
        HideAll();
    }

    public void RefreshInteractiveState()
    {
        if (_lot == null)
            return;

        if (_lot.IsEnglish)
        {
            RefreshHintState(isEbay: false);
            RefreshEnglishControls();
            RefreshEnglishResultPanel();
            return;
        }

        RefreshHintState(isEbay: true);
        RefreshEbayControls();
        RefreshEbayResultPanel();
    }

    private void Update()
    {
        if (_lot == null)
            return;

        if (!_lot.IsEnglish && _lot.State == GateAuctionLotState.PendingResult)
            RefreshEbayResultPanel();
        else if (_lot.IsEnglish &&
                 (_lot.IsEnglishSessionActive ||
                  _lot.State is GateAuctionLotState.Won or GateAuctionLotState.Lost))
            RefreshEnglishResultPanel();
    }

    private void HideAll()
    {
        if (_ebayCard != null)
            _ebayCard.gameObject.SetActive(false);
        if (_engCard != null)
            _engCard.gameObject.SetActive(false);
        if (_ebayResultRoot != null)
            _ebayResultRoot.SetActive(false);
        if (_engResultRoot != null)
            _engResultRoot.SetActive(false);
    }

    private void ApplyLotToCard(GateAuctionLotRuntime lot, GateDatabase database, bool isEbay)
    {
        var card = isEbay ? _ebayCard : _engCard;
        if (card == null)
            return;

        var title = isEbay ? _ebayTitle : _engTitle;
        var minBid = isEbay ? _ebayMinBid : _engMinBid;
        var auctionType = isEbay ? _ebayAuctionType : _engAuctionType;
        var gateIcon = isEbay ? _ebayGateIcon : _engGateIcon;
        var mainImage = isEbay ? _ebayMainImage : _engMainImage;
        var energyBar = isEbay ? _ebayEnergyBar : _engEnergyBar;
        var energyPercent = isEbay ? _ebayEnergyPercent : _engEnergyPercent;

        var gateName = lot.Lot.gateId;
        if (title != null)
            title.text = string.IsNullOrWhiteSpace(gateName) ? "GT-7781" : gateName;

        if (minBid != null)
            minBid.text = $"{lot.UserMinBid:N0} G";

        if (auctionType != null)
            auctionType.text = isEbay ? "Ebay" : "English";

        if (gateIcon != null)
        {
            var icon = GateGradeIconCache.GetIcon(lot.Lot.grade);
            if (icon != null)
                gateIcon.sprite = icon;
        }

        if (mainImage != null)
        {
            var preview = GateAuctionImageCache.GetImage(lot.Lot.grade);
            if (preview != null)
            {
                mainImage.sprite = preview;
                mainImage.color = Color.white;
            }
        }

        var percent = database != null
            ? database.GetEnergyDisplayPercent(lot.Lot)
            : lot.Lot.energy * 100f / GateDatabase.EnergyPercentMax;
        percent = Mathf.Clamp(percent, 0f, 100f);

        if (energyBar != null)
        {
            energyBar.isOn = false;
            energyBar.ChangeValue(percent);
        }

        if (energyPercent != null)
            energyPercent.text = $"{percent:0}%";

        if (!isEbay && _engAiCount != null)
            _engAiCount.text = $"{Mathf.Max(1, lot.Lot.englishAiCount)}명";
    }

    private void RefreshEbayControls()
    {
        var canBid = _lot.State == GateAuctionLotState.Bidding;
        var locked = GateAuctionManager.Instance != null &&
                     GateAuctionManager.Instance.IsEnglishGoldSpendLocked;

        if (_ebayBidButton != null)
            _ebayBidButton.interactable = canBid && !locked;
        if (_ebayBidInput != null)
            _ebayBidInput.interactable = canBid && !locked;

        RefreshEbayBidInput();
    }

    private void RefreshEbayBidInput()
    {
        if (_ebayBidInput == null || _lot == null)
            return;

        if (_lot.HasUserBid)
        {
            AuctionBidInputUtility.SetAmount(_ebayBidInput, _lot.UserBid.GetValueOrDefault());
            return;
        }

        if (_lot.State == GateAuctionLotState.Bidding)
            AuctionBidInputUtility.ClearInput(_ebayBidInput);
    }

    private void RefreshEnglishControls()
    {
        var canStart = _lot.State == GateAuctionLotState.Bidding && !_lot.IsEnglishSessionActive;
        var locked = GateAuctionManager.Instance != null &&
                     GateAuctionManager.Instance.IsEnglishGoldSpendLocked &&
                     GateAuctionManager.Instance.ActiveEnglishLotId != _lot.LotId;

        if (_engStartButton != null)
            _engStartButton.interactable = canStart && !locked;
    }

    private void RefreshHintState(bool isEbay)
    {
        if (_lot == null)
            return;

        var buyButton = isEbay ? _ebayBuyInfoButton : _engBuyInfoButton;
        var buyLabel = isEbay ? _ebayBuyInfoLabelText : _engBuyInfoLabelText;
        var hintAmount = isEbay ? _ebayHintAmount : _engHintAmount;
        var before = isEbay ? _ebayHintBefore : _engHintBefore;
        var after = isEbay ? _ebayHintAfter : _engHintAfter;
        var hintText = isEbay ? _ebayHintText : _engHintText;

        var englishLocked = GateAuctionManager.Instance != null &&
                            GateAuctionManager.Instance.IsEnglishGoldSpendLocked;
        var canPurchase = _lot.State == GateAuctionLotState.Bidding &&
                          !_lot.HintPurchased &&
                          !englishLocked;
        var showHintArea = _lot.HintPurchased || _lot.HintRevealed;

        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(_lot.State == GateAuctionLotState.Bidding);
            buyButton.interactable = canPurchase;
        }

        if (buyLabel != null)
            buyLabel.text = _lot.HintPurchased ? BuyInfoPurchasedText : BuyInfoDefaultText;

        if (hintAmount != null)
        {
            if (_lot.HintPurchased)
            {
                hintAmount.gameObject.SetActive(false);
            }
            else if (_database != null && _lot.State == GateAuctionLotState.Bidding)
            {
                hintAmount.gameObject.SetActive(true);
                var cost = _database.GetBrokerHintCost(_lot.Lot.grade, _lot.Lot.auction.bidBandMin);
                hintAmount.text = $"{cost:N0} G";
            }
        }

        if (before != null)
            before.SetActive(canPurchase);

        if (after != null)
        {
            after.SetActive(showHintArea);
            DisableAfPayClick(after);
        }

        if (hintText != null)
        {
            hintText.text = _lot.HintRevealed
                ? BuildRevealedHintText()
                : _lot.HintPurchased
                    ? "힌트 확인"
                    : string.Empty;
        }
    }

    private static void DisableAfPayClick(GameObject afPay)
    {
        var button = afPay.GetComponent<Button>();
        if (button != null)
            Object.Destroy(button);

        var image = afPay.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = false;
    }

    private string BuildRevealedHintText()
    {
        if (_lot == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(_lot.BrokerHintInfoText))
            return _lot.BrokerHintInfoText;

        return _lot.BrokerHintText;
    }

    private void RefreshEnglishResultPanel()
    {
        if (_lot == null || _engResultRoot == null)
            return;

        var inSession = _lot.IsEnglishSessionActive;
        var hasResult = _lot.State is GateAuctionLotState.Won or GateAuctionLotState.Lost;
        var show = inSession || hasResult;

        _engResultRoot.SetActive(show);
        if (!show)
            return;

        if (inSession)
        {
            SetActiveSafe(_engCountRoot, true);
            SetActiveSafe(_engResultStatus?.gameObject, false);
            SetActiveSafe(_engStatusSucc, false);
            SetActiveSafe(_engStatusFail, false);
            SetActiveSafe(_engBidText?.gameObject, false);
            SetActiveSafe(_engWinnerNameText?.gameObject, false);
            return;
        }

        SetActiveSafe(_engCountRoot, false);
        SetActiveSafe(_engResultStatus?.gameObject, true);
        SetActiveSafe(_engBidText?.gameObject, true);
        SetActiveSafe(_engWinnerNameText?.gameObject, true);

        var won = _lot.State == GateAuctionLotState.Won;
        SetActiveSafe(_engStatusSucc, won);
        SetActiveSafe(_engStatusFail, !won);

        if (_engResultStatus != null)
        {
            _engResultStatus.text = won
                ? "낙찰 성공!"
                : "낙찰 실패";
        }

        if (_engWinnerNameText != null)
        {
            var winner = _lot.EnglishWinnerGuildName;
            _engWinnerNameText.text = string.IsNullOrWhiteSpace(winner) ? "-" : winner;
        }
    }

    private void RefreshEbayResultPanel()
    {
        if (_lot == null || _ebayResultRoot == null)
            return;

        var show = _lot.State is GateAuctionLotState.PendingResult
            or GateAuctionLotState.Won
            or GateAuctionLotState.Lost;

        _ebayResultRoot.SetActive(show);
        if (!show)
            return;

        switch (_lot.State)
        {
            case GateAuctionLotState.PendingResult:
                SetActiveSafe(_ebayResultCountdown?.gameObject, true);
                SetActiveSafe(_ebayResultStatus?.gameObject, false);
                SetActiveSafe(_ebayStatusSucc, false);
                SetActiveSafe(_ebayStatusFail, false);
                SetActiveSafe(_ebayBidsRoot, false);
                if (_ebayResultCountdown != null)
                    _ebayResultCountdown.text = $"{Mathf.CeilToInt(_lot.RemainingSeconds)}";
                break;

            case GateAuctionLotState.Won:
                ShowEbayResultFinal(won: true);
                break;

            case GateAuctionLotState.Lost:
                ShowEbayResultFinal(won: false);
                break;
        }

        if (_lot.State != GateAuctionLotState.PendingResult)
            RefreshEbayBidBreakdown();
    }

    private void ShowEbayResultFinal(bool won)
    {
        SetActiveSafe(_ebayResultCountdown?.gameObject, false);
        SetActiveSafe(_ebayResultStatus?.gameObject, true);
        SetActiveSafe(_ebayStatusSucc, won);
        SetActiveSafe(_ebayStatusFail, !won);
        SetActiveSafe(_ebayBidsRoot, true);

        if (_ebayResultStatus != null)
            _ebayResultStatus.text = _lot.LastEbayResolve.GetResultStatusText(won);
    }

    private void RefreshEbayBidBreakdown()
    {
        if (_lot == null)
            return;

        var topBids = BuildEbayTopBids(3);
        var rankTexts = new List<TextMeshProUGUI>(4);
        if (_ebayPlayerBidText != null)
            rankTexts.Add(_ebayPlayerBidText);
        for (var i = 0; i < _ebayAiBidTexts.Length; i++)
        {
            if (_ebayAiBidTexts[i] != null)
                rankTexts.Add(_ebayAiBidTexts[i]);
        }

        for (var i = 0; i < rankTexts.Count; i++)
        {
            var text = rankTexts[i];
            if (i < topBids.Count)
            {
                text.gameObject.SetActive(true);
                text.text = $"{topBids[i].Label}: {topBids[i].Amount:N0}G";
            }
            else
            {
                text.text = string.Empty;
                text.gameObject.SetActive(false);
            }
        }
    }

    private List<EbayBidRankEntry> BuildEbayTopBids(int maxCount)
    {
        var entries = new List<EbayBidRankEntry>(4);

        if (_lot.HasUserBid)
        {
            entries.Add(new EbayBidRankEntry(
                "내 입찰",
                _lot.UserBid.GetValueOrDefault(),
                GuildCombatPower.GetPlayerPower(),
                isPlayer: true));
        }

        var aiBids = _lot.AiBids;
        for (var i = 0; i < aiBids.Count; i++)
        {
            var amount = aiBids[i];
            if (amount <= 0)
                continue;

            var label = i < AiLabels.Length ? AiLabels[i] : $"AI{i + 1}";
            entries.Add(new EbayBidRankEntry(label, amount, _lot.GetAiCombatPower(i), isPlayer: false));
        }

        entries.Sort((a, b) =>
        {
            var byAmount = b.Amount.CompareTo(a.Amount);
            if (byAmount != 0)
                return byAmount;

            var byPower = b.CombatPower.CompareTo(a.CombatPower);
            if (byPower != 0)
                return byPower;

            var aIsPlayer = a.IsPlayer;
            var bIsPlayer = b.IsPlayer;
            if (aIsPlayer != bIsPlayer)
                return aIsPlayer ? -1 : 1;

            return 0;
        });

        if (entries.Count > maxCount)
            entries.RemoveRange(maxCount, entries.Count - maxCount);

        return entries;
    }

    private void CacheCardRefs(Transform card, bool isEbay)
    {
        if (card == null)
            return;

        if (isEbay)
        {
            _ebayGateIcon = card.Find("gate_icon")?.GetComponent<Image>();
            _ebayMainImage = card.Find("gate_image/main_img")?.GetComponent<Image>();
            _ebayTitle = card.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            _ebayMinBid = card.Find("auction/min_bid")?.GetComponent<TextMeshProUGUI>();
            _ebayAuctionType = card.Find("auction/auction type")?.GetComponent<TextMeshProUGUI>();
            _ebayBidInput = card.Find("InputField (TMP)")?.GetComponent<TMP_InputField>();
            _ebayBidButton = card.Find("auction/start_Button")?.GetComponent<Button>();
            _ebayBuyInfoButton = card.Find("auction/buyinfo_Button")?.GetComponent<Button>();
            _ebayBuyInfoLabelText = card.Find("auction/buyinfo_Button/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            _ebayHelpButton = card.Find("auction/help_Button")?.GetComponent<Button>();
            _ebayHintAmount = card.Find("auction/buyinfo_Button/amount")?.GetComponent<TextMeshProUGUI>();

            var energyRoot = card.Find("gate_image/Energy_Percent");
            if (energyRoot != null)
            {
                _ebayEnergyBar = energyRoot.GetComponent<ProgressBar>();
                _ebayEnergyPercent = energyRoot.Find("percent")?.GetComponent<TextMeshProUGUI>();
            }

            CacheHintRefs(card, isEbay: true);
            if (_ebayBidInput != null)
                AuctionBidInputUtility.Configure(_ebayBidInput);
            return;
        }

        _engGateIcon = card.Find("gate_icon")?.GetComponent<Image>();
        _engMainImage = card.Find("gate_image/main_img")?.GetComponent<Image>();
        _engTitle = card.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
        _engMinBid = card.Find("auction/min_bid")?.GetComponent<TextMeshProUGUI>();
        _engAuctionType = card.Find("auction/auction type")?.GetComponent<TextMeshProUGUI>();
        _engAiCount = card.Find("auction/Ai_count")?.GetComponent<TextMeshProUGUI>();
        _engStartButton = card.Find("auction/start_Button")?.GetComponent<Button>();
        _engBuyInfoButton = card.Find("auction/buyinfo_Button")?.GetComponent<Button>();
        _engBuyInfoLabelText = card.Find("auction/buyinfo_Button/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
        _engHelpButton = card.Find("auction/help_Button")?.GetComponent<Button>();
        _engHintAmount = card.Find("auction/buyinfo_Button/amount")?.GetComponent<TextMeshProUGUI>();

        var engEnergyRoot = card.Find("gate_image/Energy_Percent");
        if (engEnergyRoot != null)
        {
            _engEnergyBar = engEnergyRoot.GetComponent<ProgressBar>();
            _engEnergyPercent = engEnergyRoot.Find("percent")?.GetComponent<TextMeshProUGUI>();
        }

        CacheHintRefs(card, isEbay: false);
    }

    private void CacheHintRefs(Transform card, bool isEbay)
    {
        var hintRoot = card.Find("gate_hint");
        if (hintRoot == null)
            return;

        if (isEbay)
        {
            _ebayHintBefore = hintRoot.Find("bf_pay")?.gameObject;
            _ebayHintAfter = hintRoot.Find("af_pay")?.gameObject;
            _ebayHintText = hintRoot.Find("af_pay/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            if (_ebayHintAfter != null)
                DisableAfPayClick(_ebayHintAfter);
            return;
        }

        _engHintBefore = hintRoot.Find("bf_pay")?.gameObject;
        _engHintAfter = hintRoot.Find("af_pay")?.gameObject;
        _engHintText = hintRoot.Find("af_pay/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
        if (_engHintAfter != null)
            DisableAfPayClick(_engHintAfter);
    }

    private void CacheEbayResultRefs(Transform ebayCard)
    {
        if (ebayCard == null)
            return;

        var root = ebayCard.Find("EbayResultPanel");
        if (root == null)
        {
            Debug.LogWarning(
                "[AuctionGateDetail] EbayResultPanel missing under gate_card_ebay. Run The Guild/UI/Wire Auction Result Panels.",
                this);
            return;
        }

        _ebayResultRoot = root.gameObject;
        _ebayResultCountdown = root.Find("Countdown")?.GetComponent<TextMeshProUGUI>();
        var statusRoot = root.Find("Status");
        _ebayResultStatus = statusRoot?.GetComponent<TextMeshProUGUI>();
        _ebayStatusSucc = statusRoot?.Find("succ")?.gameObject;
        _ebayStatusFail = statusRoot?.Find("fail")?.gameObject;
        _ebayBidsRoot = root.Find("Bids")?.gameObject;

        var bids = root.Find("Bids");
        _ebayPlayerBidText = bids?.Find("player")?.GetComponent<TextMeshProUGUI>();
        for (var i = 0; i < _ebayAiBidTexts.Length; i++)
            _ebayAiBidTexts[i] = bids?.Find(AiLabels[i])?.GetComponent<TextMeshProUGUI>();

        DisableAckButton(root);
        _ebayResultRoot.SetActive(false);
    }

    private void CacheEnglishResultRefs(Transform engCard)
    {
        if (engCard == null)
            return;

        var root = engCard.Find("EnglishResultPanel");
        if (root == null)
        {
            Debug.LogWarning(
                "[AuctionGateDetail] EnglishResultPanel missing under gate_card_eng. Run The Guild/UI/Wire Auction Result Panels.",
                this);
            return;
        }

        _engResultRoot = root.gameObject;
        _engCountRoot = root.Find("count")?.gameObject;
        var engCountText = _engCountRoot?.GetComponent<TextMeshProUGUI>();
        if (engCountText != null)
            engCountText.text = InProgressLabel;

        var statusRoot = root.Find("Status");
        _engResultStatus = statusRoot?.GetComponent<TextMeshProUGUI>();
        _engStatusSucc = statusRoot?.Find("succ")?.gameObject;
        _engStatusFail = statusRoot?.Find("fail")?.gameObject;
        _engBidText = root.Find("Bid")?.GetComponent<TextMeshProUGUI>();
        _engWinnerNameText = root.Find("name")?.GetComponent<TextMeshProUGUI>();

        DisableAckButton(root);
        _engResultRoot.SetActive(false);
    }

    private static void DisableAckButton(Transform resultRoot)
    {
        var ack = resultRoot.Find("AckButton");
        if (ack != null)
            ack.gameObject.SetActive(false);
    }

    private static void SetActiveSafe(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    private void WireControls()
    {
        if (_ebayBidButton != null)
        {
            _ebayBidButton.onClick.RemoveAllListeners();
            _ebayBidButton.onClick.AddListener(OnEbayBidClicked);
        }

        if (_engStartButton != null)
        {
            _engStartButton.onClick.RemoveAllListeners();
            _engStartButton.onClick.AddListener(OnEnglishStartClicked);
        }

        WireHintButton(_ebayBuyInfoButton, isEbay: true);
        WireHintButton(_engBuyInfoButton, isEbay: false);

        if (_ebayHelpButton != null)
        {
            _ebayHelpButton.onClick.RemoveAllListeners();
            _ebayHelpButton.onClick.AddListener(() => _helpPopup?.Toggle(AuctionType.Ebay, _ebayHelpButton.transform));
        }

        if (_engHelpButton != null)
        {
            _engHelpButton.onClick.RemoveAllListeners();
            _engHelpButton.onClick.AddListener(() => _helpPopup?.Toggle(AuctionType.English, _engHelpButton.transform));
        }
    }

    private void WireHintButton(Button buyButton, bool isEbay)
    {
        if (buyButton == null)
            return;

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() =>
        {
            if (_lot == null || GateAuctionManager.Instance == null)
                return;

            if (GateAuctionManager.Instance.TryPurchaseHint(_lot.LotId))
                RefreshHintState(isEbay);
        });
    }

    private void OnEbayBidClicked()
    {
        if (_lot == null || GateAuctionManager.Instance == null || _ebayBidInput == null)
            return;

        var min = _lot.UserMinBid;
        var step = _lot.BidIncrement > 0 ? _lot.BidIncrement : _database?.GetEbayBidIncrement() ?? 5;
        var fallback = min + step;
        var amount = AuctionBidInputUtility.ResolveAmount(_ebayBidInput, fallback, min, step);
        GateAuctionManager.Instance.TryBid(_lot.LotId, amount);
    }

    private void OnEnglishStartClicked()
    {
        if (_lot == null || GateAuctionManager.Instance == null)
            return;

        if (_englishModal == null)
        {
            Debug.LogWarning("[AuctionGateDetail] start_auction_re modal not assigned.", this);
            return;
        }

        if (!GateAuctionManager.Instance.TryStartEnglishSession(_lot, out var session) || session == null)
            return;

        _englishModal.Open(_lot, _database, session);
        RefreshEnglishResultPanel();
    }
}
