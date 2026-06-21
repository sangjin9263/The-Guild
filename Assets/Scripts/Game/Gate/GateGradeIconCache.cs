using System.Collections.Generic;
using UnityEngine;

/// <summary>Gate Icon.png / gate_img_auction.png 슬라이스 런타임 fallback (GateDatabase.icon 미해결 시).</summary>
public static class GateGradeIconCache
{
    private const string GateIconAtlasPath = "Art/Icon/Gate Icon";
    private const string LegacyAtlasResourcePath = "Art/Icon/gate_img_auction";

    private static Dictionary<GateGrade, Sprite> _byGrade;
    private static Dictionary<string, Sprite> _byName;

    public static Sprite GetIcon(GateGrade grade, string overrideSpriteName = null)
    {
        EnsureLoaded();

        if (!string.IsNullOrWhiteSpace(overrideSpriteName) &&
            _byName.TryGetValue(overrideSpriteName.Trim(), out var custom))
            return custom;

        _byGrade.TryGetValue(grade, out var sprite);
        return sprite;
    }

    private static void EnsureLoaded()
    {
        if (_byGrade != null)
            return;

        _byGrade = new Dictionary<GateGrade, Sprite>();
        _byName = new Dictionary<string, Sprite>();

        RegisterSprites(Resources.LoadAll<Sprite>(GateIconAtlasPath));
        RegisterSprites(Resources.LoadAll<Sprite>(LegacyAtlasResourcePath));

        foreach (GateGrade grade in System.Enum.GetValues(typeof(GateGrade)))
        {
            var gradeName = GateGradeUtility.GetDisplayName(grade);
            if (_byName.TryGetValue($"Gate Icon_{gradeName}", out var gateIconSprite))
                _byGrade[grade] = gateIconSprite;
            else if (_byName.TryGetValue(gradeName, out var legacySprite))
                _byGrade[grade] = legacySprite;
        }
    }

    private static void RegisterSprites(Sprite[] sprites)
    {
        if (sprites == null)
            return;

        foreach (var sprite in sprites)
        {
            if (sprite == null || string.IsNullOrEmpty(sprite.name))
                continue;

            _byName[sprite.name] = sprite;
        }
    }
}
