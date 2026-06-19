using System;
using UnityEngine;

public enum DungeonRewardKind
{
    Gold = 0,
    LootPool = 1,
    Mutation = 2
}

public static class DungeonRewardKindUtility
{
    public static bool TryParse(string text, out DungeonRewardKind kind)
    {
        kind = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        switch (text.Trim().ToLowerInvariant())
        {
            case "gold":
                kind = DungeonRewardKind.Gold;
                return true;
            case "loot_pool":
                kind = DungeonRewardKind.LootPool;
                return true;
            case "mutation":
                kind = DungeonRewardKind.Mutation;
                return true;
            default:
                return false;
        }
    }
}

[Serializable]
public struct LootPoolEntryDefinition
{
    public string poolId;
    /// <summary>Iteminfo.xlsx Items.item_key 참조.</summary>
    public string itemKey;
    public int weight;
    public int quantityMin;
    public int quantityMax;
}

[Serializable]
public struct ArchetypeLootDefinition
{
    public string archetype;
    public DungeonRewardKind rewardKind;
    public string lootPoolId;
}

[Serializable]
public struct ClearRewardDefinition
{
    public GateGrade grade;
    public int tierId;
    public int goldMin;
    public int goldMax;
    public int clearTimeSec;
}

[Serializable]
public struct MutationRewardMixDefinition
{
    public GateGrade gradeMin;
    public GateGrade gradeMax;
    public string hiddenArchetype;
    public int weight;
    public string lootPoolId;
}

[Serializable]
public struct MutationModifierDefinition
{
    public string modifierTier;
    public int weight;
    public float difficultyMult;
    public float rewardMult;
}

[Serializable]
public struct DungeonConstantDefinition
{
    public string key;
    public string value;
}
