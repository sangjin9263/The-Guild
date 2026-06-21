using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class WorkspaceLayoutSetup
{
    private const string MainScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("The Guild/Layout/Setup Workspace In Main")]
    public static void SetupWorkspaceInMain()
    {
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        ApplyWorkspaceLayout();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Workspace layout applied to Main scene.");
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
            var workspace = GameObject.Find("GameWorkspace")?.GetComponent<RectTransform>();
            var layoutRoot = workspace != null ? workspace.Find("LayoutRoot") as RectTransform : null;
            if (layoutRoot != null)
                DesktopOverlaySettings.ApplyLayoutRootTransform(layoutRoot);

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
        var auctionContent = EnsureAuctionContent(panelContentsRoot.transform);
        var dungeon1Content = EnsureDungeonContent(panelContentsRoot.transform, 0, "Dungeon1Content", DesktopOverlaySettings.GetDungeonDisplayName(0));
        var dungeon2Content = EnsureDungeonContent(panelContentsRoot.transform, 1, "Dungeon2Content", DesktopOverlaySettings.GetDungeonDisplayName(1));
        var dungeon3Content = EnsureDungeonContent(panelContentsRoot.transform, 2, "Dungeon3Content", DesktopOverlaySettings.GetDungeonDisplayName(2));
        var dungeon4Content = EnsureDungeonContent(panelContentsRoot.transform, 3, "Dungeon4Content", DesktopOverlaySettings.GetDungeonDisplayName(3));

        MigrateLegacyWorldContent(townContent, dungeon3Content);

        EnsureWorkspaceCanvas(
            townContent,
            uiContent,
            auctionContent,
            dungeon1Content,
            dungeon2Content,
            dungeon3Content,
            dungeon4Content);

        BuildingUISetup.EnsureBuildingPanel();
        WireUiZoneContent(uiContent);
        EnsureAuctionWorkspaceUi(auctionContent);
        HideLockedDungeonPanels();
        CleanupLegacyHierarchy();
        CleanupLegacyAuctionPanel();
        CleanupDuplicateBuildingUiCanvas();
        NormalizeAllDungeonBattleRoots();
    }

    private const int PrimaryCombatSlotIndex = 2;

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

        if (bootstrapObject.GetComponent<GameManager>() == null)
            bootstrapObject.AddComponent<GameManager>();

        if (bootstrapObject.GetComponent<GateAuctionManager>() == null)
            bootstrapObject.AddComponent<GateAuctionManager>();

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
        var villagePortal = PortalVisualSetup.ResolveTownPortal(contentObject.transform);

        WireTownContent(content, ground, props, villagePortal);
        return content;
    }

    public static void EnsureAuctionWorkspaceUi(AuctionZonePanelContent auctionContent = null)
    {
        if (auctionContent == null)
        {
            var panelContentsRoot = EnsureRoot("PanelContents");
            auctionContent = EnsureAuctionContent(panelContentsRoot.transform);
        }

        var workspace = GameObject.Find("GameWorkspace");
        if (workspace == null)
        {
            Debug.LogWarning("GameWorkspace not found. Run The Guild/Layout/Setup Workspace first.");
            return;
        }

        var layoutRoot = workspace.transform.Find("LayoutRoot");
        if (layoutRoot == null)
        {
            Debug.LogWarning("LayoutRoot not found under GameWorkspace.");
            return;
        }

        var auctionZonePanel = layoutRoot.Find("AuctionZonePanel");
        if (auctionZonePanel == null)
        {
            EnsurePanel(
                layoutRoot,
                WorkspacePanelId.Auction,
                "AuctionZonePanel",
                "경매",
                DesktopOverlaySettings.GetDefaultAuctionRect(),
                DesktopOverlaySettings.AuctionPanelHeight,
                new Color(0.85f, 0.65f, 0.15f, 0.35f),
                null,
                null,
                auctionContent);
            auctionZonePanel = layoutRoot.Find("AuctionZonePanel");
        }

        if (auctionZonePanel == null)
        {
            Debug.LogWarning("AuctionZonePanel could not be created.");
            return;
        }

        MigrateLegacyAuctionPanel(auctionZonePanel);

        var panelRoot = FindOrCreateAuctionPanel(auctionZonePanel);
        var closeButton = panelRoot.transform.Find("CloseButton")?.GetComponent<Button>();

        var controller = workspace.GetComponent<AuctionPanelUI>();
        if (controller == null)
            controller = workspace.AddComponent<AuctionPanelUI>();

        var auctionSerialized = new SerializedObject(controller);
        auctionSerialized.FindProperty("panelRoot").objectReferenceValue = panelRoot;
        auctionSerialized.FindProperty("titleText").objectReferenceValue =
            panelRoot.transform.Find("Title")?.GetComponent<Text>();
        auctionSerialized.FindProperty("bodyText").objectReferenceValue =
            panelRoot.transform.Find("Body")?.GetComponent<Text>();
        auctionSerialized.FindProperty("closeButton").objectReferenceValue = closeButton;
        auctionSerialized.ApplyModifiedPropertiesWithoutUndo();

        ConfigureAuctionPanelRaycasts(panelRoot);

        var contentSerialized = new SerializedObject(auctionContent);
        contentSerialized.FindProperty("panelRoot").objectReferenceValue = panelRoot.GetComponent<RectTransform>();
        contentSerialized.ApplyModifiedPropertiesWithoutUndo();

        foreach (var panel in Object.FindObjectsByType<WorkspacePanel>(FindObjectsSortMode.None))
        {
            if (panel.PanelId != WorkspacePanelId.Auction)
                continue;

            var panelSerialized = new SerializedObject(panel);
            panelSerialized.FindProperty("auctionZoneContent").objectReferenceValue = auctionContent;
            panelSerialized.ApplyModifiedPropertiesWithoutUndo();
            panel.gameObject.SetActive(true);
        }

        ApplyAuctionPanelLayout(panelRoot);
        EnsureAuctionPanelLayoutFitInScene(panelRoot);
    }

    private static void EnsureAuctionPanelLayoutFitInScene(GameObject panelRoot)
    {
        AuctionPanelSceneMigration.EnsureAuctionPanelLayoutFit(panelRoot);
    }

    private static void ApplyAuctionPanelLayout(GameObject panelRoot)
    {
        if (panelRoot == null)
            return;

        var panelRect = panelRoot.GetComponent<RectTransform>();
        if (panelRect == null)
            return;

        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = new Vector2(12f, 12f);
        panelRect.offsetMax = new Vector2(-12f, -12f);
        panelRoot.SetActive(false);
    }

    private static UiZonePanelContent EnsureUiContent(Transform parent)
    {
        var contentObject = EnsureChildObject(parent, "UiContent");
        var content = contentObject.GetComponent<UiZonePanelContent>();
        if (content == null)
            content = contentObject.AddComponent<UiZonePanelContent>();
        return content;
    }

    private static AuctionZonePanelContent EnsureAuctionContent(Transform parent)
    {
        var contentObject = EnsureChildObject(parent, "AuctionZoneContent");
        var content = contentObject.GetComponent<AuctionZonePanelContent>();
        if (content == null)
            content = contentObject.AddComponent<AuctionZonePanelContent>();
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
        var portal = PortalVisualSetup.ResolveDungeonPortal(contentObject.transform);
        var battle = NormalizeBattleRoot(contentObject.transform, slotIndex);

        WireDungeonContent(content, slotIndex, displayName, ground, props, portal, battle);
        return content;
    }

    private static Transform NormalizeBattleRoot(Transform contentRoot, int slotIndex)
    {
        Transform canonicalBattle = null;
        var duplicateBattles = new System.Collections.Generic.List<Transform>();

        for (var i = 0; i < contentRoot.childCount; i++)
        {
            var child = contentRoot.GetChild(i);
            if (child.name != "Battle")
                continue;

            if (canonicalBattle == null)
                canonicalBattle = child;
            else
                duplicateBattles.Add(child);
        }

        if (canonicalBattle == null)
            canonicalBattle = EnsureChildObject(contentRoot, "Battle").transform;

        foreach (var duplicate in duplicateBattles)
        {
            while (duplicate.childCount > 0)
                duplicate.GetChild(0).SetParent(canonicalBattle, false);

            Object.DestroyImmediate(duplicate.gameObject);
        }

        ConfigureBattleComponents(canonicalBattle.gameObject, slotIndex == PrimaryCombatSlotIndex);
        return canonicalBattle;
    }

    private static void ConfigureBattleComponents(GameObject battleObject, bool isPrimaryCombatSlot)
    {
        var arena = battleObject.GetComponent<BattleArena>();
        var controller = battleObject.GetComponent<BattleController>();

        if (isPrimaryCombatSlot)
        {
            if (arena == null)
                arena = battleObject.AddComponent<BattleArena>();

            if (controller == null)
                controller = battleObject.AddComponent<BattleController>();

            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("arena").objectReferenceValue = arena;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            return;
        }

        if (arena != null)
            Object.DestroyImmediate(arena);

        if (controller != null)
            Object.DestroyImmediate(controller);
    }

    private static void NormalizeAllDungeonBattleRoots()
    {
        foreach (var content in Object.FindObjectsByType<DungeonPanelContent>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            NormalizeBattleRoot(content.transform, content.SlotIndex);
        }
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
        // 레거시 Battle은 Dungeon3 아래로 옮긴 뒤 NormalizeAllDungeonBattleRoots에서 하나로 합칩니다.

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
            PortalVisualSetup.ResolveTownPortal(townContent.transform));

        WireDungeonContent(
            dungeon3Content,
            2,
            DesktopOverlaySettings.GetDungeonDisplayName(2),
            dungeon3Ground,
            dungeon3Content.transform.Find("Props"),
            PortalVisualSetup.ResolveDungeonPortal(dungeon3Content.transform),
            NormalizeBattleRoot(dungeon3Content.transform, PrimaryCombatSlotIndex));

        PortalVisualSetup.NormalizeAllPanelPortals();
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
        AuctionZonePanelContent auctionContent,
        DungeonPanelContent dungeon1Content,
        DungeonPanelContent dungeon2Content,
        DungeonPanelContent dungeon3Content,
        DungeonPanelContent dungeon4Content)
    {
        var workspaceObject = GameObject.Find("GameWorkspace");
        if (workspaceObject == null)
            workspaceObject = new GameObject("GameWorkspace");

        var canvas = workspaceObject.GetComponent<Canvas>();
        if (canvas == null)
            canvas = workspaceObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        DesktopOverlaySettings.ApplyFullScreenOverlayRoot(workspaceObject.GetComponent<RectTransform>());

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

        var layoutRoot = EnsureLayoutRoot(workspaceObject.transform);

        EnsurePanel(
            layoutRoot,
            WorkspacePanelId.Town,
            "TownPanel",
            "마을",
            DesktopOverlaySettings.GetDefaultTownRect(),
            DesktopOverlaySettings.TownPanelHeight,
            new Color(1f, 0.45f, 0.1f, 0.35f),
            townContent.transform,
            null,
            null);

        EnsurePanel(
            layoutRoot,
            WorkspacePanelId.UiZone,
            "UiZonePanel",
            "UI",
            DesktopOverlaySettings.GetDefaultUiZoneRect(),
            DesktopOverlaySettings.UiZonePanelHeight,
            new Color(0.65f, 0.65f, 0.65f, 0.25f),
            null,
            uiContent,
            null);

        EnsurePanel(
            layoutRoot,
            WorkspacePanelId.Auction,
            "AuctionZonePanel",
            "경매",
            DesktopOverlaySettings.GetDefaultAuctionRect(),
            DesktopOverlaySettings.AuctionPanelHeight,
            new Color(0.85f, 0.65f, 0.15f, 0.35f),
            null,
            null,
            auctionContent);

        EnsurePanel(
            layoutRoot,
            WorkspacePanelId.DungeonSlot1,
            "DungeonPanel_1",
            DesktopOverlaySettings.GetDungeonDisplayLabel(0),
            DesktopOverlaySettings.GetDefaultDungeonRectForSlot(0),
            DesktopOverlaySettings.DungeonSlotHeight,
            new Color(0.35f, 0.55f, 0.95f, 0.35f),
            dungeon1Content.transform,
            null,
            null);

        EnsurePanel(
            layoutRoot,
            WorkspacePanelId.DungeonSlot2,
            "DungeonPanel_2",
            DesktopOverlaySettings.GetDungeonDisplayLabel(1),
            DesktopOverlaySettings.GetDefaultDungeonRectForSlot(1),
            DesktopOverlaySettings.DungeonSlotHeight,
            new Color(0.35f, 0.55f, 0.95f, 0.35f),
            dungeon2Content.transform,
            null,
            null);

        EnsurePanel(
            layoutRoot,
            WorkspacePanelId.DungeonSlot3,
            "DungeonPanel_3",
            DesktopOverlaySettings.GetDungeonDisplayLabel(2),
            DesktopOverlaySettings.GetDefaultDungeonRectForSlot(2),
            DesktopOverlaySettings.DungeonSlotHeight,
            new Color(0.35f, 0.55f, 0.95f, 0.35f),
            dungeon3Content.transform,
            null,
            null);

        EnsurePanel(
            layoutRoot,
            WorkspacePanelId.DungeonSlot4,
            "DungeonPanel_4",
            DesktopOverlaySettings.GetDungeonDisplayLabel(3),
            DesktopOverlaySettings.GetDefaultDungeonRectForSlot(3),
            DesktopOverlaySettings.DungeonSlotHeight,
            new Color(0.35f, 0.55f, 0.95f, 0.35f),
            dungeon4Content.transform,
            null,
            null);
    }

    private static Transform EnsureLayoutRoot(Transform workspaceTransform)
    {
        var layoutTransform = workspaceTransform.Find("LayoutRoot");
        if (layoutTransform == null)
        {
            var layoutObject = new GameObject("LayoutRoot", typeof(RectTransform));
            layoutObject.transform.SetParent(workspaceTransform, false);
            layoutTransform = layoutObject.transform;
        }

        var layoutRoot = layoutTransform.GetComponent<RectTransform>();
        DesktopOverlaySettings.ApplyLayoutRootTransform(layoutRoot);
        return layoutRoot;
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
        UiZonePanelContent uiZoneContent,
        AuctionZonePanelContent auctionZoneContent)
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
        serialized.FindProperty("auctionZoneContent").objectReferenceValue = auctionZoneContent;
        serialized.FindProperty("worldScaleReferenceHeight").floatValue = scaleReferenceHeight;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        panel.Configure(panelId, defaultRect, label, tint, scaleReferenceHeight);
        panel.ApplyReferenceRect(defaultRect);

        EnsureLabel(panelObject.transform, label);
    }

    private static void WireUiZoneContent(UiZonePanelContent uiContent)
    {
        BuildingUISetup.EnsureBuildingPanel();

        var buildingUi = Object.FindFirstObjectByType<BuildingPanelUI>();
        if (buildingUi == null || uiContent == null)
            return;

        var buildingSerialized = new SerializedObject(buildingUi);
        var panelRootObject = buildingSerialized.FindProperty("panelRoot").objectReferenceValue as GameObject;

        var serialized = new SerializedObject(uiContent);
        serialized.FindProperty("panelRoot").objectReferenceValue =
            panelRootObject != null ? panelRootObject.GetComponent<RectTransform>() : null;
        serialized.ApplyModifiedPropertiesWithoutUndo();

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
        serialized.FindProperty("portalOffsetX").floatValue = TownPanelLayout.PortalLocalX;
        serialized.FindProperty("portalOffsetY").floatValue = TownPanelLayout.PortalLocalY;
        serialized.FindProperty("overrideWorldY").boolValue = true;
        serialized.FindProperty("worldYOverride").floatValue = DesktopOverlaySettings.DefaultTownContentWorldY;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        portal.localPosition = new Vector3(
            TownPanelLayout.PortalLocalX,
            TownPanelLayout.PortalLocalY,
            0f);
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
        serialized.FindProperty("portalOffsetX").floatValue =
            slotIndex == 2
                ? DesktopOverlaySettings.PrimaryDungeonPortalLocalX
                : DesktopOverlaySettings.BattlePortalX;
        serialized.FindProperty("portalOffsetY").floatValue =
            slotIndex == 2
                ? DesktopOverlaySettings.PrimaryDungeonPortalLocalY
                : DesktopOverlaySettings.PortalY;
        if (slotIndex == 2)
        {
            serialized.FindProperty("overrideWorldY").boolValue = true;
            serialized.FindProperty("worldYOverride").floatValue =
                DesktopOverlaySettings.DefaultDungeon3ContentWorldY;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();

        portal.localPosition = new Vector3(
            slotIndex == 2
                ? DesktopOverlaySettings.PrimaryDungeonPortalLocalX
                : DesktopOverlaySettings.BattlePortalX,
            slotIndex == 2
                ? DesktopOverlaySettings.PrimaryDungeonPortalLocalY
                : DesktopOverlaySettings.PortalY,
            0f);
    }

    private static void HideLockedDungeonPanels()
    {
        var unlockedSlotCount = ResolveUnlockedSlotCountForEditor();

        foreach (var panel in Object.FindObjectsByType<WorkspacePanel>(FindObjectsSortMode.None))
        {
            if (panel.DungeonContent == null)
                continue;

            var slotIndex = panel.DungeonContent.SlotIndex;
            var unlocked = GameManager.IsSlotUnlockedForCount(slotIndex, unlockedSlotCount);
            panel.gameObject.SetActive(unlocked);
            panel.SetContentVisible(unlocked);
        }
    }

    private static int ResolveUnlockedSlotCountForEditor()
    {
        var gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager == null)
            return 1;

        var serialized = new SerializedObject(gameManager);
        return serialized.FindProperty("unlockedSlotCount").intValue;
    }

    private static void CleanupDuplicateBuildingUiCanvas()
    {
        var controllers = Object.FindObjectsByType<BuildingPanelUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (controllers.Length <= 1)
            return;

        BuildingPanelUI keep = null;
        foreach (var controller in controllers)
        {
            if (controller.gameObject.activeInHierarchy)
            {
                keep = controller;
                break;
            }
        }

        keep ??= controllers[0];

        foreach (var controller in controllers)
        {
            if (controller == keep)
                continue;

            Object.DestroyImmediate(controller.gameObject);
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

        foreach (var legacyName in new[] { "Town", "UiZone", "Auction", "DungeonSlot_1", "DungeonSlot_2", "DungeonSlot_3", "DungeonSlot_4" })
        {
            var legacyPanel = GameObject.Find("GameWorkspace")?.transform.Find(legacyName);
            if (legacyPanel != null)
                Object.DestroyImmediate(legacyPanel.gameObject);
        }

        CleanupDuplicateWorkspacePanels();
    }

    private static void CleanupDuplicateWorkspacePanels()
    {
        var workspace = GameObject.Find("GameWorkspace")?.transform;
        var layoutRoot = workspace != null ? workspace.Find("LayoutRoot") : null;
        if (workspace == null || layoutRoot == null)
            return;

        WorkspaceLayoutController.RemoveDuplicateWorkspacePanels(workspace, layoutRoot);
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
        text.raycastTarget = true;

        if (labelTransform.GetComponent<WorkspacePanelDragHandle>() == null)
            labelTransform.gameObject.AddComponent<WorkspacePanelDragHandle>();

        var rect = labelTransform.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ConfigureAuctionPanelRaycasts(GameObject panelRoot)
    {
        if (panelRoot == null)
            return;

        var panelImage = panelRoot.GetComponent<Image>();
        if (panelImage != null)
            panelImage.raycastTarget = false;

        foreach (var text in panelRoot.GetComponentsInChildren<Text>(true))
            text.raycastTarget = false;
    }

    private static void MigrateLegacyAuctionPanel(Transform auctionZonePanel)
    {
        var workspace = GameObject.Find("GameWorkspace")?.transform;
        if (workspace == null)
            return;

        var legacyPanel = workspace.Find("AuctionPanel");
        if (legacyPanel == null || legacyPanel.parent == auctionZonePanel)
            return;

        legacyPanel.SetParent(auctionZonePanel, false);
        legacyPanel.SetAsLastSibling();
    }

    private static void CleanupLegacyAuctionPanel()
    {
        var workspace = GameObject.Find("GameWorkspace")?.transform;
        if (workspace == null)
            return;

        var legacyPanel = workspace.Find("AuctionPanel");
        if (legacyPanel != null && legacyPanel.parent == workspace)
            Object.DestroyImmediate(legacyPanel.gameObject);
    }

    private static GameObject FindOrCreateAuctionPanel(Transform auctionZonePanel)
    {
        var existing = auctionZonePanel.Find("AuctionPanel");
        if (existing != null)
            return existing.gameObject;

        return CreateAuctionPanel(auctionZonePanel);
    }

    private static GameObject CreateAuctionPanel(Transform parent)
    {
        const string prefabPath = AuctionPanelSceneMigration.AuctionPanelShellPrefabPath;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"WorkspaceLayoutSetup: AuctionPanel prefab not found at {prefabPath}");
            return CreateLegacyAuctionPanel(parent);
        }

        var panel = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        panel.name = "AuctionPanel";
        panel.transform.SetAsLastSibling();

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        panel.SetActive(false);
        return panel;
    }

    private static GameObject CreateLegacyAuctionPanel(Transform parent)
    {
        var panel = new GameObject("AuctionPanel", typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        panel.transform.SetAsLastSibling();

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.08f, 0.12f, 0.96f);
        panelImage.raycastTarget = false;

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = new Vector2(12f, 12f);
        panelRect.offsetMax = new Vector2(-12f, -12f);

        CreateAuctionText("Title", panel.transform, 32, new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.97f),
            "게이트 경매장", FontStyle.Bold);
        CreateAuctionText("Body", panel.transform, 20, new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.86f),
            "경매 UI 준비 중.", FontStyle.Normal);

        var closeObject = new GameObject("CloseButton", typeof(RectTransform));
        closeObject.transform.SetParent(panel.transform, false);
        var closeRect = closeObject.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.78f, 0.02f);
        closeRect.anchorMax = new Vector2(0.96f, 0.08f);
        closeRect.offsetMin = Vector2.zero;
        closeRect.offsetMax = Vector2.zero;

        var closeImage = closeObject.AddComponent<Image>();
        closeImage.color = new Color(0.25f, 0.32f, 0.42f, 1f);
        var closeButton = closeObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;

        var closeLabel = CreateAuctionText("Label", closeObject.transform, 18, Vector2.zero, Vector2.one, "닫기",
            FontStyle.Normal);
        closeLabel.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        StretchAuctionRect(closeLabel.GetComponent<RectTransform>());

        panel.SetActive(false);
        return panel;
    }

    private static GameObject CreateAuctionText(
        string name,
        Transform parent,
        int fontSize,
        Vector2 anchorMin,
        Vector2 anchorMax,
        string text,
        FontStyle style)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var label = obj.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = Color.white;
        label.text = text;
        label.alignment = TextAnchor.UpperLeft;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        return obj;
    }

    private static void StretchAuctionRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
