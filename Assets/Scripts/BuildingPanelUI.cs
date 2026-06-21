using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>헌터 길드 UI — UiZone BuildingPanel prefab 표시.</summary>
public class BuildingPanelUI : MonoBehaviour
{
    public static BuildingPanelUI Instance { get; private set; }

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI guildNameText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Vector2 panelPadding = Vector2.zero;

    private GuildPanelController _guildController;
    private BuildingPanelLayoutFit _layoutFit;

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

        ResolveGuildController();

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);

        ConfigurePanelRaycasts();
        ResolveLayoutFit();
        ApplyToUiZone();
    }

    private void OnEnable()
    {
        GameManager.StateChanged += HandleGameStateChanged;
        WorkspaceLayoutController.LayoutApplied += HandleLayoutApplied;
    }

    private void OnDisable()
    {
        GameManager.StateChanged -= HandleGameStateChanged;
        WorkspaceLayoutController.LayoutApplied -= HandleLayoutApplied;
    }

    private void OnDestroy()
    {
        GameManager.StateChanged -= HandleGameStateChanged;
        WorkspaceLayoutController.LayoutApplied -= HandleLayoutApplied;

        if (Instance == this)
            Instance = null;
    }

    private void HandleLayoutApplied()
    {
        ApplyPanelLayoutFit();
    }

    private void HandleGameStateChanged()
    {
        if (panelRoot != null && panelRoot.activeSelf)
            RefreshHeader();
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

    public void Show(string title = null, string description = null)
    {
        if (panelRoot == null)
            return;

        BringUiZoneToFront();
        ApplyPanelToUiZone();
        ApplyPanelLayoutFit();
        RefreshHeader(title);
        panelRoot.SetActive(true);
        GameUIFont.ApplyToHierarchy(panelRoot);
        ResolveGuildController();
        _guildController?.PrepareForOpen();
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public bool TryHide()
    {
        Hide();
        return true;
    }

    private void OnCloseClicked() => TryHide();

    private void RefreshHeader(string titleOverride = null)
    {
        if (guildNameText == null)
            return;

        var game = GameManager.Instance;
        var guildName = game != null ? game.GuildName : null;
        guildNameText.text = !string.IsNullOrWhiteSpace(titleOverride)
            ? titleOverride
            : string.IsNullOrWhiteSpace(guildName) ? "헌터 길드" : guildName;
    }

    private void ResolveGuildController()
    {
        if (_guildController != null)
            return;

        if (panelRoot == null)
            return;

        _guildController = panelRoot.GetComponent<GuildPanelController>();
        if (_guildController == null)
            _guildController = panelRoot.AddComponent<GuildPanelController>();
    }

    private void ResolveLayoutFit()
    {
        if (_layoutFit != null)
            return;

        if (panelRoot == null)
            return;

        _layoutFit = panelRoot.GetComponent<BuildingPanelLayoutFit>();
        if (_layoutFit == null)
            _layoutFit = panelRoot.AddComponent<BuildingPanelLayoutFit>();
    }

    private void ApplyPanelLayoutFit()
    {
        ResolveLayoutFit();
        _layoutFit?.EnsureDesignRoot();
        _layoutFit?.ApplyFit();
    }

    private void ApplyPanelToUiZone()
    {
        var uiZonePanel = WorkspaceLayoutController.Instance != null
            ? WorkspaceLayoutController.Instance.GetPanel(WorkspacePanelId.UiZone)
            : null;

        if (uiZonePanel != null)
        {
            uiZonePanel.ApplyUiZoneLayout();
            ApplyPanelLayoutFit();
            return;
        }

        ApplyPanelRectFallback();
        ApplyPanelLayoutFit();
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

    private static void BringUiZoneToFront()
    {
        if (WorkspaceLayoutController.Instance == null)
            return;

        var uiZone = WorkspaceLayoutController.Instance.GetPanel(WorkspacePanelId.UiZone);
        if (uiZone != null)
            uiZone.transform.SetAsLastSibling();
    }

    private void ConfigurePanelRaycasts()
    {
        if (panelRoot == null)
            return;

        var panelImage = panelRoot.GetComponent<Image>();
        if (panelImage != null)
            panelImage.raycastTarget = false;

        foreach (var text in panelRoot.GetComponentsInChildren<Text>(true))
            text.raycastTarget = false;

        foreach (var text in panelRoot.GetComponentsInChildren<TextMeshProUGUI>(true))
            text.raycastTarget = false;
    }
}
