using System.Collections.Generic;
using UnityEngine;

/// <summary>Ebay 경매 1건 런타임. GateLot + AI 입찰·유저 입찰·5초 판정.</summary>
public sealed class GateAuctionLotRuntime
{
    public const int AiBidderCount = 3;

    public int LotId { get; }
    public GateLot Lot { get; }
    public GateAuctionLotState State { get; private set; }
    public int OpeningBid { get; }
    public int BidIncrement { get; }
    public IReadOnlyList<int> AiBids { get; }
    public int HighestAiBid { get; }
    public int? UserBid { get; private set; }

    private float _endsAtUnscaledTime;
    private const float NoTimer = float.PositiveInfinity;

    public bool HasUserBid => UserBid.HasValue;

    public float RemainingSeconds =>
        State == GateAuctionLotState.PendingResult
            ? Mathf.Max(0f, _endsAtUnscaledTime - Time.unscaledTime)
            : 0f;

    public bool IsPendingExpired =>
        State == GateAuctionLotState.PendingResult && Time.unscaledTime >= _endsAtUnscaledTime;

    public bool HintPurchased { get; private set; }
    public bool HintRevealed { get; private set; }
    public string BrokerHintText { get; private set; } = string.Empty;
    public string BrokerHintInfoText { get; private set; } = string.Empty;
    public int BrokerHintUpArrow { get; private set; }

    public GateAuctionLotRuntime(int lotId, GateLot lot, int openingBid, int bidIncrement, IReadOnlyList<int> aiBids)
    {
        LotId = lotId;
        Lot = lot;
        OpeningBid = openingBid;
        BidIncrement = Mathf.Max(1, bidIncrement);
        AiBids = aiBids ?? System.Array.Empty<int>();
        HighestAiBid = ResolveHighestAiBid(AiBids);
        State = GateAuctionLotState.Bidding;
        _endsAtUnscaledTime = NoTimer;
    }

    private static int ResolveHighestAiBid(IReadOnlyList<int> aiBids)
    {
        var highest = 0;
        for (var i = 0; i < aiBids.Count; i++)
            highest = Mathf.Max(highest, aiBids[i]);

        return highest;
    }

    public bool IsEnglish => Lot.auctionType == AuctionType.English;
    public bool IsEnglishSessionActive => EnglishSession != null && EnglishSession.IsActive;
    public EnglishAuctionSession EnglishSession { get; private set; }

    public int UserMinBid => Lot.auction.bidBandMin;

    public bool CanPlaceBid(int amount)
    {
        if (State != GateAuctionLotState.Bidding || HasUserBid)
            return false;

        return amount >= UserMinBid;
    }

    public bool TryLockUserBid(int amount)
    {
        if (!CanPlaceBid(amount))
            return false;

        UserBid = amount;
        State = GateAuctionLotState.PendingResult;
        _endsAtUnscaledTime = Time.unscaledTime + GateDatabase.EbayResultWaitSec;
        return true;
    }

    public bool PlayerWonAuction { get; private set; }

    public void MarkWon()
    {
        PlayerWonAuction = true;
        State = GateAuctionLotState.Won;
    }

    public void MarkLost()
    {
        PlayerWonAuction = false;
        State = GateAuctionLotState.Lost;
    }

    public void MarkAcknowledged() => State = GateAuctionLotState.Acknowledged;

    public void MarkHintPurchased(string hintText, string hintInfo, int upArrow)
    {
        HintPurchased = true;
        BrokerHintText = hintText ?? string.Empty;
        BrokerHintInfoText = hintInfo ?? string.Empty;
        BrokerHintUpArrow = Mathf.Clamp(upArrow, 0, 3);
    }

    public void MarkHintRevealed() => HintRevealed = true;

    public void AttachEnglishSession(EnglishAuctionSession session)
    {
        EnglishSession = session;
    }

    public void ClearEnglishSession() => EnglishSession = null;

    public void CompleteEnglishSession(bool playerWon, int finalBid)
    {
        EnglishSession = null;

        if (playerWon)
        {
            UserBid = finalBid;
            MarkWon();
            return;
        }

        if (finalBid > 0)
            UserBid = finalBid;

        MarkLost();
    }
}
