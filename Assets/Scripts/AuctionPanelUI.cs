using UnityEngine;
using UnityEngine.UI;

/// <summary>경매 UI. BuildingPanelUI와 동일하게 WorkspacePanel 프레임 안에 표시.</summary>
public class AuctionPanelUI : MonoBehaviour
{
    public static AuctionPanelUI Instance { get; private set; }

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Text titleText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Vector2 panelPadding = Vector2.zero;

    private AuctionPanelController _panelController;
    private AuctionPanelLayoutFit _layoutFit;
    private string _defaultBodyText = "8건 진열 · Ebay / English 경매";

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

        ResolveCloseButton();
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseClicked);
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        _panelController = GetComponent<AuctionPanelController>();
        var controllerHost = panelRoot != null ? panelRoot : gameObject;
        if (_panelController == null)
            _panelController = controllerHost.GetComponent<AuctionPanelController>();
        if (_panelController == null)
            _panelController = controllerHost.AddComponent<AuctionPanelController>();

        GateAuctionManager.AuctionsChanged += OnAuctionsChanged;
        GateAuctionManager.EnglishSessionChanged += OnEnglishSessionChanged;
        ConfigurePanelRaycasts();
        ResolveLayoutFit();
        ApplyToAuctionZone();
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
        ApplyPanelLayoutFit();
    }

    private void OnDestroy()
    {
        GateAuctionManager.AuctionsChanged -= OnAuctionsChanged;
        GateAuctionManager.EnglishSessionChanged -= OnEnglishSessionChanged;
        WorkspaceLayoutController.LayoutApplied -= HandleLayoutApplied;

        if (Instance == this)
            Instance = null;
    }

    private void OnAuctionsChanged() => RefreshCloseState();

    private void OnEnglishSessionChanged(int _) => RefreshCloseState();

    private void RefreshCloseState()
    {
        if (panelRoot == null || !panelRoot.activeSelf || bodyText == null)
            return;

        if (GateAuctionManager.Instance != null &&
            GateAuctionManager.Instance.CanCloseAuctionPanel(out _))
            bodyText.text = _defaultBodyText;
    }

    public static void ApplyToAuctionZone()
    {
        if (Instance == null)
            return;

        Instance.ApplyPanelToAuctionZone();
    }

    public void Toggle()
    {
        if (panelRoot != null && panelRoot.activeSelf)
        {
            TryHide();
            return;
        }

        Show();
    }

    public void Show()
    {
        if (panelRoot == null)
            return;

        BringAuctionFrameToFront();
        ApplyPanelToAuctionZone();
        ApplyPanelLayoutFit();
        ResolveCloseButton();

        if (titleText != null)
            titleText.text = "게이트 경매장";

        if (bodyText != null)
            bodyText.text = _defaultBodyText;

        panelRoot.SetActive(true);

        GameUIFont.ApplyToHierarchy(panelRoot);

        if (_panelController == null)
        {
            var host = panelRoot != null ? panelRoot : gameObject;
            _panelController = host.GetComponent<AuctionPanelController>();
        }

        _panelController?.PrepareForPanelOpen();

        GateAuctionManager.Instance?.RefreshLotsOnPanelOpen();
        _panelController?.RefreshFromManager();
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public bool TryHide()
    {
        var manager = GateAuctionManager.Instance;
        if (manager != null)
        {
            if (manager.HasActiveEnglishSession)
                manager.CancelActiveEnglishSessionForPanelClose();

            _panelController?.DismissEnglishModal();

            if (!manager.CanCloseAuctionPanel(out var reason))
            {
                if (bodyText != null)
                    bodyText.text = reason;
                return false;
            }
        }

        Hide();
        return true;
    }

    public bool CanClose()
    {
        if (GateAuctionManager.Instance == null)
            return true;

        if (GateAuctionManager.Instance.HasActiveEnglishSession)
            return true;

        if (GateAuctionManager.Instance.CanCloseAuctionPanel(out var reason))
            return true;

        if (bodyText != null)
            bodyText.text = reason;

        return false;
    }

    private void OnCloseClicked() => TryHide();

    private void ResolveCloseButton()
    {
        if (panelRoot == null)
            return;

        var found = AuctionPanelLayoutFit
            .FindPanelTransform(panelRoot.transform, "TitleBar/ButtonX")
            ?.GetComponent<Button>();

        if (found != null)
            closeButton = found;
    }

    private void ApplyPanelToAuctionZone()
    {
        var auctionZone = WorkspaceLayoutController.Instance != null
            ? WorkspaceLayoutController.Instance.GetPanel(WorkspacePanelId.Auction)
            : null;

        if (auctionZone != null)
        {
            auctionZone.ApplyAuctionZoneLayout();
            ApplyPanelLayoutFit();
            return;
        }

        ApplyPanelRectFallback();
        ApplyPanelLayoutFit();
    }

    private void ResolveLayoutFit()
    {
        if (_layoutFit != null)
            return;

        if (panelRoot == null)
            return;

        _layoutFit = panelRoot.GetComponent<AuctionPanelLayoutFit>();
        if (_layoutFit == null)
            _layoutFit = panelRoot.AddComponent<AuctionPanelLayoutFit>();
    }

    private void ApplyPanelLayoutFit()
    {
        ResolveLayoutFit();
        _layoutFit?.EnsureDesignRoot();
        _layoutFit?.ApplyFit();
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

    private static void BringAuctionFrameToFront()
    {
        if (WorkspaceLayoutController.Instance == null)
            return;

        var auctionZone = WorkspaceLayoutController.Instance.GetPanel(WorkspacePanelId.Auction);
        if (auctionZone != null)
            auctionZone.transform.SetAsLastSibling();
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
    }
}
