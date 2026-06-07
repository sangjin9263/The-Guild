using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class SyncParticleSorting : MonoBehaviour
{
    [SerializeField] private string sortingLayerName = "Layer 1";
    [SerializeField] private int sortingOrderOffset = 20;
    [SerializeField] private SortingGroup referenceGroup;

    public void Configure(SortingGroup group)
    {
        referenceGroup = group;
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void Apply()
    {
        var group = referenceGroup;
        if (group == null && transform.parent != null)
            group = transform.parent.GetComponent<SortingGroup>();

        string layer = group != null
            ? SortingLayer.IDToName(group.sortingLayerID)
            : sortingLayerName;
        int order = group != null
            ? group.sortingOrder + sortingOrderOffset
            : sortingOrderOffset;

        foreach (var renderer in GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            renderer.sortingLayerName = layer;
            renderer.sortingOrder = order;
        }

        foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.sortingLayerName = layer;
            renderer.sortingOrder = order;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!isActiveAndEnabled)
            return;

        Apply();
    }
#endif
}
