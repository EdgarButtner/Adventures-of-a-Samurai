using UnityEngine;
using UnityEngine.UI;

// Sizes a panel from screen size and content
[RequireComponent(typeof(RectTransform))]
public class ResponsivePanelSize : MonoBehaviour
{
    [Header("Width")]
    // Share of screen width
    [Range(0f, 1f)] public float widthPercent = 0.25f;
    public float minWidth = 220f;
    public float maxWidth = 420f;

    [Header("Height")]
    // Fit content, else share of screen height
    public bool fitContentHeight = true;
    [Range(0f, 1f)] public float heightPercent = 0.3f;
    public float minHeight = 100f;
    public float maxHeight = 320f;

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

    // Resize to the current screen
    public void Apply()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        RectTransform canvasRect = CanvasRect;
        if (canvasRect == null)
            return;

        Vector2 canvasSize = canvasRect.rect.size;

        float width = Mathf.Clamp(canvasSize.x * widthPercent, minWidth, maxWidth);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

        float height;
        if (fitContentHeight)
        {
            // Measure content at this width
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            height = LayoutUtility.GetPreferredHeight(rectTransform);
        }
        else
        {
            height = canvasSize.y * heightPercent;
        }

        height = Mathf.Clamp(height, minHeight, maxHeight);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        // Lay out children at final size
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }
}
