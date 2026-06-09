using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class BuildingUISetup
{
    private const string MainScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("The Guild/UI/Setup Building Panel")]
    public static void SetupBuildingPanelInActiveScene()
    {
        EnsureBuildingPanel();
        ApplyBuildingPanelLayout();
        WireBuildingClickables();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Building panel UI setup complete.");
    }

    [MenuItem("The Guild/UI/Apply Building Panel Layout")]
    public static void ApplyBuildingPanelLayoutInActiveScene()
    {
        ApplyBuildingPanelLayout();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Building panel layout applied.");
    }

    [MenuItem("The Guild/UI/Setup Building Panel In Main")]
    public static void SetupBuildingPanelInMain()
    {
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        SetupBuildingPanelInActiveScene();
        EditorSceneManager.SaveScene(scene);
    }

    public static void EnsureBuildingPanel()
    {
        if (Object.FindFirstObjectByType<BuildingPanelUI>() != null)
            return;

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        var canvasObject = new GameObject("BuildingUICanvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        ConfigureCanvasScaler(canvasObject.GetComponent<CanvasScaler>());

        var panelRoot = CreatePanel(canvasObject.transform);
        var closeButton = panelRoot.transform.Find("CloseButton")?.GetComponent<Button>();

        var controller = canvasObject.AddComponent<BuildingPanelUI>();

        var serialized = new SerializedObject(controller);
        serialized.FindProperty("panelRoot").objectReferenceValue = panelRoot;
        serialized.FindProperty("titleText").objectReferenceValue =
            panelRoot.transform.Find("Title")?.GetComponent<Text>();
        serialized.FindProperty("bodyText").objectReferenceValue =
            panelRoot.transform.Find("Body")?.GetComponent<Text>();
        serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void ApplyBuildingPanelLayout()
    {
        var controller = Object.FindFirstObjectByType<BuildingPanelUI>();
        if (controller == null)
        {
            Debug.LogWarning("BuildingPanelUI not found in scene.");
            return;
        }

        ConfigureCanvasScaler(controller.GetComponent<CanvasScaler>());

        var serialized = new SerializedObject(controller);
        var panelRoot = serialized.FindProperty("panelRoot").objectReferenceValue as GameObject;
        if (panelRoot == null)
            panelRoot = controller.transform.Find("BuildingPanel")?.gameObject;

        if (panelRoot == null)
        {
            Debug.LogWarning("BuildingPanel not found.");
            return;
        }

        ApplyPanelRect(panelRoot.GetComponent<RectTransform>());
    }

    private static void ConfigureCanvasScaler(CanvasScaler scaler)
    {
        if (scaler == null)
            return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = DesktopOverlaySettings.WindowSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        // Match window height so UI fills the full overlay (game + UI headroom).
        scaler.matchWidthOrHeight = 1f;
    }

    private static void ApplyPanelRect(RectTransform panelRect)
    {
        if (panelRect == null)
            return;

        var uiZone = DesktopOverlaySettings.GetDefaultUiZoneRect();
        var refWidth = DesktopOverlaySettings.ReferenceWidth;
        var refHeight = DesktopOverlaySettings.ReferenceHeight;
        panelRect.anchorMin = new Vector2(uiZone.x / refWidth, uiZone.y / refHeight);
        panelRect.anchorMax = new Vector2(
            (uiZone.x + uiZone.width) / refWidth,
            (uiZone.y + uiZone.height) / refHeight);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
    }

    public static void WireBuildingClickables()
    {
        var buildingRoots = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var transform in buildingRoots)
        {
            if (!transform.name.StartsWith("Building"))
                continue;

            if (transform.GetComponent<BuildingClickable>() != null)
                continue;

            EnsureBuildingCollider(transform.gameObject);
            transform.gameObject.AddComponent<BuildingClickable>();
        }
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
        var uiZone = DesktopOverlaySettings.GetDefaultUiZoneRect();
        var refWidth = DesktopOverlaySettings.ReferenceWidth;
        var refHeight = DesktopOverlaySettings.ReferenceHeight;
        panelRect.anchorMin = new Vector2(uiZone.x / refWidth, uiZone.y / refHeight);
        panelRect.anchorMax = new Vector2(
            (uiZone.x + uiZone.width) / refWidth,
            (uiZone.y + uiZone.height) / refHeight);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

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
