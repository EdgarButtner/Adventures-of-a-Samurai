using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Player Health")]
    public int maxHealth = 100;
    int currentHealth;
    public TMP_Text healthText;
    public Slider healthSlider;
    public bool isAlive;

    [Header("References")]
    private PlayerMovement playerMovement;
    private Animator animator;
    private GameManager gameManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;
        isAlive = true;

        playerMovement = GetComponent<PlayerMovement>();
        animator = GetComponent<Animator>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();

        UpdateHealthSlider();
    }

    // Update is called once per frame
    void Update()
    {
        if (currentHealth <= 0 && isAlive) 
        {
            isAlive = false;
            Die();
        }
    }

    public void Die() 
    {
        Debug.Log("Player Died");
        animator.SetTrigger("Die");

        // Disable movement and collisions
        playerMovement.enabled = false;
        GetComponent<Rigidbody2D>().linearVelocity  = Vector2.zero;
        GetComponent<Rigidbody2D>().gravityScale = 0;
        GetComponent<Collider2D>().enabled = false;

        gameManager.OnPlayerDeath();
    }

    public void TakeDamage(int damage)
    {
        Debug.Log("Player took damage");

        if (!isAlive || currentHealth <= 0)
            return; 

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthSlider();
    }

    public void TakeHealth(int health)
    {
        Debug.Log("Player healed");
        if (!isAlive || currentHealth <= 0)
            return; 

        currentHealth += health;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthSlider();
    }

    public void UpdateHealthSlider() 
    {
        Debug.Log("Health slider updated");

        if (healthSlider)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;

            if (healthText != null)
                healthText.text = currentHealth.ToString();
        }
    }

}
