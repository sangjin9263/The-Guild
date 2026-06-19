using System;

public static class GateLotGenerator
{
    public static GateLot RollLot(GateDatabase db, int buildingLevel, System.Random rng)
    {
        if (db == null)
            throw new ArgumentNullException(nameof(db));

        if (rng == null)
            throw new ArgumentNullException(nameof(rng));

        var grade = RollGrade(db, buildingLevel, rng);
        var tier = RollTier(db, grade, rng);
        var energy = RollEnergy(tier, rng);
        var auction = db.GetAuction(grade, tier.tierId)
                     ?? throw new InvalidOperationException(
                         $"Missing Auction row for {grade} tier {tier.tierId}");

        var auctionType = auction.bidType;
        var archetype = RollArchetype(db, grade, tier.tierId, rng);

        var gateIdNumber = GateIdFormatter.RollFourDigitId(rng);
        var regionCode = GateIdFormatter.DefaultRegionCode;
        var englishAiCount = 0;

        if (auctionType == AuctionType.English)
            englishAiCount = db.RollEnglishAiCount(auction, rng);

        return new GateLot
        {
            buildingLevel = buildingLevel,
            grade = grade,
            tierId = tier.tierId,
            energy = energy,
            auctionType = auctionType,
            archetype = archetype,
            auction = auction,
            gateId = GateIdFormatter.Format(grade, gateIdNumber, regionCode),
            gateIdNumber = gateIdNumber,
            regionCode = regionCode,
            regionDisplay = GateIdFormatter.DefaultRegionDisplay,
            englishAiCount = englishAiCount
        };
    }

    private static GateGrade RollGrade(GateDatabase db, int buildingLevel, System.Random rng)
    {
        var weights = new System.Collections.Generic.List<GateSpawnWeightDefinition>();
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

    private static GateEnergyTierDefinition RollTier(GateDatabase db, GateGrade grade, System.Random rng)
    {
        var tiers = new System.Collections.Generic.List<GateEnergyTierDefinition>();
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

    private static int RollEnergy(GateEnergyTierDefinition tier, System.Random rng)
    {
        var candidates = BuildEnergyCandidates(tier);
        if (candidates.Count == 0)
            throw new InvalidOperationException(
                $"Invalid energy range {tier.energyMin}-{tier.energyMax} for {tier.grade} tier {tier.tierId}");

        return candidates[rng.Next(candidates.Count)];
    }

    private static string RollArchetype(GateDatabase db, GateGrade grade, int tierId, System.Random rng)
    {
        var weights = new System.Collections.Generic.List<GateArchetypeDefinition>();
        var total = 0;

        foreach (var row in db.GetArchetypeWeights(grade, tierId))
        {
            weights.Add(row);
            total += row.weight;
        }

        if (weights.Count == 0 || total <= 0)
            return "gold";

        var roll = rng.Next(total);
        var cumulative = 0;

        foreach (var row in weights)
        {
            cumulative += row.weight;
            if (roll < cumulative)
                return row.archetype;
        }

        return weights[weights.Count - 1].archetype;
    }

    private static System.Collections.Generic.List<int> BuildEnergyCandidates(GateEnergyTierDefinition tier)
    {
        var step = GateDatabase.EnergyRollStep;
        var min = AlignUp(tier.energyMin, step);
        var max = AlignDown(tier.energyMax, step);

        var list = new System.Collections.Generic.List<int>();
        for (var value = min; value <= max; value += step)
            list.Add(value);

        return list;
    }

    private static int AlignUp(int value, int step) =>
        value % step == 0 ? value : value + (step - (value % step));

    private static int AlignDown(int value, int step) =>
        value - (value % step);
}
