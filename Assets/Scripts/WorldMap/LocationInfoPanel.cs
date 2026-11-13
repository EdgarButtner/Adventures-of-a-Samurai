using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Location name, description and travel
public class LocationInfoPanel : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public Button travelButton;
    public Button closeButton;

    // Found or added automatically
    public ResponsivePanelSize panelSize;
    public PanelPlacer panelPlacer;

    private MapLocation currentLocation;
    private RectTransform markerRect;
    private bool movedToLayer = false;
    private Vector2 lastCanvasSize;

    // Only one open
    private static LocationInfoPanel openPanel;

    public static bool IsAnyOpen => openPanel != null;

    void Awake()
    {
        if (panelSize == null)
            panelSize = GetComponent<ResponsivePanelSize>();
        if (panelSize == null)
            panelSize = gameObject.AddComponent<ResponsivePanelSize>();

        if (panelPlacer == null)
            panelPlacer = GetComponent<PanelPlacer>();
        if (panelPlacer == null)
            panelPlacer = gameObject.AddComponent<PanelPlacer>();

        if (travelButton != null)
            travelButton.onClick.AddListener(Travel);

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    public void Show(MapLocation location)
    {
        // Close the other one
        if (openPanel != null && openPanel != this)
            openPanel.Hide();

        openPanel = this;
        currentLocation = location;
        markerRect = location.transform as RectTransform;

        MoveToPanelLayer(location);
        gameObject.SetActive(true);

        // Text first, size depends on it
        if (nameText != null && !string.IsNullOrEmpty(location.locationName))
            nameText.text = location.locationName;

        if (descriptionText != null && !string.IsNullOrEmpty(location.description))
            descriptionText.text = location.description;

        Resize();
        panelPlacer.PlaceBeside(markerRect);
    }

    void LateUpdate()
    {
        // Close once behind the globe
        if (currentLocation != null && !currentLocation.IsVisible)
        {
            Hide();
            return;
        }

        // Resize when the screen changes
        RectTransform canvasRect = panelSize.CanvasRect;
        if (canvasRect != null && canvasRect.rect.size != lastCanvasSize)
            Resize();

        // Follow the marker
        panelPlacer.PlaceBeside(markerRect);
    }

    private void Resize()
    {
        panelSize.Apply();

        RectTransform canvasRect = panelSize.CanvasRect;
        if (canvasRect != null)
            lastCanvasSize = canvasRect.rect.size;
    }

    // Above the globe and markers
    private void MoveToPanelLayer(MapLocation location)
    {
        if (!movedToLayer && location.globe != null && location.globe.PanelLayer != null)
        {
            transform.SetParent(location.globe.PanelLayer, false);
            movedToLayer = true;
        }

        // Newest on top
        transform.SetAsLastSibling();
    }

    // Hides every panel under root
    public static void HideAllUnder(GameObject root)
    {
        if (root == null)
            return;

        foreach (LocationInfoPanel panel in root.GetComponentsInChildren<LocationInfoPanel>(true))
            panel.HideQuietly();
    }

    // Hide without holding the globe
    private void HideQuietly()
    {
        if (openPanel == this)
            openPanel = null;

        currentLocation = null;
        markerRect = null;
        gameObject.SetActive(false);
    }

    public void Hide()
    {
        if (openPanel == this)
            openPanel = null;

        // Restart the hold after closing
        if (currentLocation != null && currentLocation.globe != null)
            currentLocation.globe.HoldRotation();

        gameObject.SetActive(false);
    }

    public void Travel()
    {
        if (currentLocation == null || string.IsNullOrEmpty(currentLocation.sceneName))
            return;

        GameSession.Instance.LoadLevel(currentLocation.sceneName);
    }
}
