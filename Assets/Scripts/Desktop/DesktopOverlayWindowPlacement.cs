using System;
using System.Runtime.InteropServices;
using UnityEngine;

public static class DesktopOverlayWindowPlacement
{
    public static Vector2 GetMonitorBottomLeft(Vector2 windowSize, float extraBottomMargin)
    {
        var bottomLeft = GetWorkAreaBottomLeft();
        return new Vector2(bottomLeft.x, bottomLeft.y + extraBottomMargin);
    }

    public static Vector2 GetBottomStripPosition(Vector2 windowSize, float extraBottomMargin) =>
        GetMonitorBottomLeft(windowSize, extraBottomMargin);

    private static Vector2 GetWorkAreaBottomLeft()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (TryGetWindowsWorkAreaBottomLeft(out var bottomLeft))
            return bottomLeft;
#endif

        return Vector2.zero;
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private const int SpiGetWorkArea = 0x0030;

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool SystemParametersInfo(int uiAction, int uiParam, ref Rect rect, int fWinIni);

    private static bool TryGetWindowsWorkAreaBottomLeft(out Vector2 bottomLeft)
    {
        bottomLeft = Vector2.zero;

        var workArea = new Rect();
        if (!SystemParametersInfo(SpiGetWorkArea, 0, ref workArea, 0))
            return false;

        var screenHeight = Display.main.systemHeight;
        if (screenHeight <= 0)
            screenHeight = Screen.currentResolution.height;

        bottomLeft.x = workArea.Left;
        bottomLeft.y = screenHeight - workArea.Bottom;
        return true;
    }
#endif
}
