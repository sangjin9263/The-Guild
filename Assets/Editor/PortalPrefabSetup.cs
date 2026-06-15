#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>색상별 portal 스프라이트 1~9 루프 애니메이션 프리팹 생성.</summary>
public static class PortalPrefabSetup
{
    private const string PortalsRoot = "Assets/Resources/building/Portals";
    private const string AnimFolder = "Assets/Art/Ani";
    private const string PrefabFolder = "Assets/Resources/Prefabs";

    private readonly struct PortalVariant
    {
        public readonly int Index;
        public readonly string ColorFolder;
        public readonly string SpritePrefix;

        public PortalVariant(int index, string colorFolder, string spritePrefix)
        {
            Index = index;
            ColorFolder = colorFolder;
            SpritePrefix = spritePrefix;
        }

        public string PrefabName => $"Dimensional_Portal_{Index}";
        public string AnimPath => $"{AnimFolder}/Portal{Index}.anim";
        public string ControllerPath => $"{AnimFolder}/Portal{Index}.controller";
        public string PrefabPath => $"{PrefabFolder}/{PrefabName}.prefab";
        public string SpriteFolder => $"{PortalsRoot}/{ColorFolder}";
    }

    private static readonly PortalVariant[] Variants =
    {
        new(0, "red", "red_portal"),
        new(1, "green", "green_portal"),
        new(2, "blue", "portal2"),
        new(3, "black", "black_portal"),
        new(4, "orange", "orange_portal"),
        new(5, "purple", "purple_portal"),
        new(6, "white", "white_portal"),
    };

    [MenuItem("The Guild/Map/Create All Portal Prefabs")]
    public static void CreateAllPortalPrefabsFromMenu()
    {
        if (!CreateAllPortalPrefabs())
            return;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PortalPrefab] Created Dimensional_Portal_0~6 (red, green, blue, black, orange, purple, white).");
    }

    public static bool CreateAllPortalPrefabs()
    {
        var ok = true;
        foreach (var variant in Variants)
        {
            if (!CreatePortalPrefab(variant))
                ok = false;
        }

        return ok;
    }

    public static bool CreatePortalPrefab(int index)
    {
        foreach (var variant in Variants)
        {
            if (variant.Index != index)
                continue;

            return CreatePortalPrefab(variant);
        }

        Debug.LogError($"[PortalPrefab] Unknown portal index: {index}");
        return false;
    }

    private static bool CreatePortalPrefab(PortalVariant variant)
    {
        if (!LoadPortalSprites(variant, out var sprites))
            return false;

        var clip = CreateLoopClip(sprites, $"Portal{variant.Index}");
        SaveAnimationClip(clip, variant.AnimPath);

        var controller = EnsureController(clip, variant.ControllerPath);
        CreatePrefabAsset(variant, sprites[0], controller);
        return true;
    }

    private static bool LoadPortalSprites(PortalVariant variant, out Sprite[] sprites)
    {
        sprites = new Sprite[9];
        for (var i = 1; i <= 9; i++)
        {
            var texturePath = $"{variant.SpriteFolder}/{variant.SpritePrefix}_{i}.png";
            sprites[i - 1] = LoadPrimarySprite(texturePath, $"{variant.SpritePrefix}_{i}");
            if (sprites[i - 1] == null)
            {
                Debug.LogError($"[PortalPrefab] Missing sprite: {texturePath}");
                return false;
            }
        }

        return true;
    }

    private static Sprite LoadPrimarySprite(string texturePath, string baseName)
    {
        var direct = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
        if (direct != null && (direct.name == baseName || direct.name.StartsWith(baseName)))
            return direct;

        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(texturePath))
        {
            if (asset is not Sprite sprite)
                continue;

            if (sprite.name == $"{baseName}_0" || sprite.name == baseName)
                return sprite;
        }

        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(texturePath))
        {
            if (asset is Sprite sprite)
                return sprite;
        }

        return null;
    }

    private static AnimationClip CreateLoopClip(Sprite[] sprites, string clipName)
    {
        var clip = new AnimationClip { frameRate = 12f };
        clip.name = clipName;

        var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[sprites.Length];
        var step = 1f / sprites.Length;

        for (var i = 0; i < sprites.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i * step,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        return clip;
    }

    private static AnimatorController EnsureController(AnimationClip clip, string controllerPath)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var root = controller.layers[0].stateMachine;
            var loopState = root.AddState("PortalLoop");
            loopState.motion = clip;
            root.defaultState = loopState;
            return controller;
        }

        var stateMachine = controller.layers[0].stateMachine;
        foreach (var childState in stateMachine.states)
        {
            if (childState.state.name != "PortalLoop")
                continue;

            childState.state.motion = clip;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        var state = stateMachine.AddState("PortalLoop");
        state.motion = clip;
        stateMachine.defaultState = state;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void CreatePrefabAsset(PortalVariant variant, Sprite defaultSprite, RuntimeAnimatorController controller)
    {
        var root = new GameObject(variant.PrefabName);

        var spriteRenderer = root.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = defaultSprite;
        spriteRenderer.sortingOrder = 20;

        var animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(variant.PrefabPath);
        if (existing != null)
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, variant.PrefabPath, InteractionMode.AutomatedAction);
        else
            PrefabUtility.SaveAsPrefabAsset(root, variant.PrefabPath);

        Object.DestroyImmediate(root);
    }

    private static void SaveAnimationClip(AnimationClip clip, string animPath)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath) != null)
            AssetDatabase.DeleteAsset(animPath);

        AssetDatabase.CreateAsset(clip, animPath);
    }
}
#endif
