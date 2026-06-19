using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>PF스타더스트 3.0 TTF 3종 → TMP SDF 베이크 + 경매 UI 프리팹 적용.</summary>
public static class GameUIFontBaker
{
    private const string SourceRegularPath = "Assets/Resources/Art/PF스타더스트 3.0.ttf";
    private const string SourceBoldPath = "Assets/Resources/Art/PF스타더스트 3.0 Bold.ttf";
    private const string SourceExtraBoldPath = "Assets/Resources/Art/PF스타더스트 3.0 ExtraBold.ttf";

    public const string OutputRegularPath = "Assets/Resources/Fonts/PF_Stardust3 SDF.asset";
    public const string OutputBoldPath = "Assets/Resources/Fonts/PF_Stardust3 Bold SDF.asset";
    public const string OutputExtraBoldPath = "Assets/Resources/Fonts/PF_Stardust3 ExtraBold SDF.asset";

    /// <summary>GateImport 등 레거시 참조용.</summary>
    public const string OutputFontAssetPath = OutputRegularPath;

    private static readonly (string source, string output, string assetName)[] FontTargets =
    {
        (SourceRegularPath, OutputRegularPath, "PF_Stardust3 SDF"),
        (SourceBoldPath, OutputBoldPath, "PF_Stardust3 Bold SDF"),
        (SourceExtraBoldPath, OutputExtraBoldPath, "PF_Stardust3 ExtraBold SDF"),
    };

    private static readonly string[] AdditionalUiText =
    {
        "확인",
        "결과 대기…",
        "낙찰 성공!",
        "낙찰 실패",
        "낙찰 완료 · 입장권 수령함",
        "입찰 실패 · 환불 완료",
        "내 입찰",
        "확인 후 입장권을 받습니다.",
        "입찰금이 환불되었습니다.",
        "에너지 예측",
        "예상 최저 보상",
        "예상 최대 보상",
        "최소 입찰가",
        "최대 입찰가",
        "최소 에너지",
        "입찰 금액",
        "입찰 하기",
        "입찰 중",
        "등급",
        "전체",
        "검색",
        "게이트 경매장",
        "게이트 입장권 경매",
        "골드 확률",
        "정보 구매",
        "입찰 결과를 기다리는 중입니다.",
        "입찰 결과를 확인해 주세요.",
        "초",
        "G",
        "·",
        "…",
        "!",
        "0123456789",
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
        "abcdefghijklmnopqrstuvwxyz",
        "F 등급",
        "E 등급",
        "D 등급",
        "C 등급",
        "B 등급",
        "A 등급",
        "S 등급",
        "SS 등급",
        "SSS 등급",
        "경매\n시작",
        "경매시작",
        "낙찰 성공!\n확인 후 입장권을 받습니다.",
        "내 입찰:",
        "...5초",
        "이 정도면 광산이 게이트 안에 있는 수준입니다.",
        "%",
        ":",
        ",",
        "현재가",
        "시작가",
        "최고 입찰",
        "입찰하기",
        "게이트 ID",
        "등급",
        "지역",
        "에너지 %",
        "대기중",
        "용병단",
        "플레이어",
        "포기",
        "경매 종료\n결과: 성공",
        "경매 종료\n결과: 실패",
        "진행 중인 경매를 포기 합니다.\n경매 포기 시 골드는 차감되지 않습니다.",
        "입찰금을 입력하세요.",
        "최소",
        " G 입찰",
        " G 단위 입찰",
        "입찰에 실패했습니다.",
        "철의 길드",
        "석양 길드",
        "붉은 방패",
        "은월 상단",
        "어둠의 경매단",
        "창공 길드",
        "대지 수호대",
        "심연 탐험대",
        "왕관 연합",
        "폭풍 기사단",
        "진행 중인 영국식 경매가 있습니다.",
        "입찰 결과를 확인해 주세요.",
        "입찰 결과를 기다리는 중입니다.",
        "경매가 종료되었습니다.",
        "이미 최고 입찰자입니다.",
        "골드가 부족합니다.",
        "경매 중",
        "입찰하기",
        "게이트 경매",
        "명",
    };

    [MenuItem("The Guild/Data/Bake Game UI Fonts (PF Stardust)")]
    public static void BakeFromMenu()
    {
        try
        {
            var db = AssetDatabase.LoadAssetAtPath<GateDatabase>(GateDatabase.DefaultAssetPath);
            if (db == null)
            {
                Debug.LogError("[GameUIFont] GateDatabase.asset not found. Import Gateinfo first.");
                return;
            }

            var result = BakeFromDatabase(db);
            var applied = ApplyToAllProjectAssets();
            Debug.Log($"[GameUIFont] OK\n  {result}\n  applied={applied}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GameUIFont] Failed: {ex.Message}\n{ex}");
        }
    }

    public static string BakeFromDatabase(GateDatabase db)
    {
        if (db == null)
            throw new System.ArgumentNullException(nameof(db));

        var characters = CollectCharacters(db);
        var summaries = new List<string>();

        foreach (var target in FontTargets)
        {
            var fontAsset = CreateFontAsset(target.source, target.output, target.assetName, characters);
            summaries.Add($"{target.assetName}={fontAsset.characterTable.Count}");
        }

        return $"characters={characters.Length}, {string.Join(", ", summaries)}";
    }

    private static TMP_FontAsset CreateFontAsset(
        string sourcePath,
        string outputPath,
        string assetName,
        string characters)
    {
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
        if (sourceFont == null)
            throw new FileNotFoundException($"Source font not found: {sourcePath}");

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath) != null)
            AssetDatabase.DeleteAsset(outputPath);

        var fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontAsset.name = assetName;

        if (!fontAsset.TryAddCharacters(characters, out var missing) && !string.IsNullOrEmpty(missing))
            throw new System.InvalidOperationException($"[{assetName}] Could not bake glyphs: {missing}");

        foreach (var c in characters)
        {
            if (char.IsControl(c))
                continue;

            if (!fontAsset.HasCharacter(c))
                throw new System.InvalidOperationException(
                    $"[{assetName}] Glyph missing after bake: '{c}' (U+{(int)c:X4})");
        }

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

        EnsureOutputFolder();
        AssetDatabase.CreateAsset(fontAsset, outputPath);
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

        if (fontAsset.atlasTextures != null)
        {
            foreach (var atlas in fontAsset.atlasTextures)
            {
                if (atlas != null)
                    AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            }
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        return fontAsset;
    }

    [MenuItem("The Guild/Data/Apply Auction UI Font To Prefabs")]
    public static void ApplyAuctionUiFontToPrefabsFromMenu()
    {
        EnsureFontsBaked();
        var updated = ApplyToAllProjectAssets();
        AssetDatabase.SaveAssets();
        Debug.Log($"[GameUIFont] Applied PF Stardust to {updated} TMP labels (all project assets).");
    }

    [MenuItem("The Guild/Data/Apply Game UI Font To All Project Assets")]
    public static void ApplyToAllProjectAssetsFromMenu()
    {
        EnsureFontsBaked();
        var updated = ApplyToAllProjectAssets();
        AssetDatabase.SaveAssets();
        Debug.Log($"[GameUIFont] Applied PF Stardust to {updated} TMP labels (all project assets).");
    }

    public static int ApplyToAllProjectAssets()
    {
        var updated = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (ShouldSkipAssetPath(path))
                continue;

            updated += ApplyFontToPrefab(path);
        }

        var activeScenePath = SceneManager.GetActiveScene().path;
        foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (ShouldSkipAssetPath(path))
                continue;

            updated += ApplyFontToScene(path);
        }

        if (!string.IsNullOrEmpty(activeScenePath) && AssetDatabase.LoadAssetAtPath<SceneAsset>(activeScenePath) != null)
            EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);

        return updated;
    }

    private static bool ShouldSkipAssetPath(string path) =>
        string.IsNullOrEmpty(path)
        || path.StartsWith("Packages/")
        || path.StartsWith("Assets/Store/")
        || path.Contains("/TextMesh Pro/");

    private static void EnsureFontsBaked()
    {
        foreach (var target in FontTargets)
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(target.output) == null)
                throw new FileNotFoundException(
                    $"Font asset missing: {target.output}. Run Bake Game UI Fonts first.");
        }
    }

    private static int ApplyFontToPrefab(string prefabPath)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var count = 0;
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (ApplyFontToLabel(label))
                    count++;
            }

            foreach (var input in root.GetComponentsInChildren<TMP_InputField>(true))
            {
                if (ApplyFontToInputField(input))
                    count++;
            }

            foreach (var dropdown in root.GetComponentsInChildren<TMP_Dropdown>(true))
            {
                if (dropdown.captionText != null && ApplyFontToLabel(dropdown.captionText))
                    count++;

                if (dropdown.itemText != null && ApplyFontToLabel(dropdown.itemText))
                    count++;
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            return count;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static int ApplyFontToScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var count = 0;

        foreach (var label in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (RevertPrefabFontOverrides(label))
                continue;

            if (ApplyFontToLabel(label))
                count++;
        }

        foreach (var input in Object.FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (RevertPrefabFontOverrides(input))
                continue;

            if (ApplyFontToInputField(input))
                count++;
        }

        foreach (var dropdown in Object.FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (dropdown.captionText != null && ApplyFontToLabel(dropdown.captionText))
                count++;

            if (dropdown.itemText != null && ApplyFontToLabel(dropdown.itemText))
                count++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return count;
    }

    private static bool ApplyFontToInputField(TMP_InputField input)
    {
        if (input == null)
            return false;

        var font = LoadEditorFont(GameUIFontWeight.Regular);
        if (font == null)
            return false;

        input.fontAsset = font;

        if (input.textComponent != null)
            ApplyFontToLabel(input.textComponent);

        if (input.placeholder is TMP_Text placeholder)
            ApplyFontToLabel(placeholder);

        EditorUtility.SetDirty(input);
        return true;
    }

    private static bool RevertPrefabFontOverrides(Component component)
    {
        if (component == null || !PrefabUtility.IsPartOfPrefabInstance(component))
            return false;

        RevertPrefabProperty(component, "m_fontAsset");
        RevertPrefabProperty(component, "m_sharedMaterial");
        RevertPrefabProperty(component, "m_fontStyle");
        RevertPrefabProperty(component, "m_GlobalFontAsset");
        return true;
    }

    private static void RevertPrefabProperty(Component component, string propertyName)
    {
        var serializedObject = new SerializedObject(component);
        var property = serializedObject.FindProperty(propertyName);
        if (property == null || !property.prefabOverride)
            return;

        PrefabUtility.RevertPropertyOverride(property, InteractionMode.AutomatedAction);
    }

    private static bool ApplyFontToLabel(TMP_Text label)
    {
        if (label == null)
            return false;

        var weight = ResolvePrefabWeight(label);
        var font = LoadEditorFont(weight);
        if (font == null)
            return false;

        label.font = font;
        label.fontSharedMaterial = font.material;
        label.fontStyle = FontStyles.Normal;
        EditorUtility.SetDirty(label);
        return true;
    }

    private static GameUIFontWeight ResolvePrefabWeight(TMP_Text label)
    {
        if (label.name.IndexOf("Title", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return GameUIFontWeight.ExtraBold;

        var text = label.text;
        if (text == "게이트 입장권 경매" || text == "게이트 경매장")
            return GameUIFontWeight.ExtraBold;

        if ((label.fontStyle & FontStyles.Bold) != 0)
            return GameUIFontWeight.Bold;

        return GameUIFontWeight.Regular;
    }

    private static TMP_FontAsset LoadEditorFont(GameUIFontWeight weight) => weight switch
    {
        GameUIFontWeight.Bold =>
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputBoldPath),
        GameUIFontWeight.ExtraBold =>
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputExtraBoldPath),
        _ => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputRegularPath)
    };

    private static void EnsureOutputFolder()
    {
        if (AssetDatabase.IsValidFolder("Assets/Resources/Fonts"))
            return;

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        AssetDatabase.CreateFolder("Assets/Resources", "Fonts");
    }

    private static string CollectCharacters(GateDatabase db)
    {
        var set = new HashSet<char>();

        foreach (var hint in db.brokerHints)
        {
            AddText(set, hint.hintText);
            AddText(set, hint.hintInfo);
        }

        var itemDb = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabase.DefaultAssetPath);
        var dungeonDb = AssetDatabase.LoadAssetAtPath<DungeonDatabase>(DungeonDatabase.DefaultAssetPath);
        if (itemDb != null && dungeonDb != null)
        {
            foreach (var loot in dungeonDb.lootPools)
            {
                var itemDef = itemDb.GetItemByKey(loot.itemKey);
                if (itemDef != null)
                    AddText(set, itemDef.Value.displayName);
            }
        }

        foreach (var text in AdditionalUiText)
            AddText(set, text);

        foreach (var name in EnglishAuctionSession.AiGuildNamePool)
            AddText(set, name);

        CollectCharactersFromProjectAssets(set);

        var ordered = set.Where(c => !char.IsControl(c)).OrderBy(c => c).ToArray();
        return new string(ordered);
    }

    private static void CollectCharactersFromProjectAssets(HashSet<char> set)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (ShouldSkipAssetPath(path))
                continue;

            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null)
                continue;

            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
                AddText(set, label.text);
        }
    }

    private static void AddText(HashSet<char> set, string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        foreach (var c in text)
            set.Add(c);
    }
}
