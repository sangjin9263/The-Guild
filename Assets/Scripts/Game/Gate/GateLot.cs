using System;

/// <summary>스폰된 단일 게이트 경매 로트 (런타임 생성 결과).</summary>
[Serializable]
public struct GateLot
{
    public int buildingLevel;
    public GateGrade grade;
    public int tierId;
    public int energy;
    public AuctionType auctionType;
    public string archetype;
    public AuctionDefinition auction;
    /// <summary>G - SSS - 7459 - 6S</summary>
    public string gateId;
    public int gateIdNumber;
    public string regionCode;
    public string regionDisplay;
    /// <summary>English 로트 생성 시 굴린 AI 참가자 수.</summary>
    public int englishAiCount;
}
