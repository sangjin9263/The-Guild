using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Gateinfo.xlsx → GateDatabase.asset (BrokerHints/Archetypes Import)
/// 규칙: 시트명·컬럼명이 '#'으로 시작하면 스킵.
/// </summary>
public static class GateDatabaseImporter
{
    private const string XlsxPath = "Assets/Data/Gateinfo.xlsx";
    private const string OutputPath = GateDatabase.DefaultAssetPath;
    private const string GateIconAtlasPath = "Assets/Resources/Art/Icon/gate_img_auction.png";

    private static readonly string[] ExpectedSheets =
    {
        "GateConstants",
        "GateGradeBands",
        "GateGrades",
        "GateEnergyTiers",
        "Auction",
        "EnglishTimerTiers",
        "EnglishAiBehavior",
        "GateBrokerHints",
        "GateArchetypes",
        "GateBrokerPricing",
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
                $"  grades={db.grades.Count}, tiers={db.energyTiers.Count}, auctions={db.auctions.Count}, " +
                $"englishTimers={db.englishTimerTiers.Count}, englishAi={db.englishAiBehaviors.Count}, " +
                $"brokerHints={db.brokerHints.Count}, archetypes={db.archetypes.Count}, " +
                $"unlocks={db.unlocks.Count}, spawn={db.spawnWeights.Count}");
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

        if (!byName.ContainsKey("Auction") && !byName.ContainsKey("EbayAuction"))
            throw new InvalidOperationException("Missing sheet: Auction (or legacy EbayAuction)");

        var db = ScriptableObject.CreateInstance<GateDatabase>();
        db.constants = ParseConstants(byName["GateConstants"]);
        db.gradeBands = ParseGradeBands(byName["GateGradeBands"]);
        db.grades = ParseGrades(byName["GateGrades"]);
        ResolveGradeIcons(db.grades);
        db.energyTiers = ParseEnergyTiers(byName["GateEnergyTiers"]);
        db.auctions = ParseAuction(
            byName.TryGetValue("Auction", out var auctionTable)
                ? auctionTable
                : byName["EbayAuction"]);
        db.englishTimerTiers = byName.TryGetValue("EnglishTimerTiers", out var timerTable)
            ? ParseEnglishTimerTiers(timerTable)
            : DefaultEnglishTimerTiers();
        db.englishAiBehaviors = byName.TryGetValue("EnglishAiBehavior", out var aiTable)
            ? ParseEnglishAiBehavior(aiTable)
            : new List<EnglishAiBehaviorDefinition>();
        db.brokerHints = ParseBrokerHints(byName["GateBrokerHints"]);
        db.archetypes = ParseArchetypes(byName["GateArchetypes"]);
        db.brokerPricing = ParseBrokerPricing(byName["GateBrokerPricing"]);
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
                existing.auctions = db.auctions;
                existing.englishTimerTiers = db.englishTimerTiers;
                existing.englishAiBehaviors = db.englishAiBehaviors;
                existing.gradeBands = db.gradeBands;
                existing.constants = db.constants;
                existing.brokerHints = db.brokerHints;
                existing.archetypes = db.archetypes;
                existing.brokerPricing = db.brokerPricing;
                existing.unlocks = db.unlocks;
                existing.spawnWeights = db.spawnWeights;
                existing.energyTiers = db.energyTiers;
                existing.grades = db.grades;
                EditorUtility.SetDirty(existing);
                db = existing;
            }

            AssetDatabase.SaveAssets();
            var bakeResult = GameUIFontBaker.BakeFromDatabase(db);
            Debug.Log($"[GateImport] Font bake → {GameUIFontBaker.OutputRegularPath} ({bakeResult})");
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

    private static int ParseIntOrDefault(Dictionary<string, string> row, string column, int defaultValue)
    {
        var text = Get(row, column);
        return int.TryParse(text, out var value) ? value : defaultValue;
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
                iconSprite = Get(row, "icon_sprite")
            });
        }

        return list;
    }

    private static void ResolveGradeIcons(List<GateGradeDefinition> grades)
    {
        var spritesByName = LoadGateIconSpritesByName();

        for (var i = 0; i < grades.Count; i++)
        {
            var row = grades[i];
            var spriteName = string.IsNullOrWhiteSpace(row.iconSprite)
                ? GateGradeUtility.GetDisplayName(row.grade)
                : row.iconSprite.Trim();

            if (!spritesByName.TryGetValue(spriteName, out var sprite))
                throw new InvalidOperationException(
                    $"GateGrades {row.grade}: gate icon sprite not found '{spriteName}' in {GateIconAtlasPath}");

            row.icon = sprite;
            grades[i] = row;
        }
    }

    private static Dictionary<string, Sprite> LoadGateIconSpritesByName()
    {
        var map = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        var assets = AssetDatabase.LoadAllAssetsAtPath(GateIconAtlasPath);

        foreach (var asset in assets)
        {
            if (asset is not Sprite sprite || string.IsNullOrEmpty(sprite.name))
                continue;

            map[sprite.name] = sprite;
        }

        if (map.Count == 0)
            throw new InvalidOperationException($"No sprites found at {GateIconAtlasPath}");

        return map;
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
                energyMin = ParseInt(row, "energy_min", "GateEnergyTiers"),
                energyMax = ParseInt(row, "energy_max", "GateEnergyTiers"),
                tierWeightPercent = ParseInt(row, "tier_weight", "GateEnergyTiers"),
                displayOrder = ParseInt(row, "display_order", "GateEnergyTiers"),
                auctionTypeOverride = Get(row, "auction_type_override")
            });
        }

        return list;
    }

    private static List<GateConstantDefinition> ParseConstants(GateXlsxReader.SheetTable table)
    {
        var list = new List<GateConstantDefinition>();
        foreach (var row in table.Rows)
        {
            var key = Get(row, "key");
            if (string.IsNullOrWhiteSpace(key))
                continue;

            list.Add(new GateConstantDefinition
            {
                key = key.Trim(),
                value = Get(row, "value").Trim()
            });
        }

        return list;
    }

    private static List<GateGradeBandDefinition> ParseGradeBands(GateXlsxReader.SheetTable table)
    {
        var list = new List<GateGradeBandDefinition>();
        foreach (var row in table.Rows)
        {
            var gradeText = Get(row, "grade");
            if (string.IsNullOrWhiteSpace(gradeText))
                continue;

            list.Add(new GateGradeBandDefinition
            {
                grade = GateGradeUtility.Parse(gradeText),
                gradeBand = Get(row, "grade_band").Trim()
            });
        }

        return list;
    }

    private static List<GateBrokerHintDefinition> ParseBrokerHints(GateXlsxReader.SheetTable table)
    {
        var list = new List<GateBrokerHintDefinition>();
        foreach (var row in table.Rows)
        {
            var archetype = Get(row, "archetype");
            if (string.IsNullOrWhiteSpace(archetype))
                continue;

            list.Add(new GateBrokerHintDefinition
            {
                archetype = archetype.Trim(),
                tierId = ParseInt(row, "tier_id", "GateBrokerHints"),
                hintText = Get(row, "hint_text").Trim(),
                hintInfo = Get(row, "hint_info").Trim(),
                upArrow = ParseIntOrDefault(row, "up_arrow", 0)
            });
        }

        return list;
    }

    private static List<GateArchetypeDefinition> ParseArchetypes(GateXlsxReader.SheetTable table)
    {
        var list = new List<GateArchetypeDefinition>();
        foreach (var row in table.Rows)
        {
            var gradeText = Get(row, "grade");
            if (string.IsNullOrWhiteSpace(gradeText))
                continue;

            list.Add(new GateArchetypeDefinition
            {
                grade = GateGradeUtility.Parse(gradeText),
                tierId = ParseInt(row, "tier_id", "GateArchetypes"),
                archetype = Get(row, "archetype").Trim(),
                weight = ParseInt(row, "weight", "GateArchetypes")
            });
        }

        return list;
    }

    private static List<GateBrokerPricingDefinition> ParseBrokerPricing(GateXlsxReader.SheetTable table)
    {
        var list = new List<GateBrokerPricingDefinition>();
        foreach (var row in table.Rows)
        {
            var gradeText = Get(row, "grade");
            if (string.IsNullOrWhiteSpace(gradeText))
                continue;

            list.Add(new GateBrokerPricingDefinition
            {
                grade = GateGradeUtility.Parse(gradeText),
                hintCostPctOfMinBid = ParseInt(row, "hint_cost_pct_of_min_bid", "GateBrokerPricing")
            });
        }

        return list;
    }

    private static List<AuctionDefinition> ParseAuction(GateXlsxReader.SheetTable table)
    {
        var list = new List<AuctionDefinition>();
        var hasBidType = table.Rows.Count > 0 && table.Rows[0].ContainsKey("bid_type");

        foreach (var row in table.Rows)
        {
            var gradeText = Get(row, "grade");
            if (string.IsNullOrWhiteSpace(gradeText))
                continue;

            var grade = GateGradeUtility.Parse(gradeText);
            var tierId = ParseInt(row, "tier_id", "Auction");
            AuctionType bidType;

            if (hasBidType && AuctionTypeUtility.TryParse(Get(row, "bid_type"), out var parsedType))
                bidType = parsedType;
            else
                bidType = ResolveBidTypeFromRules(grade, tierId);

            var aiMin = ParseIntOrDefault(row, "ai_count_min", 2);
            var aiMax = ParseIntOrDefault(row, "ai_count_max", aiMin);

            list.Add(new AuctionDefinition
            {
                grade = grade,
                tierId = tierId,
                bidType = bidType,
                bidBandMin = ParseInt(row, "bid_band_min", "Auction"),
                bidBandMax = ParseInt(row, "bid_band_max", "Auction"),
                aiCountMin = aiMin,
                aiCountMax = aiMax
            });
        }

        return list;
    }

    private static AuctionType ResolveBidTypeFromRules(GateGrade grade, int tierId)
    {
        if (tierId >= GateDatabase.DefaultEnglishTierId)
            return AuctionType.English;

        if (GateGradeUtility.IsAtLeast(grade, GateDatabase.DefaultEnglishMinGrade))
            return AuctionType.English;

        return AuctionType.Ebay;
    }

    private static List<EnglishTimerTierDefinition> ParseEnglishTimerTiers(GateXlsxReader.SheetTable table)
    {
        var list = new List<EnglishTimerTierDefinition>();
        foreach (var row in table.Rows)
        {
            if (string.IsNullOrWhiteSpace(Get(row, "bid_count_gt")))
                continue;

            list.Add(new EnglishTimerTierDefinition
            {
                bidCountGt = ParseInt(row, "bid_count_gt", "EnglishTimerTiers"),
                timerSec = ParseInt(row, "timer_sec", "EnglishTimerTiers")
            });
        }

        return list;
    }

    private static List<EnglishTimerTierDefinition> DefaultEnglishTimerTiers() =>
        new()
        {
            new EnglishTimerTierDefinition { bidCountGt = 0, timerSec = 60 },
            new EnglishTimerTierDefinition { bidCountGt = 4, timerSec = 45 },
            new EnglishTimerTierDefinition { bidCountGt = 7, timerSec = 30 },
            new EnglishTimerTierDefinition { bidCountGt = 10, timerSec = 15 }
        };

    private static List<EnglishAiBehaviorDefinition> ParseEnglishAiBehavior(GateXlsxReader.SheetTable table)
    {
        var list = new List<EnglishAiBehaviorDefinition>();
        foreach (var row in table.Rows)
        {
            var gradeText = Get(row, "grade");
            if (string.IsNullOrWhiteSpace(gradeText))
                continue;

            list.Add(new EnglishAiBehaviorDefinition
            {
                grade = GateGradeUtility.Parse(gradeText),
                stepCountMin = ParseInt(row, "step_count_min", "EnglishAiBehavior"),
                stepCountMax = ParseInt(row, "step_count_max", "EnglishAiBehavior"),
                playerCounterStepMin = ParseInt(row, "player_counter_step_min", "EnglishAiBehavior"),
                playerCounterStepMax = ParseInt(row, "player_counter_step_max", "EnglishAiBehavior"),
                reactDelaySecMin = ParseInt(row, "react_delay_sec_min", "EnglishAiBehavior"),
                reactDelaySecMax = ParseInt(row, "react_delay_sec_max", "EnglishAiBehavior"),
                counterBidChancePct = ParseInt(row, "counter_bid_chance_pct", "EnglishAiBehavior")
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

            var energyBarText = Get(row, "energy_bar_max");
            var energyBarMax = string.IsNullOrWhiteSpace(energyBarText)
                ? GateDatabase.EnergyPercentMax
                : ParseInt(row, "energy_bar_max", "GateUnlock");

            list.Add(new GateUnlockDefinition
            {
                buildingLevel = ParseInt(row, "building_level", "GateUnlock"),
                maxUnlockedGrade = GateGradeUtility.Parse(Get(row, "max_unlocked_grade")),
                energyBarMax = energyBarMax,
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

        foreach (var group in db.archetypes.GroupBy(a => (a.grade, a.tierId)))
        {
            var sum = group.Sum(a => a.weight);
            if (sum != 100)
                Debug.LogWarning(
                    $"[GateImport] GateArchetypes {group.Key.grade} tier{group.Key.tierId}: weight sum={sum} (expected 100)");
        }

        foreach (var grade in Enum.GetValues(typeof(GateGrade)).Cast<GateGrade>())
        {
            var row = db.GetGrade(grade);
            if (row == null)
            {
                Debug.LogWarning($"[GateImport] GateGrades missing row for {grade}");
                continue;
            }

            if (row.Value.icon == null)
                Debug.LogWarning($"[GateImport] GateGrades {grade}: icon sprite not resolved.");
        }

        if (db.auctions.Count == 0)
            throw new InvalidOperationException(
                "[GateImport] Auction sheet produced 0 rows — check Gateinfo.xlsx Auction tab.");

        foreach (var tier in db.energyTiers)
        {
            if (db.GetAuction(tier.grade, tier.tierId) != null)
                continue;

            throw new InvalidOperationException(
                $"[GateImport] Auction missing row for {GateGradeUtility.GetDisplayName(tier.grade)} tier {tier.tierId}.");
        }
    }
}
