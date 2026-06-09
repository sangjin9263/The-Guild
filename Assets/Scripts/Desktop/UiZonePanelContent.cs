using UnityEngine;
using UnityEngine.UI;

public class UiZonePanelContent : MonoBehaviour
{
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Vector2 padding = new(12f, 12f);

    public Canvas UiCanvas => uiCanvas;
    public RectTransform PanelRoot => panelRoot;

    public void ApplyLayout(WorkspacePanelRect rect)
    {
        if (uiCanvas != null)
        {
            var canvasRect = uiCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
                ApplyRectToTransform(canvasRect, rect, Vector2.zero);
        }

        if (panelRoot != null)
            ApplyRectToTransform(panelRoot, rect, padding);

        if (backgroundImage != null)
        {
            var backgroundRect = backgroundImage.rectTransform;
            ApplyRectToTransform(backgroundRect, rect, Vector2.zero);
        }
    }

    private static void ApplyRectToTransform(
        RectTransform rectTransform,
        WorkspacePanelRect rect,
        Vector2 inset)
    {
        var refWidth = DesktopOverlaySettings.ReferenceWidth;
        var refHeight = DesktopOverlaySettings.ReferenceHeight;
        rectTransform.anchorMin = new Vector2(rect.x / refWidth, rect.y / refHeight);
        rectTransform.anchorMax = new Vector2(
            (rect.x + rect.width) / refWidth,
            (rect.y + rect.height) / refHeight);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.offsetMin = new Vector2(inset.x, inset.y);
        rectTransform.offsetMax = new Vector2(-inset.x, -inset.y);
    }
}
