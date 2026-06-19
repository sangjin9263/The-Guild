/// <summary>게이트 등급 (F ~ SSS).</summary>
public enum GateGrade
{
    F = 0,
    E = 1,
    D = 2,
    C = 3,
    B = 4,
    A = 5,
    S = 6,
    SS = 7,
    SSS = 8
}

public static class GateGradeUtility
{
    private static readonly string[] OrderedNames = { "F", "E", "D", "C", "B", "A", "S", "SS", "SSS" };

    public static bool TryParse(string text, out GateGrade grade)
    {
        grade = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim().ToUpperInvariant();
        for (var i = OrderedNames.Length - 1; i >= 0; i--)
        {
            if (trimmed != OrderedNames[i])
                continue;

            grade = (GateGrade)i;
            return true;
        }

        return false;
    }

    public static GateGrade Parse(string text)
    {
        if (!TryParse(text, out var grade))
            throw new System.ArgumentException($"Unknown gate grade: '{text}'");

        return grade;
    }

    public static string GetDisplayName(GateGrade grade) => OrderedNames[(int)grade];

    public static bool IsAtLeast(GateGrade grade, GateGrade minGrade) => grade >= minGrade;
}
