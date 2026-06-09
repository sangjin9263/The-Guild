using UnityEngine;
using UnityEngine.Tilemaps;

public static class SideViewCamera
{
    public static void Apply(Camera camera, Tilemap combatGround = null)
    {
        if (camera == null)
            return;

        camera.orthographic = true;
        camera.transform.rotation = Quaternion.identity;
        camera.orthographicSize = DesktopOverlaySettings.GetReferenceOrthographicSize();
        camera.rect = new Rect(0f, 0f, 1f, 1f);
        camera.transform.position = new Vector3(0f, 0f, -10f);
    }
}
