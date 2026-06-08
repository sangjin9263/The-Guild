using UnityEngine;

public static class SlashAimUtility
{
    public static Vector2 NormalizeAim(Vector2 aimDir, Vector2 fallback)
    {
        if (aimDir.sqrMagnitude < 0.0001f)
            aimDir = fallback;

        return aimDir.normalized;
    }

    public static float GetAimAngleDegrees(Vector2 aimDir)
    {
        return Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
    }

    public static Vector2 GetAimRight(Vector2 aimDir)
    {
        return new Vector2(-aimDir.y, aimDir.x);
    }

    public static Vector2 RotateDirection(Vector2 direction, float angleDegrees)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return direction;

        var radians = angleDegrees * Mathf.Deg2Rad;
        var cos = Mathf.Cos(radians);
        var sin = Mathf.Sin(radians);
        var x = direction.x;
        var y = direction.y;
        return new Vector2(x * cos - y * sin, x * sin + y * cos).normalized;
    }

    public static Vector2 DirectionFromAngle(float angleDegrees)
    {
        var radians = angleDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    public static Vector2 GetAuthoredPartOffset(SlashHitboxPart part, float authoredForwardAngle)
    {
        var authoredAimDir = DirectionFromAngle(authoredForwardAngle);
        var authoredPartDir = RotateDirection(authoredAimDir, part.angleOffset);
        var authoredPartRight = GetAimRight(authoredPartDir);

        return authoredPartDir * (part.forward + part.length * 0.5f)
             + authoredPartRight * part.side
             + Vector2.up * part.height;
    }

    public static Vector2 GetAuthoredPartCenter(
        Vector3 anchor,
        SlashHitboxPart part,
        float aimAngle,
        float authoredForwardAngle)
    {
        var localOffset = GetAuthoredPartOffset(part, authoredForwardAngle);
        var aimDelta = GetAimDeltaRotation(aimAngle, authoredForwardAngle);
        var worldOffset = aimDelta * (Vector3)localOffset;
        return (Vector2)anchor + (Vector2)worldOffset;
    }

    public static float GetAuthoredPartAngle(float aimAngle, float partAngleOffset)
    {
        return aimAngle + partAngleOffset;
    }

    public static Vector3 BuildAimLocalOffset(Vector2 aimDir, float forward, float side, float height)
    {
        var aimRight = GetAimRight(aimDir);

        return (Vector3)(aimDir * forward)
             + (Vector3)(aimRight * side)
             + Vector3.up * height;
    }

    public static Vector3 BuildAimLocalOffset(Vector2 aimDir, SlashSpawnLocal spawn)
    {
        return BuildAimLocalOffset(aimDir, spawn.forward, spawn.side, spawn.height);
    }

    public static Quaternion GetAimDeltaRotation(float aimAngleDegrees, float authoredForwardAngle)
    {
        var authored = Quaternion.Euler(0f, 0f, authoredForwardAngle);
        var current = Quaternion.Euler(0f, 0f, aimAngleDegrees);
        return current * Quaternion.Inverse(authored);
    }

    public static Vector3 GetAttackAnchor(
        Vector3 origin,
        Vector3 pivotOffset,
        SlashSpawnLocal spawn,
        Vector2 aimDir,
        float forwardDistance)
    {
        return origin + pivotOffset + BuildAimLocalOffset(aimDir, spawn) + (Vector3)(aimDir * forwardDistance);
    }
}
