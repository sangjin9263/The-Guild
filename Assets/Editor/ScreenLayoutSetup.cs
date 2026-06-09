using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ScreenLayoutSetup
{
    private const string MainScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("The Guild/Layout/Setup Screen Layout In Main")]
    public static void SetupScreenLayoutInMain()
    {
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        WorkspaceLayoutSetup.ApplyWorkspaceLayout();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Workspace screen layout applied to Main scene.");
    }

    [MenuItem("The Guild/Layout/Setup Screen Layout")]
    public static void SetupScreenLayoutInActiveScene()
    {
        WorkspaceLayoutSetup.ApplyWorkspaceLayout();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Workspace screen layout applied.");
    }
}
