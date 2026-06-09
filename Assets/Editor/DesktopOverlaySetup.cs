using System.Linq;
using System.Reflection;
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

    [MenuItem("The Guild/Desktop Overlay/Setup Game View Preview")]
    public static void SetupGameViewPreview()
    {
        var width = (int)DesktopOverlaySettings.WindowWidth;
        var height = (int)DesktopOverlaySettings.WindowHeight;
        const string label = "Guild Overlay";

        var index = FindOrAddGameViewSize(width, height, label);
        if (index >= 0)
            SetGameViewSize(index);

        Debug.Log(
            "Game view preview configured.\n" +
            $"- Aspect: {width} x {height} (Guild Overlay)\n" +
            "- Develop at 1920x1080, then add a 3440x1440 (or your monitor) Game view size to verify ultrawide.\n" +
            "- Builds use the full monitor when Match Display Size is enabled on DesktopOverlay.");
    }

    private static int FindOrAddGameViewSize(int width, int height, string label)
    {
        var index = FindGameViewSize(GameViewSizeGroupType.Standalone, width, height);
        if (index >= 0)
            return index;

        var group = GetGameViewSizeGroup(GameViewSizeGroupType.Standalone);
        if (group == null)
            return -1;

        var groupType = group.GetType();
        var addCustomSize = groupType.GetMethod("AddCustomSize");
        if (addCustomSize == null)
            return -1;

        var gameViewSizeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
        var gameViewSizeEnumType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
        if (gameViewSizeType == null || gameViewSizeEnumType == null)
            return -1;

        var constructor = gameViewSizeType.GetConstructor(new[]
        {
            gameViewSizeEnumType,
            typeof(int),
            typeof(int),
            typeof(string)
        });

        if (constructor == null)
            return -1;

        var sizeType = System.Enum.Parse(gameViewSizeEnumType, "FixedResolution");
        var newSize = constructor.Invoke(new object[] { sizeType, width, height, label });
        addCustomSize.Invoke(group, new object[] { newSize });
        return FindGameViewSize(GameViewSizeGroupType.Standalone, width, height);
    }

    private static int FindGameViewSize(GameViewSizeGroupType sizeGroupType, int width, int height)
    {
        var group = GetGameViewSizeGroup(sizeGroupType);
        if (group == null)
            return -1;

        var groupType = group.GetType();
        var getBuiltinCount = groupType.GetMethod("GetBuiltinCount");
        var getCustomCount = groupType.GetMethod("GetCustomCount");
        var sizesCount = (int)getBuiltinCount.Invoke(group, null) + (int)getCustomCount.Invoke(group, null);
        var getGameViewSize = groupType.GetMethod("GetGameViewSize");
        var gameViewSizeType = getGameViewSize.ReturnType;
        var widthProp = gameViewSizeType.GetProperty("width");
        var heightProp = gameViewSizeType.GetProperty("height");
        var indexValue = new object[1];

        for (var i = 0; i < sizesCount; i++)
        {
            indexValue[0] = i;
            var size = getGameViewSize.Invoke(group, indexValue);
            if ((int)widthProp.GetValue(size) == width && (int)heightProp.GetValue(size) == height)
                return i;
        }

        return -1;
    }

    private static object GetGameViewSizeGroup(GameViewSizeGroupType type)
    {
        var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var instance = singletonType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static)
            ?.GetValue(null);
        var getGroup = sizesType?.GetMethod("GetGroup");
        return getGroup?.Invoke(instance, new object[] { (int)type });
    }

    private static void SetGameViewSize(int index)
    {
        var gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var selectedSizeIndexProp = gameViewType?.GetProperty(
            "selectedSizeIndex",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var gameView = EditorWindow.GetWindow(gameViewType);
        selectedSizeIndexProp?.SetValue(gameView, index);
        gameView.Repaint();
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
