using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전역 경매 풀을 유지합니다. Lv5 테스트 · 최대 10개 활성 로트.
/// 낙찰/만료 시 1건씩 리필합니다.
/// </summary>
[DefaultExecutionOrder(-40)]
public class GateAuctionManager : MonoBehaviour
{
    public const int TestBuildingLevel = 5;
    public const int MaxActiveLots = 10;

    public static GateAuctionManager Instance { get; private set; }

    public static event Action AuctionsChanged;

    [SerializeField] private GateDatabase database;

    private readonly List<GateAuctionLotRuntime> _activeLots = new();
    private System.Random _rng;
    private int _nextLotId = 1;

    public IReadOnlyList<GateAuctionLotRuntime> ActiveLots => _activeLots;

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

    private void Start()
    {
        if (_activeLots.Count == 0)
            FillPool();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        TickExpiredLots();
    }

    public void SetDatabase(GateDatabase db)
    {
        database = db;
    }

    /// <summary>풀을 비우고 MaxActiveLots만큼 새로 스폰합니다.</summary>
    public void ResetPool(int? seed = null)
    {
        _activeLots.Clear();
        _rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random(Environment.TickCount);
        FillPool();
        NotifyChanged();
    }

    public IReadOnlyList<GateAuctionLotRuntime> GetLotsByGrade(GateGrade grade)
    {
        var list = new List<GateAuctionLotRuntime>();
        foreach (var lot in _activeLots)
        {
            if (lot.State == GateAuctionLotState.Active && lot.Lot.grade == grade)
                list.Add(lot);
        }

        return list;
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

    /// <summary>다음 필요 입찰가로 1회 입찰합니다. (플레이어 단독 스텁)</summary>
    public bool TryBid(int lotId)
    {
        var lot = FindLot(lotId);
        if (lot == null || lot.State != GateAuctionLotState.Active)
            return false;

        var amount = lot.NextBidAmount;
        var game = GameManager.Instance;
        if (game == null || !game.TrySpendGold(amount))
            return false;

        if (!lot.TryPlaceBid(amount))
            return false;

        NotifyChanged();
        return true;
    }

    private void FillPool()
    {
        EnsureDatabase();
        if (database == null)
            return;

        while (_activeLots.Count < MaxActiveLots)
            _activeLots.Add(CreateLot());
    }

    private GateAuctionLotRuntime CreateLot()
    {
        var lot = GateLotGenerator.RollLot(database, TestBuildingLevel, _rng);
        var startingPrice = RollStartingPrice(lot);
        var durationSec = RollDurationSec(lot);
        return new GateAuctionLotRuntime(_nextLotId++, lot, startingPrice, durationSec);
    }

    private void TickExpiredLots()
    {
        var changed = false;

        for (var i = 0; i < _activeLots.Count; i++)
        {
            var lot = _activeLots[i];
            if (!lot.IsExpired)
                continue;

            CloseLot(lot);
            _activeLots[i] = CreateLot();
            changed = true;
        }

        if (changed)
            NotifyChanged();
    }

    private void CloseLot(GateAuctionLotRuntime lot)
    {
        if (lot.HasBids)
        {
            lot.MarkSold();
            GameManager.Instance?.TryAddEntryTicket();
            Debug.Log(
                $"[GateAuction] Sold {lot.Lot.grade} t{lot.Lot.tierId} energy={lot.Lot.energy} " +
                $"for {lot.CurrentBid}g → entry ticket +1");
        }
        else
        {
            lot.MarkExpiredNoSale();
            Debug.Log(
                $"[GateAuction] Expired (no bids) {lot.Lot.grade} t{lot.Lot.tierId} energy={lot.Lot.energy}");
        }
    }

    private int RollStartingPrice(GateLot lot)
    {
        var econ = lot.economy;
        var increment = Mathf.Max(1, econ.bidIncrement);
        var min = AlignUp(econ.startingPriceMin, increment);
        var max = AlignDown(econ.startingPriceMax, increment);

        if (min > max)
            return econ.startingPriceMin;

        var steps = ((max - min) / increment) + 1;
        return min + (_rng.Next(steps) * increment);
    }

    private float RollDurationSec(GateLot lot)
    {
        // TODO: English 라운드 경매는 english_round_sec 기반으로 분리
        return lot.auctionType == AuctionType.Ebay
            ? lot.economy.ebayDurationSec
            : lot.economy.ebayDurationSec;
    }

    private void EnsureDatabase()
    {
        if (database != null)
            return;

#if UNITY_EDITOR
        database = UnityEditor.AssetDatabase.LoadAssetAtPath<GateDatabase>(GateDatabase.DefaultAssetPath);
#endif

        if (database == null)
            Debug.LogWarning("[GateAuction] GateDatabase not assigned.", this);
    }

    private static int AlignUp(int value, int step) =>
        value % step == 0 ? value : value + (step - (value % step));

    private static int AlignDown(int value, int step) =>
        value - (value % step);

    private static void NotifyChanged() => AuctionsChanged?.Invoke();
}
