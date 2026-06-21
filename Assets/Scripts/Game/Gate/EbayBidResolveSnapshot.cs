/// <summary>Ebay 입찰가·전투력 판정 결과.</summary>
public readonly struct EbayBidResolveSnapshot
{
    public bool PlayerWins { get; }
    public bool BidAmountTie { get; }
    public bool PowerEqualTie { get; }
    public int PlayerPower { get; }
    public int WinningPower { get; }

    public EbayBidResolveSnapshot(
        bool playerWins,
        bool bidAmountTie,
        bool powerEqualTie,
        int playerPower,
        int winningPower)
    {
        PlayerWins = playerWins;
        BidAmountTie = bidAmountTie;
        PowerEqualTie = powerEqualTie;
        PlayerPower = playerPower;
        WinningPower = winningPower;
    }

    public string GetResultStatusText(bool won)
    {
        if (won)
        {
            if (!BidAmountTie)
                return "낙찰 성공!";

            return PowerEqualTie
                ? $"낙찰 성공!\n동점 · 전투력 {PlayerPower} (동률)"
                : $"낙찰 성공!\n동점 · 전투력 {PlayerPower}";
        }

        if (BidAmountTie)
            return $"낙찰 실패\n동점 · 전투력 {PlayerPower} < {WinningPower}";

        return "낙찰 실패";
    }
}
