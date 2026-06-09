using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class WorkspaceLayoutSetup
{
    private const string MainScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("The Guild/Layout/Setup Independent Panels In Main")]
    [MenuItem("The Guild/Layout/Setup Workspace In Main")]
    public static void SetupWorkspaceInMain()
    {
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        ApplyWorkspaceLayout();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Independent workspace panels applied to Main scene.");
    }

    [MenuItem("The Guild/Layout/Setup Workspace")]
    public static void SetupWorkspaceInActiveScene()
    {
        ApplyWorkspaceLayout();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Independent workspace panels applied.");
    }

    [MenuItem("The Guild/Layout/Reset Workspace Panel Positions")]
    public static void ResetWorkspacePanelPositions()
    {
        WorkspacePanelLayoutStore.ClearAll();

        if (Application.isPlaying)
        {
            if (WorkspaceLayoutController.Instance != null)
                WorkspaceLayoutController.Instance.ResetAllPanelsToDefaultLayout();
            else
            {
                foreach (var panel in Object.FindObjectsByType<WorkspacePanel>(
                             FindObjectsInactive.Include,
                             FindObjectsSortMode.None))
                    panel.ResetToDefault();
            }
        }
        else
        {
            foreach (var panel in Object.FindObjectsByType<WorkspacePanel>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
                panel.ResetToDefault();

            var controller = Object.FindFirstObjectByType<WorkspaceLayoutController>();
            controller?.ApplyEditorPreviewLayout();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        SceneView.RepaintAll();
        Debug.Log(
            "Workspace panel positions reset to the default non-overlapping layout. " +
            "Save the scene (Ctrl+S) if you are not in Play mode.");
    }

    public static void ApplyWorkspaceLayout()
    {
        EnsureEventSystem();
        EnsureBootstrap();

        var panelContentsRoot = EnsureRoot("PanelContents");
        var townContent = EnsureTownContent(panelContentsRoot.transform);
        var uiContent = EnsureUiContent(panelContentsRoot.transform);
        var dungeon1Content = EnsureDungeonContent(panelContentsRoot.transform, 0, "Dungeon1Content", "Dungeon 1");
        var dungeon2Content = EnsureDungeonContent(panelContentsRoot.transform, 1, "Dungeon2Content", "Dungeon 2");
        var dungeon3Content = EnsureDungeonContent(panelContentsRoot.transform, 2, "Dungeon3Content", "Dungeon 3");

        MigrateLegacyWorldContent(townContent, dungeon3Content);

        EnsureWorkspaceCanvas(
            townContent,
            uiContent,
            dungeon1Content,
            dungeon2Content,
            dungeon3Content);

        BuildingUISetup.EnsureBuildingPanel();
        WireUiZoneContent(uiContent);
        HideLockedDungeonPanels();
        CleanupLegacyHierarchy();
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static void EnsureBootstrap()
    {
        var bootstrapObject = GameObject.Find("DesktopOverlay");
        if (bootstrapObject == null)
            bootstrapObject = new GameObject("DesktopOverlay");

        if (bootstrapObject.GetComponent<DesktopOverlayBootstrap>() == null)
            bootstrapObject.AddComponent<DesktopOverlayBootstrap>();

        if (bootstrapObject.GetComponent<DungeonUnlockManager>() == null)
            bootstrapObject.AddComponent<DungeonUnlockManager>();

        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(bootstrapObject);
    }

    private static GameObject EnsureRoot(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
            return existing;

        return new GameObject(name);
    }

    private static TownPanelContent EnsureTownContent(Transform parent)
    {
        var contentObject = EnsureChildObject(parent, "TownContent");
        var content = contentObject.GetComponent<TownPanelContent>();
        if (content == null)
            content = contentObject.AddComponent<TownPanelContent>();

        var grid = EnsureChildObject(contentObject.transform, "Grid");
        if (grid.GetComponent<Grid>() == null)
        {
            var gridComponent = grid.AddComponent<Grid>();
            gridComponent.cellSize = new Vector3(1f, 1f, 0f);
        }

        var ground = ZoneLayoutSetup.EnsureTilemapUnder(grid.transform, "Ground");
        var props = EnsureChildObject(contentObject.transform, "Props").transform;
        var portals = EnsureChildObject(contentObject.transform, "Portals").transform;
        var villagePortal = EnsureChildObject(portals, "VillagePortal").transform;
        PortalVisualSetup.EnsurePortalVisual(villagePortal);

        WireTownContent(content, ground, props, villagePortal);
        return content;
    }

    private static UiZonePanelContent EnsureUiContent(Transform parent)
    {
        var contentObject = EnsureChildObject(parent, "UiContent");
        var content = contentObject.GetComponent<UiZonePanelContent>();
        if (content == null)
            content = contentObject.AddComponent<UiZonePanelContent>();
        return content;
    }

    private static DungeonPanelContent EnsureDungeonContent(
        Transform parent,
        int slotIndex,
        string objectName,
        string displayName)
    {
        var contentObject = EnsureChildObject(parent, objectName);
        var content = contentObject.GetComponent<DungeonPanelContent>();
        if (content == null)
            content = contentObject.AddComponent<DungeonPanelContent>();

        var grid = EnsureChildObject(contentObject.transform, "Grid");
        if (grid.GetComponent<Grid>() == null)
        {
            var gridComponent = grid.AddComponent<Grid>();
            gridComponent.cellSize = new Vector3(1f, 1f, 0f);
        }

        var ground = ZoneLayoutSetup.EnsureTilemapUnder(grid.transform, "Ground");
        var props = EnsureChildObject(contentObject.transform, "Props").transform;
        var portals = EnsureChildObject(contentObject.transform, "Portals").transform;
        var portal = EnsureChildObject(portals, "Portal").transform;
        PortalVisualSetup.EnsurePortalVisual(portal);
        var battle = EnsureChildObject(contentObject.transform, "Battle").transform;

        if (battle.GetComponent<BattleArena>() == null)
            battle.gameObject.AddComponent<BattleArena>();

        WireDungeonContent(content, slotIndex, displayName, ground, props, portal, battle);
        return content;
    }

    private static void MigrateLegacyWorldContent(
        TownPanelContent townContent,
        DungeonPanelContent dungeon3Content)
    {
        ReparentIfFound("Village_Ground", townContent.transform, "Grid/Ground");
        ReparentIfFound("Village_Props", townContent.transform, "Props");
        ReparentIfFound("Portals/VillagePortal", townContent.transform, "Portals/VillagePortal");

        ReparentIfFound("Combat_Ground", dungeon3Content.transform, "Grid/Ground");
        ReparentIfFound("Combat_Props", dungeon3Content.transform, "Props");
        ReparentIfFound("Portals/BattlePortal", dungeon3Content.transform, "Portals/Portal");
        ReparentIfFound("Battle", dungeon3Content.transform, "Battle");

        var townGround = townContent.transform.Find("Grid/Ground")?.GetComponent<Tilemap>();
        if (townGround != null && !ZoneLayoutSetup.HasPaintedTiles(townGround))
            ZoneLayoutSetup.PaintVillageGround(townGround);

        var dungeon3Ground = dungeon3Content.transform.Find("Grid/Ground")?.GetComponent<Tilemap>();
        if (dungeon3Ground != null && !ZoneLayoutSetup.HasPaintedTiles(dungeon3Ground))
            ZoneLayoutSetup.PaintCombatGround(dungeon3Ground);

        WireTownContent(
            townContent,
            townGround,
            townContent.transform.Find("Props"),
            townContent.transform.Find("Portals/VillagePortal"));

        WireDungeonContent(
            dungeon3Content,
            2,
            "Dungeon 3",
            dungeon3Ground,
            dungeon3Content.transform.Find("Props"),
            dungeon3Content.transform.Find("Portals/Portal"),
            dungeon3Content.transform.Find("Battle"));
    }

    private static void ReparentIfFound(string path, Transform newRoot, string newPath)
    {
        var source = GameObject.Find(path);
        if (source == null)
            return;

        var targetParent = newRoot;
        var segments = newPath.Split('/');
        for (var i = 0; i < segments.Length - 1; i++)
            targetParent = EnsureChildObject(targetParent, segments[i]).transform;

        source.transform.SetParent(targetParent, false);
        source.name = segments[^1];
    }

    private static void EnsureWorkspaceCanvas(
        TownPanelContent townContent,
        UiZonePanelContent uiContent,
        DungeonPanelContent dungeon1Content,
        DungeonPanelContent dungeon2Content,
        DungeonPanelContent dungeon3Content)
    {
        var workspaceObject = GameObject.Find("GameWorkspace");
        if (workspaceObject == null)
            workspaceObject = new GameObject("GameWorkspace");

        var canvas = workspaceObject.GetComponent<Canvas>();
        if (canvas == null)
            canvas = workspaceObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        if (workspaceObject.GetComponent<CanvasScaler>() == null)
            workspaceObject.AddComponent<CanvasScaler>();

        if (workspaceObject.GetComponent<GraphicRaycaster>() == null)
            workspaceObject.AddComponent<GraphicRaycaster>();

        var controller = workspaceObject.GetComponent<WorkspaceLayoutController>();
        if (controller == null)
            controller = workspaceObject.AddComponent<WorkspaceLayoutController>();

        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("workspaceCanvas").objectReferenceValue = canvas;
        serializedController.FindProperty("workspaceRoot").objectReferenceValue =
            workspaceObject.GetComponent<RectTransform>();
        serializedController.FindProperty("primaryCombatSlotIndex").intValue = 2;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EnsurePanel(
            workspaceObject.transform,
            WorkspacePanelId.Town,
            "TownPanel",
            "마을",
            DesktopOverlaySettings.GetDefaultTownRect(),
            DesktopOverlaySettings.TownPanelHeight,
            new Color(1f, 0.45f, 0.1f, 0.35f),
            townContent.transform,
            null);

        EnsurePanel(
            workspaceObject.transform,
            WorkspacePanelId.UiZone,
            "UiZonePanel",
            "UI",
            DesktopOverlaySettings.GetDefaultUiZoneRect(),
            DesktopOverlaySettings.ReferenceHeight - DesktopOverlaySettings.TownPanelHeight,
            new Color(0.65f, 0.65f, 0.65f, 0.25f),
            null,
            uiContent);

        EnsurePanel(
            workspaceObject.transform,
            WorkspacePanelId.DungeonSlot1,
            "DungeonPanel_1",
            "던전 1",
            DesktopOverlaySettings.GetDefaultDungeonRectForSlot(0),
            DesktopOverlaySettings.DungeonSlotHeight,
            new Color(0.35f, 0.55f, 0.95f, 0.35f),
            dungeon1Content.transform,
            null);

        EnsurePanel(
            workspaceObject.transform,
            WorkspacePanelId.DungeonSlot2,
            "DungeonPanel_2",
            "던전 2",
            DesktopOverlaySettings.GetDefaultDungeonRectForSlot(1),
            DesktopOverlaySettings.DungeonSlotHeight,
            new Color(0.35f, 0.55f, 0.95f, 0.35f),
            dungeon2Content.transform,
            null);

        EnsurePanel(
            workspaceObject.transform,
            WorkspacePanelId.DungeonSlot3,
            "DungeonPanel_3",
            "던전 3",
            DesktopOverlaySettings.GetDefaultDungeonRectForSlot(2),
            DesktopOverlaySettings.DungeonSlotHeight,
            new Color(0.35f, 0.55f, 0.95f, 0.35f),
            dungeon3Content.transform,
            null);
    }

    private static void EnsurePanel(
        Transform parent,
        string panelId,
        string objectName,
        string label,
        WorkspacePanelRect defaultRect,
        float scaleReferenceHeight,
        Color tint,
        Transform worldContentRoot,
        UiZonePanelContent uiZoneContent)
    {
        var panelTransform = parent.Find(objectName) ?? parent.Find(panelId);
        GameObject panelObject;
        if (panelTransform == null)
        {
            panelObject = new GameObject(objectName, typeof(RectTransform));
            panelObject.transform.SetParent(parent, false);
        }
        else
        {
            panelObject = panelTransform.gameObject;
            panelObject.name = objectName;
        }

        var panel = panelObject.GetComponent<WorkspacePanel>();
        if (panel == null)
            panel = panelObject.AddComponent<WorkspacePanel>();

        var serialized = new SerializedObject(panel);
        serialized.FindProperty("panelId").stringValue = panelId;
        serialized.FindProperty("label").stringValue = label;
        serialized.FindProperty("frameColor").colorValue = tint;
        SetSerializedRect(serialized.FindProperty("defaultRect"), defaultRect);
        serialized.FindProperty("worldContentRoot").objectReferenceValue = worldContentRoot;
        serialized.FindProperty("uiZoneContent").objectReferenceValue = uiZoneContent;
        serialized.FindProperty("worldScaleReferenceHeight").floatValue = scaleReferenceHeight;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        panel.Configure(panelId, defaultRect, label, tint, scaleReferenceHeight);
        panel.ApplyReferenceRect(defaultRect);

        EnsureLabel(panelObject.transform, label);
    }

    private static void WireUiZoneContent(UiZonePanelContent uiContent)
    {
        var buildingUi = Object.FindFirstObjectByType<BuildingPanelUI>();
        if (buildingUi == null || uiContent == null)
            return;

        var canvas = buildingUi.GetComponent<Canvas>();
        var buildingSerialized = new SerializedObject(buildingUi);
        var panelRootObject = buildingSerialized.FindProperty("panelRoot").objectReferenceValue as GameObject;

        var serialized = new SerializedObject(uiContent);
        serialized.FindProperty("uiCanvas").objectReferenceValue = canvas;
        serialized.FindProperty("panelRoot").objectReferenceValue =
            panelRootObject != null ? panelRootObject.GetComponent<RectTransform>() : null;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        if (canvas != null && canvas.transform.parent != uiContent.transform)
            canvas.transform.SetParent(uiContent.transform, true);

        foreach (var panel in Object.FindObjectsByType<WorkspacePanel>(FindObjectsSortMode.None))
        {
            if (panel.PanelId != WorkspacePanelId.UiZone)
                continue;

            var panelSerialized = new SerializedObject(panel);
            panelSerialized.FindProperty("uiZoneContent").objectReferenceValue = uiContent;
            panelSerialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetSerializedRect(SerializedProperty property, WorkspacePanelRect rect)
    {
        property.FindPropertyRelative("x").floatValue = rect.x;
        property.FindPropertyRelative("y").floatValue = rect.y;
        property.FindPropertyRelative("width").floatValue = rect.width;
        property.FindPropertyRelative("height").floatValue = rect.height;
    }

    private static void WireTownContent(
        TownPanelContent content,
        Tilemap ground,
        Transform props,
        Transform portal)
    {
        var serialized = new SerializedObject(content);
        serialized.FindProperty("groundTilemap").objectReferenceValue = ground;
        serialized.FindProperty("propsRoot").objectReferenceValue = props;
        serialized.FindProperty("portalRoot").objectReferenceValue = portal;
        serialized.FindProperty("portalOffsetX").floatValue = DesktopOverlaySettings.VillagePortalX;
        serialized.FindProperty("portalOffsetY").floatValue = DesktopOverlaySettings.PortalY;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireDungeonContent(
        DungeonPanelContent content,
        int slotIndex,
        string displayName,
        Tilemap ground,
        Transform props,
        Transform portal,
        Transform battle)
    {
        var serialized = new SerializedObject(content);
        serialized.FindProperty("slotIndex").intValue = slotIndex;
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("groundTilemap").objectReferenceValue = ground;
        serialized.FindProperty("propsRoot").objectReferenceValue = props;
        serialized.FindProperty("portalRoot").objectReferenceValue = portal;
        serialized.FindProperty("battleRoot").objectReferenceValue = battle;
        serialized.FindProperty("portalOffsetX").floatValue = DesktopOverlaySettings.BattlePortalX;
        serialized.FindProperty("portalOffsetY").floatValue = DesktopOverlaySettings.PortalY;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void HideLockedDungeonPanels()
    {
        foreach (var panel in Object.FindObjectsByType<WorkspacePanel>(FindObjectsSortMode.None))
        {
            if (panel.DungeonContent == null)
                continue;

            var unlocked = panel.DungeonContent.SlotIndex == 2;
            panel.gameObject.SetActive(unlocked);
            panel.SetContentVisible(unlocked);
            panel.DungeonContent.SetVisible(unlocked);
        }
    }

    private static void CleanupLegacyHierarchy()
    {
        RemoveIfEmptyRoot("Grid");
        RemoveIfEmptyRoot("Props");
        RemoveIfEmptyRoot("Portals");
        RemoveLegacyObject("DungeonSlots");
        RemoveLegacyObject("LeftColumn");
        RemoveLegacyObject("RightColumn");
        RemoveLegacyObject("ZoneLayout");

        foreach (var legacyName in new[] { "Town", "UiZone", "DungeonSlot_1", "DungeonSlot_2", "DungeonSlot_3" })
        {
            var legacyPanel = GameObject.Find("GameWorkspace")?.transform.Find(legacyName);
            if (legacyPanel != null)
                Object.DestroyImmediate(legacyPanel.gameObject);
        }
    }

    private static void RemoveIfEmptyRoot(string name)
    {
        var root = GameObject.Find(name);
        if (root == null || root.transform.childCount > 0)
            return;

        Object.DestroyImmediate(root);
    }

    private static void RemoveLegacyObject(string name)
    {
        var legacy = GameObject.Find(name);
        if (legacy != null)
            Object.DestroyImmediate(legacy);
    }

    private static GameObject EnsureChildObject(Transform parent, string childName)
    {
        var child = parent.Find(childName);
        if (child != null)
            return child.gameObject;

        var childObject = new GameObject(childName);
        childObject.transform.SetParent(parent, false);
        return childObject;
    }

    private static void EnsureLabel(Transform parent, string label)
    {
        var labelTransform = parent.Find("Label");
        if (labelTransform == null)
        {
            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            labelTransform = labelObject.transform;
        }

        var text = labelTransform.GetComponent<Text>();
        if (text == null)
            text = labelTransform.gameObject.AddComponent<Text>();

        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.fontSize = 28;
        text.raycastTarget = false;

        var rect = labelTransform.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
