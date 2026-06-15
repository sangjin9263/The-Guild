using System;
using System.Collections.Generic;

[Serializable]
public struct GateGradeDefinition
{
    public GateGrade grade;
    public int energyMin;
    public int energyMax;
    public string gradeBand;
}

[Serializable]
public struct GateEnergyTierDefinition
{
    public GateGrade grade;
    public int tierId;
    public string gradeBand;
    public int energyMin;
    public int energyMax;
    public int tierWeightPercent;
    public int displayOrder;
    public string auctionTypeOverride;
}

[Serializable]
public struct GateHintBandDefinition
{
    public int energyMin;
    public int energyMax;
    public string hintDisplay1;
    public string hintDisplay2;
    public string hintDisplay3;

    public IEnumerable<string> GetHints()
    {
        if (!string.IsNullOrWhiteSpace(hintDisplay1))
            yield return hintDisplay1.Trim();

        if (!string.IsNullOrWhiteSpace(hintDisplay2))
            yield return hintDisplay2.Trim();

        if (!string.IsNullOrWhiteSpace(hintDisplay3))
            yield return hintDisplay3.Trim();
    }
}

[Serializable]
public struct GateAuctionEconomyDefinition
{
    public GateGrade grade;
    public int tierId;
    public int startingPriceMin;
    public int startingPriceMax;
    public int rewardGoldMin;
    public int rewardGoldMax;
    public int clearTimeSec;
    public int ebayDurationSec;
    public int bidIncrement;
    public int englishRoundSec;
}

[Serializable]
public struct GateUnlockDefinition
{
    public int buildingLevel;
    public GateGrade maxUnlockedGrade;
    public int energyBarMax;
    public AuctionTabMode auctionTabs;
}

[Serializable]
public struct GateSpawnWeightDefinition
{
    public int buildingLevel;
    public GateGrade grade;
    public int spawnWeight;
}
