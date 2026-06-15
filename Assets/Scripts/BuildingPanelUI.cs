using UnityEngine;
using UnityEngine.UI;

public class BuildingPanelUI : MonoBehaviour
{
    public static BuildingPanelUI Instance { get; private set; }

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Text titleText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Vector2 panelPadding = new(12f, 12f);

    public Vector2 PanelPadding => panelPadding;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

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

        ApplyPanelRectFallback();
    }

    private void ApplyPanelRectFallback()
    {
        if (panelRoot == null)
            return;

        var panelRect = panelRoot.GetComponent<RectTransform>();
        if (panelRect == null)
            return;

        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelPadding;
        panelRect.offsetMax = -panelPadding;
    }
}
