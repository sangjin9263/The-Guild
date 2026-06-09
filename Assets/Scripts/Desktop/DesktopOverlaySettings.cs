using UnityEngine;

/// <summary>
/// Reference monitor layout (1920x1080) with draggable Town / UI / Dungeon panels.
/// </summary>
public static class DesktopOverlaySettings
{
    public const float ReferenceWidth = 1920f;
    public const float ReferenceHeight = 1080f;

    public const float TownPanelWidth = 640f;
    public const float TownPanelHeight = 480f;
    public const float UiZonePanelWidth = 640f;
    public const float DungeonSlotWidth = 640f;
    public const float DungeonSlotHeight = 240f;
    public const float PanelGap = 20f;

    public const int MaxDungeonSlots = 3;
    public const int DungeonSlotCount = MaxDungeonSlots;

    public const float CameraOrthographicSize = 6f;
    public const float ContentScale = 1f;
    public const float BottomMargin = 0f;
    public const float PlatformerGroundBottomPadding = 0.75f;
    public const float PortalVisualScale = 5.6f;
    public const float ReferencePixelsPerWorldUnit = 30f;

    public const float DungeonMinX = -32f;
    public const float DungeonMaxX = -2f;
    public const float BuildingMinX = 2f;
    public const float BuildingMaxX = 32f;

    public const float BattlePortalX = -3f;
    public const float VillagePortalX = 26f;
    public const float PortalY = -3f;

    public const int GroundMinY = -6;
    public const int CombatGroundMaxY = -2;
    public const int VillageGroundMaxY = 3;

    public static float ReferenceOrthographicSize =>
        ReferenceHeight / (2f * ReferencePixelsPerWorldUnit);

    public static float EffectiveOrthographicSize => ReferenceOrthographicSize * ContentScale;

    public static float WindowWidth => ReferenceWidth;
    public static float WindowHeight => ReferenceHeight;

    public static Vector2 WindowSize => new(ReferenceWidth, ReferenceHeight);

    public static float GetDefaultGroundSurfaceY() =>
        CombatGroundMaxY + 0.5f;

    public static float GetReferenceToScreenScale()
    {
        if (Screen.height <= 0)
            return 1f;

        return Screen.height / ReferenceHeight;
    }

    public static float GetReferenceOrthographicSize() => EffectiveOrthographicSize;

    public static float GetViewHalfWidth(float referenceWidth)
    {
        var aspect = referenceWidth / ReferenceHeight;
        return EffectiveOrthographicSize * aspect;
    }

    public static float GetViewHalfWidth(Camera camera)
    {
        if (camera == null)
            return GetViewHalfWidth(ReferenceWidth);

        return camera.orthographicSize * camera.aspect;
    }

    public static Vector2 ReferenceRectCenterToWorld(WorkspacePanelRect rect)
    {
        return ReferenceRectCenterToWorld(rect, ResolveLayoutCamera());
    }

    public static Vector2 ReferenceRectCenterToWorld(WorkspacePanelRect rect, Camera camera)
    {
        ReferenceToWorld(rect.CenterX, rect.CenterY, camera, out var worldX, out var worldY);
        return new Vector2(worldX, worldY);
    }

    public static void ReferenceRectToWorldBounds(
        WorkspacePanelRect rect,
        Camera camera,
        out Vector2 bottomLeft,
        out Vector2 topRight)
    {
        ReferenceToWorld(rect.x, rect.y, camera, out var minX, out var minY);
        ReferenceToWorld(rect.x + rect.width, rect.y + rect.height, camera, out var maxX, out var maxY);
        bottomLeft = new Vector2(minX, minY);
        topRight = new Vector2(maxX, maxY);
    }

    public static float GetPanelWorldHeight(WorkspacePanelRect rect, Camera camera)
    {
        ReferenceToWorld(rect.x, rect.y, camera, out _, out var bottomY);
        ReferenceToWorld(rect.x, rect.y + rect.height, camera, out _, out var topY);
        return topY - bottomY;
    }

    public static Camera ResolveLayoutCamera()
    {
        if (WorkspaceLayoutController.Instance != null)
        {
            var controllerCamera = WorkspaceLayoutController.Instance.TargetCamera;
            if (controllerCamera != null)
                return controllerCamera;
        }

        return Camera.main;
    }

    public static float ResolveLayoutAspect(Camera camera)
    {
        if (Application.isPlaying && Screen.height > 0)
            return (float)Screen.width / Screen.height;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var gameViewSize = UnityEditor.Handles.GetMainGameViewSize();
            if (gameViewSize.y > 0f)
                return gameViewSize.x / gameViewSize.y;
        }
#endif

        if (camera != null && camera.pixelHeight > 0)
            return camera.aspect;

        if (Screen.height > 0)
            return (float)Screen.width / Screen.height;

        return ReferenceWidth / ReferenceHeight;
    }

    private static void ReferenceToWorld(
        float referenceX,
        float referenceY,
        Camera camera,
        out float worldX,
        out float worldY)
    {
        var orthoSize = camera != null ? camera.orthographicSize : EffectiveOrthographicSize;
        var aspect = ResolveLayoutAspect(camera);
        var worldWidth = orthoSize * 2f * aspect;
        var worldHeight = orthoSize * 2f;
        var normalizedX = referenceX / ReferenceWidth - 0.5f;
        var normalizedY = referenceY / ReferenceHeight - 0.5f;
        worldX = normalizedX * worldWidth;
        worldY = normalizedY * worldHeight;
    }

    public static float GetPanelWorldScale(WorkspacePanelRect rect, float referencePanelHeight)
    {
        if (referencePanelHeight <= 0f)
            return 1f;

        return rect.height / referencePanelHeight;
    }

    public static WorkspacePanelRect GetDefaultTownRect() =>
        new(ReferenceWidth - TownPanelWidth, 0f, TownPanelWidth, TownPanelHeight);

    public static WorkspacePanelRect GetDefaultUiZoneRect()
    {
        var town = GetDefaultTownRect();
        var height = ReferenceHeight - town.height;
        return new WorkspacePanelRect(town.x, town.height, UiZonePanelWidth, height);
    }

    public static WorkspacePanelRect GetDefaultDungeonRectForSlot(int slotIndex)
    {
        var town = GetDefaultTownRect();
        var x = town.x - PanelGap - DungeonSlotWidth;
        var stackFromBottom = MaxDungeonSlots - 1 - slotIndex;
        var y = stackFromBottom * DungeonSlotHeight;
        return new WorkspacePanelRect(x, y, DungeonSlotWidth, DungeonSlotHeight);
    }

    public static WorkspacePanelRect GetDefaultPanelRect(string panelId)
    {
        return panelId switch
        {
            WorkspacePanelId.Town => GetDefaultTownRect(),
            WorkspacePanelId.UiZone => GetDefaultUiZoneRect(),
            WorkspacePanelId.DungeonSlot1 => GetDefaultDungeonRectForSlot(0),
            WorkspacePanelId.DungeonSlot2 => GetDefaultDungeonRectForSlot(1),
            WorkspacePanelId.DungeonSlot3 => GetDefaultDungeonRectForSlot(2),
            _ => GetDefaultTownRect()
        };
    }

    public static Vector2 BattlePortalPosition => new(BattlePortalX, PortalY);
    public static Vector2 VillagePortalPosition => new(VillagePortalX, PortalY);
}
