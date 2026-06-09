using System;
using UnityEngine;

[Serializable]
public struct WorkspacePanelRect
{
    public float x;
    public float y;
    public float width;
    public float height;

    public WorkspacePanelRect(float x, float y, float width, float height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    public float CenterX => x + width * 0.5f;
    public float CenterY => y + height * 0.5f;

    public Vector2 Center => new(CenterX, CenterY);

    public WorkspacePanelRect ClampInside(float maxWidth, float maxHeight)
    {
        var clampedWidth = Mathf.Min(width, maxWidth);
        var clampedHeight = Mathf.Min(height, maxHeight);
        var clampedX = Mathf.Clamp(x, 0f, maxWidth - clampedWidth);
        var clampedY = Mathf.Clamp(y, 0f, maxHeight - clampedHeight);
        return new WorkspacePanelRect(clampedX, clampedY, clampedWidth, clampedHeight);
    }

    public WorkspacePanelRect Offset(float deltaX, float deltaY) =>
        new(x + deltaX, y + deltaY, width, height);

    public bool Overlaps(WorkspacePanelRect other) =>
        x < other.x + other.width
        && x + width > other.x
        && y < other.y + other.height
        && y + height > other.y;
}
