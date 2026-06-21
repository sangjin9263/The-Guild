using System.Collections.Generic;
using UnityEngine;

/// <summary>gate_img_auction.png — 슬롯/상세 main_img용 게이트 프리뷰 (등급별).</summary>
public static class GateAuctionImageCache
{
    private const string AtlasResourcePath = "Art/Icon/gate_img_auction";

    private static Dictionary<GateGrade, Sprite> _byGrade;
    private static Dictionary<string, Sprite> _byName;

    public static Sprite GetImage(GateGrade grade)
    {
        EnsureLoaded();
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
            var gradeName = GateGradeUtility.GetDisplayName(grade);
            if (_byName.TryGetValue(gradeName, out var sprite))
                _byGrade[grade] = sprite;
        }
    }
}
