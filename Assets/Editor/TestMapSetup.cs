using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

public static class TestMapSetup
{
    private const string ScenePath = "Assets/Scenes/TestMap.unity";
    private const string CainosPalettePath =
        "Assets/Store/Cainos/Pixel Art Platformer - Village Props/Tileset Palette/TP Ground.prefab";
    private const string ProjectPaletteFolder = "Assets/Art/TilePalettes";
    private const string ProjectPalettePath = ProjectPaletteFolder + "/Ground Palette.prefab";

    [MenuItem("The Guild/Map/Setup TestMap Scene")]
    public static void SetupTestMapScene()
    {
        EnsureProjectPalette();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ConfigureCamera();
        ZoneLayoutSetup.ApplyZoneLayout(paintStarterGroundIfEmpty: true);
        var combatTilemap = ZoneLayoutSetup.EnsureTilemap("Combat_Ground");
        ConfigureTilePalette(combatTilemap);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();

        EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
        Selection.activeGameObject = combatTilemap.gameObject;

        Debug.Log($"TestMap scene ready: {ScenePath}. Zone layout applied.");
    }

    private static void EnsureProjectPalette()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art"))
            AssetDatabase.CreateFolder("Assets", "Art");

        if (!AssetDatabase.IsValidFolder(ProjectPaletteFolder))
            AssetDatabase.CreateFolder("Assets/Art", "TilePalettes");

        if (AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPalettePath) != null)
            return;

        if (!AssetDatabase.CopyAsset(CainosPalettePath, ProjectPalettePath))
            Debug.LogWarning($"Could not copy palette to {ProjectPalettePath}. Using Cainos palette directly.");
    }

    private static void ConfigureCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";

        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.orthographic = true;
        camera.orthographicSize = DesktopOverlaySettings.EffectiveOrthographicSize;
        SideViewCamera.Apply(camera);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        var urpCamera = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        urpCamera.renderPostProcessing = false;
    }

    private static void ConfigureTilePalette(Tilemap groundTilemap)
    {
        var palette = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPalettePath);
        if (palette == null)
            palette = AssetDatabase.LoadAssetAtPath<GameObject>(CainosPalettePath);

        if (palette == null)
        {
            Debug.LogWarning("Ground tile palette prefab not found.");
            return;
        }

        GridPaintingState.palette = palette;
        GridPaintingState.scenePaintTarget = groundTilemap.gameObject;
    }
}
