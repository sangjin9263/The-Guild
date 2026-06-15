using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class BuildingUISetup
{
    public static void EnsureBuildingPanel()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        var workspace = GameObject.Find("GameWorkspace");
        if (workspace == null)
        {
            Debug.LogWarning("GameWorkspace not found. Run The Guild/Layout/Setup Workspace Layout first.");
            return;
        }

        var uiZonePanel = FindUiZonePanelTransform(workspace.transform);
        if (uiZonePanel == null)
        {
            Debug.LogWarning("UiZonePanel not found under GameWorkspace/LayoutRoot.");
            return;
        }

        CleanupDuplicateBuildingUiCanvas();

        var panelRoot = FindOrCreateBuildingPanel(uiZonePanel);
        var closeButton = panelRoot.transform.Find("CloseButton")?.GetComponent<Button>();

        var controller = workspace.GetComponent<BuildingPanelUI>();
        if (controller == null)
            controller = workspace.AddComponent<BuildingPanelUI>();

        var buildingSerialized = new SerializedObject(controller);
        buildingSerialized.FindProperty("panelRoot").objectReferenceValue = panelRoot;
        buildingSerialized.FindProperty("titleText").objectReferenceValue =
            panelRoot.transform.Find("Title")?.GetComponent<Text>();
        buildingSerialized.FindProperty("bodyText").objectReferenceValue =
            panelRoot.transform.Find("Body")?.GetComponent<Text>();
        buildingSerialized.FindProperty("closeButton").objectReferenceValue = closeButton;
        buildingSerialized.ApplyModifiedPropertiesWithoutUndo();

        WireUiZonePanelContent(uiZonePanel, panelRoot);
        DestroyLegacyBuildingUiCanvas();
    }

    public static void ApplyBuildingPanelLayout()
    {
        var controller = Object.FindFirstObjectByType<BuildingPanelUI>();
        if (controller == null)
        {
            Debug.LogWarning("BuildingPanelUI not found in scene.");
            return;
        }

        BuildingPanelUI.ApplyToUiZone();
    }

    private static Transform FindUiZonePanelTransform(Transform workspace)
    {
        var layoutRoot = workspace.Find("LayoutRoot");
        if (layoutRoot == null)
            return null;

        return layoutRoot.Find("UiZonePanel");
    }

    private static GameObject FindOrCreateBuildingPanel(Transform uiZonePanel)
    {
        var existing = uiZonePanel.Find("BuildingPanel");
        if (existing != null)
            return existing.gameObject;

        var legacyCanvas = GameObject.Find("BuildingUICanvas");
        if (legacyCanvas != null)
        {
            var legacyPanel = legacyCanvas.transform.Find("BuildingPanel");
            if (legacyPanel != null)
            {
                legacyPanel.SetParent(uiZonePanel, false);
                return legacyPanel.gameObject;
            }
        }

        return CreatePanel(uiZonePanel);
    }

    private static void WireUiZonePanelContent(Transform uiZonePanel, GameObject panelRoot)
    {
        var workspacePanel = uiZonePanel.GetComponent<WorkspacePanel>();
        if (workspacePanel == null)
            return;

        var uiContent = workspacePanel.UiZoneContent;
        if (uiContent == null)
            return;

        var serialized = new SerializedObject(uiContent);
        serialized.FindProperty("panelRoot").objectReferenceValue =
            panelRoot != null ? panelRoot.GetComponent<RectTransform>() : null;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        var panelSerialized = new SerializedObject(workspacePanel);
        panelSerialized.FindProperty("uiZoneContent").objectReferenceValue = uiContent;
        panelSerialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void DestroyLegacyBuildingUiCanvas()
    {
        var legacyCanvas = GameObject.Find("BuildingUICanvas");
        if (legacyCanvas == null)
            return;

        if (legacyCanvas.transform.childCount > 0)
            return;

        Object.DestroyImmediate(legacyCanvas);
    }

    private static void CleanupDuplicateBuildingUiCanvas()
    {
        var controllers = Object.FindObjectsByType<BuildingPanelUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        BuildingPanelUI keep = null;
        var workspace = GameObject.Find("GameWorkspace");
        if (workspace != null)
            keep = workspace.GetComponent<BuildingPanelUI>();

        if (keep == null)
        {
            foreach (var controller in controllers)
            {
                if (controller.gameObject.activeInHierarchy)
                {
                    keep = controller;
                    break;
                }
            }
        }

        keep ??= controllers.Length > 0 ? controllers[0] : null;
        if (keep == null)
            return;

        foreach (var controller in controllers)
        {
            if (controller == keep)
                continue;

            var panel = controller.transform.Find("BuildingPanel");
            if (panel != null && workspace != null)
            {
                var uiZone = FindUiZonePanelTransform(workspace.transform);
                if (uiZone != null && panel.parent != uiZone)
                    panel.SetParent(uiZone, false);
            }

            if (controller.gameObject.name == "BuildingUICanvas")
                Object.DestroyImmediate(controller.gameObject);
            else
                Object.DestroyImmediate(controller);
        }
    }

    public static void RemoveMisplacedBuildingClickables()
    {
        foreach (var controller in Object.FindObjectsByType<BuildingPanelUI>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            var host = controller.gameObject;
            var misplacedClickable = host.GetComponent<BuildingClickable>();
            if (misplacedClickable != null)
                Object.DestroyImmediate(misplacedClickable);

            var misplacedCollider = host.GetComponent<BoxCollider2D>();
            if (misplacedCollider != null)
                Object.DestroyImmediate(misplacedCollider);
        }
    }

    public static void WireTownPropBuildings()
    {
        var town = Object.FindFirstObjectByType<TownPanelContent>(FindObjectsInactive.Include);
        if (town == null)
            return;

        var props = town.transform.Find("Props");
        if (props == null)
            return;

        for (var i = 0; i < props.childCount; i++)
        {
            var child = props.GetChild(i);
            if (child.GetComponent<SpriteRenderer>() == null)
                continue;

            if (child.GetComponent<AuctionBuildingClickable>() != null)
                continue;

            if (child.name.StartsWith("Auction"))
                continue;

            WireBuildingObject(
                child.gameObject,
                "헌터 길드",
                "HUNTER COMPANY\n용병 관리 · 파견. (준비 중)");
        }
    }

    public static void WireBuildingClickables()
    {
        var buildingRoots = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var transform in buildingRoots)
        {
            if (!IsBuildingObjectName(transform.name))
                continue;

            WireBuildingObject(
                transform.gameObject,
                "Building 1",
                "Guild building. Upgrade and manage mercenaries here.");
        }
    }

    private static bool IsBuildingObjectName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return false;

        if (objectName.StartsWith("Building"))
            return true;

        return objectName.Contains("건물") || objectName.Contains("BuildingLV");
    }

    private static void WireBuildingObject(GameObject buildingObject, string title, string description)
    {
        EnsureBuildingCollider(buildingObject);

        var clickable = buildingObject.GetComponent<BuildingClickable>();
        if (clickable == null)
            clickable = buildingObject.AddComponent<BuildingClickable>();

        var serialized = new SerializedObject(clickable);
        serialized.FindProperty("buildingTitle").stringValue = title;
        serialized.FindProperty("buildingDescription").stringValue = description;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureBuildingCollider(GameObject buildingObject)
    {
        if (buildingObject.GetComponent<Collider2D>() != null)
            return;

        var spriteRenderer = buildingObject.GetComponent<SpriteRenderer>();
        var collider = buildingObject.AddComponent<BoxCollider2D>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        var bounds = spriteRenderer.sprite.bounds;
        collider.size = bounds.size;
        collider.offset = bounds.center;
    }

    private static GameObject CreatePanel(Transform parent)
    {
        var panel = CreateUiObject("BuildingPanel", parent);
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = new Vector2(12f, 12f);
        panelRect.offsetMax = new Vector2(-12f, -12f);

        var title = CreateText("Title", panel.transform, 28, TextAnchor.UpperLeft);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.06f, 0.78f);
        titleRect.anchorMax = new Vector2(0.94f, 0.94f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        title.GetComponent<Text>().text = "Building";
        title.GetComponent<Text>().fontStyle = FontStyle.Bold;

        var body = CreateText("Body", panel.transform, 18, TextAnchor.UpperLeft);
        var bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.06f, 0.18f);
        bodyRect.anchorMax = new Vector2(0.94f, 0.76f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;
        body.GetComponent<Text>().text = "Select a building.";
        body.GetComponent<Text>().alignment = TextAnchor.UpperLeft;

        CreateCloseButton(panel.transform);

        panel.SetActive(false);
        return panel;
    }

    private static Button CreateCloseButton(Transform parent)
    {
        var buttonObject = CreateUiObject("CloseButton", parent);
        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.72f, 0.04f);
        buttonRect.anchorMax = new Vector2(0.94f, 0.14f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.25f, 0.32f, 0.42f, 1f);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        var label = CreateText("Label", buttonObject.transform, 16, TextAnchor.MiddleCenter);
        Stretch(label.GetComponent<RectTransform>());
        label.GetComponent<Text>().text = "Close";
        label.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

        return button;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static GameObject CreateText(string name, Transform parent, int fontSize, TextAnchor anchor)
    {
        var obj = CreateUiObject(name, parent);
        var text = obj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return obj;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
