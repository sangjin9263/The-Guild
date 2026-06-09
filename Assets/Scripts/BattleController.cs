using UnityEngine;

/// <summary>
/// 같은 BattleArena 안 유닛만 보고 승패를 판정합니다.
/// </summary>
public class BattleController : MonoBehaviour
{
    [SerializeField] private BattleArena arena;

    private bool battleEnded;

    private void Awake()
    {
        if (arena == null)
            arena = GetComponent<BattleArena>();
    }

    private void Update()
    {
        if (battleEnded || arena == null || !arena.IsCombatRunning)
            return;

        var hasAllies = false;
        var hasEnemies = false;

        // arena.Units에 등록된 유닛만 검사해 다른 던전 유닛과 섞이지 않습니다.
        foreach (var unit in arena.Units)
        {
            if (unit == null || !unit.IsAlive)
                continue;

            if (unit.Team == AutoCombatTeam.Ally)
                hasAllies = true;
            else
                hasEnemies = true;
        }

        if (hasAllies && hasEnemies)
            return;

        battleEnded = true;
        arena.EndCombat();

        if (hasAllies)
            Debug.Log("Battle ended: Allies win.", this);
        else if (hasEnemies)
            Debug.Log("Battle ended: Enemies win.", this);
        else
            Debug.Log("Battle ended: Draw.", this);
    }
}
