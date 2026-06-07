using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class SproutLandsRuleTileBuilder
{
    private static readonly Dictionary<string, Vector3Int> PeeringToUnity = new()
    {
        { "top_left_corner", new Vector3Int(-1, 1, 0) },
        { "top_side", new Vector3Int(0, 1, 0) },
        { "top_right_corner", new Vector3Int(1, 1, 0) },
        { "left_side", new Vector3Int(-1, 0, 0) },
        { "right_side", new Vector3Int(1, 0, 0) },
        { "bottom_left_corner", new Vector3Int(-1, -1, 0) },
        { "bottom_side", new Vector3Int(0, -1, 0) },
        { "bottom_right_corner", new Vector3Int(1, -1, 0) },
    };

    private static readonly Vector3Int[] AllNeighborPositions =
    {
        new(-1, 1, 0), new(0, 1, 0), new(1, 1, 0),
        new(-1, 0, 0), new(1, 0, 0),
        new(-1, -1, 0), new(0, -1, 0), new(1, -1, 0),
    };

    [MenuItem("Tools/Sprout Lands/Build Rule Tiles")]
    public static void BuildAll()
    {
        var referencePath = "Assets/Tile/Reference/sprout_lands_godot_tilemap.txt";
        if (!File.Exists(referencePath))
        {
            Debug.LogError("Missing reference file: " + referencePath);
            return;
        }

        var text = File.ReadAllText(referencePath);
        var atlasBlocks = ParseAtlasBlocks(text);

        var configs = new[]
        {
            new BuildConfig("Grass Rule Tile", "Grass", "Assets/Store/Envi & tile/Sprout Lands - Sprites - Basic pack/Tilesets/Grass.png", atlasBlocks["Grass.png"], 0, 0),
            new BuildConfig("Tilled Dirt Rule Tile", "Tilled_Dirt", "Assets/Store/Envi & tile/Sprout Lands - Sprites - Basic pack/Tilesets/Tilled_Dirt.png", atlasBlocks["Grass.png"], 0, 0),
            new BuildConfig("Water Rule Tile", "Water", "Assets/Store/Envi & tile/Sprout Lands - Sprites - Basic pack/Tilesets/Water.png", atlasBlocks.GetValueOrDefault("Water.png") ?? string.Empty, -1, -1, true),
        };

        if (!AssetDatabase.IsValidFolder("Assets/Tile/RuleTiles"))
            AssetDatabase.CreateFolder("Assets/Tile", "RuleTiles");

        var built = new List<RuleTile>();
        foreach (var config in configs)
        {
            var tile = BuildRuleTile(config);
            if (tile != null)
                built.Add(tile);
        }

        UpdatePalette(built);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Sprout Lands Rule Tiles built: {string.Join(", ", built.Select(t => t.name))}");
    }

    private static Dictionary<string, string> ParseAtlasBlocks(string text)
    {
        var result = new Dictionary<string, string>();
        var textureRegex = new Regex(@"texture = ExtResource\(""([^""]+)""\)", RegexOptions.Compiled);
        var extRegex = new Regex(@"\[ext_resource[^\]]*path=""(?<path>[^""]+)""[^\]]*id=""(?<id>[^""]+)""", RegexOptions.Compiled);

        var idToFile = new Dictionary<string, string>();
        foreach (Match match in extRegex.Matches(text))
            idToFile[match.Groups["id"].Value] = Path.GetFileName(match.Groups["path"].Value);

        var sections = text.Split("[sub_resource type=\"TileSetAtlasSource\"", StringSplitOptions.RemoveEmptyEntries);
        foreach (var section in sections.Skip(1))
        {
            var texMatch = textureRegex.Match(section);
            if (!texMatch.Success)
                continue;

            if (!idToFile.TryGetValue(texMatch.Groups[1].Value, out var fileName))
                continue;

            result[fileName] = section;
        }

        return result;
    }

    private static RuleTile BuildRuleTile(BuildConfig config)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(config.TexturePath);
        if (texture == null)
        {
            Debug.LogWarning("Texture not found: " + config.TexturePath);
            return null;
        }

        var rows = texture.height / 16;
        var cols = texture.width / 16;
        var sprites = AssetDatabase.LoadAllAssetsAtPath(config.TexturePath)
            .OfType<Sprite>()
            .ToDictionary(s => s.name, s => s);

        var tileEntries = config.IsAnimatedWater
            ? BuildWaterRules(config, sprites)
            : ParseTerrainRules(config.BlockText, config.TerrainSet, config.TerrainId, config.SpritePrefix, rows, cols, sprites);

        if (tileEntries.Count == 0)
        {
            Debug.LogWarning("No rules generated for " + config.AssetName);
            return null;
        }

        tileEntries = tileEntries
            .OrderByDescending(e => e.Specificity)
            .ThenBy(e => e.SpriteName)
            .ToList();

        var assetPath = $"Assets/Tile/RuleTiles/{config.AssetName}.asset";
        var ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(assetPath);
        if (ruleTile == null)
        {
            ruleTile = ScriptableObject.CreateInstance<RuleTile>();
            AssetDatabase.CreateAsset(ruleTile, assetPath);
        }

        ruleTile.m_TilingRules = new List<RuleTile.TilingRule>();
        ruleTile.m_DefaultSprite = tileEntries[0].Sprite;
        ruleTile.m_DefaultColliderType = Tile.ColliderType.Sprite;

        var id = 0;
        foreach (var entry in tileEntries)
        {
            var rule = new RuleTile.TilingRule
            {
                m_Id = id++,
                m_Sprites = entry.Output == RuleTile.TilingRuleOutput.OutputSprite.Animation
                    ? entry.AnimationSprites
                    : new[] { entry.Sprite },
                m_Output = entry.Output,
                m_ColliderType = Tile.ColliderType.Sprite,
                m_RuleTransform = RuleTile.TilingRuleOutput.Transform.Fixed,
                m_MinAnimationSpeed = 1f,
                m_MaxAnimationSpeed = 1f,
            };

            var neighbors = new Dictionary<Vector3Int, int>();
            foreach (var pos in entry.ThisNeighbors)
                neighbors[pos] = (int)RuleTile.TilingRuleOutput.Neighbor.This;

            rule.ApplyNeighbors(neighbors);
            ruleTile.m_TilingRules.Add(rule);
        }

        EditorUtility.SetDirty(ruleTile);
        return ruleTile;
    }

    private static List<TileRuleEntry> ParseTerrainRules(
        string blockText,
        int terrainSet,
        int terrainId,
        string spritePrefix,
        int rows,
        int cols,
        Dictionary<string, Sprite> sprites)
    {
        var entries = new List<TileRuleEntry>();
        var atlasRegex = new Regex(@"^(?<col>\d+):(?<row>\d+)/0/terrain_set = (?<set>\d+)\s*$", RegexOptions.Multiline);
        var terrainRegex = new Regex(@"^(?<col>\d+):(?<row>\d+)/0/terrain = (?<terrain>\d+)\s*$", RegexOptions.Multiline);
        var peeringRegex = new Regex(
            @"^(?<col>\d+):(?<row>\d+)/0/terrains_peering_bit/(?<bit>[a-z_]+) = (?<value>\d+)\s*$",
            RegexOptions.Multiline);

        var terrainTiles = new Dictionary<(int col, int row), HashSet<string>>();
        var tileTerrainIds = new Dictionary<(int col, int row), int>();

        foreach (Match match in terrainRegex.Matches(blockText))
        {
            var key = (int.Parse(match.Groups["col"].Value), int.Parse(match.Groups["row"].Value));
            tileTerrainIds[key] = int.Parse(match.Groups["terrain"].Value);
        }

        foreach (Match match in atlasRegex.Matches(blockText))
        {
            if (int.Parse(match.Groups["set"].Value) != terrainSet)
                continue;

            var col = int.Parse(match.Groups["col"].Value);
            var row = int.Parse(match.Groups["row"].Value);
            var key = (col, row);
            if (tileTerrainIds.TryGetValue(key, out var tileTerrain) && tileTerrain != terrainId)
                continue;

            terrainTiles[key] = new HashSet<string>();
        }

        foreach (Match match in peeringRegex.Matches(blockText))
        {
            var key = (int.Parse(match.Groups["col"].Value), int.Parse(match.Groups["row"].Value));
            if (!terrainTiles.TryGetValue(key, out var bits))
                continue;

            if (int.Parse(match.Groups["value"].Value) != terrainId)
                continue;

            bits.Add(match.Groups["bit"].Value);
        }

        foreach (var ((col, godotRow), peerBits) in terrainTiles)
        {
            var unityRow = rows - 1 - godotRow;
            if (col < 0 || col >= cols || unityRow < 0 || unityRow >= rows)
                continue;

            var spriteName = $"{spritePrefix}_{col}_{unityRow}";
            if (!sprites.TryGetValue(spriteName, out var sprite))
                continue;

            var thisNeighbors = new HashSet<Vector3Int>();
            foreach (var bit in peerBits)
            {
                if (PeeringToUnity.TryGetValue(bit, out var pos))
                    thisNeighbors.Add(pos);
            }

            if (thisNeighbors.Count == 0)
                continue;

            entries.Add(new TileRuleEntry
            {
                Sprite = sprite,
                SpriteName = spriteName,
                ThisNeighbors = thisNeighbors,
                Specificity = thisNeighbors.Count,
                Output = RuleTile.TilingRuleOutput.OutputSprite.Single,
            });
        }

        return entries;
    }

    private static List<TileRuleEntry> BuildWaterRules(
        BuildConfig config,
        Dictionary<string, Sprite> sprites)
    {
        var frames = Enumerable.Range(0, 4)
            .Select(i => sprites.TryGetValue($"Water_{i}_0", out var s) ? s : null)
            .Where(s => s != null)
            .ToArray();

        if (frames.Length == 0)
            return new List<TileRuleEntry>();

        return new List<TileRuleEntry>
        {
            new()
            {
                Sprite = frames[0],
                AnimationSprites = frames,
                SpriteName = "Water_Animated",
                ThisNeighbors = new HashSet<Vector3Int>(),
                Specificity = 0,
                Output = RuleTile.TilingRuleOutput.OutputSprite.Animation,
            },
        };
    }

    private static void UpdatePalette(List<RuleTile> ruleTiles)
    {
        var palettePath = "Assets/Tile/SproutLands Palette.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(palettePath);
        if (prefab == null)
            return;

        AssetDatabase.SaveAssets();

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var tilemap = instance.GetComponentInChildren<Tilemap>(true);
        if (tilemap == null)
        {
            UnityEngine.Object.DestroyImmediate(instance);
            return;
        }

        var startX = 0;
        var y = tilemap.cellBounds.yMax + 2;
        if (y < 10)
            y = 10;

        for (var i = 0; i < ruleTiles.Count; i++)
        {
            tilemap.SetTile(new Vector3Int(startX + (i * 2), y, 0), ruleTiles[i]);
        }

        tilemap.CompressBounds();
        PrefabUtility.SaveAsPrefabAsset(instance, palettePath);
        UnityEngine.Object.DestroyImmediate(instance);
    }

    private sealed class BuildConfig
    {
        public BuildConfig(string assetName, string spritePrefix, string texturePath, string blockText, int terrainSet, int terrainId, bool isAnimatedWater = false)
        {
            AssetName = assetName;
            SpritePrefix = spritePrefix;
            TexturePath = texturePath;
            BlockText = blockText ?? string.Empty;
            TerrainSet = terrainSet;
            TerrainId = terrainId;
            IsAnimatedWater = isAnimatedWater;
        }

        public string AssetName { get; }
        public string SpritePrefix { get; }
        public string TexturePath { get; }
        public string BlockText { get; }
        public int TerrainSet { get; }
        public int TerrainId { get; }
        public bool IsAnimatedWater { get; }
    }

    private sealed class TileRuleEntry
    {
        public Sprite Sprite;
        public Sprite[] AnimationSprites;
        public string SpriteName;
        public HashSet<Vector3Int> ThisNeighbors;
        public int Specificity;
        public RuleTile.TilingRuleOutput.OutputSprite Output;
    }
}
