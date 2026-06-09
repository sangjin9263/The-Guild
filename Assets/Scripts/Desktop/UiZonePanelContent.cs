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
        if (backgroundImage != null)
        {
            var backgroundRect = backgroundImage.rectTransform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
        }

        if (uiCanvas != null)
        {
            var canvasRect = uiCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
                DesktopOverlaySettings.ApplyFixedReferenceRectOnScreen(canvasRect, rect);
        }

        if (panelRoot != null)
        {
            panelRoot.anchorMin = Vector2.zero;
            panelRoot.anchorMax = Vector2.one;
            panelRoot.offsetMin = padding;
            panelRoot.offsetMax = -padding;
        }
    }
}
