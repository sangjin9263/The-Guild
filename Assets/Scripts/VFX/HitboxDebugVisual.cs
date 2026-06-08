using UnityEngine;

public static class HitboxDebugVisual
{
    private static Sprite fillSprite;

    public static void Show(
        Vector2 center,
        Vector2 size,
        float angleDegrees,
        float duration,
        Color fillColor,
        string sortingLayerName = "Layer 1",
        int sortingOrder = 50)
    {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
        return;
#endif
        if (duration <= 0f || size.x <= 0f || size.y <= 0f)
            return;

        EnsureFillSprite();

        var root = new GameObject("HitboxDebug");
        root.transform.position = new Vector3(center.x, center.y, 0f);
        root.transform.rotation = Quaternion.Euler(0f, 0f, angleDegrees);
        root.transform.localScale = new Vector3(size.x, size.y, 1f);

        var renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = fillSprite;
        renderer.color = fillColor;
        renderer.sortingLayerName = sortingLayerName;
        renderer.sortingOrder = sortingOrder;

        Object.Destroy(root, duration);
    }

    private static void EnsureFillSprite()
    {
        if (fillSprite != null)
            return;

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, true);
        fillSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }
}
