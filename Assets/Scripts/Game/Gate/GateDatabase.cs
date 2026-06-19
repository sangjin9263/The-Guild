using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "GateDatabase", menuName = "The Guild/Gate Database")]
public class GateDatabase : ScriptableObject
{
    public const string DefaultAssetPath = "Assets/Data/Generated/GateDatabase.asset";

    /// <summary>에너지 % 분모. energy / EnergyPercentMax × 100 (건물 Lv 무관).</summary>
    public const int EnergyPercentMax = 200;

    /// <summary>에너지 바 기본 분모(해금 행 없을 때 UI 폴백).</summary>
    public const int EnglishEnergyThreshold = 100;

    /// <summary>이 등급 이상이면 English 경매 (GateConstants.english_min_grade 폴백).</summary>
    public const GateGrade DefaultEnglishMinGrade = GateGrade.S;

    /// <summary>이 tier_id면 English 경매 (GateConstants.english_tier_id 폴백).</summary>
    public const int DefaultEnglishTierId = 2;

    /// <summary>Ebay: 입찰 후 결과 대기 시간(초).</summary>
    public const float EbayResultWaitSec = 5f;

    /// <summary>English 라운드 경매 1라운드 시간(초). 미구현 — 예약 상수.</summary>
    public const float EnglishRoundSec = 15f;

    /// <summary>energy 롤 step (GateEnergyTiers min~max, 5 단위 고정).</summary>
    public const int EnergyRollStep = 5;

    /// <summary>Ebay 입찰 금액 step (GateConstants.ebay_bid_increment).</summary>
    public const int DefaultEbayBidIncrement = 5;

    public List<GateGradeDefinition> grades = new();
    public List<GateEnergyTierDefinition> energyTiers = new();
    public List<AuctionDefinition> auctions = new();
    public List<EnglishTimerTierDefinition> englishTimerTiers = new();
    public List<EnglishAiBehaviorDefinition> englishAiBehaviors = new();
    public List<GateUnlockDefinition> unlocks = new();
    public List<GateSpawnWeightDefinition> spawnWeights = new();
    public List<GateGradeBandDefinition> gradeBands = new();
    public List<GateConstantDefinition> constants = new();
    public List<GateBrokerHintDefinition> brokerHints = new();
    public List<GateArchetypeDefinition> archetypes = new();
    public List<GateBrokerPricingDefinition> brokerPricing = new();

    public GateGradeDefinition? GetGrade(GateGrade grade)
    {
        foreach (var row in grades)
        {
            if (row.grade == grade)
                return row;
        }

        return null;
    }

    public Sprite GetGradeIcon(GateGrade grade)
    {
        var row = GetGrade(grade);
        if (row != null && row.Value.icon != null)
            return row.Value.icon;

        return GateGradeIconCache.GetIcon(grade, row?.iconSprite);
    }

    public GateUnlockDefinition? GetUnlock(int buildingLevel)
    {
        foreach (var row in unlocks)
        {
            if (row.buildingLevel == buildingLevel)
                return row;
        }

        return null;
    }

    /// <summary>UI 에너지 바와 동일: energy / energyBarMax × 100.</summary>
    public float GetEnergyDisplayPercent(GateLot lot)
    {
        var unlock = GetUnlock(lot.buildingLevel);
        var barMax = unlock?.energyBarMax ?? EnglishEnergyThreshold;
        return barMax > 0 ? lot.energy * 100f / barMax : 0f;
    }

    public IEnumerable<GateSpawnWeightDefinition> GetSpawnWeights(int buildingLevel) =>
        spawnWeights.Where(w => w.buildingLevel == buildingLevel);

    public IEnumerable<GateEnergyTierDefinition> GetTiers(GateGrade grade) =>
        energyTiers.Where(t => t.grade == grade).OrderBy(t => t.tierId);

    public AuctionDefinition? GetAuction(GateGrade grade, int tierId)
    {
        foreach (var row in auctions)
        {
            if (row.grade == grade && row.tierId == tierId)
                return row;
        }

        return null;
    }

    public int GetEbayBidIncrement()
    {
        foreach (var row in constants)
        {
            if (row.key == "ebay_bid_increment" &&
                int.TryParse(row.value, out var parsed) &&
                parsed > 0)
                return parsed;
        }

        return DefaultEbayBidIncrement;
    }

    public int GetEnglishBidIncrement()
    {
        foreach (var row in constants)
        {
            if (row.key == "english_bid_increment" &&
                int.TryParse(row.value, out var parsed) &&
                parsed > 0)
                return parsed;
        }

        return GetEbayBidIncrement();
    }

    public int GetEnglishAiMaxStepsPerBid()
    {
        foreach (var row in constants)
        {
            if (row.key == "english_ai_max_steps_per_bid" &&
                int.TryParse(row.value, out var parsed) &&
                parsed > 0)
                return parsed;
        }

        return 8;
    }

    public float GetEnglishAiEvalIntervalSec()
    {
        foreach (var row in constants)
        {
            if (row.key == "english_ai_eval_interval_sec" &&
                float.TryParse(row.value, out var parsed) &&
                parsed > 0f)
                return parsed;
        }

        return 1f;
    }

    public int ResolveEnglishTimerSec(int bidCount)
    {
        var bestSec = (int)EnglishRoundSec;
        var bestGt = int.MinValue;

        foreach (var tier in englishTimerTiers)
        {
            if (bidCount <= tier.bidCountGt)
                continue;

            if (tier.bidCountGt >= bestGt)
            {
                bestGt = tier.bidCountGt;
                bestSec = tier.timerSec;
            }
        }

        if (bestGt == int.MinValue)
        {
            foreach (var tier in englishTimerTiers)
            {
                if (tier.bidCountGt == 0)
                    return tier.timerSec;
            }
        }

        return bestSec;
    }

    public EnglishAiBehaviorDefinition? GetEnglishAiBehavior(GateGrade grade)
    {
        foreach (var row in englishAiBehaviors)
        {
            if (row.grade == grade)
                return row;
        }

        return null;
    }

    public int RollEnglishAiCount(AuctionDefinition auction, System.Random rng)
    {
        var min = Mathf.Max(1, auction.aiCountMin);
        var max = Mathf.Max(min, auction.aiCountMax);
        return min + rng.Next(max - min + 1);
    }

    public bool TryGetBrokerHint(string archetype, int tierId, out GateBrokerHintDefinition hint)
    {
        foreach (var row in brokerHints)
        {
            if (row.archetype == archetype && row.tierId == tierId)
            {
                hint = row;
                return true;
            }
        }

        hint = default;
        return false;
    }

    public int GetBrokerHintCostPct(GateGrade grade)
    {
        foreach (var row in brokerPricing)
        {
            if (row.grade == grade)
                return row.hintCostPctOfMinBid;
        }

        return 8;
    }

    public int GetBrokerHintCost(GateGrade grade, int bidBandMin)
    {
        var pct = GetBrokerHintCostPct(grade);
        return Mathf.Max(1, bidBandMin * pct / 100);
    }

    public IEnumerable<GateArchetypeDefinition> GetArchetypeWeights(GateGrade grade, int tierId) =>
        archetypes.Where(a => a.grade == grade && a.tierId == tierId && a.weight > 0);

    public GateGrade GetEnglishMinGrade()
    {
        foreach (var row in constants)
        {
            if (row.key == "english_min_grade" &&
                GateGradeUtility.TryParse(row.value, out var grade))
                return grade;
        }

        return DefaultEnglishMinGrade;
    }

    public int GetEnglishTierId()
    {
        foreach (var row in constants)
        {
            if (row.key == "english_tier_id" &&
                int.TryParse(row.value, out var parsed) &&
                parsed >= 0)
                return parsed;
        }

        return DefaultEnglishTierId;
    }
}
