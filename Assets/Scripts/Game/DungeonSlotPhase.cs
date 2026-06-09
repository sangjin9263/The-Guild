/// <summary>
/// 던전 슬롯 하나가 지금 어떤 단계인지 나타냅니다.
/// </summary>
public enum DungeonSlotPhase
{
    /// <summary>입장권 없음 — 슬롯 비어 있음</summary>
    Empty,

    /// <summary>용병 파견 후 클리어 시간 진행 중</summary>
    InProgress,

    /// <summary>클리어 완료 — 보상 수령 대기</summary>
    ReadyToClaim
}
