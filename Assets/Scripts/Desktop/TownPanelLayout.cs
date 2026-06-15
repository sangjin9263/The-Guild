using UnityEngine;

/// <summary>
/// TownContent 로컬 좌표계 기준 타일·포털 배치 값 (패널 fit 후에도 동일).
/// </summary>
public static class TownPanelLayout
{
    public const int GroundMinX = -11;
    public const int GroundMaxX = 11;
    public const int GroundMinY = -7;
    public const int GroundMaxY = -5;

    public const float PortalLocalX = -10f;
    public const float PortalLocalY = -3.36f;

    public const float PortalVisualScaleX = -1.5f;
    public const float PortalVisualScaleY = 1.5f;

    public static Vector2 PortalLocalPosition => new(PortalLocalX, PortalLocalY);
    public static Vector3 PortalVisualScale => new(PortalVisualScaleX, PortalVisualScaleY, PortalVisualScaleY);
}
