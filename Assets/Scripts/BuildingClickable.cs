using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class BuildingClickable : MonoBehaviour
{
    [SerializeField] private string buildingTitle = "Building 1";
    [SerializeField] [TextArea(2, 6)] private string buildingDescription =
        "Guild building. Upgrade and manage mercenaries here.";

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

        if (BuildingPanelUI.Instance == null)
        {
            Debug.LogWarning("BuildingPanelUI not found in scene.", this);
            return;
        }

        BuildingPanelUI.Instance.Toggle(buildingTitle, buildingDescription);
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
