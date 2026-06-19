using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "DungeonDatabase", menuName = "The Guild/Dungeon Database")]
public class DungeonDatabase : ScriptableObject
{
    public const string DefaultAssetPath = "Assets/Data/Generated/DungeonDatabase.asset";

    public const int DefaultGoldRollStep = 5;
    public const int DefaultLootRollCount = 1;

    public List<LootPoolEntryDefinition> lootPools = new();
    public List<ArchetypeLootDefinition> archetypeLoot = new();
    public List<ClearRewardDefinition> clearRewards = new();
    public List<MutationRewardMixDefinition> mutationRewardMix = new();
    public List<MutationModifierDefinition> mutationModifiers = new();
    public List<DungeonConstantDefinition> constants = new();

    public IEnumerable<LootPoolEntryDefinition> GetLootPool(string poolId) =>
        lootPools.Where(i => i.poolId == poolId && i.weight > 0);

    public bool TryGetArchetypeLoot(string archetype, out ArchetypeLootDefinition row)
    {
        foreach (var entry in archetypeLoot)
        {
            if (entry.archetype == archetype)
            {
                row = entry;
                return true;
            }
        }

        row = default;
        return false;
    }

    public ClearRewardDefinition? GetClearReward(GateGrade grade, int tierId)
    {
        foreach (var row in clearRewards)
        {
            if (row.grade == grade && row.tierId == tierId)
                return row;
        }

        return null;
    }

    public IEnumerable<MutationRewardMixDefinition> GetMutationRewardMix(GateGrade grade)
    {
        foreach (var row in mutationRewardMix)
        {
            if (grade >= row.gradeMin && grade <= row.gradeMax)
                yield return row;
        }
    }

    public IEnumerable<MutationModifierDefinition> GetMutationModifiers() =>
        mutationModifiers.Where(m => m.weight > 0);

    public int GetGoldRollStep()
    {
        foreach (var row in constants)
        {
            if (row.key == "gold_roll_step" &&
                int.TryParse(row.value, out var parsed) &&
                parsed > 0)
                return parsed;
        }

        return DefaultGoldRollStep;
    }

    public int GetLootRollCount()
    {
        foreach (var row in constants)
        {
            if (row.key == "loot_roll_count" &&
                int.TryParse(row.value, out var parsed) &&
                parsed > 0)
                return parsed;
        }

        return DefaultLootRollCount;
    }
}
