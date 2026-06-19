using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>hide/corner 드래그 → 스티키 노트 peel 입력.</summary>
[DisallowMultipleComponent]
public sealed class GateHintPeelHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private GateHintRowView _row;

    public void Bind(GateHintRowView row) => _row = row;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_row != null)
            _row.OnPeelBegin();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_row != null)
            _row.OnPeelDrag(eventData.delta.x);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_row != null)
            _row.OnPeelEnd();
    }
}
