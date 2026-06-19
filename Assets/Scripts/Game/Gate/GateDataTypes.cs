using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct GateGradeDefinition
{
    public GateGrade grade;
    /// <summary>Excel icon_sprite. 비어 있으면 import 시 gate_img_auction 슬라이스(등급명) 사용.</summary>
    public string iconSprite;
    public Sprite icon;
}

[Serializable]
public struct GateEnergyTierDefinition
{
    public GateGrade grade;
    public int tierId;
    public int energyMin;
    public int energyMax;
    public int tierWeightPercent;
    public int displayOrder;
    /// <summary>English / Ebay. 비우면 english_min_grade·english_tier_id 규칙 적용.</summary>
    public string auctionTypeOverride;
}

/// <summary>
/// 경매 Lot 입찰 밴드 (grade × tier_id). Ebay·English 공용 lookup.
/// </summary>
[Serializable]
public struct AuctionDefinition
{
    public GateGrade grade;
    public int tierId;
    public AuctionType bidType;
    public int bidBandMin;
    public int bidBandMax;
    public int aiCountMin;
    public int aiCountMax;
}

[Serializable]
public struct EnglishTimerTierDefinition
{
    public int bidCountGt;
    public int timerSec;
}

[Serializable]
public struct EnglishAiBehaviorDefinition
{
    public GateGrade grade;
    public int stepCountMin;
    public int stepCountMax;
    public int playerCounterStepMin;
    public int playerCounterStepMax;
    public int reactDelaySecMin;
    public int reactDelaySecMax;
    public int counterBidChancePct;
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

[Serializable]
public struct GateGradeBandDefinition
{
    public GateGrade grade;
    public string gradeBand;
}

[Serializable]
public struct GateConstantDefinition
{
    public string key;
    public string value;
}

[Serializable]
public struct GateBrokerHintDefinition
{
    public string archetype;
    public int tierId;
    public string hintText;
    public string hintInfo;
    public int upArrow;
}

[Serializable]
public struct GateArchetypeDefinition
{
    public GateGrade grade;
    public int tierId;
    public string archetype;
    public int weight;
}

[Serializable]
public struct GateBrokerPricingDefinition
{
    public GateGrade grade;
    public int hintCostPctOfMinBid;
}
