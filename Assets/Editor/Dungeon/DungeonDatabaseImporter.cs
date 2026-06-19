using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Dungeoninfo.xlsx → DungeonDatabase.asset</summary>
public static class DungeonDatabaseImporter
{
    private const string XlsxPath = "Assets/Data/Dungeoninfo.xlsx";
    private const string OutputPath = DungeonDatabase.DefaultAssetPath;

    private static readonly string[] ExpectedSheets =
    {
        "LootPools",
        "ArchetypeLoot",
        "ClearRewards",
        "MutationRewardMix",
        "MutationModifiers",
        "DungeonConstants"
    };

    [MenuItem("The Guild/Data/Import Dungeoninfo")]
    public static void ImportFromMenu()
    {
        try
        {
            var db = Import(writeAsset: true);
            Debug.Log(
                $"[DungeonImport] OK → {OutputPath}\n" +
                $"  lootPools={db.lootPools.Count}, archetypeLoot={db.archetypeLoot.Count}, " +
                $"clearRewards={db.clearRewards.Count}, mutationMix={db.mutationRewardMix.Count}, " +
                $"mutationModifiers={db.mutationModifiers.Count}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DungeonImport] Failed: {ex.Message}\n{ex}");
        }
    }

    public static DungeonDatabase Import(bool writeAsset)
    {
        var fullPath = Path.GetFullPath(XlsxPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Dungeoninfo.xlsx not found at {XlsxPath}", fullPath);

        var tables = GateXlsxReader.ReadAllSheets(fullPath);
        var byName = tables.ToDictionary(t => t.SheetName, StringComparer.OrdinalIgnoreCase);

        foreach (var expected in ExpectedSheets)
        {
            if (!byName.ContainsKey(expected))
                throw new InvalidOperationException($"Missing sheet: {expected}");
        }

        var db = ScriptableObject.CreateInstance<DungeonDatabase>();
        db.lootPools = ParseLootPools(byName["LootPools"]);
        db.archetypeLoot = ParseArchetypeLoot(byName["ArchetypeLoot"]);
        db.clearRewards = ParseClearRewards(byName["ClearRewards"]);
        db.mutationRewardMix = ParseMutationRewardMix(byName["MutationRewardMix"]);
        db.mutationModifiers = ParseMutationModifiers(byName["MutationModifiers"]);
        db.constants = ParseConstants(byName["DungeonConstants"]);

        ValidateDatabase(db);

        if (writeAsset)
        {
            EnsureGeneratedFolder();

            var existing = AssetDatabase.LoadAssetAtPath<DungeonDatabase>(OutputPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(db, OutputPath);
            }
            else
            {
                existing.lootPools = db.lootPools;
                existing.archetypeLoot = db.archetypeLoot;
                existing.clearRewards = db.clearRewards;
                existing.mutationRewardMix = db.mutationRewardMix;
                existing.mutationModifiers = db.mutationModifiers;
                existing.constants = db.constants;
                EditorUtility.SetDirty(existing);
                db = existing;
            }

            AssetDatabase.SaveAssets();
        }

        return db;
    }

    private static bool ShouldSkipColumn(string columnName) =>
        !string.IsNullOrEmpty(columnName) && columnName.StartsWith("#", StringComparison.Ordinal);

    private static string Get(Dictionary<string, string> row, string column)
    {
        if (ShouldSkipColumn(column))
            return string.Empty;

        return row.TryGetValue(column, out var value) ? value : string.Empty;
    }

    private static int ParseInt(Dictionary<string, string> row, string column, string context)
    {
        var text = Get(row, column);
        if (!int.TryParse(text, out var value))
            throw new FormatException($"{context}: invalid int '{column}' = '{text}'");

        return value;
    }

    private static float ParseFloat(Dictionary<string, string> row, string column, string context)
    {
        var text = Get(row, column);
        if (!float.TryParse(text, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var value))
            throw new FormatException($"{context}: invalid float '{column}' = '{text}'");

        return value;
    }

    private static List<LootPoolEntryDefinition> ParseLootPools(GateXlsxReader.SheetTable table)
    {
        var list = new List<LootPoolEntryDefinition>();
        foreach (var row in table.Rows)
        {
            var poolId = Get(row, "pool_id");
            if (string.IsNullOrWhiteSpace(poolId))
                continue;

            list.Add(new LootPoolEntryDefinition
            {
                poolId = poolId.Trim(),
                itemKey = Get(row, "item_key").Trim(),
                weight = ParseInt(row, "weight", "LootPools"),
                quantityMin = ParseInt(row, "quantity_min", "LootPools"),
                quantityMax = ParseInt(row, "quantity_max", "LootPools")
            });
        }

        return list;
    }

    private static List<ArchetypeLootDefinition> ParseArchetypeLoot(GateXlsxReader.SheetTable table)
    {
        var list = new List<ArchetypeLootDefinition>();
        foreach (var row in table.Rows)
        {
            var archetype = Get(row, "archetype");
            if (string.IsNullOrWhiteSpace(archetype))
                continue;

            var kindText = Get(row, "reward_kind");
            if (!DungeonRewardKindUtility.TryParse(kindText, out var kind))
                throw new FormatException($"ArchetypeLoot: invalid reward_kind '{kindText}'");

            list.Add(new ArchetypeLootDefinition
            {
                archetype = archetype.Trim(),
                rewardKind = kind,
                lootPoolId = Get(row, "loot_pool_id").Trim()
            });
        }

        return list;
    }

    private static List<ClearRewardDefinition> ParseClearRewards(GateXlsxReader.SheetTable table)
    {
        var list = new List<ClearRewardDefinition>();
        foreach (var row in table.Rows)
        {
            var gradeText = Get(row, "grade");
            if (string.IsNullOrWhiteSpace(gradeText))
                continue;

            list.Add(new ClearRewardDefinition
            {
                grade = GateGradeUtility.Parse(gradeText),
                tierId = ParseInt(row, "tier_id", "ClearRewards"),
                goldMin = ParseInt(row, "gold_min", "ClearRewards"),
                goldMax = ParseInt(row, "gold_max", "ClearRewards"),
                clearTimeSec = ParseInt(row, "clear_time_sec", "ClearRewards")
            });
        }

        return list;
    }

    private static List<MutationRewardMixDefinition> ParseMutationRewardMix(GateXlsxReader.SheetTable table)
    {
        var list = new List<MutationRewardMixDefinition>();
        foreach (var row in table.Rows)
        {
            var gradeMinText = Get(row, "grade_min");
            if (string.IsNullOrWhiteSpace(gradeMinText))
                continue;

            list.Add(new MutationRewardMixDefinition
            {
                gradeMin = GateGradeUtility.Parse(gradeMinText),
                gradeMax = GateGradeUtility.Parse(Get(row, "grade_max")),
                hiddenArchetype = Get(row, "hidden_archetype").Trim(),
                weight = ParseInt(row, "weight", "MutationRewardMix"),
                lootPoolId = Get(row, "loot_pool_id").Trim()
            });
        }

        return list;
    }

    private static List<MutationModifierDefinition> ParseMutationModifiers(GateXlsxReader.SheetTable table)
    {
        var list = new List<MutationModifierDefinition>();
        foreach (var row in table.Rows)
        {
            var tier = Get(row, "modifier_tier");
            if (string.IsNullOrWhiteSpace(tier))
                continue;

            list.Add(new MutationModifierDefinition
            {
                modifierTier = tier.Trim(),
                weight = ParseInt(row, "weight", "MutationModifiers"),
                difficultyMult = ParseFloat(row, "difficulty_mult", "MutationModifiers"),
                rewardMult = ParseFloat(row, "reward_mult", "MutationModifiers")
            });
        }

        return list;
    }

    private static List<DungeonConstantDefinition> ParseConstants(GateXlsxReader.SheetTable table)
    {
        var list = new List<DungeonConstantDefinition>();
        foreach (var row in table.Rows)
        {
            var key = Get(row, "key");
            if (string.IsNullOrWhiteSpace(key))
                continue;

            list.Add(new DungeonConstantDefinition
            {
                key = key.Trim(),
                value = Get(row, "value").Trim()
            });
        }

        return list;
    }

    private static void EnsureGeneratedFolder()
    {
        if (AssetDatabase.IsValidFolder("Assets/Data/Generated"))
            return;

        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");

        AssetDatabase.CreateFolder("Assets/Data", "Generated");
    }

    private static void ValidateDatabase(DungeonDatabase db)
    {
        foreach (var pool in db.lootPools.GroupBy(i => i.poolId))
        {
            var sum = pool.Sum(i => i.weight);
            if (sum != 100)
                Debug.LogWarning($"[DungeonImport] LootPools {pool.Key}: weight sum={sum} (expected 100)");
        }

        var mutationModifierSum = db.mutationModifiers.Sum(m => m.weight);
        if (mutationModifierSum != 100)
            Debug.LogWarning(
                $"[DungeonImport] MutationModifiers: weight sum={mutationModifierSum} (expected 100)");

        foreach (var group in db.mutationRewardMix.GroupBy(m => (m.gradeMin, m.gradeMax)))
        {
            var sum = group.Sum(m => m.weight);
            if (sum != 100)
                Debug.LogWarning(
                    $"[DungeonImport] MutationRewardMix {group.Key.gradeMin}~{group.Key.gradeMax}: sum={sum}");
        }
    }
}
