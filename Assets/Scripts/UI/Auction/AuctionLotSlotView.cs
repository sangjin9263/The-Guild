using Michsky.UI.ModernUIPack;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>AuctionPanel_re LotViewport slot — 요약 표시 + 선택 하이라이트 + 상태 오버레이.</summary>
[DisallowMultipleComponent]
public sealed class AuctionLotSlotView : MonoBehaviour
{
    private static readonly Color SelectedColor = new(0.55f, 0.72f, 1f, 0.58f);
    private static readonly Color BorderColor = Color.black;
    private static readonly Color CornerArrowColor = new(60f / 255f, 100f / 255f, 1f, 1f);

    private const string MinBidLabelText = "최소 입찰금";
    private const string InProgressLabel = "입찰 진행 중";
    private const string SelectionOverlayName = "SelectionOverlay";
    private const string ArrowDownResourcePath = "Art/Icon/ArrowDown";
    private const float CornerArrowSize = 70f;

    private static Sprite _arrowDownSprite;

    private Button _button;
    private Image _borderImage;
    private Image _selectionOverlay;
    private Transform _gateIconRoot;
    private Image _gateIconImage;
    private Transform _mainImageRoot;
    private Image _mainImage;
    private TextMeshProUGUI _minBidLabelText;
    private TextMeshProUGUI _minBidAmountText;
    private ProgressBar _energyBar;
    private TextMeshProUGUI _energyPercentText;
    private GameObject _statusRoot;
    private GameObject _countRoot;
    private GameObject _statusSuccRoot;
    private GameObject _statusFailRoot;
    private bool _compactLayoutApplied;

    private GateAuctionLotRuntime _lot;
    private GateDatabase _database;
    private int _slotIndex;

    public int SlotIndex => _slotIndex;
    public GateAuctionLotRuntime Lot => _lot;

    public void Initialize(int slotIndex)
    {
        _slotIndex = slotIndex;
        CacheRefs();
        ApplyCompactSlotLayout();
        SetSelected(false);
        Clear();
    }

    public void Bind(GateAuctionLotRuntime lot, GateDatabase database)
    {
        _lot = lot;
        _database = database;
        gameObject.SetActive(true);

        if (_minBidLabelText != null)
            _minBidLabelText.text = MinBidLabelText;

        if (_minBidAmountText != null && lot != null)
            _minBidAmountText.text = $"{lot.UserMinBid:N0} G";

        RefreshGateIcon();
        RefreshMainImage();
        RefreshEnergy();
        RefreshStatus();
    }

    public void Clear()
    {
        _lot = null;
        gameObject.SetActive(false);
        SetSelected(false);
        HideStatus();
    }

    public void SetSelected(bool selected)
    {
        if (_borderImage != null)
            _borderImage.color = BorderColor;

        if (_selectionOverlay != null)
            _selectionOverlay.gameObject.SetActive(selected);
    }

    public void WireClick(System.Action<int> onClicked)
    {
        if (_button == null)
            return;

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => onClicked?.Invoke(_slotIndex));
    }

    public void RefreshStatus()
    {
        if (_lot == null || _statusRoot == null)
        {
            HideStatus();
            return;
        }

        if (_lot.IsEnglishSessionActive || _lot.State == GateAuctionLotState.PendingResult)
        {
            ShowInProgress();
            return;
        }

        switch (_lot.State)
        {
            case GateAuctionLotState.Won:
                ShowResult(won: true);
                break;
            case GateAuctionLotState.Lost:
                ShowResult(won: false);
                break;
            case GateAuctionLotState.Acknowledged:
                HideStatus();
                break;
            default:
                HideStatus();
                break;
        }
    }

    private void Update()
    {
        if (_lot == null)
            return;

        if (_lot.State == GateAuctionLotState.PendingResult || _lot.IsEnglishSessionActive)
            RefreshStatus();
    }

    private void CacheRefs()
    {
        _borderImage = GetComponent<Image>();
        _button = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
        ConfigureClickButton();

        var gateImage = transform.Find("gate_image");
        _mainImageRoot = gateImage != null ? gateImage.Find("main_img") : null;
        _gateIconRoot = transform.Find("gate_icon (1)") ?? transform.Find("gate_icon");
        _gateIconImage = _gateIconRoot != null ? _gateIconRoot.GetComponent<Image>() : null;
        _mainImage = _mainImageRoot != null ? _mainImageRoot.GetComponent<Image>() : null;
        _minBidLabelText = transform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
        _minBidAmountText = transform.Find("min_bid")?.GetComponent<TextMeshProUGUI>();

        var energyRoot = gateImage != null ? gateImage.Find("Energy_Percent") : null;
        if (energyRoot != null)
        {
            _energyBar = energyRoot.GetComponent<ProgressBar>();
            _energyPercentText = energyRoot.Find("percent")?.GetComponent<TextMeshProUGUI>();
        }

        EnsureSelectionOverlay();
        CacheStatusOverlay();
    }

    private void ConfigureClickButton()
    {
        if (_button == null)
            return;

        _button.transition = Selectable.Transition.None;
        if (_button.targetGraphic == null && _borderImage != null)
            _button.targetGraphic = _borderImage;
    }

    private void EnsureSelectionOverlay()
    {
        Transform overlayRoot;

        var existing = transform.Find(SelectionOverlayName);
        if (existing != null)
        {
            overlayRoot = existing;
            _selectionOverlay = existing.GetComponent<Image>();
        }
        else
        {
            var go = new GameObject(SelectionOverlayName, typeof(RectTransform));
            overlayRoot = go.transform;
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.SetAsLastSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _selectionOverlay = go.AddComponent<Image>();
            _selectionOverlay.color = SelectedColor;
            _selectionOverlay.raycastTarget = false;
            go.SetActive(false);
        }

        EnsureCornerArrows(overlayRoot);
    }

    private void EnsureCornerArrows(Transform overlayRoot)
    {
        if (overlayRoot.Find("CornerTopLeft") != null)
            return;

        var arrow = GetArrowDownSprite();
        if (arrow == null)
        {
            Debug.LogWarning("[AuctionLotSlot] ArrowDown sprite missing at Resources/" + ArrowDownResourcePath, this);
            return;
        }

        var slotWidth = ((RectTransform)transform).rect.width;
        var farEdgeX = slotWidth > 0f ? slotWidth - 10f : 290f;

        CreateCornerArrow(overlayRoot, "CornerTopLeft", new Vector2(0f, 1f), new Vector2(10f, -10f), new Vector3(0f, 0f, 225f), arrow);
        CreateCornerArrow(overlayRoot, "CornerTopRight", new Vector2(0f, 1f), new Vector2(farEdgeX, -10f), new Vector3(0f, 180f, 225f), arrow);
        CreateCornerArrow(overlayRoot, "CornerBottomRight", new Vector2(1f, 0f), new Vector2(-10f, 10f), new Vector3(0f, 0f, 45f), arrow);
        CreateCornerArrow(overlayRoot, "CornerBottomLeft", new Vector2(1f, 0f), new Vector2(-farEdgeX, 10f), new Vector3(0f, 180f, 45f), arrow);
    }

    private static void CreateCornerArrow(
        Transform parent,
        string name,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector3 eulerAngles,
        Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(CornerArrowSize, CornerArrowSize);
        rect.localRotation = Quaternion.Euler(eulerAngles);

        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = CornerArrowColor;
        image.raycastTarget = false;
    }

    private static Sprite GetArrowDownSprite()
    {
        if (_arrowDownSprite == null)
            _arrowDownSprite = Resources.Load<Sprite>(ArrowDownResourcePath);

        return _arrowDownSprite;
    }

    private void ApplyCompactSlotLayout()
    {
        if (_compactLayoutApplied)
            return;

        _compactLayoutApplied = true;

        if (_minBidLabelText != null)
            _minBidLabelText.text = MinBidLabelText;
    }

    private void RefreshGateIcon()
    {
        if (_gateIconImage == null || _lot == null)
            return;

        var icon = GateGradeIconCache.GetIcon(_lot.Lot.grade);
        if (icon == null)
            return;

        _gateIconImage.sprite = icon;
        if (_gateIconRoot != null)
            _gateIconRoot.gameObject.SetActive(true);
    }

    private void RefreshMainImage()
    {
        if (_mainImage == null || _lot == null)
            return;

        var preview = GateAuctionImageCache.GetImage(_lot.Lot.grade);
        if (preview == null)
            return;

        _mainImage.sprite = preview;
        _mainImage.color = Color.white;
        if (_mainImageRoot != null)
            _mainImageRoot.gameObject.SetActive(true);
    }

    private void CacheStatusOverlay()
    {
        var overlay = transform.Find("StatusOverlay");
        if (overlay == null)
        {
            Debug.LogWarning(
                "[AuctionLotSlot] StatusOverlay missing. Run The Guild/UI/Wire Auction Result Panels.",
                this);
            return;
        }

        _statusRoot = overlay.gameObject;
        _countRoot = overlay.Find("count")?.gameObject;
        _statusSuccRoot = overlay.Find("status_succ")?.gameObject;
        _statusFailRoot = overlay.Find("status_fail")?.gameObject;

        var countText = _countRoot != null ? _countRoot.GetComponent<TextMeshProUGUI>() : null;
        if (countText != null)
            countText.text = InProgressLabel;

        _statusRoot.SetActive(false);
    }

    private void RefreshEnergy()
    {
        if (_lot == null)
            return;

        var percent = _database != null
            ? _database.GetEnergyDisplayPercent(_lot.Lot)
            : _lot.Lot.energy * 100f / GateDatabase.EnergyPercentMax;
        percent = Mathf.Clamp(percent, 0f, 100f);

        if (_energyBar != null)
        {
            _energyBar.isOn = false;
            _energyBar.ChangeValue(percent);
        }

        if (_energyPercentText != null)
            _energyPercentText.text = $"{percent:0}%";
    }

    private void ShowInProgress()
    {
        if (_statusRoot == null)
            return;

        SetOverlayChildActive(_countRoot, true);
        SetOverlayChildActive(_statusSuccRoot, false);
        SetOverlayChildActive(_statusFailRoot, false);
        _statusRoot.SetActive(true);
    }

    private void ShowResult(bool won)
    {
        if (_statusRoot == null)
            return;

        SetOverlayChildActive(_countRoot, false);
        SetOverlayChildActive(_statusSuccRoot, won);
        SetOverlayChildActive(_statusFailRoot, !won);
        _statusRoot.SetActive(true);
    }

    private void HideStatus()
    {
        if (_statusRoot != null)
            _statusRoot.SetActive(false);
    }

    private static void SetOverlayChildActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
