using UnityEngine;

/// <summary>활성 경매 1건의 런타임 상태. GateLot(스폰 결과) + 입찰·타이머.</summary>
public sealed class GateAuctionLotRuntime
{
    private int _bidCount;

    public int LotId { get; }
    public GateLot Lot { get; }
    public GateAuctionLotState State { get; private set; }
    public int StartingPrice { get; }
    public int CurrentBid { get; private set; }
    public float EndsAtUnscaledTime { get; private set; }

    public bool HasBids => _bidCount > 0;

    public int NextBidAmount =>
        HasBids ? CurrentBid + Mathf.Max(1, Lot.economy.bidIncrement) : StartingPrice;

    public float RemainingSeconds =>
        State == GateAuctionLotState.Active
            ? Mathf.Max(0f, EndsAtUnscaledTime - Time.unscaledTime)
            : 0f;

    public bool IsExpired =>
        State == GateAuctionLotState.Active && Time.unscaledTime >= EndsAtUnscaledTime;

    public GateAuctionLotRuntime(int lotId, GateLot lot, int startingPrice, float durationSec)
    {
        LotId = lotId;
        Lot = lot;
        StartingPrice = startingPrice;
        State = GateAuctionLotState.Active;
        EndsAtUnscaledTime = Time.unscaledTime + durationSec;
    }

    public bool TryPlaceBid(int amount)
    {
        if (State != GateAuctionLotState.Active)
            return false;

        if (amount < NextBidAmount)
            return false;

        CurrentBid = amount;
        _bidCount++;
        return true;
    }

    public void MarkSold() => State = GateAuctionLotState.Sold;

    public void MarkExpiredNoSale() => State = GateAuctionLotState.Expired;
}
