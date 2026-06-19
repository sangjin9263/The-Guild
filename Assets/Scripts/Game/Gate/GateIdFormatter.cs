using System;

/// <summary>경매 UI용 Gate ID — G - {등급} - {4자리} - {지역코드}.</summary>
public static class GateIdFormatter
{
    public const string DefaultRegionCode = "6S";
    public const string DefaultRegionDisplay = "SEOUL, DANGSAN";

    public static string Format(GateGrade grade, int fourDigitId, string regionCode = DefaultRegionCode)
    {
        var gradeText = GateGradeUtility.GetDisplayName(grade);
        return $"G - {gradeText} - {fourDigitId:D4} - {regionCode}";
    }

    public static int RollFourDigitId(System.Random rng)
    {
        if (rng == null)
            throw new ArgumentNullException(nameof(rng));

        return rng.Next(1000, 10000);
    }
}
