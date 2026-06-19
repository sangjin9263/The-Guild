using System.Collections.Generic;
using UnityEngine;

/// <summary>gate_img_auction.png 슬라이스 런타임 fallback (GateDatabase.icon 미해결 시).</summary>
public static class GateGradeIconCache
{
    private const string AtlasResourcePath = "Art/Icon/gate_img_auction";

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

        foreach (var sprite in Resources.LoadAll<Sprite>(AtlasResourcePath))
        {
            if (sprite == null || string.IsNullOrEmpty(sprite.name))
                continue;

            _byName[sprite.name] = sprite;
        }

        foreach (GateGrade grade in System.Enum.GetValues(typeof(GateGrade)))
        {
            var spriteName = GateGradeUtility.GetDisplayName(grade);
            if (_byName.TryGetValue(spriteName, out var sprite))
                _byGrade[grade] = sprite;
        }
    }
}
