#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TownBuildingSetup
{
    private const string GuildPrefabPath = "Assets/Resources/Prefabs/BuildingLV5.prefab";
    private const string AuctionPrefabPath = "Assets/Resources/Prefabs/Auction_houseLV5.prefab";

    private static readonly Vector3 GuildLocalPosition = new(8.6f, 0.65f, 0f);
    private static readonly Vector3 GuildLocalScale = new(0.8f, 0.8f, 0.8f);
    private static readonly Vector3 AuctionLocalPosition = new(2f, -1.1f, 0f);
    private static readonly Vector3 AuctionLocalScale = new(0.6f, 0.6f, 0.6f);

    [MenuItem("The Guild/Map/Place Town Buildings LV5")]
    public static void PlaceTownBuildingsLv5()
    {
        var town = Object.FindFirstObjectByType<TownPanelContent>(FindObjectsInactive.Include);
        if (town == null)
        {
            Debug.LogWarning("TownPanelContent not found.");
            return;
        }

        var props = town.transform.Find("Props");
        if (props == null)
        {
            Debug.LogWarning("Town Props not found.");
            return;
        }

        ClearLegacyBuildings(props);

        var guildPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GuildPrefabPath);
        var auctionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AuctionPrefabPath);
        if (guildPrefab == null || auctionPrefab == null)
        {
            Debug.LogError("Missing BuildingLV5 or Auction_houseLV5 prefab.");
            return;
        }

        PlacePrefab(props, guildPrefab, GuildLocalPosition, GuildLocalScale);
        PlacePrefab(props, auctionPrefab, AuctionLocalPosition, AuctionLocalScale);

        WorkspaceLayoutSetup.EnsureAuctionWorkspaceUi();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[TownBuildingSetup] Placed BuildingLV5 + Auction_houseLV5 under Town Props.");
    }

    private static void ClearLegacyBuildings(Transform props)
    {
        for (var i = props.childCount - 1; i >= 0; i--)
        {
            var child = props.GetChild(i);
            if (child.GetComponent<SpriteRenderer>() == null)
                continue;

            var name = child.name;
            if (name.StartsWith("Building") || name.StartsWith("Auction"))
                Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void PlacePrefab(Transform parent, GameObject prefab, Vector3 localPosition, Vector3 localScale)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = localScale;
    }

    private static void FixGuildPrefab()
    {
        var path = GuildPrefabPath;
        var prefabRoot = PrefabUtility.LoadPrefabContents(path);
        var clickable = prefabRoot.GetComponent<BuildingClickable>();
        if (clickable != null)
        {
            var serialized = new SerializedObject(clickable);
            serialized.FindProperty("buildingTitle").stringValue = "헌터 길드";
            serialized.FindProperty("buildingDescription").stringValue =
                "HUNTER COMPANY\n용병 관리 · 파견. (준비 중)";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    private static void FixAuctionPrefab()
    {
        var path = AuctionPrefabPath;
        var prefabRoot = PrefabUtility.LoadPrefabContents(path);

        if (prefabRoot.GetComponent<AuctionBuildingClickable>() == null)
            prefabRoot.AddComponent<AuctionBuildingClickable>();

        var collider = prefabRoot.GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = prefabRoot.AddComponent<BoxCollider2D>();

        var spriteRenderer = prefabRoot.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            var bounds = spriteRenderer.sprite.bounds;
            collider.size = bounds.size;
            collider.offset = bounds.center;
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }
}
#endif
