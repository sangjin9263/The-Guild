using UnityEngine;
using UnityEngine.UI;

public class BuildingPanelUI : MonoBehaviour
{
    public static BuildingPanelUI Instance { get; private set; }

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Text titleText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        Instance = this;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        ApplyToUiZone();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void ApplyToUiZone()
    {
        if (Instance == null)
            return;

        Instance.ApplyPanelToUiZone();
    }

    public void Toggle(string title, string description)
    {
        if (panelRoot != null && panelRoot.activeSelf)
        {
            Hide();
            return;
        }

        Show(title, description);
    }

    public void Show(string title, string description)
    {
        if (panelRoot == null)
            return;

        ApplyPanelToUiZone();

        if (titleText != null)
            titleText.text = title;

        if (bodyText != null)
            bodyText.text = description;

        panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void ApplyPanelToUiZone()
    {
        var uiZonePanel = WorkspaceLayoutController.Instance != null
            ? WorkspaceLayoutController.Instance.GetPanel(WorkspacePanelId.UiZone)
            : null;

        if (uiZonePanel != null)
        {
            uiZonePanel.ApplyUiZoneLayout();
            return;
        }

        if (panelRoot == null)
            return;

        var panelRect = panelRoot.GetComponent<RectTransform>();
        if (panelRect == null)
            return;

        var uiZoneRect = DesktopOverlaySettings.GetDefaultUiZoneRect();
        var refWidth = DesktopOverlaySettings.ReferenceWidth;
        var refHeight = DesktopOverlaySettings.ReferenceHeight;
        panelRect.anchorMin = new Vector2(uiZoneRect.x / refWidth, uiZoneRect.y / refHeight);
        panelRect.anchorMax = new Vector2(
            (uiZoneRect.x + uiZoneRect.width) / refWidth,
            (uiZoneRect.y + uiZoneRect.height) / refHeight);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.offsetMin = new Vector2(12f, 12f);
        panelRect.offsetMax = new Vector2(-12f, -12f);

        var canvasScaler = GetComponent<CanvasScaler>();
        if (canvasScaler != null)
        {
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = DesktopOverlaySettings.WindowSize;
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 1f;
        }
    }
}
