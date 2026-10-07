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

    // Up and right of the marker
    public Vector2 offsetFromMarker = new Vector2(20f, 20f);

    private MapLocation currentLocation;
    private RectTransform rectTransform;
    private RectTransform markerRect;

    // Only one open
    private static LocationInfoPanel openPanel;

    public static bool IsAnyOpen => openPanel != null;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // Grows up and right
        rectTransform.pivot = Vector2.zero;

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
        gameObject.SetActive(true);
        PlaceNearMarker();

        // Blank keeps styled text
        if (nameText != null && !string.IsNullOrEmpty(location.locationName))
            nameText.text = location.locationName;

        if (descriptionText != null && !string.IsNullOrEmpty(location.description))
            descriptionText.text = location.description;
    }

    // Follow the marker
    void LateUpdate()
    {
        // Close once behind the globe
        if (currentLocation != null && !currentLocation.IsVisible)
        {
            Hide();
            return;
        }

        PlaceNearMarker();
    }

    private void PlaceNearMarker()
    {
        if (markerRect == null || markerRect.parent == null)
            return;

        Vector3 localPoint = markerRect.localPosition + (Vector3)offsetFromMarker;
        rectTransform.position = markerRect.parent.TransformPoint(localPoint);
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

        // Unpause before leaving
        if (PauseController.instance != null)
            PauseController.instance.UnpauseGame();

        Time.timeScale = 1f;
        SceneManager.LoadScene(currentLocation.sceneName);
    }
}
