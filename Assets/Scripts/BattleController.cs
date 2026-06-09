using UnityEngine;

public class BattleController : MonoBehaviour
{
    private bool battleEnded;

    private void Update()
    {
        if (battleEnded)
            return;

        var hasAllies = false;
        var hasEnemies = false;

        foreach (var unit in FindObjectsByType<AutoCombatUnit>(FindObjectsSortMode.None))
        {
            if (!unit.IsAlive)
                continue;

            if (unit.Team == AutoCombatTeam.Ally)
                hasAllies = true;
            else
                hasEnemies = true;
        }

        if (hasAllies && hasEnemies)
            return;

        battleEnded = true;

        if (hasAllies)
            Debug.Log("Battle ended: Allies win.", this);
        else if (hasEnemies)
            Debug.Log("Battle ended: Enemies win.", this);
        else
            Debug.Log("Battle ended: Draw.", this);
    }
}
