using UnityEngine;

[DisallowMultipleComponent]
public class AttackTelegraphVisual : MonoBehaviour
{
    [SerializeField] private float lineWidth = 0.04f;
    [SerializeField] private Color outerColor = new(1f, 0.12f, 0.12f, 0.95f);
    [SerializeField] private Color innerColor = new(1f, 0.35f, 0.35f, 0.95f);
    [SerializeField] private string sortingLayerName = "Layer 1";
    [SerializeField] private int sortingOrder = 0;

    private static Material sharedLineMaterial;

    private LineRenderer outerLeft;
    private LineRenderer outerRight;
    private LineRenderer innerLeft;
    private LineRenderer innerRight;

    private float corridorLength;
    private float corridorWidth;
    private float innerWidthRatio = 0.72f;

    public bool IsVisible { get; private set; }

    private void Awake()
    {
        outerLeft = CreateLine("OuterLeft", outerColor);
        outerRight = CreateLine("OuterRight", outerColor);
        innerLeft = CreateLine("InnerLeft", innerColor);
        innerRight = CreateLine("InnerRight", innerColor);
        Hide();
    }

    public void Configure(float length, float width, float innerRatio)
    {
        corridorLength = Mathf.Max(0.1f, length);
        corridorWidth = Mathf.Max(0.1f, width);
        innerWidthRatio = Mathf.Clamp(innerRatio, 0.2f, 1f);
    }

    public void ShowAt(Vector2 origin, Vector2 direction, float fillAmount)
    {
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;

        transform.position = new Vector3(origin.x, origin.y, 0f);
        var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        UpdateLines(fillAmount);
        SetLinesActive(true);
        IsVisible = true;
    }

    public void SetFill(float fillAmount)
    {
        if (!IsVisible)
            return;

        UpdateLines(fillAmount);
    }

    public void Hide()
    {
        SetLinesActive(false);
        IsVisible = false;
    }

    private void UpdateLines(float fillAmount)
    {
        fillAmount = Mathf.Clamp01(fillAmount);
        var halfWidth = corridorWidth * 0.5f;
        var innerHalfWidth = halfWidth * innerWidthRatio;
        var innerLength = corridorLength * fillAmount;

        SetEdgeLine(outerLeft, 0f, corridorLength, -halfWidth);
        SetEdgeLine(outerRight, 0f, corridorLength, halfWidth);
        SetEdgeLine(innerLeft, 0f, innerLength, -innerHalfWidth);
        SetEdgeLine(innerRight, 0f, innerLength, innerHalfWidth);
    }

    private void SetEdgeLine(LineRenderer line, float startForward, float endForward, float lateral)
    {
        line.SetPosition(0, new Vector3(startForward, lateral, 0f));
        line.SetPosition(1, new Vector3(endForward, lateral, 0f));
    }

    private void SetLinesActive(bool active)
    {
        outerLeft.gameObject.SetActive(active);
        outerRight.gameObject.SetActive(active);
        innerLeft.gameObject.SetActive(active);
        innerRight.gameObject.SetActive(active);
    }

    private LineRenderer CreateLine(string lineName, Color color)
    {
        var lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(transform, false);

        var line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.numCapVertices = 4;
        line.numCornerVertices = 2;
        line.sortingLayerName = sortingLayerName;
        line.sortingOrder = sortingOrder;
        line.material = GetSharedLineMaterial();
        line.startColor = color;
        line.endColor = color;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.textureMode = LineTextureMode.Stretch;
        return line;
    }

    private static Material GetSharedLineMaterial()
    {
        if (sharedLineMaterial != null)
            return sharedLineMaterial;

        sharedLineMaterial = new Material(Shader.Find("Sprites/Default"));
        return sharedLineMaterial;
    }
}
