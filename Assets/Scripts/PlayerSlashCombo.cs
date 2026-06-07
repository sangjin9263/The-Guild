using UnityEngine;
using UnityEngine.Rendering;

public class PlayerSlashCombo : MonoBehaviour
{
    [SerializeField] private GameObject slashA;
    [SerializeField] private GameObject slashB;
    [Header("Slash A (1st click)")]
    [SerializeField] private Vector3 slashAPosition = Vector3.zero;
    [SerializeField] private Vector3 slashARotation = new(0f, 180f, 330f);
    [SerializeField] private Vector3 slashAScale = Vector3.one;

    [Header("Slash B (2nd click)")]
    [SerializeField] private Vector3 slashBPosition = Vector3.zero;
    [SerializeField] private Vector3 slashBRotation = new(0f, 0f, 150f);
    [SerializeField] private Vector3 slashBScale = Vector3.one;

    [Header("Common")]
    [SerializeField] private Vector3 slashOffset = Vector3.zero;
    [SerializeField] private float comboResetTime = 1f;
    [SerializeField] private float vfxLifetime = 2f;
    [SerializeField] private SPUM_Prefabs spumPrefabs;

    private int comboStep;
    private float lastAttackTime = float.MinValue;

    private void Awake()
    {
        if (spumPrefabs == null)
            spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (Time.time - lastAttackTime > comboResetTime)
            comboStep = 0;

        var prefab = comboStep == 0 ? slashA : slashB;
        var position = comboStep == 0 ? slashAPosition : slashBPosition;
        var rotation = comboStep == 0 ? slashARotation : slashBRotation;
        var scale = comboStep == 0 ? slashAScale : slashBScale;
        comboStep = (comboStep + 1) % 2;
        lastAttackTime = Time.time;

        if (prefab == null)
            return;

        SpawnSlash(prefab, position, rotation, scale);
    }

    private void SpawnSlash(GameObject prefab, Vector3 localPosition, Vector3 eulerAngles, Vector3 scale)
    {
        bool facingRight = spumPrefabs != null && spumPrefabs.transform.localScale.x < 0f;
        var offset = slashOffset + localPosition;
        var euler = eulerAngles;
        var finalScale = scale;

        if (facingRight)
        {
            offset.x = -offset.x;
            euler.z = -euler.z;
            finalScale.x = -Mathf.Abs(scale.x);
        }

        var spawnPos = transform.position + offset;
        var instance = Instantiate(prefab, spawnPos, Quaternion.Euler(euler));
        instance.SetActive(true);
        instance.transform.localScale = finalScale;

        var sorting = instance.GetComponent<SyncParticleSorting>();
        if (sorting == null)
            sorting = instance.AddComponent<SyncParticleSorting>();
        sorting.Configure(GetComponent<SortingGroup>());

        foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Clear(true);
            ps.Play(true);
        }

        Destroy(instance, vfxLifetime);
    }
}
