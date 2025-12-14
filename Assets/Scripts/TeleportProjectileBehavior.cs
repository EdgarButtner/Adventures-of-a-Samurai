using UnityEngine;
using System.Collections;

public class TeleportProjectileBehavior : MonoBehaviour
{
    public float speed = 10f;
    private Vector2 direction;
    private Transform player;
    private PlayerMovement playerMovement;
    private Transform firePoint; 
    Rigidbody2D rb;
    public LayerMask destroyOnCollisionLayers;
    
    void Start() 
    {
        // Get player transform and playerMovement script reference 
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerMovement = player.GetComponent<PlayerMovement>();

        rb = GetComponent<Rigidbody2D>();

        // Prevent projectile from colliding with player
        Collider2D projectileCol = GetComponent<Collider2D>();
        Collider2D playerCol = player.GetComponent<Collider2D>();

        // Ignores collisions with the player 
        if (projectileCol != null && playerCol != null)
        {
            Physics2D.IgnoreCollision(projectileCol, playerCol);
        }
    }

    public void Initialize(Vector2 dir, Transform passedFirePoint)
    {
        direction = dir.normalized;
        firePoint = passedFirePoint;
        StartCoroutine(TeleportAfterDelay());
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
        }
    }

    IEnumerator TeleportAfterDelay()
    {
        yield return new WaitForSeconds(1f);

        if (firePoint != null)
        {
            firePoint.position = transform.position;
            playerMovement.ResetGravityScale();
        }

        Destroy(gameObject);
    }
}
