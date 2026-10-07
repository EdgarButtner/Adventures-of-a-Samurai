using UnityEngine;
using UnityEngine.UI;

// Clickable place on the globe
[RequireComponent(typeof(Button), typeof(CanvasGroup))]
public class MapLocation : MonoBehaviour
{
    [Header("Location")]
    public string locationName;
    [TextArea] public string description;

    // Must be in Build Settings
    public string sceneName;

    [Header("Placement")]
    public WorldMapGlobe globe;
    public LocationInfoPanel infoPanel;

    // Globe rotation that centers this place
    public float longitude = 0f;

    // Up is positive
    [Range(-90f, 90f)] public float latitude = 0f;

    // Hide near the edge
    [Range(0f, 1f)] public float edgeHide = 0.15f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    // On the front of the globe
    public bool IsVisible { get; private set; }

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        GetComponent<Button>().onClick.AddListener(OnClicked);

        // Auto find globe
        if (globe == null)
            globe = GetComponentInParent<WorldMapGlobe>();

        // Auto find sibling panel
        if (infoPanel == null && transform.parent != null)
            infoPanel = transform.parent.GetComponentInChildren<LocationInfoPanel>(true);
    }

    // After the globe updates
    void LateUpdate()
    {
        if (globe == null)
            return;

        float angle = (longitude - globe.ShownRotation) * globe.MarkerDirection * Mathf.Deg2Rad;
        float lat = latitude * Mathf.Deg2Rad;

        // Sphere onto circle
        float x = globe.Radius * Mathf.Cos(lat) * Mathf.Sin(angle);
        float y = globe.Radius * Mathf.Sin(lat);
        rectTransform.anchoredPosition = new Vector2(x, y);

        // Front side only
        bool visible = Mathf.Cos(angle) * Mathf.Cos(lat) > edgeHide;
        IsVisible = visible;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    void OnClicked()
    {
        if (globe != null)
            globe.HoldRotation();

        if (infoPanel != null)
            infoPanel.Show(this);
    }
}
