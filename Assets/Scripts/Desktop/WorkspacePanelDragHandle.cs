using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Forwards drag events on the panel label (or drag bar) to the parent WorkspacePanel.
/// </summary>
[DisallowMultipleComponent]
public class WorkspacePanelDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private WorkspacePanel panel;

    private void Awake()
    {
        panel = GetComponentInParent<WorkspacePanel>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (panel != null)
            panel.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (panel != null)
            panel.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (panel != null)
            panel.OnEndDrag(eventData);
    }
}
