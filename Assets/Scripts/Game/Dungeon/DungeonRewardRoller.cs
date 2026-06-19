using System;
using System.Collections.Generic;

/// <summary>GateLot 클리어 보상 롤. Dungeoninfo + Iteminfo 기반.</summary>
public static class DungeonRewardRoller
{
    public struct LootDrop
    {
        public string itemKey;
        public int quantity;
    }

    public struct RewardResult
    {
        public int gold;
        public int clearTimeSec;
        public string mutationModifierTier;
        public string resolvedArchetype;
        public List<LootDrop> lootDrops;
    }

    public static RewardResult Roll(GateLot lot, DungeonDatabase dungeonDb, Random rng)
    {
        if (dungeonDb == null)
            throw new ArgumentNullException(nameof(dungeonDb));
        if (rng == null)
            throw new ArgumentNullException(nameof(rng));

        var result = new RewardResult
        {
            resolvedArchetype = lot.archetype,
            lootDrops = new List<LootDrop>()
        };

        if (!dungeonDb.TryGetArchetypeLoot(lot.archetype, out var archetypeLoot))
            return result;

        switch (archetypeLoot.rewardKind)
        {
            case DungeonRewardKind.Gold:
                RollGoldReward(lot, dungeonDb, rng, ref result);
                break;

            case DungeonRewardKind.LootPool:
                RollLootPoolReward(archetypeLoot.lootPoolId, dungeonDb, rng, result.lootDrops);
                break;

            case DungeonRewardKind.Mutation:
                RollMutationReward(lot, dungeonDb, rng, ref result);
                break;
        }

        return result;
    }

    private static void RollGoldReward(
        GateLot lot,
        DungeonDatabase dungeonDb,
        Random rng,
        ref RewardResult result)
    {
        var clear = dungeonDb.GetClearReward(lot.grade, lot.tierId);
        if (clear == null)
            return;

        var step = dungeonDb.GetGoldRollStep();
        var min = AlignUp(clear.Value.goldMin, step);
        var max = AlignDown(clear.Value.goldMax, step);
        if (min > max)
            min = max;

        result.gold = RollStepped(min, max, step, rng);
        result.clearTimeSec = clear.Value.clearTimeSec;
    }

    private static void RollLootPoolReward(
        string poolId,
        DungeonDatabase dungeonDb,
        Random rng,
        List<LootDrop> drops)
    {
        if (string.IsNullOrWhiteSpace(poolId))
            return;

        var rollCount = dungeonDb.GetLootRollCount();
        for (var i = 0; i < rollCount; i++)
        {
            if (!TryRollWeighted(dungeonDb.GetLootPool(poolId), rng, out var entry))
                continue;

            var qtyMin = Math.Min(entry.quantityMin, entry.quantityMax);
            var qtyMax = Math.Max(entry.quantityMin, entry.quantityMax);
            var quantity = qtyMin == qtyMax ? qtyMin : rng.Next(qtyMin, qtyMax + 1);

            drops.Add(new LootDrop
            {
                itemKey = entry.itemKey,
                quantity = quantity
            });
        }
    }

    private static void RollMutationReward(
        GateLot lot,
        DungeonDatabase dungeonDb,
        Random rng,
        ref RewardResult result)
    {
        if (!TryRollWeighted(dungeonDb.GetMutationModifiers(), rng, out var modifier))
            return;

        result.mutationModifierTier = modifier.modifierTier;

        if (!TryRollWeighted(dungeonDb.GetMutationRewardMix(lot.grade), rng, out var mix))
            return;

        result.resolvedArchetype = mix.hiddenArchetype;

        if (!dungeonDb.TryGetArchetypeLoot(mix.hiddenArchetype, out var hiddenLoot))
            return;

        switch (hiddenLoot.rewardKind)
        {
            case DungeonRewardKind.Gold:
            {
                var clear = dungeonDb.GetClearReward(lot.grade, lot.tierId);
                if (clear == null)
                    return;

                var step = dungeonDb.GetGoldRollStep();
                var min = AlignUp(clear.Value.goldMin, step);
                var max = AlignDown(clear.Value.goldMax, step);
                if (min > max)
                    min = max;

                var gold = RollStepped(min, max, step, rng);
                result.gold = Math.Max(1, (int)Math.Round(gold * modifier.rewardMult));
                result.clearTimeSec = Math.Max(
                    1,
                    (int)Math.Round(clear.Value.clearTimeSec / modifier.difficultyMult));
                break;
            }

            case DungeonRewardKind.LootPool:
                RollLootPoolReward(hiddenLoot.lootPoolId, dungeonDb, rng, result.lootDrops);
                break;
        }
    }

    private static bool TryRollWeighted<T>(
        IEnumerable<T> entries,
        Random rng,
        out T picked) where T : struct
    {
        picked = default;
        var list = new List<T>();
        var total = 0;

        foreach (var entry in entries)
        {
            var weight = GetWeight(entry);
            if (weight <= 0)
                continue;

            list.Add(entry);
            total += weight;
        }

        if (list.Count == 0 || total <= 0)
            return false;

        var roll = rng.Next(total);
        var cumulative = 0;

        foreach (var entry in list)
        {
            cumulative += GetWeight(entry);
            if (roll < cumulative)
            {
                picked = entry;
                return true;
            }
        }

        picked = list[list.Count - 1];
        return true;
    }

    private static int GetWeight<T>(T entry) => entry switch
    {
        LootPoolEntryDefinition loot => loot.weight,
        MutationRewardMixDefinition mix => mix.weight,
        MutationModifierDefinition modifier => modifier.weight,
        _ => 0
    };

    private static int RollStepped(int min, int max, int step, Random rng)
    {
        if (min > max)
            return min;

        var steps = ((max - min) / step) + 1;
        return min + (rng.Next(steps) * step);
    }

    private static int AlignUp(int value, int step) =>
        value % step == 0 ? value : value + (step - (value % step));

    private static int AlignDown(int value, int step) =>
        value - (value % step);
}
