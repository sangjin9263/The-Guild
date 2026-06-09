using System;
using UnityEngine;

/// <summary>
/// 길드 골드·입장권·던전 슬롯 등 게임 전체 진행 상태를 한곳에서 관리합니다.
/// </summary>
[DefaultExecutionOrder(-50)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>골드·입장권·슬롯 등 상태가 바뀔 때 UI 등이 구독합니다.</summary>
    public static event Action StateChanged;

    [Header("Guild")]
    [SerializeField] private string guildName = "용병단";
    [SerializeField] private int gold = 1000;

    [Header("Entry Tickets")]
    [SerializeField] private int entryTicketCount;

    [Header("Dungeon Slots")]
    [SerializeField] private int unlockedSlotCount = 1;
    [SerializeField] private DungeonSlotRuntimeState[] dungeonSlots =
        CreateDefaultSlots(DesktopOverlaySettings.DungeonSlotCount);

    public string GuildName => guildName;
    public int Gold => gold;
    public int EntryTicketCount => entryTicketCount;
    public int UnlockedSlotCount => unlockedSlotCount;
    public int DungeonSlotCount => dungeonSlots.Length;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureSlotArray();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public DungeonSlotRuntimeState GetSlotState(int slotIndex)
    {
        EnsureSlotArray();
        return dungeonSlots[slotIndex];
    }

    public bool IsSlotEmpty(int slotIndex) => GetSlotState(slotIndex).Phase == DungeonSlotPhase.Empty;

    /// <summary>
    /// 아래쪽 슬롯(표시 이름 「던전 1」)부터 순서대로 열립니다. unlockedSlotCount=1이면 slotIndex 2만 true.
    /// </summary>
    public bool IsSlotUnlocked(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
            return false;

        return slotIndex >= GetFirstUnlockedSlotIndex(unlockedSlotCount);
    }

    public static bool IsSlotUnlockedForCount(int slotIndex, int slotCount)
    {
        if (slotIndex < 0 || slotIndex >= DesktopOverlaySettings.DungeonSlotCount)
            return false;

        return slotIndex >= GetFirstUnlockedSlotIndex(slotCount);
    }

    public bool TryUnlockNextSlot()
    {
        if (unlockedSlotCount >= DesktopOverlaySettings.DungeonSlotCount)
            return false;

        unlockedSlotCount++;
        NotifyStateChanged();
        return true;
    }

    public bool HasEntryTicket() => entryTicketCount > 0;

    public bool TryAddGold(int amount)
    {
        if (amount <= 0)
            return false;

        gold += amount;
        NotifyStateChanged();
        return true;
    }

    public bool TrySpendGold(int amount)
    {
        if (amount <= 0 || gold < amount)
            return false;

        gold -= amount;
        NotifyStateChanged();
        return true;
    }

    public bool TryAddEntryTicket(int count = 1)
    {
        if (count <= 0)
            return false;

        entryTicketCount += count;
        NotifyStateChanged();
        return true;
    }

    public bool TryConsumeEntryTicket(int count = 1)
    {
        if (count <= 0 || entryTicketCount < count)
            return false;

        entryTicketCount -= count;
        NotifyStateChanged();
        return true;
    }

    /// <summary>
    /// 입장권 1장을 쓰고 해당 슬롯을 InProgress로 바꿉니다. (파견·경매 연동은 이후 단계)
    /// </summary>
    public bool TryAssignEntryTicketToSlot(int slotIndex)
    {
        EnsureSlotArray();
        if (!IsValidSlotIndex(slotIndex))
            return false;

        if (dungeonSlots[slotIndex].Phase != DungeonSlotPhase.Empty)
            return false;

        if (!TryConsumeEntryTicket())
            return false;

        dungeonSlots[slotIndex].Phase = DungeonSlotPhase.InProgress;
        NotifyStateChanged();
        return true;
    }

    public void SetSlotPhase(int slotIndex, DungeonSlotPhase phase)
    {
        EnsureSlotArray();
        if (!IsValidSlotIndex(slotIndex))
            return;

        dungeonSlots[slotIndex].Phase = phase;
        NotifyStateChanged();
    }

    public void ResetSlot(int slotIndex)
    {
        EnsureSlotArray();
        if (!IsValidSlotIndex(slotIndex))
            return;

        dungeonSlots[slotIndex] = DungeonSlotRuntimeState.CreateEmpty();
        NotifyStateChanged();
    }

    /// <summary>새 게임 시작 시 골드·입장권·슬롯을 초기값으로 되돌립니다.</summary>
    public void ResetToNewGameDefaults()
    {
        gold = 1000;
        entryTicketCount = 0;
        unlockedSlotCount = 1;
        dungeonSlots = CreateDefaultSlots(DesktopOverlaySettings.DungeonSlotCount);
        NotifyStateChanged();
    }

    private void EnsureSlotArray()
    {
        if (dungeonSlots == null || dungeonSlots.Length != DesktopOverlaySettings.DungeonSlotCount)
            dungeonSlots = CreateDefaultSlots(DesktopOverlaySettings.DungeonSlotCount);
    }

    private static bool IsValidSlotIndex(int slotIndex) =>
        slotIndex >= 0 && slotIndex < DesktopOverlaySettings.DungeonSlotCount;

    private static int GetFirstUnlockedSlotIndex(int slotCount) =>
        DesktopOverlaySettings.DungeonSlotCount - Mathf.Clamp(slotCount, 1, DesktopOverlaySettings.DungeonSlotCount);

    private static DungeonSlotRuntimeState[] CreateDefaultSlots(int count)
    {
        var slots = new DungeonSlotRuntimeState[count];
        for (var i = 0; i < count; i++)
            slots[i] = DungeonSlotRuntimeState.CreateEmpty();

        return slots;
    }

    private static void NotifyStateChanged() => StateChanged?.Invoke();
}

/// <summary>
/// 던전 슬롯 하나의 런타임 상태 — GameManager.dungeonSlots 배열 원소와 1:1 대응합니다.
/// </summary>
[Serializable]
public struct DungeonSlotRuntimeState
{
    public DungeonSlotPhase Phase;

    public static DungeonSlotRuntimeState CreateEmpty() => new()
    {
        Phase = DungeonSlotPhase.Empty
    };
}
