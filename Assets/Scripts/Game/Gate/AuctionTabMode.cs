public enum AuctionTabMode
{
    grade_only = 0,
    grade_and_unified = 1
}

public static class AuctionTabModeUtility
{
    public static bool TryParse(string text, out AuctionTabMode mode)
    {
        mode = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var key = text.Trim().ToLowerInvariant();
        if (key == "grade_only")
        {
            mode = AuctionTabMode.grade_only;
            return true;
        }

        if (key == "grade_and_unified")
        {
            mode = AuctionTabMode.grade_and_unified;
            return true;
        }

        return false;
    }
}
