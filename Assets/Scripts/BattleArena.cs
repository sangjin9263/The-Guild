using System.Collections;
using UnityEngine;

public class BattleArena : MonoBehaviour
{
    private enum TeamSpawnAnchor
    {
        Center,
        LeftEdge,
        RightEdge
    }

    [Header("Spawn")]
    [SerializeField] private int allyCount = 3;
    [SerializeField] private int enemyCount = 3;
    [SerializeField] private float teamSpacing = 1.1f;
    [SerializeField] private Vector2 allyOrigin = new(-26f, -4f);
    [SerializeField] private Vector2 enemyOrigin = new(-17f, -4f);
    [SerializeField] private Vector2 unitScale = new(0.75f, 0.75f);

    [Header("Unit Defaults")]
    [SerializeField] private AutoCombatStats unitStats = AutoCombatStats.Default;

    private void Start()
    {
        StartCoroutine(SpawnWhenReady());
    }

    private IEnumerator SpawnWhenReady()
    {
        while (!WorkspaceLayoutController.IsApplied)
            yield return null;

        SpawnTeams();
    }

    private void SpawnTeams()
    {
        var camera = Camera.main;
        var allyOrigin = WorkspaceLayoutController.GetCombatAllyOrigin(camera);
        var enemyOrigin = WorkspaceLayoutController.GetCombatEnemyOrigin(camera);

        SpawnTeam(AutoCombatTeam.Enemy, enemyCount, enemyOrigin, TeamSpawnAnchor.LeftEdge);
        SpawnTeam(AutoCombatTeam.Ally, allyCount, allyOrigin, TeamSpawnAnchor.RightEdge);
    }

    private void OnEnable()
    {
        WorkspaceLayoutController.LayoutApplied += HandleLayoutApplied;
    }

    private void OnDisable()
    {
        WorkspaceLayoutController.LayoutApplied -= HandleLayoutApplied;
    }

    private void HandleLayoutApplied()
    {
        ClearUnits();
        SpawnTeams();
    }

    private void ClearUnits()
    {
        for (var i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    private void SpawnTeam(AutoCombatTeam team, int count, Vector2 origin, TeamSpawnAnchor anchor)
    {
        var rowWidth = (count - 1) * teamSpacing;
        var startX = anchor switch
        {
            TeamSpawnAnchor.LeftEdge => origin.x,
            TeamSpawnAnchor.RightEdge => origin.x - rowWidth,
            _ => origin.x - rowWidth * 0.5f
        };

        for (var i = 0; i < count; i++)
        {
            var position = new Vector2(startX + i * teamSpacing, origin.y);
            SpawnUnit(team, position);
        }
    }

    private void SpawnUnit(AutoCombatTeam team, Vector2 position)
    {
        var unitObject = new GameObject($"{team}_Unit");
        unitObject.transform.SetParent(transform, false);
        unitObject.transform.position = position;
        unitObject.transform.localScale = unitScale;

        var unit = unitObject.AddComponent<AutoCombatUnit>();
        unit.Configure(team, unitStats);
    }
}
