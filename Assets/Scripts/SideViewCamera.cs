using UnityEngine;

/// <summary>
/// 2D 사이드뷰용 카메라 크기와 위치를 DesktopOverlaySettings 기준으로 맞춥니다.
/// </summary>
public static class SideViewCamera
{
    public static void Apply(Camera camera)
    {
        if (camera == null)
            return;

        camera.orthographic = true;
        camera.transform.rotation = Quaternion.identity;
        camera.orthographicSize = DesktopOverlaySettings.GetReferenceOrthographicSize();
        camera.rect = DesktopOverlaySettings.GetLayoutCameraViewport();
        camera.transform.position = new Vector3(0f, 0f, -10f);
    }
}
