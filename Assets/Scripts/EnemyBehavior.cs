using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EnemyBehavior : MonoBehaviour
{
    public enum EnemyState { Patrol, GoToPlayer, Attacking, Die }
    public EnemyState currentState;
    public Transform playerPos;
    public GameObject playerObject;
    public PlayerMovement playerMovement;
    public GameManager gameManager;

    [Header("General Settings")]
    public int scoreValue = 50;

    [Header("Attack Settings")]
    public int damageValue = 10;
    public float attackCooldown = 2f;
    private bool isAttacking = false;
    private float lastAttackTime = -Mathf.Infinity;
    public float minDistance = 1f;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    float horizontalMovement;
    public bool isFacingRight = true;

    [Header("Health Settings")]
    public int currentHealth = 10;
    public int maxHealth = 10;
    public bool isAlive;
    public Slider healthSlider;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isAlive = true;

        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerPos = playerObject.transform;
            playerMovement = playerObject.GetComponent<PlayerMovement>();
        }

        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();

        currentHealth = maxHealth;
    }


    // Update is called once per frame
    void Update()
    {

        Flip();

        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();
                break;
            case EnemyState.GoToPlayer:
                GoToPlayer();
                break;
            case EnemyState.Attacking:
                Attack();
                break;
            case EnemyState.Die:
                Die();
                break;
        }
    }

    public void Patrol()
    {

    }

    public void GoToPlayer()
    {
        if (playerPos == null) return;

        float distance = Vector2.Distance(transform.position, playerPos.position);

        if (distance > minDistance)
        {
            float step = moveSpeed * Time.deltaTime;
            transform.position = Vector2.MoveTowards(transform.position, playerPos.position, step);
        }
        else
        {
            currentState = EnemyState.Attacking;
        }
        //horizontalMovement = playerPos.position.x - transform.position.x; 
    }

    public void Attack()
    {

        if (isAttacking || !playerPos)
            return;

        float distance = Vector2.Distance(transform.position, playerPos.position);

        if (distance > minDistance)
        {
            currentState = EnemyState.GoToPlayer;
            return;
        }

        // Check if enough time has passed since last attack
        if (Time.time - lastAttackTime >= attackCooldown && (distance <= minDistance) && !playerMovement.isDashing)
        {
            lastAttackTime = Time.time;
            isAttacking = true;

            // Deal damage to player
            PlayerHealth playerHealth = playerObject.GetComponent<PlayerHealth>();
            if (playerHealth)
            {
                Debug.Log("attacked player");
                playerHealth.TakeDamage(damageValue);
            }


            StartCoroutine(AttackCooldownCoroutine());
        }

    }

    public void Die()
    {
        // reduce cooldown 
        playerMovement.ReduceDashCooldown();

        // particle effect 
        gameManager.AddScore(scoreValue);

        Destroy(gameObject);
    }

    public void Flip()
    {
        if (isFacingRight && horizontalMovement < 0 || !isFacingRight && horizontalMovement > 0)
        {
            isFacingRight = !isFacingRight;
            Vector3 ls = transform.localScale;
            ls.x *= -1f;
            transform.localScale = ls;
        }
    }

    public void TakeDamage(int damage)
    {
        if(!isAlive || currentHealth <= 0)
            return; 

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }


    IEnumerator AttackCooldownCoroutine()
    {
        // wait for attack animation to complete
        yield return new WaitForSeconds(1.1f);

        isAttacking = false;

        // decide next state based on distance to player
        float distance = Vector2.Distance(transform.position, playerPos.position);
        currentState = distance > minDistance ? EnemyState.GoToPlayer : EnemyState.Attacking;
    }

    public void OnTriggerEnter2D(Collider2D col)
    {

        if (col.CompareTag("Player") && playerMovement.isDashing)
        {
            Debug.Log("Took Damage" + currentHealth.ToString());
            currentHealth -= playerMovement.dashDamage;
            if(healthSlider) 
            {
                healthSlider.maxValue = maxHealth;
                healthSlider.value = currentHealth;
            }

            if (currentHealth <= 0)
                currentState = EnemyState.Die;
        }
    }
}
