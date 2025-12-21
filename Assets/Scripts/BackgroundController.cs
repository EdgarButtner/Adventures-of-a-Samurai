using UnityEngine;

//Class to represent the background controller aka the scrolling background.
public class BackgroundController : MonoBehaviour
{
    // Current Camera
    public GameObject cam;

    // Speed to move relative to the camera
    public float parallaxEffect;

    // Start pos and length of the background sprite
    private float startPos, length;


    void Start()
    {
        // Set start location and length of sprite 
        startPos = transform.position.x;
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    // Every fixed update 
    void FixedUpdate()
    {
        float distance = cam.transform.position.x * parallaxEffect;
        float movement = cam.transform.position.x * (1 - parallaxEffect);

        transform.position = new Vector3(startPos + distance, transform.position.y, transform.position.z);

        if (movement > startPos + length)
        {
            startPos += length;

        }
        else if (movement < startPos - length)
        {
            startPos -= length;
        }
    }
}
