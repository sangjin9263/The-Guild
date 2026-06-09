using UnityEngine;

[ExecuteAlways]
public class ZoneGuideMarker : MonoBehaviour
{
    [SerializeField] private Color gizmoColor = new(1f, 0.85f, 0.2f, 0.9f);
    [SerializeField] private float lineTopY = 6f;
    [SerializeField] private float lineBottomY = -6f;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        var origin = transform.position;
        Gizmos.DrawLine(
            new Vector3(origin.x, lineBottomY, 0f),
            new Vector3(origin.x, lineTopY, 0f));
    }
}
