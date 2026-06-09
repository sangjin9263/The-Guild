using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 던전 패널 안에서 아군·적군 유닛을 생성하고, 같은 아레나 안 유닛 목록을 관리합니다.
/// </summary>
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
    [SerializeField] private Vector2 unitScale = new(0.75f, 0.75f);

    [Header("Unit Defaults")]
    [SerializeField] private AutoCombatStats unitStats = AutoCombatStats.Default;

    // 이 아레나에 속한 유닛만 전투 AI와 승패 판정에 사용합니다.
    private readonly List<AutoCombatUnit> units = new();

    public IReadOnlyList<AutoCombatUnit> Units => units;
    public bool IsCombatRunning { get; private set; }

    public void EndCombat() => IsCombatRunning = false;

    private void Start()
    {
        StartCoroutine(SpawnWhenReady());
    }

    private IEnumerator SpawnWhenReady()
    {
        while (!WorkspaceLayoutController.IsApplied)
            yield return null;

        if (!IsActiveCombatArena())
            yield break;

        SpawnTeams();
    }

    private void OnEnable()
    {
        WorkspaceLayoutController.LayoutApplied += HandleLayoutApplied;
    }

    private void OnDisable()
    {
        WorkspaceLayoutController.LayoutApplied -= HandleLayoutApplied;
        IsCombatRunning = false;
    }

    private void HandleLayoutApplied()
    {
        if (!IsActiveCombatArena())
        {
            ClearUnits();
            return;
        }

        ClearUnits();
        SpawnTeams();
    }

    private bool IsActiveCombatArena()
    {
        var dungeon = GetComponentInParent<DungeonPanelContent>();
        if (dungeon == null)
            return true;

        if (WorkspaceLayoutController.Instance != null)
            return WorkspaceLayoutController.Instance.IsPrimaryCombatSlot(dungeon.SlotIndex);

        return dungeon.SlotIndex == 2;
    }

    public Tilemap GetGroundTilemap()
    {
        var dungeon = GetComponentInParent<DungeonPanelContent>();
        return dungeon != null ? dungeon.GroundTilemap : null;
    }

    internal void RegisterUnit(AutoCombatUnit unit)
    {
        if (unit != null && !units.Contains(unit))
            units.Add(unit);
    }

    internal void UnregisterUnit(AutoCombatUnit unit)
    {
        units.Remove(unit);
    }

    private void SpawnTeams()
    {
        var camera = Camera.main;
        var allySpawn = WorkspaceLayoutController.GetCombatAllyOrigin(camera);
        var enemySpawn = WorkspaceLayoutController.GetCombatEnemyOrigin(camera);

        SpawnTeam(AutoCombatTeam.Enemy, enemyCount, enemySpawn, TeamSpawnAnchor.LeftEdge);
        SpawnTeam(AutoCombatTeam.Ally, allyCount, allySpawn, TeamSpawnAnchor.RightEdge);
        IsCombatRunning = units.Count > 0;
    }

    private void ClearUnits()
    {
        for (var i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        units.Clear();
        IsCombatRunning = false;
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
        unit.Configure(team, unitStats, this);
    }
}
