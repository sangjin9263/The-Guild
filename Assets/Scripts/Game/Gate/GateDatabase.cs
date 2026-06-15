using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "GateDatabase", menuName = "The Guild/Gate Database")]
public class GateDatabase : ScriptableObject
{
    public const string DefaultAssetPath = "Assets/Data/Generated/GateDatabase.asset";

    /// <summary>에너지가 이 값을 초과하면 English 경매. (#ReadMe / 코드 상수)</summary>
    public const int EnglishEnergyThreshold = 100;

    /// <summary>energy 롤: 이 값 미만 step 5, 이상 10 단위만.</summary>
    public const int EnergyStepBelow100 = 5;
    public const int EnergyStepFrom100Threshold = 100;
    public const int EnergyStepFrom100 = 10;

    public List<GateGradeDefinition> grades = new();
    public List<GateEnergyTierDefinition> energyTiers = new();
    public List<GateHintBandDefinition> hints = new();
    public List<GateAuctionEconomyDefinition> economy = new();
    public List<GateUnlockDefinition> unlocks = new();
    public List<GateSpawnWeightDefinition> spawnWeights = new();

    public GateGradeDefinition? GetGrade(GateGrade grade)
    {
        foreach (var row in grades)
        {
            if (row.grade == grade)
                return row;
        }

        return null;
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

    public IEnumerable<GateSpawnWeightDefinition> GetSpawnWeights(int buildingLevel) =>
        spawnWeights.Where(w => w.buildingLevel == buildingLevel);

    public IEnumerable<GateEnergyTierDefinition> GetTiers(GateGrade grade) =>
        energyTiers.Where(t => t.grade == grade).OrderBy(t => t.tierId);

    public GateAuctionEconomyDefinition? GetEconomy(GateGrade grade, int tierId)
    {
        foreach (var row in economy)
        {
            if (row.grade == grade && row.tierId == tierId)
                return row;
        }

        return null;
    }

    public static AuctionType ResolveAuctionType(int energy, string auctionTypeOverride)
    {
        if (!string.IsNullOrWhiteSpace(auctionTypeOverride) &&
            AuctionTypeUtility.TryParse(auctionTypeOverride, out var forced))
            return forced;

        return energy > EnglishEnergyThreshold ? AuctionType.English : AuctionType.Ebay;
    }
}
