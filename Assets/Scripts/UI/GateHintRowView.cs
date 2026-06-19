using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Lot_prefab Hintinfo — 힌트 구매(bf_pay) + 스티키 peel(af_pay).</summary>
[DisallowMultipleComponent]
public sealed class GateHintRowView : MonoBehaviour
{
    private const float PeelCompleteThreshold = 0.82f;
    private const int MaxHintArrows = 3;
    private const string HintRootPath = "Gate_Info/Hintinfo";

    private GateAuctionLotRuntime _lot;
    private GateDatabase _database;

    private GameObject _bfPay;
    private GameObject _afPay;
    private RectTransform _hideRect;
    private Image _hideImage;
    private RectTransform _cornerRect;
    private RectTransform _pullAnimRect;
    private GameObject _reveal;
    private TextMeshProUGUI _hintAmountText;
    private TextMeshProUGUI _hintText;
    private TextMeshProUGUI _hintInfoText;
    private readonly GameObject[] _hintArrows = new GameObject[MaxHintArrows];
    private GateHintPullAnim _pullAnim;
    private GateHintPeelHandle _peelHandle;
    private Button _purchaseButton;

    private Vector2 _hideRestPosition;
    private Vector2 _cornerRestPosition;
    private Vector2 _pullAnimRestPosition;
    private float _hideWidth;
    private float _leftEdgeX;
    private float _rightEdgeX;
    private float _peelLead;
    private float _pullAnimLead;
    private float _peelOffset;
    private bool _peelDragging;

    private void Awake()
    {
        CacheUiRefs();
        EnsurePeelVisualSetup();
        EnsurePeelHandle();
        EnsurePurchaseButton();
    }

    public void Bind(GateAuctionLotRuntime lot, GateDatabase database)
    {
        _lot = lot;
        _database = database;
        gameObject.SetActive(true);
        ResetPeelVisual();
        Refresh();
    }

    public void Clear()
    {
        _lot = null;
        _peelDragging = false;
        _pullAnim?.Stop();
    }

    public void Refresh()
    {
        if (_lot == null || _database == null)
            return;

        var canPurchase = _lot.State == GateAuctionLotState.Bidding && !_lot.HintPurchased;
        var readyToPeel = _lot.HintPurchased && !_lot.HintRevealed;
        var revealed = _lot.HintRevealed;

        if (_hintAmountText != null)
        {
            var cost = _database.GetBrokerHintCost(_lot.Lot.grade, _lot.Lot.auction.bidBandMin);
            _hintAmountText.text = cost.ToString("N0");
        }

        ApplyHintDisplay(_lot.HintPurchased || revealed);

        if (_bfPay != null)
            _bfPay.SetActive(canPurchase);

        if (_afPay != null)
            _afPay.SetActive(_lot.HintPurchased || revealed);

        if (_reveal != null)
            _reveal.SetActive(_lot.HintPurchased || revealed);

        if (_hideRect != null)
        {
            _hideRect.gameObject.SetActive(readyToPeel);
            if (readyToPeel)
                ResetPeelVisual();
        }

        if (_purchaseButton != null)
            _purchaseButton.interactable = canPurchase;

        if (_peelHandle != null)
            _peelHandle.enabled = readyToPeel;

        UpdatePullAnim(readyToPeel && !_peelDragging);
    }

    public void OnPeelBegin()
    {
        if (_lot == null || !_lot.HintPurchased || _lot.HintRevealed)
            return;

        _peelDragging = true;
        _pullAnim?.Stop();
    }

    public void OnPeelDrag(float deltaX)
    {
        if (_lot == null || !_lot.HintPurchased || _lot.HintRevealed || _hideRect == null)
            return;

        if (deltaX <= 0f)
            return;

        _peelOffset = Mathf.Clamp(_peelOffset + deltaX, 0f, _hideWidth);
        ApplyPeelOffset(_peelOffset);

        if (_peelOffset / _hideWidth >= PeelCompleteThreshold)
            CompletePeel();
    }

    public void OnPeelEnd()
    {
        _peelDragging = false;

        if (_lot == null || _lot.HintRevealed)
            return;

        if (_peelOffset / _hideWidth >= PeelCompleteThreshold * 0.55f)
        {
            CompletePeel();
            return;
        }

        ResetPeelVisual();
        UpdatePullAnim(true);
    }

    private void CompletePeel()
    {
        if (_lot == null || _lot.HintRevealed)
            return;

        _peelOffset = _hideWidth;
        _pullAnim?.Stop();

        if (_hideRect != null)
            _hideRect.gameObject.SetActive(false);

        if (GateAuctionManager.Instance != null)
            GateAuctionManager.Instance.NotifyHintRevealed(_lot.LotId);

        Refresh();
    }

    /// <summary>
    /// hide 박스 고정. peel edge(남은 hide 오른쪽 경계)는 offset만큼 왼쪽으로 이동.
    /// corner는 peel edge에 붙어서 같이 움직임 (초기엔 flap inset 유지).
    /// </summary>
    private void ApplyPeelOffset(float offset)
    {
        if (_hideRect != null)
            _hideRect.anchoredPosition = _hideRestPosition;

        var peelEdge = _rightEdgeX - offset;
        var fillAmount = Mathf.Clamp01((peelEdge - _leftEdgeX) / _hideWidth);

        if (_hideImage != null)
            _hideImage.fillAmount = fillAmount;

        var cornerX = offset <= _peelLead
            ? _cornerRestPosition.x + offset
            : peelEdge;

        if (_cornerRect != null)
            _cornerRect.anchoredPosition = new Vector2(cornerX, _cornerRestPosition.y);

        if (_pullAnimRect != null)
            _pullAnimRect.anchoredPosition = new Vector2(cornerX - _pullAnimLead, _pullAnimRestPosition.y);
    }

    private void ResetPeelVisual()
    {
        _peelOffset = 0f;
        ApplyPeelOffset(0f);
    }

    private void UpdatePullAnim(bool shouldPlay)
    {
        if (_pullAnim == null)
            return;

        if (shouldPlay)
            _pullAnim.Play();
        else
            _pullAnim.Stop();
    }

    private void OnPurchaseClicked()
    {
        if (_lot == null || GateAuctionManager.Instance == null)
            return;

        if (GateAuctionManager.Instance.TryPurchaseHint(_lot.LotId))
            Refresh();
    }

    private void CacheUiRefs()
    {
        var root = transform.Find(HintRootPath);
        if (root == null)
        {
            Debug.LogWarning("[GateHintRowView] Hintinfo not found.", this);
            return;
        }

        _bfPay = root.Find("bf_pay")?.gameObject;
        _afPay = root.Find("af_pay")?.gameObject;

        var hide = root.Find("af_pay/hide");
        if (hide != null)
        {
            _hideRect = hide.GetComponent<RectTransform>();
            _hideRestPosition = _hideRect.anchoredPosition;
            _hideImage = hide.GetComponent<Image>();

            var corner = hide.Find("corner");
            if (corner != null)
            {
                _cornerRect = corner.GetComponent<RectTransform>();
                _cornerRestPosition = _cornerRect.anchoredPosition;
            }

            var pull = hide.Find("pull_anim");
            if (pull != null)
            {
                _pullAnimRect = pull.GetComponent<RectTransform>();
                _pullAnimRestPosition = _pullAnimRect.anchoredPosition;
                _pullAnim = pull.GetComponent<GateHintPullAnim>() ?? pull.gameObject.AddComponent<GateHintPullAnim>();
            }

            if (_cornerRect != null && _pullAnimRect != null)
                _pullAnimLead = _cornerRestPosition.x - _pullAnimRestPosition.x;

            CacheHideMetrics();
        }

        _reveal = root.Find("af_pay/reveal")?.gameObject;
        _hintAmountText = root.Find("bf_pay/hint_amount")?.GetComponent<TextMeshProUGUI>();
        _hintText = root.Find("af_pay/reveal/gate_hint")?.GetComponent<TextMeshProUGUI>();
        _hintInfoText = root.Find("af_pay/reveal/hint_info")?.GetComponent<TextMeshProUGUI>();

        var hintArrowRoot = root.Find("af_pay/reveal/hint_arrow");
        for (var i = 0; i < _hintArrows.Length; i++)
            _hintArrows[i] = hintArrowRoot?.Find($"hint_arrow{i + 1}")?.gameObject;

        if (_hintInfoText == null)
            Debug.LogWarning("[GateHintRowView] hint_info TMP not found.", this);

        if (hintArrowRoot == null)
            Debug.LogWarning("[GateHintRowView] hint_arrow root not found.", this);

        ApplyHintArrows(0);
        EnsureRevealBehindHide(root);
    }

    private void ApplyHintDisplay(bool show)
    {
        if (!show)
        {
            if (_hintText != null)
                _hintText.text = string.Empty;

            if (_hintInfoText != null)
                _hintInfoText.text = string.Empty;

            ApplyHintArrows(0);
            return;
        }

        var hintText = _lot.BrokerHintText;
        var hintInfo = _lot.BrokerHintInfoText;
        var upArrow = _lot.BrokerHintUpArrow;

        if (_database.TryGetBrokerHint(_lot.Lot.archetype, _lot.Lot.tierId, out var hint))
        {
            if (!string.IsNullOrEmpty(hint.hintText))
                hintText = hint.hintText;

            hintInfo = hint.hintInfo;
            upArrow = hint.upArrow;
        }

        if (_hintText != null)
        {
            var text = hintText ?? string.Empty;
            _hintText.text = text;
            GameUIFont.EnsureGlyphs(_hintText, text);
        }

        if (_hintInfoText != null)
        {
            var info = hintInfo ?? string.Empty;
            _hintInfoText.text = info;
            GameUIFont.EnsureGlyphs(_hintInfoText, info);
        }

        ApplyHintArrows(upArrow);
    }

    private void ApplyHintArrows(int visibleCount)
    {
        visibleCount = Mathf.Clamp(visibleCount, 0, _hintArrows.Length);

        for (var i = 0; i < _hintArrows.Length; i++)
        {
            if (_hintArrows[i] != null)
                _hintArrows[i].SetActive(i < visibleCount);
        }
    }

    private void CacheHideMetrics()
    {
        if (_hideRect == null)
            return;

        _hideWidth = _hideRect.rect.width;
        var pivotX = _hideRect.pivot.x;
        _leftEdgeX = -_hideWidth * pivotX;
        _rightEdgeX = _hideWidth * (1f - pivotX);

        if (_cornerRect != null)
            _peelLead = _rightEdgeX - _cornerRestPosition.x;
    }

    private void EnsurePeelVisualSetup()
    {
        if (_hideImage == null)
            return;

        _hideImage.type = Image.Type.Filled;
        _hideImage.fillMethod = Image.FillMethod.Horizontal;
        _hideImage.fillOrigin = (int)Image.OriginHorizontal.Right;
        _hideImage.fillAmount = 1f;
    }

    private static void EnsureRevealBehindHide(Transform hintRoot)
    {
        var reveal = hintRoot.Find("af_pay/reveal");
        var hide = hintRoot.Find("af_pay/hide");
        if (reveal == null || hide == null)
            return;

        reveal.SetSiblingIndex(0);
        hide.SetSiblingIndex(1);
    }

    private void EnsurePeelHandle()
    {
        var corner = transform.Find($"{HintRootPath}/af_pay/hide/corner");
        if (corner == null)
            return;

        foreach (var graphic in corner.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = true;

        _peelHandle = corner.GetComponent<GateHintPeelHandle>() ?? corner.gameObject.AddComponent<GateHintPeelHandle>();
        _peelHandle.Bind(this);
    }

    private void EnsurePurchaseButton()
    {
        if (_bfPay == null)
            return;

        _purchaseButton = _bfPay.GetComponent<Button>();
        if (_purchaseButton == null)
            _purchaseButton = _bfPay.AddComponent<Button>();

        var image = _bfPay.GetComponent<Image>();
        if (image != null)
        {
            _purchaseButton.targetGraphic = image;
            image.raycastTarget = true;
        }

        _purchaseButton.onClick.RemoveListener(OnPurchaseClicked);
        _purchaseButton.onClick.AddListener(OnPurchaseClicked);
    }

    private void OnDestroy()
    {
        if (_purchaseButton != null)
            _purchaseButton.onClick.RemoveListener(OnPurchaseClicked);
    }
}
