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

        ConfigurePanelRaycasts();
        ApplyToAuctionZone();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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
            Hide();
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

        if (titleText != null)
            titleText.text = "게이트 경매장";

        if (bodyText != null)
            bodyText.text = "경매 UI 준비 중.\n(10건 진열 · 수고비 50G · Ebay/English)";

        panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void ApplyPanelToAuctionZone()
    {
        var auctionZone = WorkspaceLayoutController.Instance != null
            ? WorkspaceLayoutController.Instance.GetPanel(WorkspacePanelId.Auction)
            : null;

        if (auctionZone != null)
        {
            auctionZone.ApplyAuctionZoneLayout();
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
