using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Gateinfo.xlsx → GateDatabase.asset
/// 규칙: 시트명·컬럼명이 '#'으로 시작하면 스킵.
/// </summary>
public static class GateDatabaseImporter
{
    private const string XlsxPath = "Assets/Data/Gateinfo.xlsx";
    private const string OutputPath = GateDatabase.DefaultAssetPath;

    private static readonly string[] ExpectedSheets =
    {
        "GateGrades",
        "GateEnergyTiers",
        "GateHints",
        "GateAuctionEconomy",
        "GateUnlock",
        "GateSpawnWeights"
    };

    [MenuItem("The Guild/Data/Import Gateinfo")]
    public static void ImportFromMenu()
    {
        try
        {
            var db = Import(writeAsset: true);
            Debug.Log(
                $"[GateImport] OK → {OutputPath}\n" +
                $"  grades={db.grades.Count}, tiers={db.energyTiers.Count}, hints={db.hints.Count}, " +
                $"economy={db.economy.Count}, unlocks={db.unlocks.Count}, spawn={db.spawnWeights.Count}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GateImport] Failed: {ex.Message}\n{ex}");
        }
    }

    public static GateDatabase Import(bool writeAsset)
    {
        var fullPath = Path.GetFullPath(XlsxPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Gateinfo.xlsx not found at {XlsxPath}", fullPath);

        var tables = GateXlsxReader.ReadAllSheets(fullPath);
        var byName = tables.ToDictionary(t => t.SheetName, StringComparer.OrdinalIgnoreCase);

        var skippedSheets = tables.Where(t => ShouldSkipSheet(t.SheetName)).Select(t => t.SheetName).ToList();
        if (skippedSheets.Count > 0)
            Debug.Log($"[GateImport] Skipped sheets: {string.Join(", ", skippedSheets)}");

        foreach (var expected in ExpectedSheets)
        {
            if (!byName.ContainsKey(expected))
                throw new InvalidOperationException($"Missing sheet: {expected}");
        }

        var db = ScriptableObject.CreateInstance<GateDatabase>();
        db.grades = ParseGrades(byName["GateGrades"]);
        db.energyTiers = ParseEnergyTiers(byName["GateEnergyTiers"]);
        db.hints = ParseHints(byName["GateHints"]);
        db.economy = ParseEconomy(byName["GateAuctionEconomy"]);
        db.unlocks = ParseUnlocks(byName["GateUnlock"]);
        db.spawnWeights = ParseSpawnWeights(byName["GateSpawnWeights"]);

        ValidateDatabase(db);

        if (writeAsset)
        {
            EnsureGeneratedFolder();

            var existing = AssetDatabase.LoadAssetAtPath<GateDatabase>(OutputPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(db, OutputPath);
            }
            else
            {
                existing.grades = db.grades;
                existing.energyTiers = db.energyTiers;
                existing.hints = db.hints;
                existing.economy = db.economy;
                existing.unlocks = db.unlocks;
                existing.spawnWeights = db.spawnWeights;
                EditorUtility.SetDirty(existing);
                db = existing;
            }

            AssetDatabase.SaveAssets();
        }

        return db;
    }

    private static bool ShouldSkipSheet(string sheetName) =>
        !string.IsNullOrEmpty(sheetName) && sheetName.StartsWith("#", StringComparison.Ordinal);

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

    private static List<GateGradeDefinition> ParseGrades(GateXlsxReader.SheetTable table)
    {
        var list = new List<GateGradeDefinition>();
        foreach (var row in table.Rows)
        {
            var gradeText = Get(row, "grade");
            if (string.IsNullOrWhiteSpace(gradeText))
                continue;

            list.Add(new GateGradeDefinition
            {
                grade = GateGradeUtility.Parse(gradeText),
                energyMin = ParseInt(row, "energy_min", "GateGrades"),
                energyMax = ParseInt(row, "energy_max", "GateGrades"),
                gradeBand = Get(row, "grade_band")
            });
        }

        return list;
    }

    private static List<GateEnergyTierDefinition> ParseEnergyTiers(GateXlsxReader.SheetTable table)
    {
        var list = new List<GateEnergyTierDefinition>();
        foreach (var row in table.Rows)
        {
            var gradeText = Get(row, "grade");
            if (string.IsNullOrWhiteSpace(gradeText))
                continue;

            list.Add(new GateEnergyTierDefinition
            {
                grade = GateGradeUtility.Parse(gradeText),
                tierId = ParseInt(row, "tier_id", "GateEnergyTiers"),
                gradeBand = Get(row, "grade_band"),
                energyMin = ParseInt(row, "energy_min", "GateEnergyTiers"),
                energyMax = ParseInt(row, "energy_max", "GateEnergyTiers"),
                tierWeightPercent = ParseInt(row, "tier_weight", "GateEnergyTiers"),
                displayOrder = ParseInt(row, "display_order", "GateEnergyTiers"),
                auctionTypeOverride = Get(row, "auction_type_override")
            });
        }

        return list;
    }

    private static List<GateHintBandDefinition> ParseHints(GateXlsxReader.SheetTable table)
    {
        var list = new List<GateHintBandDefinition>();
        foreach (var row in table.Rows)
        {
            if (string.IsNullOrWhiteSpace(Get(row, "energy_min")))
                continue;

            list.Add(new GateHintBandDefinition
            {
                energyMin = ParseInt(row, "energy_min", "GateHints"),
                energyMax = ParseInt(row, "energy_max", "GateHints"),
                hintDisplay1 = Get(row, "hint_display1"),
                hintDisplay2 = Get(row, "hint_display2"),
                hintDisplay3 = Get(row, "hint_display3")
            });
        }

        return list;
    }

    private static List<GateAuctionEconomyDefinition> ParseEconomy(GateXlsxReader.SheetTable table)
    {
        var list = new List<GateAuctionEconomyDefinition>();
        foreach (var row in table.Rows)
        {
            var gradeText = Get(row, "grade");
            if (string.IsNullOrWhiteSpace(gradeText))
                continue;

            list.Add(new GateAuctionEconomyDefinition
            {
                grade = GateGradeUtility.Parse(gradeText),
                tierId = ParseInt(row, "tier_id", "GateAuctionEconomy"),
                startingPriceMin = ParseInt(row, "starting_price_min", "GateAuctionEconomy"),
                startingPriceMax = ParseInt(row, "starting_price_max", "GateAuctionEconomy"),
                rewardGoldMin = ParseInt(row, "reward_gold_min", "GateAuctionEconomy"),
                rewardGoldMax = ParseInt(row, "reward_gold_max", "GateAuctionEconomy"),
                clearTimeSec = ParseInt(row, "clear_time_sec", "GateAuctionEconomy"),
                ebayDurationSec = ParseInt(row, "ebay_duration_sec", "GateAuctionEconomy"),
                bidIncrement = ParseInt(row, "bid_increment", "GateAuctionEconomy"),
                englishRoundSec = ParseInt(row, "english_round_sec", "GateAuctionEconomy")
            });
        }

        return list;
    }

    private static List<GateUnlockDefinition> ParseUnlocks(GateXlsxReader.SheetTable table)
    {
        var list = new List<GateUnlockDefinition>();
        foreach (var row in table.Rows)
        {
            if (string.IsNullOrWhiteSpace(Get(row, "building_level")))
                continue;

            var tabsText = Get(row, "auction_tabs");
            if (!AuctionTabModeUtility.TryParse(tabsText, out var tabs))
                throw new FormatException($"GateUnlock: invalid auction_tabs '{tabsText}'");

            list.Add(new GateUnlockDefinition
            {
                buildingLevel = ParseInt(row, "building_level", "GateUnlock"),
                maxUnlockedGrade = GateGradeUtility.Parse(Get(row, "max_unlocked_grade")),
                energyBarMax = ParseInt(row, "energy_bar_max", "GateUnlock"),
                auctionTabs = tabs
            });
        }

        return list;
    }

    private static List<GateSpawnWeightDefinition> ParseSpawnWeights(GateXlsxReader.SheetTable table)
    {
        var list = new List<GateSpawnWeightDefinition>();
        foreach (var row in table.Rows)
        {
            var gradeText = Get(row, "grade");
            if (string.IsNullOrWhiteSpace(gradeText))
                continue;

            list.Add(new GateSpawnWeightDefinition
            {
                buildingLevel = ParseInt(row, "building_level", "GateSpawnWeights"),
                grade = GateGradeUtility.Parse(gradeText),
                spawnWeight = ParseInt(row, "spawn_weight", "GateSpawnWeights")
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

    private static void ValidateDatabase(GateDatabase db)
    {
        foreach (var levelGroup in db.spawnWeights.GroupBy(w => w.buildingLevel))
        {
            var sum = levelGroup.Sum(w => w.spawnWeight);
            if (sum != 100)
                Debug.LogWarning($"[GateImport] GateSpawnWeights Lv{levelGroup.Key}: sum={sum} (expected 100)");
        }

        foreach (var gradeGroup in db.energyTiers.GroupBy(t => t.grade))
        {
            var sum = gradeGroup.Sum(t => t.tierWeightPercent);
            if (sum != 100)
                Debug.LogWarning($"[GateImport] GateEnergyTiers {gradeGroup.Key}: tier_weight sum={sum} (expected 100)");
        }

        foreach (var grade in Enum.GetValues(typeof(GateGrade)).Cast<GateGrade>())
        {
            if (db.GetGrade(grade) == null)
                Debug.LogWarning($"[GateImport] GateGrades missing row for {grade}");
        }
    }
}
