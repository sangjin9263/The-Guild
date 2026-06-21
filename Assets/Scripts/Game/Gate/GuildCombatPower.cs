/// <summary>
/// 길드 전투력(임시). 용병 시스템 연동 전 Ebay 동점 판정에 사용합니다.
/// </summary>
public static class GuildCombatPower
{
    public const int PlayerTemporary = 500;

    /// <summary>AI1 중상 · AI2 약함 · AI3 강함 — 동점 낙찰 변화용.</summary>
    public static readonly int[] DefaultAiCombatPowers = { 580, 420, 650 };

    public static int GetPlayerPower() => PlayerTemporary;

    public static int GetAiPower(int aiIndex)
    {
        if (aiIndex < 0 || aiIndex >= DefaultAiCombatPowers.Length)
            return PlayerTemporary;

        return DefaultAiCombatPowers[aiIndex];
    }
}
