public enum AuctionType
{
    Ebay = 0,
    English = 1
}

public static class AuctionTypeUtility
{
    public static bool TryParse(string text, out AuctionType auctionType)
    {
        auctionType = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return text.Trim().ToLowerInvariant() switch
        {
            "ebay" => Assign(AuctionType.Ebay, out auctionType),
            "english" => Assign(AuctionType.English, out auctionType),
            _ => false
        };
    }

    private static bool Assign(AuctionType value, out AuctionType auctionType)
    {
        auctionType = value;
        return true;
    }
}
