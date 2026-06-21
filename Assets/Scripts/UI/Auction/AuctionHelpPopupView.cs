using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>경매 방식(Ebay / English) 설명 팝업.</summary>
[DisallowMultipleComponent]
public sealed class AuctionHelpPopupView : MonoBehaviour
{
    private const string EbayHelp =
        "Ebay 경매: 원하는 금액을 입력하고 입찰합니다. " +
        "잠시 후 AI 입찰과 비교해 낙찰 여부가 결정됩니다.";

    private const string EnglishHelp =
        "English 경매: 경매 시작 후 실시간으로 입찰가가 오릅니다. " +
        "최고가를 유지하면 입장권을 획득합니다.";

    private GameObject _root;
    private TextMeshProUGUI _bodyText;

    public void Toggle(AuctionType auctionType, Transform anchor)
    {
        EnsureUi();
        if (_root.activeSelf)
        {
            _root.SetActive(false);
            return;
        }

        _bodyText.text = auctionType == AuctionType.English ? EnglishHelp : EbayHelp;
        _root.SetActive(true);

        if (anchor is RectTransform anchorRect && _root.transform is RectTransform popupRect)
        {
            popupRect.position = anchorRect.position;
        }
    }

    public void Hide()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    private void EnsureUi()
    {
        if (_root != null)
            return;

        _root = new GameObject("AuctionHelpPopup", typeof(RectTransform));
        var rect = _root.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.sizeDelta = new Vector2(360f, 120f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 1f);

        var bg = _root.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.92f);

        var textGo = new GameObject("Body", typeof(RectTransform));
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 12f);
        textRect.offsetMax = new Vector2(-12f, -12f);

        _bodyText = textGo.AddComponent<TextMeshProUGUI>();
        _bodyText.fontSize = 18f;
        _bodyText.color = Color.white;
        _bodyText.textWrappingMode = TextWrappingModes.Normal;
        _root.SetActive(false);
    }
}
