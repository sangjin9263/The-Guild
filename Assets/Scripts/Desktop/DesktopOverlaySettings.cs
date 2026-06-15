using UnityEngine;

/// <summary>
/// Reference monitor layout (1920x1080) with draggable Town / UI / Dungeon panels.
/// </summary>
public static class DesktopOverlaySettings
{
    public const float ReferenceWidth = 1920f;
    public const float ReferenceHeight = 1080f;

    public static float ReferenceAspect => ReferenceWidth / ReferenceHeight;

    public const float TownPanelWidth = 640f;
    public const float TownPanelHeight = 480f;
    public const float UiZonePanelWidth = 920f;
    public const float UiZonePanelHeight = 600f;
    public const float AuctionPanelWidth = 1000f;
    public const float AuctionPanelHeight = 600f;
    public const float DungeonSlotWidth = 640f;
    public const float DungeonSlotHeight = 240f;
    public const float PanelGap = 20f;

    public const int MaxDungeonSlots = 4;
    public const int DungeonSlotCount = MaxDungeonSlots;

    /// <summary>1920×1080 기준 고정 패널 배치 (LayoutRoot 좌표, 왼쪽 아래 원점).</summary>
    public const float DefaultTownX = 1280f;
    public const float DefaultAuctionX = 0f;
    public const float DefaultAuctionY = 480f;
    public const float DefaultUiZoneX = 1000f;
    public const float DefaultUiZoneY = 480f;

    public const float CameraOrthographicSize = 6f;
    public const float ContentScale = 1f;
    public const float BottomMargin = 0f;
    public const float PlatformerGroundBottomPadding = 0.75f;
    public const float PortalVisualScale = 1.5f;
    public const float ReferencePixelsPerWorldUnit = 30f;

    public const float DungeonMinX = -32f;
    public const float DungeonMaxX = -2f;
    public const float BuildingMinX = 2f;
    public const float BuildingMaxX = 32f;

    public const float BattlePortalX = -3f;
    public const float VillagePortalX = 26f;
    public const float PortalY = -1.5f;

    /// <summary>던전 1(Dungeon3Content) Portals/Portal 로컬 좌표.</summary>
    public const float PrimaryDungeonPortalLocalX = 9.7f;
    public const float PrimaryDungeonPortalLocalY = -1.5f;

    /// <summary>패널 월드 Y 튜닝 기본값.</summary>
    public const float DefaultTownContentWorldY = -11.99261f;
    public const float DefaultDungeon3ContentWorldY = -14.13515f;

    public const int GroundMinY = -6;
    public const int CombatGroundMaxY = -2;
    public const int VillageGroundMaxY = 3;

    public static float ReferenceOrthographicSize =>
        ReferenceHeight / (2f * ReferencePixelsPerWorldUnit);

    public static float EffectiveOrthographicSize => ReferenceOrthographicSize * ContentScale;

    public static float WindowWidth => ReferenceWidth;
    public static float WindowHeight => ReferenceHeight;

    public static Vector2 WindowSize => new(ReferenceWidth, ReferenceHeight);

    /// <summary>
    /// 패널 UI 픽셀 크기(640×480 등)는 고정하고, 배치·드래그 좌표계는 모니터 전체를 씁니다.
    /// </summary>
    public const bool UseFixedPanelLayout = true;

    /// <summary>
    /// 고정 레이아웃 좌표계(ReferenceWidth×ReferenceHeight). UI 패널과 월드 매핑에 동일하게 사용.
    /// </summary>
    public static float GetLayoutBoundsWidth() =>
        UseFixedPanelLayout
            ? ReferenceWidth
            : DesktopOverlayBootstrap.GetLayoutWidth();

    /// <summary>
    /// 고정 레이아웃 좌표계 높이.
    /// </summary>
    public static float GetLayoutBoundsHeight() =>
        UseFixedPanelLayout
            ? ReferenceHeight
            : DesktopOverlayBootstrap.GetLayoutHeight();

    public static Vector2 GetLayoutBoundsSize() =>
        new(GetLayoutBoundsWidth(), GetLayoutBoundsHeight());

    /// <summary>
    /// LayoutRoot 좌표 = 화면 좌표 (왼쪽 아래 원점).
    /// </summary>
    public static Vector2 GetLayoutScreenOrigin() => Vector2.zero;

    public static void ApplyLayoutRootTransform(RectTransform layoutRoot)
    {
        if (layoutRoot == null)
            return;

        var bounds = GetLayoutBoundsSize();
        layoutRoot.anchorMin = Vector2.zero;
        layoutRoot.anchorMax = Vector2.zero;
        layoutRoot.pivot = Vector2.zero;
        layoutRoot.anchoredPosition = Vector2.zero;
        layoutRoot.sizeDelta = bounds;
    }

    public static void ApplyFullScreenOverlayRoot(RectTransform overlayRoot)
    {
        if (overlayRoot == null)
            return;

        overlayRoot.anchorMin = Vector2.zero;
        overlayRoot.anchorMax = Vector2.one;
        overlayRoot.pivot = new Vector2(0.5f, 0.5f);
        overlayRoot.offsetMin = Vector2.zero;
        overlayRoot.offsetMax = Vector2.zero;
    }

    public static float GetDefaultGroundSurfaceY() =>
        CombatGroundMaxY + 0.5f;

    public static float GetReferenceToScreenScale()
    {
        if (UseFixedPanelLayout)
            return 1f;

        if (Screen.height <= 0)
            return 1f;

        return Screen.height / ReferenceHeight;
    }

    /// <summary>
    /// 모니터 레이아웃 좌표계에서 referenceRect를 고정 픽셀 크기로 배치합니다 (LayoutRoot 자식용).
    /// </summary>
    public static void ApplyFixedReferenceRect(
        RectTransform target,
        WorkspacePanelRect rect,
        Vector2 inset = default)
    {
        if (target == null)
            return;

        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.zero;
        target.pivot = Vector2.zero;
        target.anchoredPosition = new Vector2(rect.x + inset.x, rect.y + inset.y);
        target.sizeDelta = new Vector2(
            Mathf.Max(0f, rect.width - inset.x * 2f),
            Mathf.Max(0f, rect.height - inset.y * 2f));
    }

    /// <summary>
    /// 화면 전체 Canvas 위에 LayoutRoot 좌표계로 패널을 배치합니다 (BuildingUICanvas 등).
    /// </summary>
    public static void ApplyFixedReferenceRectOnScreen(
        RectTransform target,
        WorkspacePanelRect rect,
        Vector2 inset = default)
    {
        if (target == null)
            return;

        var origin = GetLayoutScreenOrigin();
        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.zero;
        target.pivot = Vector2.zero;
        target.anchoredPosition = new Vector2(
            origin.x + rect.x + inset.x,
            origin.y + rect.y + inset.y);
        target.sizeDelta = new Vector2(
            Mathf.Max(0f, rect.width - inset.x * 2f),
            Mathf.Max(0f, rect.height - inset.y * 2f));
    }

    public static WorkspacePanelRect ReadFixedReferenceRect(RectTransform target)
    {
        if (target == null)
            return default;

        return new WorkspacePanelRect(
            target.anchoredPosition.x,
            target.anchoredPosition.y,
            target.sizeDelta.x,
            target.sizeDelta.y);
    }

    /// <summary>
    /// 월드 렌더링을 UI와 같은 모니터 전체 영역에 맞춥니다.
    /// </summary>
    public static Rect GetLayoutCameraViewport() => new(0f, 0f, 1f, 1f);

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
        if (UseFixedPanelLayout)
        {
            var layoutHeight = GetLayoutBoundsHeight();
            if (layoutHeight > 0f)
                return GetLayoutBoundsWidth() / layoutHeight;

            return ReferenceAspect;
        }

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
        var aspect = camera != null && camera.pixelHeight > 0
            ? camera.aspect
            : ResolveLayoutAspect(camera);
        var worldWidth = orthoSize * 2f * aspect;
        var worldHeight = orthoSize * 2f;
        var layoutWidth = Mathf.Max(1f, GetLayoutBoundsWidth());
        var layoutHeight = Mathf.Max(1f, GetLayoutBoundsHeight());
        var normalizedX = referenceX / layoutWidth - 0.5f;
        var normalizedY = referenceY / layoutHeight - 0.5f;
        worldX = normalizedX * worldWidth;
        worldY = normalizedY * worldHeight;
    }

    /// <summary>레이아웃 Y(패널 하단 등) → 월드 Y. 인접 패널 지면 정렬에 사용.</summary>
    public static float ReferenceYToWorld(float referenceY, Camera camera = null)
    {
        camera ??= ResolveLayoutCamera();
        ReferenceToWorld(0f, referenceY, camera, out _, out var worldY);
        return worldY;
    }

    /// <summary>모든 패널 월드 콘텐츠에 공통 적용하는 레이아웃→월드 균일 스케일.</summary>
    public static float GetLayoutToWorldUniform(Camera camera = null)
    {
        camera ??= ResolveLayoutCamera();
        var orthoSize = camera != null ? camera.orthographicSize : EffectiveOrthographicSize;
        var layoutHeight = Mathf.Max(1f, GetLayoutBoundsHeight());
        return (orthoSize * 2f) / layoutHeight;
    }

    public static float GetPanelWorldScale(WorkspacePanelRect rect, float referencePanelHeight)
    {
        if (referencePanelHeight <= 0f)
            return 1f;

        return rect.height / referencePanelHeight;
    }

    public static WorkspacePanelRect GetDefaultTownRect() =>
        new(DefaultTownX, 0f, TownPanelWidth, TownPanelHeight);

    public static WorkspacePanelRect GetDefaultUiZoneRect() =>
        new(DefaultUiZoneX, DefaultUiZoneY, UiZonePanelWidth, UiZonePanelHeight);

    public static WorkspacePanelRect GetDefaultAuctionRect() =>
        new(DefaultAuctionX, DefaultAuctionY, AuctionPanelWidth, AuctionPanelHeight);

    /// <summary>DungeonPanel_N ↔ slotIndex N-1.</summary>
    public static WorkspacePanelRect GetDefaultDungeonRectForSlot(int slotIndex) => slotIndex switch
    {
        0 => new WorkspacePanelRect(0f, 240f, DungeonSlotWidth, DungeonSlotHeight),
        1 => new WorkspacePanelRect(640f, 240f, DungeonSlotWidth, DungeonSlotHeight),
        2 => new WorkspacePanelRect(640f, 0f, DungeonSlotWidth, DungeonSlotHeight),
        3 => new WorkspacePanelRect(0f, 0f, DungeonSlotWidth, DungeonSlotHeight),
        _ => new WorkspacePanelRect(640f, 0f, DungeonSlotWidth, DungeonSlotHeight)
    };

    /// <summary>
    /// slot 2(DungeonPanel_3)=던전 1부터 순차 해금. slot 3(DungeonPanel_4)=4번째 해금.
    /// </summary>
    public static bool IsDungeonSlotUnlocked(int slotIndex, int unlockedSlotCount)
    {
        if (slotIndex < 0 || slotIndex >= DungeonSlotCount)
            return false;

        if (slotIndex == 3)
            return unlockedSlotCount >= 4;

        return slotIndex >= 3 - Mathf.Clamp(unlockedSlotCount, 1, 3);
    }

    /// <summary>
    /// slot 2=던전 1(DungeonPanel_3), slot 1=던전 2, slot 0=던전 3, slot 3=던전 4(DungeonPanel_4).
    /// </summary>
    public static int GetDungeonDisplayNumber(int slotIndex) =>
        slotIndex == 3 ? 4 : 3 - slotIndex;

    public static string GetDungeonDisplayLabel(int slotIndex) =>
        $"던전 {GetDungeonDisplayNumber(slotIndex)}";

    public static string GetDungeonDisplayName(int slotIndex) =>
        $"Dungeon {GetDungeonDisplayNumber(slotIndex)}";

    public static WorkspacePanelRect GetDefaultPanelRect(string panelId)
    {
        return panelId switch
        {
            WorkspacePanelId.Town => GetDefaultTownRect(),
            WorkspacePanelId.UiZone => GetDefaultUiZoneRect(),
            WorkspacePanelId.Auction => GetDefaultAuctionRect(),
            WorkspacePanelId.DungeonSlot1 => GetDefaultDungeonRectForSlot(0),
            WorkspacePanelId.DungeonSlot2 => GetDefaultDungeonRectForSlot(1),
            WorkspacePanelId.DungeonSlot3 => GetDefaultDungeonRectForSlot(2),
            WorkspacePanelId.DungeonSlot4 => GetDefaultDungeonRectForSlot(3),
            _ => GetDefaultTownRect()
        };
    }

    public static Vector2 BattlePortalPosition => new(BattlePortalX, PortalY);
    public static Vector2 VillagePortalPosition => new(VillagePortalX, PortalY);
}
