using UnityEngine;

// Places a panel beside a target, kept on screen
[RequireComponent(typeof(RectTransform))]
public class PanelPlacer : MonoBehaviour
{
    public enum Side { UpRight, UpLeft, DownRight, DownLeft }

    // First that fits wins
    public Side[] preferredSides = { Side.UpRight, Side.UpLeft, Side.DownRight, Side.DownLeft };

    [Header("Gap from target")]
    // Share of screen height
    [Range(0f, 0.2f)] public float gapPercent = 0.01f;
    public float minGap = 4f;
    public float maxGap = 24f;

    [Header("Screen edge")]
    public float screenMargin = 16f;

    // Side used last
    public Side currentSide = Side.UpRight;

    private RectTransform rectTransform;

    public RectTransform CanvasRect
    {
        get
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            return canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
        }
    }

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // Place beside target
    public void PlaceBeside(RectTransform target)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        RectTransform canvasRect = CanvasRect;
        if (target == null || canvasRect == null)
            return;

        Rect screen = canvasRect.rect;
        Rect safe = new Rect(screen.xMin + screenMargin, screen.yMin + screenMargin,
            screen.width - screenMargin * 2f, screen.height - screenMargin * 2f);

        Rect targetBounds = GetBoundsInCanvas(target, canvasRect);
        Vector2 size = GetSizeInCanvas(canvasRect);
        float gap = Mathf.Clamp(screen.height * gapPercent, minGap, maxGap);

        // Try each side
        foreach (Side side in preferredSides)
        {
            Rect candidate = GetCandidate(side, targetBounds, size, gap);
            if (Fits(candidate, safe))
            {
                Apply(side, candidate, canvasRect);
                return;
            }
        }

        // Nothing fits, clamp first choice
        Side fallback = preferredSides.Length > 0 ? preferredSides[0] : Side.UpRight;
        Rect clamped = GetCandidate(fallback, targetBounds, size, gap);
        clamped.x = Mathf.Clamp(clamped.x, safe.xMin, Mathf.Max(safe.xMin, safe.xMax - clamped.width));
        clamped.y = Mathf.Clamp(clamped.y, safe.yMin, Mathf.Max(safe.yMin, safe.yMax - clamped.height));
        Apply(fallback, clamped, canvasRect);
    }

    private Rect GetCandidate(Side side, Rect target, Vector2 size, float gap)
    {
        bool right = side == Side.UpRight || side == Side.DownRight;
        bool up = side == Side.UpRight || side == Side.UpLeft;

        float x = right ? target.xMax + gap : target.xMin - gap - size.x;
        float y = up ? target.yMax + gap : target.yMin - gap - size.y;
        return new Rect(x, y, size.x, size.y);
    }

    private bool Fits(Rect rect, Rect safe)
    {
        return rect.xMin >= safe.xMin && rect.xMax <= safe.xMax && rect.yMin >= safe.yMin && rect.yMax <= safe.yMax;
    }

    // Pivot on the corner facing the target
    private void Apply(Side side, Rect rect, RectTransform canvasRect)
    {
        currentSide = side;

        bool right = side == Side.UpRight || side == Side.DownRight;
        bool up = side == Side.UpRight || side == Side.UpLeft;
        Vector2 pivot = new Vector2(right ? 0f : 1f, up ? 0f : 1f);

        Vector2 corner = new Vector2(right ? rect.xMin : rect.xMax, up ? rect.yMin : rect.yMax);

        rectTransform.pivot = pivot;
        rectTransform.position = canvasRect.TransformPoint(corner);
    }

    private Vector2 GetSizeInCanvas(RectTransform canvasRect)
    {
        float scale = rectTransform.lossyScale.x / Mathf.Max(canvasRect.lossyScale.x, 0.0001f);
        return rectTransform.rect.size * scale;
    }

    private static Rect GetBoundsInCanvas(RectTransform target, RectTransform canvasRect)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Vector2 min = canvasRect.InverseTransformPoint(corners[0]);
        Vector2 max = min;
        for (int i = 1; i < 4; i++)
        {
            Vector2 point = canvasRect.InverseTransformPoint(corners[i]);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
}
