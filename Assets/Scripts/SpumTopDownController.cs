using UnityEngine;

public class SpumTopDownController : MonoBehaviour
{
    public float speed = 3f;
    public SPUM_Prefabs spumPrefabs;

    private Rigidbody2D rb;
    private float facingScaleX = 1f;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (spumPrefabs == null)
            spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();

        if (spumPrefabs == null)
            return;

        facingScaleX = Mathf.Abs(spumPrefabs.transform.localScale.x);
        if (facingScaleX < 0.01f)
            facingScaleX = 1f;

        if (!spumPrefabs.allListsHaveItemsExist())
            spumPrefabs.PopulateAnimationLists();
        spumPrefabs.OverrideControllerInit();
        spumPrefabs.PlayAnimation(PlayerState.IDLE, 0);
    }

    private void Update()
    {
        if (spumPrefabs == null || rb == null)
            return;

        Vector2 dir = Vector2.zero;
        if (Input.GetKey(KeyCode.A))
            dir.x = -1;
        else if (Input.GetKey(KeyCode.D))
            dir.x = 1;

        if (Input.GetKey(KeyCode.W))
            dir.y = 1;
        else if (Input.GetKey(KeyCode.S))
            dir.y = -1;

        dir.Normalize();
        rb.linearVelocity = speed * dir;

        if (dir.x > 0f)
            SetFacing(-facingScaleX);
        else if (dir.x < 0f)
            SetFacing(facingScaleX);

        spumPrefabs.PlayAnimation(dir.sqrMagnitude > 0f ? PlayerState.MOVE : PlayerState.IDLE, 0);
    }

    private void SetFacing(float scaleX)
    {
        var scale = spumPrefabs.transform.localScale;
        spumPrefabs.transform.localScale = new Vector3(scaleX, scale.y, scale.z);
    }
}
