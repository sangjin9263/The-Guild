using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnglishAuctionParticipant
{
    public string GuildName { get; }
    public bool IsPlayer { get; }
    public int BidAmount { get; set; }

    public bool HasBid => BidAmount > 0;

    public EnglishAuctionParticipant(string guildName, bool isPlayer)
    {
        GuildName = guildName;
        IsPlayer = isPlayer;
    }
}

/// <summary>English 라이브 경매 1세션.</summary>
public sealed class EnglishAuctionSession
{
    public static readonly string[] AiGuildNamePool =
    {
        "철의 길드", "석양 길드", "붉은 방패", "은월 상단", "어둠의 경매단",
        "창공 길드", "대지 수호대", "심연 탐험대", "왕관 연합", "폭풍 기사단"
    };

    public event Action Changed;

    public int OpeningBid { get; }
    public int BidBandMin { get; }
    public int BidIncrement { get; }
    public int MaxStepsPerBid { get; }
    public IReadOnlyList<EnglishAuctionParticipant> Participants => _participants;

    public int CurrentHighBid { get; private set; }
    public int PreviousHighBid { get; private set; }
    public int BidCount { get; private set; }
    public float RemainingSeconds { get; private set; }
    public int PlayerEscrow { get; private set; }
    public int? LeaderParticipantIndex { get; private set; }
    public bool IsActive { get; private set; }
    public bool Ended { get; private set; }

    private readonly List<EnglishAuctionParticipant> _participants = new();
    private readonly List<AiReactState> _aiStates = new();
    private float _aiEvalTimer;
    private EnglishAiBehaviorDefinition _aiBehavior;
    private System.Random _rng;

    private struct AiReactState
    {
        public float ReactAt;
    }

    public EnglishAuctionSession(
        GateLot lot,
        GateDatabase database,
        int openingBid,
        int bidIncrement,
        int maxStepsPerBid,
        int aiCount,
        System.Random rng)
    {
        if (database == null)
            throw new ArgumentNullException(nameof(database));

        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        OpeningBid = openingBid;
        CurrentHighBid = openingBid;
        PreviousHighBid = openingBid;
        BidBandMin = lot.auction.bidBandMin;
        BidIncrement = Mathf.Max(1, bidIncrement);
        MaxStepsPerBid = Mathf.Max(1, maxStepsPerBid);
        _aiBehavior = database.GetEnglishAiBehavior(lot.grade) ?? DefaultAiBehavior(lot.grade);

        var playerName = GameManager.Instance != null ? GameManager.Instance.GuildName : "플레이어";
        _participants.Add(new EnglishAuctionParticipant(playerName, isPlayer: true));

        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < aiCount; i++)
        {
            var name = PickAiGuildName(usedNames, i);
            _participants.Add(new EnglishAuctionParticipant(name, isPlayer: false));
            _aiStates.Add(new AiReactState { ReactAt = 0f });
        }

        ResetTimer(database);
        IsActive = true;
        ScheduleOpeningActivity();
    }

    public void Tick(float deltaTime, GateDatabase database)
    {
        if (!IsActive || Ended)
            return;

        RemainingSeconds = Mathf.Max(0f, RemainingSeconds - deltaTime);
        _aiEvalTimer += deltaTime;

        if (_aiEvalTimer >= database.GetEnglishAiEvalIntervalSec())
        {
            _aiEvalTimer = 0f;
            TickAi(database);
        }

        if (RemainingSeconds <= 0f)
            EndSession();
    }

    public bool TryGetRequiredMinBid(out int minBid)
    {
        minBid = 0;
        if (LeaderParticipantIndex == 0)
            return false;

        minBid = AlignToValidBid(CurrentHighBid + BidIncrement);
        return minBid > CurrentHighBid;
    }

    public int GetMinBid() =>
        TryGetRequiredMinBid(out var minBid) ? minBid : AlignToValidBid(CurrentHighBid + BidIncrement);

    private int AlignedBidFloor => AlignUp(BidBandMin, BidIncrement);

    private int AlignToValidBid(int amount)
    {
        if (amount <= AlignedBidFloor)
            return AlignedBidFloor;

        if (IsAlignedToIncrement(amount))
            return amount;

        var offset = amount - AlignedBidFloor;
        var steps = Mathf.CeilToInt(offset / (float)BidIncrement);
        return AlignedBidFloor + (steps * BidIncrement);
    }

    public bool TryValidatePlayerBid(int amount, out string failReason)
    {
        failReason = null;
        if (!IsActive || Ended)
        {
            failReason = "경매가 종료되었습니다.";
            return false;
        }

        if (!TryGetRequiredMinBid(out var minBid))
        {
            failReason = "이미 최고 입찰자입니다.";
            return false;
        }

        if (amount <= CurrentHighBid || amount < minBid)
        {
            failReason = $"최소 {minBid:N0} G 입찰";
            return false;
        }

        if (LeaderParticipantIndex == 0 && amount <= CurrentHighBid)
        {
            failReason = "이미 최고 입찰자입니다.";
            return false;
        }

        if (!IsAlignedToIncrement(amount))
        {
            failReason = $"{BidIncrement:N0} G 단위 입찰";
            return false;
        }

        var gold = GameManager.Instance != null ? GameManager.Instance.Gold : 0;
        if (amount > gold)
        {
            failReason = "골드가 부족합니다.";
            return false;
        }

        return true;
    }

    public bool TryPlacePlayerBid(int amount, GateDatabase database)
    {
        if (!TryValidatePlayerBid(amount, out _))
            return false;

        ApplyBid(participantIndex: 0, amount, database, lastBidderWasPlayer: true);
        return true;
    }

    public bool TryWithdraw(out int refundedGold)
    {
        refundedGold = PlayerEscrow;
        if (!IsActive)
            return false;

        RefundPlayerEscrow();
        IsActive = false;
        Ended = true;
        NotifyChanged();
        return true;
    }

    public bool PlayerWon =>
        Ended && LeaderParticipantIndex == 0;

    public EnglishAuctionParticipant GetLeader()
    {
        if (!LeaderParticipantIndex.HasValue)
            return null;

        return _participants[LeaderParticipantIndex.Value];
    }

    private void EndSession()
    {
        if (Ended)
            return;

        IsActive = false;
        Ended = true;

        if (!LeaderParticipantIndex.HasValue)
        {
            ClearPlayerBid();
            NotifyChanged();
            return;
        }

        if (LeaderParticipantIndex == 0)
            ChargePlayerWin();
        else
            ClearPlayerBid();

        NotifyChanged();
    }

    public bool HasWinner => Ended && LeaderParticipantIndex.HasValue;

    private void ApplyBid(int participantIndex, int amount, GateDatabase database, bool lastBidderWasPlayer)
    {
        if (amount <= CurrentHighBid)
            return;

        PreviousHighBid = CurrentHighBid;
        CurrentHighBid = amount;
        BidCount++;
        LeaderParticipantIndex = participantIndex;
        _participants[participantIndex].BidAmount = amount;

        if (participantIndex != 0 && PlayerEscrow > 0)
            PlayerEscrow = 0;

        if (participantIndex == 0)
            PlayerEscrow = amount;

        var leadingAiIndex = participantIndex > 0 ? participantIndex : -1;
        ScheduleAiReactions(leadingAiIndex);

        ResetTimer(database);
        NotifyChanged();
    }

    private void ResetTimer(GateDatabase database)
    {
        RemainingSeconds = database.ResolveEnglishTimerSec(BidCount);
    }

    private void TickAi(GateDatabase database)
    {
        var openingPhase = !LeaderParticipantIndex.HasValue;
        var lastBidderWasPlayer = LeaderParticipantIndex == 0;
        TryProcessScheduledAiBids(database, openingPhase, lastBidderWasPlayer);
    }

    private void ScheduleOpeningActivity()
    {
        for (var i = 0; i < _aiStates.Count; i++)
        {
            if (_rng.Next(100) >= 65)
                continue;

            var delay = _rng.Next(2, 6) + (i * 2);
            _aiStates[i] = new AiReactState { ReactAt = Time.unscaledTime + delay };
        }
    }

    private void TryProcessScheduledAiBids(GateDatabase database, bool openingPhase, bool lastBidderWasPlayer)
    {
        var now = Time.unscaledTime;

        for (var i = 0; i < _aiStates.Count; i++)
        {
            var aiIndex = i + 1;
            if (!openingPhase && LeaderParticipantIndex == aiIndex)
                continue;

            var state = _aiStates[i];
            if (state.ReactAt <= 0f || now < state.ReactAt)
                continue;

            _aiStates[i] = new AiReactState { ReactAt = 0f };

            var chance = openingPhase
                ? Mathf.Min(100, _aiBehavior.counterBidChancePct + 20)
                : _aiBehavior.counterBidChancePct;
            if (_rng.Next(100) >= chance)
                continue;

            var steps = RollAiSteps(openingPhase ? false : lastBidderWasPlayer);
            var amount = CurrentHighBid + (steps * BidIncrement);
            if (amount <= CurrentHighBid || amount < GetMinBid())
                continue;

            ApplyBid(aiIndex, amount, database, lastBidderWasPlayer: false);
            return;
        }
    }

    private void ScheduleAiReactions(int leadingAiIndex)
    {
        for (var i = 0; i < _aiStates.Count; i++)
        {
            var aiIndex = i + 1;
            if (leadingAiIndex >= 0 && aiIndex == leadingAiIndex)
                continue;

            var delay = _rng.Next(_aiBehavior.reactDelaySecMin, _aiBehavior.reactDelaySecMax + 1);
            _aiStates[i] = new AiReactState { ReactAt = Time.unscaledTime + delay };
        }
    }

    private int RollAiSteps(bool lastBidderWasPlayer)
    {
        var steps = _rng.Next(_aiBehavior.stepCountMin, _aiBehavior.stepCountMax + 1);
        if (lastBidderWasPlayer)
        {
            steps += _rng.Next(
                _aiBehavior.playerCounterStepMin,
                _aiBehavior.playerCounterStepMax + 1);
        }

        return Mathf.Min(steps, MaxStepsPerBid);
    }

    private bool IsAlignedToIncrement(int amount)
    {
        if (amount < AlignedBidFloor)
            return false;

        var offset = amount - AlignedBidFloor;
        return offset % BidIncrement == 0;
    }

    private static int AlignUp(int value, int step) =>
        value % step == 0 ? value : value + (step - (value % step));

    private void ClearPlayerBid()
    {
        PlayerEscrow = 0;
        _participants[0].BidAmount = 0;
    }

    private void RefundPlayerEscrow() => ClearPlayerBid();

    private void ChargePlayerWin()
    {
        if (PlayerEscrow <= 0)
            return;

        GameManager.Instance?.TrySpendGold(PlayerEscrow, allowDuringEnglishAuction: true);
        PlayerEscrow = 0;
    }

    private string PickAiGuildName(HashSet<string> usedNames, int index)
    {
        for (var attempt = 0; attempt < AiGuildNamePool.Length; attempt++)
        {
            var candidate = AiGuildNamePool[_rng.Next(AiGuildNamePool.Length)];
            if (usedNames.Add(candidate))
                return candidate;
        }

        return $"AI 길드 {index + 1}";
    }

    private static EnglishAiBehaviorDefinition DefaultAiBehavior(GateGrade grade) =>
        new()
        {
            grade = grade,
            stepCountMin = 1,
            stepCountMax = 3,
            playerCounterStepMin = 1,
            playerCounterStepMax = 2,
            reactDelaySecMin = 4,
            reactDelaySecMax = 8,
            counterBidChancePct = 50
        };

    private void NotifyChanged() => Changed?.Invoke();
}
