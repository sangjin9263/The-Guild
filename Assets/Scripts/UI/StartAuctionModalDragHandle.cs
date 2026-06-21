using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>start_auction_re TitleBar 드래그 — setting 패널 이동.</summary>
[DisallowMultipleComponent]
public sealed class StartAuctionModalDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private RectTransform _target;
    private RectTransform _bounds;
    private Vector2 _dragOffset;

    public void Configure(RectTransform target, RectTransform bounds)
    {
        _target = target;
        _bounds = bounds;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_target == null || _target.parent is not RectTransform parent)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, eventData.position, eventData.pressEventCamera, out var local))
            return;

        _dragOffset = _target.anchoredPosition - local;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_target == null || _target.parent is not RectTransform parent)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, eventData.position, eventData.pressEventCamera, out var local))
            return;

        _target.anchoredPosition = ClampToBounds(local + _dragOffset);
    }

    private Vector2 ClampToBounds(Vector2 anchoredPosition)
    {
        if (_bounds == null || _target == null)
            return anchoredPosition;

        var boundsRect = _bounds.rect;
        var half = _target.rect.size * 0.5f;

        var minX = boundsRect.xMin + half.x;
        var maxX = boundsRect.xMax - half.x;
        var minY = boundsRect.yMin + half.y;
        var maxY = boundsRect.yMax - half.y;

        if (minX <= maxX)
            anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, minX, maxX);
        if (minY <= maxY)
            anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, minY, maxY);

        return anchoredPosition;
    }
}
