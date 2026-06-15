using System.Linq;
using Kirurobo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class DesktopOverlaySetup
{
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const string UniWindowPrefabPath = "Packages/com.kirurobo.uniwinc/Runtime/Prefabs/UniWindowController.prefab";
    private const string DragMoveCanvasPrefabPath = "Packages/com.kirurobo.uniwinc/Runtime/Prefabs/DragMoveCanvas.prefab";
    private const string BuildOutputPath = "Builds/DesktopOverlayTest/TheGuild.exe";
    private const string UrpAssetPath = "Assets/Settings/UniversalRP.asset";

    [MenuItem("The Guild/Desktop Overlay/Apply Project Settings")]
    public static void ApplyProjectSettings()
    {
        PlayerSettings.runInBackground = true;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultIsFullScreen = false;
        PlayerSettings.allowFullscreenSwitch = false;
        PlayerSettings.useFlipModelSwapchain = false;
        PlayerSettings.preserveFramebufferAlpha = true;
        PlayerSettings.defaultScreenWidth = (int)DesktopOverlaySettings.WindowWidth;
        PlayerSettings.defaultScreenHeight = (int)DesktopOverlaySettings.WindowHeight;
        PlayerSettings.SetGraphicsAPIs(
            BuildTarget.StandaloneWindows64,
            new[] { GraphicsDeviceType.Direct3D11 });

        var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
        if (urpAsset != null)
        {
            GraphicsSettings.defaultRenderPipeline = urpAsset;
            QualitySettings.renderPipeline = urpAsset;

            var serializedUrp = new SerializedObject(urpAsset);
            serializedUrp.FindProperty("m_SupportsHDR").boolValue = false;
            serializedUrp.FindProperty("m_AllowPostProcessAlphaOutput").boolValue = true;
            serializedUrp.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(urpAsset);
        }
        else
        {
            Debug.LogWarning($"URP asset not found: {UrpAssetPath}");
        }

        Debug.Log("Desktop overlay project settings applied.");
    }

    [MenuItem("The Guild/Desktop Overlay/Setup Main Scene")]
    public static void SetupMainScene()
    {
        ApplyProjectSettings();

        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        ConfigureMainCamera();
        EnsureDesktopOverlayObjects();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Main scene configured for desktop overlay.");
    }

    [MenuItem("The Guild/Desktop Overlay/Build Windows Test")]
    public static void BuildWindowsTest()
    {
        ApplyProjectSettings();
        PrepareMainSceneForBuild();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainScenePath, true)
        };

        var options = new BuildPlayerOptions
        {
            scenes = new[] { MainScenePath },
            locationPathName = BuildOutputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development | BuildOptions.AllowDebugging
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            Debug.Log($"Desktop overlay test build succeeded: {BuildOutputPath}");
        else
            Debug.LogError($"Desktop overlay test build failed: {report.summary.result}");
    }

    private static void ConfigureMainCamera()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("Main Camera not found in scene.");
            return;
        }

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.allowHDR = false;
        camera.allowMSAA = false;
        camera.orthographic = true;
        camera.orthographicSize = DesktopOverlaySettings.EffectiveOrthographicSize;
        SideViewCamera.Apply(camera);

        var urpCamera = camera.GetComponent<UniversalAdditionalCameraData>();
        if (urpCamera == null)
            urpCamera = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();

        urpCamera.renderPostProcessing = false;
    }

    private static void EnsureDesktopOverlayObjects()
    {
        EnsurePrefabInstance(UniWindowPrefabPath, "UniWindowController");
        EnsurePrefabInstance(DragMoveCanvasPrefabPath, "DragMoveCanvas");

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        var bootstrapObject = GameObject.Find("DesktopOverlay");
        if (bootstrapObject == null)
            bootstrapObject = new GameObject("DesktopOverlay");

        if (bootstrapObject.GetComponent<DesktopOverlayBootstrap>() == null)
            bootstrapObject.AddComponent<DesktopOverlayBootstrap>();

        WorkspaceLayoutSetup.ApplyWorkspaceLayout();

        var windowController = Object.FindFirstObjectByType<UniWindowController>();
        var bootstrap = bootstrapObject.GetComponent<DesktopOverlayBootstrap>();
        if (windowController != null && bootstrap != null)
        {
            var serializedBootstrap = new SerializedObject(bootstrap);
            serializedBootstrap.FindProperty("windowController").objectReferenceValue = windowController;
            serializedBootstrap.FindProperty("windowSize").vector2Value = DesktopOverlaySettings.WindowSize;
            serializedBootstrap.FindProperty("bottomMargin").floatValue = DesktopOverlaySettings.BottomMargin;
            serializedBootstrap.FindProperty("matchDisplaySize").boolValue = true;
            serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void PrepareMainSceneForBuild()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != MainScenePath)
            scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

        ConfigureMainCamera();
        EnsureDesktopOverlayObjects();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void EnsurePrefabInstance(string prefabPath, string objectName)
    {
        if (Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Any(t => t.name == objectName))
            return;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Prefab not found: {prefabPath}");
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = objectName;
    }
}
