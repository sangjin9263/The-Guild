using System;
using UnityEngine;

[Serializable]
public struct SlashHitboxPart
{
    [Tooltip("Box length along this part's facing.")]
    public float length;

    [Tooltip("Box width perpendicular to this part's facing.")]
    public float width;

    [Tooltip("Move center forward/back along this part's facing in authored slash space.")]
    public float forward;

    [Tooltip("Move center sideways in authored slash space.")]
    public float side;

    [Tooltip("Move center up/down in world space.")]
    public float height;

    [Tooltip("Extra rotation in degrees from Slash Forward Angle (left = 180).")]
    public float angleOffset;
}

[Serializable]
public struct SlashHitboxGroup
{
    [Tooltip("Scales length and width of every part in this slash.")]
    [Min(1f)]
    public float sizeMultiplier;

    [Tooltip("2-3 boxes placed along the slash arc.")]
    public SlashHitboxPart[] parts;
}
