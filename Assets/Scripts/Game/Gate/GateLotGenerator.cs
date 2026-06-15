using System;
using System.Collections.Generic;

public static class GateLotGenerator
{
    public static GateLot RollLot(GateDatabase db, int buildingLevel, Random rng)
    {
        if (db == null)
            throw new ArgumentNullException(nameof(db));

        if (rng == null)
            throw new ArgumentNullException(nameof(rng));

        var grade = RollGrade(db, buildingLevel, rng);
        var tier = RollTier(db, grade, rng);
        var energy = RollEnergy(tier, rng);
        var auctionType = GateDatabase.ResolveAuctionType(energy, tier.auctionTypeOverride);
        var hint = PickHint(db, energy, rng);
        var economy = db.GetEconomy(grade, tier.tierId)
                     ?? throw new InvalidOperationException($"Missing economy for {grade} tier {tier.tierId}");

        return new GateLot
        {
            buildingLevel = buildingLevel,
            grade = grade,
            gradeBand = tier.gradeBand,
            tierId = tier.tierId,
            energy = energy,
            auctionType = auctionType,
            hint = hint,
            economy = economy
        };
    }

    public static GateGrade RollGrade(GateDatabase db, int buildingLevel, Random rng)
    {
        var weights = new List<GateSpawnWeightDefinition>();
        var total = 0;

        foreach (var row in db.GetSpawnWeights(buildingLevel))
        {
            if (row.spawnWeight <= 0)
                continue;

            weights.Add(row);
            total += row.spawnWeight;
        }

        if (weights.Count == 0 || total <= 0)
            throw new InvalidOperationException($"No spawn weights for building level {buildingLevel}");

        var roll = rng.Next(total);
        var cumulative = 0;

        foreach (var row in weights)
        {
            cumulative += row.spawnWeight;
            if (roll < cumulative)
                return row.grade;
        }

        return weights[weights.Count - 1].grade;
    }

    public static GateEnergyTierDefinition RollTier(GateDatabase db, GateGrade grade, Random rng)
    {
        var tiers = new List<GateEnergyTierDefinition>();
        var total = 0;

        foreach (var tier in db.GetTiers(grade))
        {
            if (tier.tierWeightPercent <= 0)
                continue;

            tiers.Add(tier);
            total += tier.tierWeightPercent;
        }

        if (tiers.Count == 0 || total <= 0)
            throw new InvalidOperationException($"No energy tiers for grade {grade}");

        var roll = rng.Next(total);
        var cumulative = 0;

        foreach (var tier in tiers)
        {
            cumulative += tier.tierWeightPercent;
            if (roll < cumulative)
                return tier;
        }

        return tiers[tiers.Count - 1];
    }

    public static int RollEnergy(GateEnergyTierDefinition tier, Random rng)
    {
        var candidates = BuildEnergyCandidates(tier);
        if (candidates.Count == 0)
            throw new InvalidOperationException(
                $"Invalid energy range {tier.energyMin}-{tier.energyMax} for {tier.grade} tier {tier.tierId}");

        return candidates[rng.Next(candidates.Count)];
    }

    /// <summary>100 미만 step 5, 100 이상 10 단위만 허용.</summary>
    private static List<int> BuildEnergyCandidates(GateEnergyTierDefinition tier)
    {
        var step = GateDatabase.EnergyStepBelow100;
        var min = AlignUp(tier.energyMin, step);
        var max = AlignDown(tier.energyMax, step);

        var list = new List<int>();
        for (var value = min; value <= max; value += step)
        {
            if (value >= GateDatabase.EnergyStepFrom100Threshold && value % GateDatabase.EnergyStepFrom100 != 0)
                continue;

            list.Add(value);
        }

        return list;
    }

    public static string PickHint(GateDatabase db, int energy, Random rng)
    {
        var pool = new List<string>();

        foreach (var band in db.hints)
        {
            if (energy < band.energyMin || energy > band.energyMax)
                continue;

            foreach (var hint in band.GetHints())
                pool.Add(hint);
        }

        if (pool.Count == 0)
            return string.Empty;

        return pool[rng.Next(pool.Count)];
    }

    private static int AlignUp(int value, int step) =>
        value % step == 0 ? value : value + (step - (value % step));

    private static int AlignDown(int value, int step) =>
        value - (value % step);
}
