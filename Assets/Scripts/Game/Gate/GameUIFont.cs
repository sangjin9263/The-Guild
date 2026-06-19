using TMPro;
using UnityEngine;

public enum GameUIFontWeight
{
    Regular = 0,
    Bold = 1,
    ExtraBold = 2
}

/// <summary>게임 UI 공통 폰트 — PF스타더스트 3.0 (Regular / Bold / ExtraBold).</summary>
public static class GameUIFont
{
    private const string RegularResourcePath = "Fonts/PF_Stardust3 SDF";
    private const string BoldResourcePath = "Fonts/PF_Stardust3 Bold SDF";
    private const string ExtraBoldResourcePath = "Fonts/PF_Stardust3 ExtraBold SDF";

    private static TMP_FontAsset _regular;
    private static TMP_FontAsset _bold;
    private static TMP_FontAsset _extraBold;

    public static TMP_FontAsset Regular => _regular ??= Load(RegularResourcePath);
    public static TMP_FontAsset Bold => _bold ??= Load(BoldResourcePath);
    public static TMP_FontAsset ExtraBold => _extraBold ??= Load(ExtraBoldResourcePath);

    public static TMP_FontAsset Get(GameUIFontWeight weight) => weight switch
    {
        GameUIFontWeight.Bold => Bold,
        GameUIFontWeight.ExtraBold => ExtraBold,
        _ => Regular
    };

    public static void Apply(TMP_Text label, GameUIFontWeight? weightOverride = null)
    {
        if (label == null)
            return;

        var weight = weightOverride ?? ResolveWeight(label);
        var font = Get(weight);
        if (font == null)
            return;

        label.font = font;
        label.fontSharedMaterial = font.material;
        label.fontStyle = FontStyles.Normal;
    }

    public static void ApplyDropdown(TMP_Dropdown dropdown, GameUIFontWeight weight = GameUIFontWeight.Regular)
    {
        if (dropdown == null)
            return;

        Apply(dropdown.captionText, weight);
        Apply(dropdown.itemText, weight);
    }

    public static void ApplyInputField(TMP_InputField input, GameUIFontWeight weight = GameUIFontWeight.Regular)
    {
        if (input == null)
            return;

        var font = Get(weight);
        if (font == null)
            return;

        input.fontAsset = font;

        if (input.textComponent != null)
            Apply(input.textComponent, weight);

        if (input.placeholder is TMP_Text placeholder)
            Apply(placeholder, weight);
    }

    public static void ApplyToHierarchy(GameObject root, GameUIFontWeight? defaultWeight = null)
    {
        if (root == null)
            return;

        foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            Apply(label, defaultWeight);

        foreach (var input in root.GetComponentsInChildren<TMP_InputField>(true))
            ApplyInputField(input, defaultWeight ?? GameUIFontWeight.Regular);
    }

    public static void ApplyToHierarchy(Component root, GameUIFontWeight? defaultWeight = null) =>
        ApplyToHierarchy(root != null ? root.gameObject : null, defaultWeight);

    /// <summary>런타임 동적 텍스트(길드명 등) 글리프를 SDF 아틀라스에 추가합니다.</summary>
    public static void EnsureGlyphs(TMP_Text label, string text)
    {
        if (label == null || string.IsNullOrEmpty(text))
            return;

        Apply(label);

        var font = label.font;
        if (font == null)
            return;

        if (font.HasCharacters(text))
        {
            label.ForceMeshUpdate(true);
            return;
        }

        TryAddGlyphs(font, text);
        label.font = font;
        label.ForceMeshUpdate(true);
    }

    private static void TryAddGlyphs(TMP_FontAsset font, string text)
    {
        if (font == null || string.IsNullOrEmpty(text) || font.HasCharacters(text))
            return;

        if (font.atlasPopulationMode == AtlasPopulationMode.Static)
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;

        font.TryAddCharacters(text, out _);
    }

    private static GameUIFontWeight ResolveWeight(TMP_Text label)
    {
        if (label == null)
            return GameUIFontWeight.Regular;

        if (IsExtraBoldLabel(label))
            return GameUIFontWeight.ExtraBold;

        if ((label.fontStyle & FontStyles.Bold) != 0)
            return GameUIFontWeight.Bold;

        return GameUIFontWeight.Regular;
    }

    private static bool IsExtraBoldLabel(TMP_Text label)
    {
        var path = label.transform.name;
        if (path.IndexOf("Title", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        var text = label.text;
        return text == "게이트 입장권 경매" || text == "게이트 경매장";
    }

    private static TMP_FontAsset Load(string resourcePath) =>
        Resources.Load<TMP_FontAsset>(resourcePath);
}
