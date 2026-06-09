using System;
using System.Collections;
using Kirurobo;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class DesktopOverlayBootstrap : MonoBehaviour
{
    public static event Action WindowReady;
    public static float TargetWindowWidth { get; private set; }
    public static float TargetWindowHeight { get; private set; }

    [SerializeField] private UniWindowController windowController;
    [SerializeField] private bool matchDisplaySize = true;
    [SerializeField] private Vector2 windowSize = DesktopOverlaySettings.WindowSize;
    [SerializeField] private float bottomMargin = DesktopOverlaySettings.BottomMargin;

    private void Awake()
    {
        if (windowController == null)
            windowController = FindAnyObjectByType<UniWindowController>();

        SyncWindowSize();
        ApplyCameraScale();
    }

    private void Start()
    {
        StartCoroutine(ApplyOverlaySettingsWhenReady());
    }

    public static void RefreshWindowSize()
    {
        var bootstrap = FindAnyObjectByType<DesktopOverlayBootstrap>();
        if (bootstrap == null)
            return;

        bootstrap.SyncWindowSize();
#if !UNITY_EDITOR
        if (bootstrap.windowController != null)
            bootstrap.ApplyOverlaySettings(bootstrap.GetWindowSize());
#endif
        ApplyCameraScale();
        bootstrap.StartCoroutine(bootstrap.ReapplyLayoutAfterResize());
    }

    private IEnumerator ReapplyLayoutAfterResize()
    {
        for (var i = 0; i < 60; i++)
        {
            var size = GetWindowSize();
            if (HasReachedTargetSize(size))
            {
                WorkspaceLayoutController.Instance?.RefreshLayout();
                yield break;
            }

            yield return null;
        }

        WorkspaceLayoutController.Instance?.RefreshLayout();
    }

    private void SyncWindowSize()
    {
        windowSize = GetWindowSize();
    }

    private IEnumerator ApplyOverlaySettingsWhenReady()
    {
        for (var attempt = 0; attempt < 120; attempt++)
        {
            if (windowController == null)
                windowController = FindAnyObjectByType<UniWindowController>();

            SyncWindowSize();
            var size = GetWindowSize();
            if (windowController != null && size.x > 0f && size.y > 0f)
            {
                TargetWindowWidth = size.x;
                TargetWindowHeight = size.y;
#if !UNITY_EDITOR
                ApplyOverlaySettings(size);

                for (var wait = 0; wait < 120; wait++)
                {
                    if (HasReachedTargetSize(size))
                    {
                        NotifyWindowReady();
                        yield break;
                    }

                    yield return null;
                }

                NotifyWindowReady();
                yield break;
#else
                NotifyWindowReady();
                yield break;
#endif
            }

            yield return null;
        }

#if UNITY_EDITOR
        var editorSize = GetWindowSize();
        TargetWindowWidth = editorSize.x;
        TargetWindowHeight = editorSize.y;
        NotifyWindowReady();
#else
        Debug.LogWarning("DesktopOverlayBootstrap: Overlay settings were not applied.", this);
#endif
    }

    private bool HasReachedTargetSize(Vector2 targetSize)
    {
        if (windowController != null && windowController.clientSize.x > 0f)
        {
            return Mathf.Abs(windowController.clientSize.x - targetSize.x) <= 2f
                && Mathf.Abs(windowController.clientSize.y - targetSize.y) <= 2f;
        }

        return Screen.width > 0
            && Mathf.Abs(Screen.width - targetSize.x) <= 2f
            && Mathf.Abs(Screen.height - targetSize.y) <= 2f;
    }

    private void NotifyWindowReady()
    {
        var size = GetWindowSize();
        TargetWindowWidth = size.x;
        TargetWindowHeight = size.y;

        if (windowController != null && windowController.clientSize.x > 0f)
        {
            TargetWindowWidth = windowController.clientSize.x;
            TargetWindowHeight = windowController.clientSize.y;
        }

        ApplyCameraScale();
        WorkspaceLayoutController.Instance?.RefreshLayout();
        WindowReady?.Invoke();
    }

    private static void ApplyCameraScale()
    {
        SideViewCamera.Apply(Camera.main);
    }

    private void ApplyOverlaySettings(Vector2 size)
    {
        windowController.isTransparent = true;
        windowController.isTopmost = true;
        windowController.isHitTestEnabled = true;
        windowController.hitTestType = UniWindowController.HitTestType.Opacity;
        windowController.shouldFitMonitor = false;
        windowController.windowSize = size;
        windowController.windowPosition = GetMonitorBottomLeft(size);
    }

    private Vector2 GetWindowSize()
    {
        if (matchDisplaySize)
            return new Vector2(GetDisplayWidth(), GetDisplayHeight());

        return windowSize;
    }

    public static float GetDisplayWidth()
    {
        var width = (float)Display.main.systemWidth;
        if (width <= 0f)
            width = Screen.currentResolution.width;
        if (width <= 0f)
            width = Screen.width;
        if (width <= 0f)
            width = DesktopOverlaySettings.ReferenceWidth;
        return width;
    }

    public static float GetDisplayHeight()
    {
        var height = (float)Display.main.systemHeight;
        if (height <= 0f)
            height = Screen.currentResolution.height;
        if (height <= 0f)
            height = Screen.height;
        if (height <= 0f)
            height = DesktopOverlaySettings.ReferenceHeight;
        return height;
    }

    public static float GetLayoutWidth()
    {
#if UNITY_EDITOR
        // 에디터: Game 뷰 크기 = 레이아웃 좌표계 (모니터 3440 vs Game 뷰 1920 불일치 방지)
        if (Application.isPlaying && Screen.width > 0)
            return Screen.width;

        if (!Application.isPlaying)
        {
            var gameView = Handles.GetMainGameViewSize();
            if (gameView.x > 0f)
                return gameView.x;
        }
#endif
        if (Application.isPlaying)
        {
            if (TargetWindowWidth > 0f)
                return TargetWindowWidth;
            if (Screen.width > 0)
                return Screen.width;
        }

        return GetDisplayWidth();
    }

    public static float GetLayoutHeight()
    {
#if UNITY_EDITOR
        if (Application.isPlaying && Screen.height > 0)
            return Screen.height;

        if (!Application.isPlaying)
        {
            var gameView = Handles.GetMainGameViewSize();
            if (gameView.y > 0f)
                return gameView.y;
        }
#endif
        if (Application.isPlaying)
        {
            if (TargetWindowHeight > 0f)
                return TargetWindowHeight;
            if (Screen.height > 0)
                return Screen.height;
        }

        return GetDisplayHeight();
    }

    private Vector2 GetMonitorBottomLeft(Vector2 size)
    {
        return DesktopOverlayWindowPlacement.GetMonitorBottomLeft(size, bottomMargin);
    }
}
