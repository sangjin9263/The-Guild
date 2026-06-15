using UnityEngine;
using UnityEngine.UI;

public class UiZonePanelContent : MonoBehaviour
{
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Vector2 padding = new(12f, 12f);

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

        var effectivePadding = BuildingPanelUI.Instance != null
            ? BuildingPanelUI.Instance.PanelPadding
            : padding;

        panelRoot.anchorMin = Vector2.zero;
        panelRoot.anchorMax = Vector2.one;
        panelRoot.offsetMin = effectivePadding;
        panelRoot.offsetMax = -effectivePadding;
    }
}
