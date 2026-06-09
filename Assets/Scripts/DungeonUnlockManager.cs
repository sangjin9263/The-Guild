using System;
using UnityEngine;

public class DungeonUnlockManager : MonoBehaviour
{
    public static DungeonUnlockManager Instance { get; private set; }

    public static event Action UnlocksChanged;

    [SerializeField] private int playerLevel = 1;
    [SerializeField] private int unlockDungeon2Level = 5;
    [SerializeField] private int unlockDungeon1Level = 10;

    public int PlayerLevel => playerLevel;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool IsSlotUnlocked(int slotIndex)
    {
        return slotIndex switch
        {
            2 => true,
            1 => playerLevel >= unlockDungeon2Level,
            0 => playerLevel >= unlockDungeon1Level,
            _ => false
        };
    }

    public int GetVisibleDungeonRowCount()
    {
        var count = 0;
        for (var slotIndex = 0; slotIndex < DesktopOverlaySettings.DungeonSlotCount; slotIndex++)
        {
            if (IsSlotUnlocked(slotIndex))
                count++;
        }

        return Mathf.Max(1, count);
    }

    public int GetStackIndexFromBottom(int slotIndex)
    {
        var stackIndex = 0;
        for (var index = DesktopOverlaySettings.DungeonSlotCount - 1; index > slotIndex; index--)
        {
            if (IsSlotUnlocked(index))
                stackIndex++;
        }

        return stackIndex;
    }

    public void SetPlayerLevel(int level)
    {
        playerLevel = Mathf.Max(1, level);
        UnlocksChanged?.Invoke();
    }
}
