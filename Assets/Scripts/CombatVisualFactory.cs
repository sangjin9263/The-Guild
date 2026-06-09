using UnityEngine;

public static class CombatVisualFactory
{
    private static Sprite characterSprite;
    private static Sprite squareSprite;

    public static Sprite GetCharacterSprite()
    {
        if (characterSprite != null)
            return characterSprite;

        const int width = 16;
        const int height = 24;
        const float pixelsPerUnit = 16f;

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                texture.SetPixel(x, y, Color.white);
        }

        texture.Apply(false, true);

        characterSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0f),
            pixelsPerUnit);

        return characterSprite;
    }

    public static Sprite GetSquareSprite()
    {
        if (squareSprite != null)
            return squareSprite;

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, true);

        squareSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);

        return squareSprite;
    }
}
