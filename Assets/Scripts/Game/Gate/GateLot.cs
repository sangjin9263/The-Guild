using System;

/// <summary>스폰된 단일 게이트 경매 로트 (런타임 생성 결과).</summary>
[Serializable]
public struct GateLot
{
    public int buildingLevel;
    public GateGrade grade;
    public string gradeBand;
    public int tierId;
    public int energy;
    public AuctionType auctionType;
    public string hint;
    public GateAuctionEconomyDefinition economy;
}
