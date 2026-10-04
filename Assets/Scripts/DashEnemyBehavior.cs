using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DashEnemyBehavior : MonoBehaviour
{
    public enum EnemyState { Patrol, GoToPlayer, Attacking, Die }
    public EnemyState currentState;
    public Transform playerPos;
    public GameObject playerObject;
    public PlayerMovement playerMovement;
    public GameManager gameManager;

    [Header("General Settings")]
    public int scoreValue = 50;
    public Rigidbody2D rb;

    [Header("Attack Settings")]
    public int damageValue = 10;
    public float attackCooldown = 2f;
    private bool isAttacking = false;
    private float lastAttackTime = -Mathf.Infinity;
    public float minDistance = 5f;

    [Header("Dash Settings")]
    public float dashPower = 5f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private float dashTimer = 0f;
    private float lastDashTime = -Mathf.Infinity;

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
        // Get enemies rigidbody
        rb = GetComponent<Rigidbody2D>();

        // Set isAlive to true
        isAlive = true;

        // Find the players position 
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerPos = playerObject.transform;
            playerMovement = playerObject.GetComponent<PlayerMovement>();
        }

        // Find the gamemanager
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();

        // Set current health to max health
        currentHealth = maxHealth;
    }


    // Update is called once per frame
    void Update()
    {
        // To check if the sprite is facing the correct way
        Flip();

        // Basic finite state machine for enemy AI
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

    // TO DO - Patrol state
    public void Patrol()
    {

    }

    // If the player is in range move towards them 
    public void GoToPlayer()
    {
        if (playerPos == null) 
            return;

        // Distance to player
        float distance = Vector2.Distance(transform.position, playerPos.position);

        // If not within attacking range go to player
        if (distance > minDistance)
        {
            float step = moveSpeed * Time.deltaTime;
            transform.position = Vector2.MoveTowards(transform.position, playerPos.position, step);
        }
        // Otherwise attack the player
        else
        {
            currentState = EnemyState.Attacking;
        }
    }

    // Attack the player
    public void Attack()
    {

        if (isAttacking || !playerPos)
            return;

        // Distance to player
        float distance = Vector2.Distance(transform.position, playerPos.position);

        // If not in range go to player
        if (distance > minDistance)
        {
            currentState = EnemyState.GoToPlayer;
            return;
        }

        // Tick down time until enemy can dash / attack
        dashTimer -= Time.deltaTime;

        // Check if enough time has passed since last attack
        if (Time.time - lastAttackTime >= attackCooldown && distance <= minDistance && !playerMovement.IsAttacking)
        {
            lastAttackTime = Time.time;
            isAttacking = true;

            Vector2 dashDirection = (playerPos.position - transform.position).normalized;
            StartCoroutine(DashAttack(dashDirection));
        }
    }

    public void Die()
    {
        // Reduce player dash cooldown 
        playerMovement.ReduceDashCooldown();

        // Add score 
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

        if (col.CompareTag("Player") && playerMovement.IsAttacking)
        {
            Debug.Log("Took Damage" + currentHealth.ToString());
            currentHealth -= playerMovement.AttackDamage;
            if(healthSlider) 
            {
                healthSlider.maxValue = maxHealth;
                healthSlider.value = currentHealth;
            }

            if (currentHealth <= 0)
                currentState = EnemyState.Die;
        }
    }

    IEnumerator DashAttack(Vector2 direction)
    {
        // Start dash
        rb.linearVelocity = direction * dashPower;

        yield return new WaitForSeconds(dashDuration);

        // Stop movement
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(attackCooldown - dashDuration);

        isAttacking = false;

        // Decide next state
        float distance = Vector2.Distance(transform.position, playerPos.position);
        currentState = distance > minDistance ? EnemyState.GoToPlayer : EnemyState.Attacking;
    }

}
