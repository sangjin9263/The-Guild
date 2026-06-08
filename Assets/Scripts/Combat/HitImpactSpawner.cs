using UnityEngine;
using UnityEngine.Rendering;

public static class HitImpactSpawner
{
    public static void Spawn(
        GameObject prefab,
        Vector2 hitPoint,
        Vector2 aimDirection,
        SortingGroup referenceGroup,
        float lifetime = 1f)
    {
        if (prefab == null)
            return;

        var angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        var instance = Object.Instantiate(prefab, hitPoint, Quaternion.Euler(0f, 0f, angle));
        instance.SetActive(true);

        var sorting = instance.GetComponent<SyncParticleSorting>();
        if (sorting == null)
            sorting = instance.AddComponent<SyncParticleSorting>();
        sorting.Configure(referenceGroup);

        foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Clear(true);
            ps.Play(true);
        }

        Object.Destroy(instance, lifetime);
    }
}
