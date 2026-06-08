using UnityEngine;

public static class DashPathObstacleUtility
{
    private static readonly int Layer1 = LayerMask.NameToLayer("Layer 1");
    private static readonly int Layer2 = LayerMask.NameToLayer("Layer 2");
    private static readonly int Layer3 = LayerMask.NameToLayer("Layer 3");

    public static bool ShouldBlockDash(
        Collider2D collider,
        int attackerLayer,
        System.Func<Collider2D, bool> isIgnoredCollider)
    {
        if (collider == null || collider.isTrigger)
            return false;

        if (isIgnoredCollider(collider))
            return false;

        if (IsOtherFloorWallCollider(collider, attackerLayer))
            return false;

        if (IsStairWalkSurfaceCollider(collider))
            return false;

        var attached = collider.attachedRigidbody;
        return attached == null || attached.bodyType == RigidbodyType2D.Static;
    }

    private static bool IsOtherFloorWallCollider(Collider2D collider, int attackerLayer)
    {
        var obstacleLayer = collider.gameObject.layer;
        if (obstacleLayer == attackerLayer)
            return false;

        if (obstacleLayer != Layer1 && obstacleLayer != Layer2 && obstacleLayer != Layer3)
            return false;

        return collider.name.Contains("Wall")
            || collider.GetComponent<CompositeCollider2D>() != null;
    }

    private static bool IsStairWalkSurfaceCollider(Collider2D collider)
    {
        if (collider == null)
            return false;

        var transform = collider.transform;
        var objectName = transform.name;

        if (objectName is "M" or "Stairs U" or "Stairs Layer Trigger")
            return IsUnderStairRoot(transform);

        if (objectName != "Collider Lower" || collider is not BoxCollider2D box)
            return false;

        if (!IsUnderStairRoot(transform))
            return false;

        var size = box.size;
        return size.x > size.y * 1.25f;
    }

    private static bool IsUnderStairRoot(Transform transform)
    {
        while (transform != null)
        {
            if (transform.name.StartsWith("PF Struct - Stairs"))
                return true;

            transform = transform.parent;
        }

        return false;
    }
}
