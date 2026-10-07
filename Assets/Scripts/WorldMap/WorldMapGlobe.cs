using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Spinning globe, drag to rotate
[RequireComponent(typeof(Image))]
public class WorldMapGlobe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("Frames")]
    // Spin order
    public Sprite[] frames;

    [Header("Spin")]
    // Degrees per second
    public float idleSpinSpeed = 10f;

    // Degrees per pixel dragged
    public float dragSensitivity = 0.4f;

    // How fast a fling settles
    public float flingDamping = 3f;

    // Flip if dragging feels backwards
    public bool invertDrag = false;

    // Flip if markers slide the wrong way
    public bool invertMarkers = false;

    [Header("Hold")]
    // No spin after interacting
    public float interactionHoldTime = 3f;

    // Time left holding
    public float holdTimer = 0f;

    [Header("Markers")]
    // Globe size inside the frame
    [Range(0f, 1f)] public float radiusScale = 0.95f;

    // Current angle
    public float rotation = 0f;

    private Image image;
    private RectTransform rectTransform;
    private bool isDragging = false;
    private float spinVelocity = 0f;
    private float dragVelocity = 0f;

    public int FrameIndex => frames == null || frames.Length == 0 ? 0 : Mathf.FloorToInt(Mathf.Repeat(rotation, 360f) / 360f * frames.Length) % frames.Length;

    // Angle of the shown frame
    public float ShownRotation => frames == null || frames.Length == 0 ? rotation : FrameIndex * (360f / frames.Length);

    public float Radius => rectTransform.rect.width * 0.5f * radiusScale;

    public float MarkerDirection => invertMarkers ? -1f : 1f;

    // Held while timer runs or a panel is open
    public bool IsHolding => holdTimer > 0f || LocationInfoPanel.IsAnyOpen;

    void Awake()
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        spinVelocity = idleSpinSpeed;
    }

    void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        // Count down once panels close
        if (holdTimer > 0f && !LocationInfoPanel.IsAnyOpen)
            holdTimer = Mathf.Max(holdTimer - deltaTime, 0f);

        if (!isDragging)
        {
            // Settle to idle, or stop while held
            float targetSpeed = IsHolding ? 0f : idleSpinSpeed;
            spinVelocity = Mathf.Lerp(spinVelocity, targetSpeed, flingDamping * deltaTime);
            rotation += spinVelocity * deltaTime;
        }

        rotation = Mathf.Repeat(rotation, 360f);

        if (frames != null && frames.Length > 0)
            image.sprite = frames[FrameIndex];
    }

    // Hub open button, call before showing
    public void HidePanels()
    {
        LocationInfoPanel.HideAllUnder(gameObject);
    }

    // Stops idle spin for a while
    public void HoldRotation()
    {
        holdTimer = interactionHoldTime;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        HoldRotation();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        dragVelocity = 0f;
        HoldRotation();
    }

    public void OnDrag(PointerEventData eventData)
    {
        float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        float delta = eventData.delta.x * dragSensitivity * (invertDrag ? -1f : 1f);

        rotation += delta;

        // Smoothed for the fling
        dragVelocity = Mathf.Lerp(dragVelocity, delta / deltaTime, 0.5f);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        spinVelocity = dragVelocity;
        HoldRotation();
    }
}
