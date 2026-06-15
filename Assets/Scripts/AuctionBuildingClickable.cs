using UnityEngine;

/// <summary>경매장 건물 클릭 → 경매 전용 패널.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class AuctionBuildingClickable : MonoBehaviour
{
    private void Reset()
    {
        EnsureCollider();
    }

    private void Awake()
    {
        EnsureCollider();
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        TryHandleClick();
    }

    private void TryHandleClick()
    {
        var collider = GetComponent<Collider2D>();
        if (collider == null)
            return;

        var camera = Camera.main;
        if (camera == null)
            return;

        var worldPoint = camera.ScreenToWorldPoint(Input.mousePosition);
        if (!collider.OverlapPoint(worldPoint))
            return;

        if (AuctionPanelUI.Instance == null)
        {
            Debug.LogWarning("AuctionPanelUI not found in scene.", this);
            return;
        }

        AuctionPanelUI.Instance.Toggle();
    }

    private void EnsureCollider()
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        var collider = GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = gameObject.AddComponent<BoxCollider2D>();

        var bounds = spriteRenderer.sprite.bounds;
        collider.size = bounds.size;
        collider.offset = bounds.center;
    }
}
