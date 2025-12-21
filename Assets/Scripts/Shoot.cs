using UnityEngine;

// Class to shoot a shuiken 
public class Shoot : MonoBehaviour
{
    [Header("General Settings")]
    public NPCController npccontroller;

    [Header("Shoot Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileCooldown = 3f;
    public float projectileSpeed = 10f;
    private float lastShootTime = -Mathf.Infinity;

    void Start()
    {
        //npccontroller = GameObject.FindObjectOfType<NPCController>();
    }


    void Update()
    {
        // If mouse clicked 
        //if (Input.GetMouseButtonDown(0) && Time.time - lastShootTime >= projectileCooldown && !npccontroller.dialogueStarted)
        //{
            //lastShootTime = Time.time;
            //FireProjectile();
        //}
    }

    void FireProjectile()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        Vector2 direction = (mousePos - firePoint.position).normalized;

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        TeleportProjectileBehavior teleportProj = proj.GetComponent<TeleportProjectileBehavior>();
        if (teleportProj != null)
        {
            teleportProj.Initialize(direction, transform);
        }
    }
}
