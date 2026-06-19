using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 경매 UI 세션 단위 Lot 풀. 패널을 열 때마다 10건 새로 롤합니다.
/// </summary>
[DefaultExecutionOrder(-40)]
public class GateAuctionManager : MonoBehaviour
{
    public const int TestBuildingLevel = 5;
    public const int MaxActiveLots = 10;
    private const int MaxRollAttempts = 200;

    public static GateAuctionManager Instance { get; private set; }

    public static event Action AuctionsChanged;
    public static event Action<int> EnglishSessionChanged;

    [SerializeField] private GateDatabase database;

    private readonly List<GateAuctionLotRuntime> _activeLots = new();
    private System.Random _rng;
    private int _nextLotId = 1;
    private int _activeEnglishLotId;

    public IReadOnlyList<GateAuctionLotRuntime> ActiveLots => _activeLots;
    public int ActiveEnglishLotId => _activeEnglishLotId;

    public GateDatabase Database
    {
        get
        {
            EnsureDatabase();
            return database;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _rng = new System.Random(Environment.TickCount);
        EnsureDatabase();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        TickPendingLots();
        TickEnglishSessions();
    }

    public void SetDatabase(GateDatabase db)
    {
        database = db;
    }

    public void RefreshLotsOnPanelOpen()
    {
        EndActiveEnglishSessionSilently();
        _activeLots.Clear();
        _activeEnglishLotId = 0;
        _nextLotId = 1;
        _rng = new System.Random(Environment.TickCount);
        FillMixedPool();
        NotifyChanged();
    }

    public bool CanCloseAuctionPanel(out string blockReason)
    {
        blockReason = null;
        ClearStaleActiveEnglishLot();

        if (_activeEnglishLotId > 0)
        {
            var activeLot = FindLot(_activeEnglishLotId);
            if (activeLot?.EnglishSession != null && activeLot.EnglishSession.IsActive)
            {
                blockReason = "진행 중인 영국식 경매가 있습니다.";
                return false;
            }

            _activeEnglishLotId = 0;
        }

        foreach (var lot in _activeLots)
        {
            if (lot.IsEnglish || !lot.HasUserBid)
                continue;

            if (lot.State == GateAuctionLotState.Acknowledged)
                continue;

            blockReason = lot.State == GateAuctionLotState.PendingResult
                ? "입찰 결과를 기다리는 중입니다."
                : "입찰 결과를 확인해 주세요.";

            return false;
        }

        return true;
    }

    private void ClearStaleActiveEnglishLot()
    {
        if (_activeEnglishLotId <= 0)
            return;

        var lot = FindLot(_activeEnglishLotId);
        if (lot == null)
        {
            _activeEnglishLotId = 0;
            return;
        }

        var session = lot.EnglishSession;
        if (session == null || !session.IsActive)
            _activeEnglishLotId = 0;
    }

    public GateAuctionLotRuntime FindLot(int lotId)
    {
        foreach (var lot in _activeLots)
        {
            if (lot.LotId == lotId)
                return lot;
        }

        return null;
    }

    public bool TryStartEnglishSession(int lotId, out EnglishAuctionSession session)
    {
        session = null;
        var lot = FindLot(lotId);
        return lot != null && TryStartEnglishSession(lot, out session);
    }

    public bool TryStartEnglishSession(GateAuctionLotRuntime lot, out EnglishAuctionSession session)
    {
        session = null;
        if (lot == null || !IsTrackedLot(lot))
            return false;

        if (!lot.IsEnglish || lot.State != GateAuctionLotState.Bidding)
            return false;

        if (lot.IsEnglishSessionActive)
            return false;

        if (_activeEnglishLotId > 0)
        {
            if (_activeEnglishLotId == lot.LotId && lot.EnglishSession == null)
                _activeEnglishLotId = 0;
            else if (_activeEnglishLotId != lot.LotId)
                return false;
        }

        EnsureDatabase();
        if (database == null)
            return false;

        var increment = database.GetEnglishBidIncrement();
        var openingBid = RollOpeningBid(lot.Lot, increment);
        var maxSteps = database.GetEnglishAiMaxStepsPerBid();
        var aiCount = Mathf.Max(1, lot.Lot.englishAiCount);

        session = new EnglishAuctionSession(
            lot.Lot,
            database,
            openingBid,
            increment,
            maxSteps,
            aiCount,
            _rng);

        lot.AttachEnglishSession(session);
        _activeEnglishLotId = lot.LotId;
        session.Changed += OnEnglishSessionChanged;
        NotifyChanged();
        EnglishSessionChanged?.Invoke(lot.LotId);
        return true;
    }

    private bool IsTrackedLot(GateAuctionLotRuntime lot)
    {
        for (var i = 0; i < _activeLots.Count; i++)
        {
            if (ReferenceEquals(_activeLots[i], lot))
                return true;
        }

        return false;
    }

    private void EndActiveEnglishSessionSilently()
    {
        if (_activeEnglishLotId <= 0)
            return;

        var lot = FindLot(_activeEnglishLotId);
        var session = lot?.EnglishSession;
        if (session != null)
        {
            session.Changed -= OnEnglishSessionChanged;
            session.TryWithdraw(out _);
        }

        lot?.ClearEnglishSession();
        _activeEnglishLotId = 0;
    }

    public bool TryEnglishPlayerBid(int lotId, int amount)
    {
        var lot = FindLot(lotId);
        if (lot?.EnglishSession == null || !lot.EnglishSession.IsActive)
            return false;

        EnsureDatabase();
        if (!lot.EnglishSession.TryPlacePlayerBid(amount, database))
            return false;

        NotifyChanged();
        EnglishSessionChanged?.Invoke(lotId);
        return true;
    }

    public bool TryWithdrawEnglishSession(int lotId)
    {
        var lot = FindLot(lotId);
        if (lot?.EnglishSession == null || !lot.EnglishSession.IsActive)
            return false;

        var session = lot.EnglishSession;
        session.Changed -= OnEnglishSessionChanged;
        session.TryWithdraw(out _);
        lot.ClearEnglishSession();
        _activeEnglishLotId = 0;
        NotifyChanged();
        EnglishSessionChanged?.Invoke(0);
        return true;
    }

    public void CompleteEnglishSessionIfEnded(int lotId)
    {
        var lot = FindLot(lotId);
        var session = lot?.EnglishSession;
        if (session == null || !session.Ended)
            return;

        session.Changed -= OnEnglishSessionChanged;
        var playerWon = session.PlayerWon;
        var finalBid = playerWon ? session.CurrentHighBid : 0;

        if (!session.HasWinner && session.BidCount == 0)
        {
            lot.ClearEnglishSession();
            if (_activeEnglishLotId == lotId)
                _activeEnglishLotId = 0;
            NotifyChanged();
            EnglishSessionChanged?.Invoke(0);
            return;
        }

        lot.CompleteEnglishSession(playerWon, finalBid);
        lot.ClearEnglishSession();

        if (_activeEnglishLotId == lotId)
            _activeEnglishLotId = 0;

        Debug.Log(
            playerWon
                ? $"[GateAuction] English win lot={lotId} bid={finalBid}g"
                : $"[GateAuction] English lose lot={lotId} bid={finalBid}g");

        NotifyChanged();
        EnglishSessionChanged?.Invoke(0);
    }

    public bool TryBid(int lotId, int bidAmount)
    {
        var lot = FindLot(lotId);
        if (lot == null || lot.Lot.auctionType != AuctionType.Ebay)
            return false;

        if (!lot.CanPlaceBid(bidAmount))
            return false;

        var game = GameManager.Instance;
        if (game == null || !game.TrySpendGold(bidAmount))
            return false;

        if (!lot.TryLockUserBid(bidAmount))
        {
            game.TryAddGold(bidAmount);
            return false;
        }

        NotifyChanged();
        return true;
    }

    public bool TryAcknowledge(int lotId)
    {
        var lot = FindLot(lotId);
        if (lot == null)
            return false;

        if (lot.State == GateAuctionLotState.Won)
        {
            GameManager.Instance?.TryAddEntryTicket();
            lot.MarkAcknowledged();
            NotifyChanged();
            return true;
        }

        if (lot.State == GateAuctionLotState.Lost)
        {
            lot.MarkAcknowledged();
            NotifyChanged();
            return true;
        }

        return false;
    }

    public bool TryPurchaseHint(int lotId)
    {
        var lot = FindLot(lotId);
        if (lot == null || lot.HintPurchased)
            return false;

        if (lot.State != GateAuctionLotState.Bidding)
            return false;

        EnsureDatabase();
        if (database == null)
            return false;

        var cost = database.GetBrokerHintCost(lot.Lot.grade, lot.Lot.auction.bidBandMin);
        var game = GameManager.Instance;
        if (game == null || !game.TrySpendGold(cost))
            return false;

        if (!database.TryGetBrokerHint(lot.Lot.archetype, lot.Lot.tierId, out var hint))
        {
            hint = new GateBrokerHintDefinition
            {
                hintText = "정보가 불완전합니다.",
                hintInfo = string.Empty,
                upArrow = 0
            };
        }

        if (string.IsNullOrEmpty(hint.hintText))
            hint.hintText = "정보가 불완전합니다.";

        lot.MarkHintPurchased(hint.hintText, hint.hintInfo, hint.upArrow);
        NotifyChanged();
        return true;
    }

    public void NotifyHintRevealed(int lotId)
    {
        var lot = FindLot(lotId);
        if (lot == null || !lot.HintPurchased || lot.HintRevealed)
            return;

        lot.MarkHintRevealed();
        NotifyChanged();
    }

    private void FillMixedPool()
    {
        EnsureDatabase();
        if (database == null)
            return;

        if (database.auctions == null || database.auctions.Count == 0)
        {
            Debug.LogError(
                "[GateAuction] Cannot roll lots — GateDatabase.auctions is empty. " +
                "Run The Guild → Data → Import Gateinfo.",
                this);
            return;
        }

        var attempts = 0;
        while (_activeLots.Count < MaxActiveLots && attempts < MaxRollAttempts)
        {
            attempts++;
            var lot = GateLotGenerator.RollLot(database, TestBuildingLevel, _rng);

            _activeLots.Add(lot.auctionType == AuctionType.Ebay
                ? CreateEbayLot(lot)
                : CreateEnglishLot(lot));
        }

        if (_activeLots.Count < MaxActiveLots)
        {
            Debug.LogWarning(
                $"[GateAuction] Only spawned {_activeLots.Count}/{MaxActiveLots} lots.",
                this);
        }
    }

    private GateAuctionLotRuntime CreateEbayLot(GateLot lot)
    {
        var band = lot.auction;
        var increment = Mathf.Max(1, database.GetEbayBidIncrement());
        var bandMin = AlignUp(band.bidBandMin, increment);
        var bandMax = AlignDown(band.bidBandMax, increment);
        if (bandMin > bandMax)
            bandMax = bandMin;

        var openingBid = RollOpeningBid(lot, increment);
        var aiBids = RollAiBids(bandMin, bandMax, increment, GateAuctionLotRuntime.AiBidderCount);

        return new GateAuctionLotRuntime(
            _nextLotId++,
            lot,
            openingBid,
            increment,
            aiBids);
    }

    private GateAuctionLotRuntime CreateEnglishLot(GateLot lot)
    {
        var band = lot.auction;
        var increment = Mathf.Max(1, database.GetEnglishBidIncrement());
        var openingBid = RollOpeningBid(lot, increment);

        return new GateAuctionLotRuntime(
            _nextLotId++,
            lot,
            openingBid,
            increment,
            System.Array.Empty<int>());
    }

    private void TickPendingLots()
    {
        var changed = false;

        foreach (var lot in _activeLots)
        {
            if (!lot.IsPendingExpired)
                continue;

            ResolveEbayLot(lot);
            changed = true;
        }

        if (changed)
            NotifyChanged();
    }

    private void TickEnglishSessions()
    {
        if (_activeEnglishLotId <= 0)
            return;

        var lot = FindLot(_activeEnglishLotId);
        var session = lot?.EnglishSession;
        if (lot == null || session == null)
        {
            _activeEnglishLotId = 0;
            return;
        }

        session.Tick(Time.unscaledDeltaTime, database);

        if (session.Ended)
            CompleteEnglishSessionIfEnded(lot.LotId);
    }

    private void OnEnglishSessionChanged()
    {
        if (_activeEnglishLotId <= 0)
            return;

        EnglishSessionChanged?.Invoke(_activeEnglishLotId);
    }

    private void ResolveEbayLot(GateAuctionLotRuntime lot)
    {
        var userBid = lot.UserBid.GetValueOrDefault();
        var highestAiBid = lot.HighestAiBid;

        if (userBid > highestAiBid)
        {
            lot.MarkWon();
            Debug.Log(
                $"[GateAuction] Win lot={lot.LotId} {lot.Lot.grade} t{lot.Lot.tierId} " +
                $"user={userBid}g aiMax={highestAiBid}g — 확인 후 입장권 지급");
            return;
        }

        GameManager.Instance?.TryAddGold(userBid);
        lot.MarkLost();
        Debug.Log(
            $"[GateAuction] Lose lot={lot.LotId} {lot.Lot.grade} t{lot.Lot.tierId} " +
            $"user={userBid}g aiMax={highestAiBid}g — {userBid}g 환불");
    }

    private int RollOpeningBid(GateLot lot, int increment)
    {
        var band = lot.auction;
        increment = Mathf.Max(1, increment);
        var min = AlignUp(band.bidBandMin, increment);
        var max = AlignDown(band.bidBandMax, increment);

        if (min > max)
            return band.bidBandMin;

        var steps = ((max - min) / increment) + 1;
        return min + (_rng.Next(steps) * increment);
    }

    private int[] RollAiBids(int min, int max, int increment, int count)
    {
        var bids = new int[count];
        for (var i = 0; i < count; i++)
            bids[i] = RollAiBid(min, max, increment);

        return bids;
    }

    private int RollAiBid(int min, int max, int increment)
    {
        if (min > max)
            return min;

        var steps = ((max - min) / increment) + 1;
        return min + (_rng.Next(steps) * increment);
    }

    private void EnsureDatabase()
    {
        if (database != null)
        {
            if (database.auctions == null || database.auctions.Count == 0)
                Debug.LogError(
                    "[GateAuction] GateDatabase.auctions is empty. Run The Guild → Data → Import Gateinfo.",
                    this);
            return;
        }

#if UNITY_EDITOR
        database = UnityEditor.AssetDatabase.LoadAssetAtPath<GateDatabase>(GateDatabase.DefaultAssetPath);
#endif

        if (database == null)
            Debug.LogWarning("[GateAuction] GateDatabase not assigned.", this);
        else if (database.auctions == null || database.auctions.Count == 0)
            Debug.LogError(
                "[GateAuction] GateDatabase.auctions is empty. Run The Guild → Data → Import Gateinfo.",
                this);
    }

    private static int AlignUp(int value, int step) =>
        value % step == 0 ? value : value + (step - (value % step));

    private static int AlignDown(int value, int step) =>
        value - (value % step);

    private static void NotifyChanged() => AuctionsChanged?.Invoke();
}
