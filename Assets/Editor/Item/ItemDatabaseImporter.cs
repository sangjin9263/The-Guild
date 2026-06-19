using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Iteminfo.xlsx → ItemDatabase.asset
/// 규칙: 시트명·컬럼명이 '#'으로 시작하면 스킵.
/// </summary>
public static class ItemDatabaseImporter
{
    private const string XlsxPath = "Assets/Data/Iteminfo.xlsx";
    private const string OutputPath = ItemDatabase.DefaultAssetPath;

    private static readonly string[] ExpectedSheets = { "Items", "ItemEffects" };

    [MenuItem("The Guild/Data/Import Iteminfo")]
    public static void ImportFromMenu()
    {
        try
        {
            var db = Import(writeAsset: true);
            Debug.Log(
                $"[ItemImport] OK → {OutputPath}\n" +
                $"  items={db.items.Count}, effects={db.effects.Count}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ItemImport] Failed: {ex.Message}\n{ex}");
        }
    }

    public static ItemDatabase Import(bool writeAsset)
    {
        var fullPath = Path.GetFullPath(XlsxPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Iteminfo.xlsx not found at {XlsxPath}", fullPath);

        var tables = GateXlsxReader.ReadAllSheets(fullPath);
        var byName = tables.ToDictionary(t => t.SheetName, StringComparer.OrdinalIgnoreCase);

        var skippedSheets = tables.Where(t => ShouldSkipSheet(t.SheetName)).Select(t => t.SheetName).ToList();
        if (skippedSheets.Count > 0)
            Debug.Log($"[ItemImport] Skipped sheets: {string.Join(", ", skippedSheets)}");

        foreach (var expected in ExpectedSheets)
        {
            if (!byName.ContainsKey(expected))
                throw new InvalidOperationException($"Missing sheet: {expected}");
        }

        var db = ScriptableObject.CreateInstance<ItemDatabase>();
        db.items = ParseItems(byName["Items"]);
        db.effects = ParseEffects(byName["ItemEffects"]);

        ValidateDatabase(db);

        if (writeAsset)
        {
            EnsureGeneratedFolder();

            var existing = AssetDatabase.LoadAssetAtPath<ItemDatabase>(OutputPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(db, OutputPath);
            }
            else
            {
                existing.items = db.items;
                existing.effects = db.effects;
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

    private static float ParseFloat(Dictionary<string, string> row, string column, string context)
    {
        var text = Get(row, column);
        if (!float.TryParse(text, out var value))
            throw new FormatException($"{context}: invalid float '{column}' = '{text}'");

        return value;
    }

    private static List<ItemDefinition> ParseItems(GateXlsxReader.SheetTable table)
    {
        var list = new List<ItemDefinition>();
        var seenIds = new HashSet<int>();

        foreach (var row in table.Rows)
        {
            var idStr = Get(row, "item_id").Trim();
            if (string.IsNullOrEmpty(idStr))
                continue;

            var itemId = ParseInt(row, "item_id", "Items");

            if (!seenIds.Add(itemId))
                throw new InvalidOperationException($"Items: duplicate item_id '{itemId}'");

            list.Add(new ItemDefinition
            {
                itemId = itemId,
                itemKind = ParseInt(row, "item_kind", "Items"),
                itemKey = Get(row, "item_key").Trim(),
                displayName = Get(row, "display_name").Trim(),
                iconSprite = Get(row, "icon_sprite").Trim(),
                itemTier = ParseInt(row, "item_tier", "Items"),
                sellGold = ParseInt(row, "sell_gold", "Items"),
                stackMax = ParseInt(row, "stack_max", "Items"),
                atk = ParseInt(row, "atk", "Items"),
                def = ParseInt(row, "def", "Items"),
                equip = ParseInt(row, "equip", "Items"),
            });
        }

        return list;
    }

    private static List<ItemEffectDefinition> ParseEffects(GateXlsxReader.SheetTable table)
    {
        var list = new List<ItemEffectDefinition>();

        foreach (var row in table.Rows)
        {
            var idStr = Get(row, "item_id").Trim();
            if (string.IsNullOrEmpty(idStr))
                continue;

            list.Add(new ItemEffectDefinition
            {
                itemId = ParseInt(row, "item_id", "ItemEffects"),
                effectId = ParseInt(row, "effect_id", "ItemEffects"),
                value = ParseFloat(row, "value", "ItemEffects")
            });
        }

        return list;
    }

    private static void ValidateDatabase(ItemDatabase db)
    {
        var itemIds = new HashSet<int>(db.items.Select(i => i.itemId));

        foreach (var item in db.items)
        {
            if (string.IsNullOrEmpty(item.displayName))
                Debug.LogWarning($"[ItemImport] Items {item.itemId}: display_name is empty");

            if (item.stackMax <= 0)
                Debug.LogWarning($"[ItemImport] Items {item.itemId}: stack_max must be > 0");
        }

        foreach (var effect in db.effects)
        {
            if (!itemIds.Contains(effect.itemId))
                Debug.LogWarning($"[ItemImport] ItemEffects: unknown item_id '{effect.itemId}'");
        }
    }

    private static void EnsureGeneratedFolder()
    {
        if (AssetDatabase.IsValidFolder("Assets/Data/Generated"))
            return;

        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");

        AssetDatabase.CreateFolder("Assets/Data", "Generated");
    }
}
