using UnityEngine;

public class SpumIdleUnit : MonoBehaviour
{
    [SerializeField] private SPUM_Prefabs spumPrefabs;

    private void Start()
    {
        if (GetComponent<MonsterAttackController>() != null)
            return;

        if (spumPrefabs == null)
            spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();

        if (spumPrefabs == null)
            return;

        var rect = spumPrefabs.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = Vector2.zero;

        if (!spumPrefabs.allListsHaveItemsExist())
            spumPrefabs.PopulateAnimationLists();

        spumPrefabs.OverrideControllerInit();
        spumPrefabs.PlayAnimation(PlayerState.IDLE, 0);
    }
}
