using UnityEngine;
using UnityEngine.UI;

public class AuctionZonePanelContent : MonoBehaviour
{
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Vector2 padding = Vector2.zero;

    public RectTransform PanelRoot => panelRoot;

    public void ApplyLayout(WorkspacePanelRect rect)
    {
        if (backgroundImage != null)
        {
            var backgroundRect = backgroundImage.rectTransform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
        }

        ApplyPanelPadding();
    }

    public void ApplyPanelPadding()
    {
        if (panelRoot == null)
            return;

        var effectivePadding = AuctionPanelUI.Instance != null
            ? AuctionPanelUI.Instance.PanelPadding
            : padding;

        panelRoot.anchorMin = Vector2.zero;
        panelRoot.anchorMax = Vector2.one;
        panelRoot.offsetMin = effectivePadding;
        panelRoot.offsetMax = -effectivePadding;
    }
}
