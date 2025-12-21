using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;


// Class which deals with player movement and movement abilities
public class PlayerMovement : MonoBehaviour
{
    public enum PlayerState { Idle, Running, Jumping, Falling, Dashing }
    public PlayerState currentState = PlayerState.Falling;
    public GameManager gameManager;

    [Header("General Settings")]
    private float currMultIncrease = 1f;
    public float baseMultIncrease = 1f;
    public Transform player;
    public Rigidbody2D rb;
    Animator animator;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    float horizontalMovement;
    public bool isFacingRight = true;

    [Header("Jump Settings")]
    public float jumpPower = 5f;
    public int jumpCount = 0;
    public int maxJumps = 2;
    bool isJumping = false;

    [Header("Dash Settings")]
    public float dashPower = 5f;
    public int dashDamage = 25;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private Vector2 moveInput;
    private Vector2 dashDirection;
    public bool isDashing = false;
    private float dashTimer = 0f;
    private float lastDashTime = -Mathf.Infinity;

    [Header("Glide Settings")]
    public int glideCharge = 100;
    public int maxGlideCharge = 100;
    public float glideMoveSpeed = 1.5f;
    public float glideGravityScale = 0.5f;
    public float minGlideFallSpeed = -2f;
    private bool isGliding = false;
    public Slider glideSlider;

    [Header("Ground Check")]
    public Transform groundCheckPos;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.5f);
    public LayerMask groundLayer;
    public bool isGrounded = false;
    private bool wasGrounded = false;

    [Header("Wall Check")]
    public Transform wallCheckPos;
    public Vector2 wallCheckSize = new Vector2(0.5f, 0.5f);
    public LayerMask wallLayer;

    [Header("Gravity")]
    public float baseGravity = 2f;
    public float maxFallSpeed = 7f;
    public float fallSpeedMultiplier = 2f;

    [Header("Wall Movement")]
    public float wallSlideSpeed = 2f;
    public bool isWallSliding;

    // wall jumping
    bool isWallJumping = false;
    float wallJumpDirection;
    float wallJumpTime = 0.5f;
    float wallJumpTimer;
    public Vector2 wallJumpPower = new Vector2(5f, 10f);

    [Header("Grid Settings")]
    public float gridboundx, gridboundy;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();

        // Add mult while in the air
        StartCoroutine(IncrementMult());

        // Decrease glide charge while gliding 
        StartCoroutine(DecreaseGlideCharge());
    }

    void Update()
    {
        // Was grounded set to last update grounded check and recheck 
        wasGrounded = isGrounded;
        isGrounded = IsGrounded();

        // If player is grounded and was not before handle counters and animation
        if (isGrounded && !wasGrounded)
        {
            jumpCount = 0;
            isJumping = false;
            animator.SetBool("isJumping", false);

            currMultIncrease = 1f;
            gameManager.DisplayAndResetScore();

            isGliding = false;
            animator.SetBool("isGliding", false);

            // Refill glide
            glideCharge = maxGlideCharge;
            if (glideSlider)
            {
                glideSlider.value = glideCharge;
            }
        }

        ApplyGravity();

        // If in jumping state and falling turn of jumping animation and set isJumping to false
        if (rb.linearVelocity.y <= 0 && isJumping)
        {
            isJumping = false;
            animator.SetBool("isJumping", false);
        }

        ProcessWallSlide();
        ProcessWallJump();

        // If not walljumping or dashing handle movement and flip if needed
        if (!isWallJumping && !isDashing)
        {
            if (isGliding)
            {
                rb.linearVelocity = new Vector2(horizontalMovement * glideMoveSpeed, rb.linearVelocity.y);
                Flip();
            }
            else
            {
                rb.linearVelocity = new Vector2(horizontalMovement * moveSpeed, rb.linearVelocity.y);
                Flip();
            }
        }

        // If dashing update movement, animations, bools, and timer 
        if (isDashing)
        {
            rb.linearVelocity = dashDirection * dashPower;
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                rb.gravityScale = baseGravity;
                animator.SetBool("isDashing", false);
            }
            return;
        }

        // Update the state
        UpdateState();
    }

    // Handles when to transition between each animation
    void UpdateState()
    {
        if (isDashing)
        {
            currentState = PlayerState.Dashing;
            animator.SetBool("isDashing", true);
            return;
        }

        if (!isGrounded)
        {
            if (rb.linearVelocity.y > 0.1f)
                currentState = PlayerState.Jumping;
            else if (rb.linearVelocity.y < -0.1f)
                currentState = PlayerState.Falling;
        }
        else if (Mathf.Abs(horizontalMovement) > 0.01f)
        {
            currentState = PlayerState.Running;
        }
        else
        {
            currentState = PlayerState.Idle;
        }

        UpdateAnimatorParameters();
    }

    // Updates bools and conditions for animator
    void UpdateAnimatorParameters()
    {
        animator.SetBool("isGrounded", isGrounded);
        animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
        animator.SetFloat("yVelocity", rb.linearVelocity.y);
        animator.SetBool("isDashing", isDashing); 
        animator.SetBool("isWallSliding", isWallSliding);
        animator.SetBool("isRunning", Mathf.Abs(horizontalMovement) > 0.01f && isGrounded);
    }

    // Applies Gravity
    void ApplyGravity()
    {
        if (isGliding)
        {
            rb.gravityScale = glideGravityScale;

            // Clamp vertical speed so you don't fall too fast
            if (rb.
            linearVelocity.y < minGlideFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, minGlideFallSpeed);
            }

            return;
        }

        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = baseGravity * fallSpeedMultiplier;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -maxFallSpeed));
        }
        else
        {
            ResetGravityScale();
        }
    }

    public void ResetGravityScale()
    {
        rb.gravityScale = baseGravity;
    }

    // Moves the player
    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        horizontalMovement = moveInput.x;
    }

    // To make the player either jump or wall jump
    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (wallJumpTimer > 0f)
            {
                WallJump();
            }
            else if (jumpCount < maxJumps)
            {
                NormalJump();
            }
            else if (!isGrounded && !isGliding && glideCharge > 0)
            {
                isGliding = true;
                animator.SetBool("isGliding", true);
            }
        }

        if (context.canceled && isGliding)
        {
            isGliding = false;
            animator.SetBool("isGliding", false);
        }
    }

    // Handles normal jumping
    private void NormalJump()
    {
        jumpCount++;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
        isJumping = true;
        animator.SetBool("isJumping", true);
    }

    // Handles WallJumps
    private void WallJump()
    {
        isWallJumping = true;
        isJumping = true;
        rb.linearVelocity = new Vector2(wallJumpDirection * wallJumpPower.x, wallJumpPower.y);
        animator.SetBool("isJumping", true);
        wallJumpTimer = 0;

        if (transform.localScale.x != wallJumpDirection)
            Flip();

        Invoke(nameof(CancelWallJump), wallJumpTime + 0.1f);
    }

    // An 8 way Dash 
    public void Dash(InputAction.CallbackContext context)
    {
        if (context.performed && !isDashing && Time.time >= lastDashTime + dashCooldown)
        {
            dashDirection = moveInput.normalized;

            if (dashDirection != Vector2.zero)
            {
                isDashing = true;
                dashTimer = dashDuration;
                lastDashTime = Time.time;

                rb.gravityScale = 0f;
                rb.linearVelocity = dashDirection * dashPower;

                // Override blend tree with dash animation
                animator.Play("DashAttack");
            }
        }
    }

    public void ReduceDashCooldown()
    {
        Debug.Log("Killed enemy, reset dash cooldown");
        lastDashTime = Time.time - dashCooldown;
    }


    // Wall Jumping and sliding ---------------------------------------------------------------
    private void ProcessWallSlide()
    {
        if (!isGrounded && WallCheck() && horizontalMovement != 0)
        {
            isWallSliding = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void ProcessWallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpDirection = -transform.localScale.x;
            wallJumpTimer = wallJumpTime;
            CancelInvoke(nameof(CancelWallJump));
        }
        else if (wallJumpTimer > 0f)
        {
            wallJumpTimer -= Time.deltaTime;
        }
    }

    private void CancelWallJump()
    {
        isWallJumping = false;
    }

    private bool WallCheck()
    {
        return Physics2D.OverlapBox(wallCheckPos.position, wallCheckSize, 0f, wallLayer);
    }

    // -----------------------------------------------------------------------------------------

    // Flips the player sprite
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

    // Checks if the player is grounded using a box
    private bool IsGrounded()
    {
        return Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0f, groundLayer);
    }

    // Increases mult exponentially while in the air
    IEnumerator IncrementMult()
    {
        while (true)
        {
            if (!isGrounded)
            {
                gameManager.IncreaseMultiplier(currMultIncrease);
                currMultIncrease *= 1.5f;
            }

            yield return new WaitForSeconds(1.75f);
        }
    }

    IEnumerator DecreaseGlideCharge()
    {
        while (true)
        {
            if (isGliding)
            {
                glideCharge -= 1;
                glideCharge = Mathf.Clamp(glideCharge, 0, maxGlideCharge);

                if (glideSlider)
                {
                    glideSlider.maxValue = maxGlideCharge;
                    glideSlider.value = glideCharge;
                }

                // Stop gliding if out of charge
                if (glideCharge <= 0)
                {
                    isGliding = false;
                    animator.SetBool("isGliding", false);
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    // Sets the currentPlayerState to the given PlayerState
    public void SetPlayerState(PlayerState state)
    {
        PlayerState prevState = currentState;
        currentState = state;
        Debug.Log($"PlayerState changed from {prevState} to {currentState}");
    }

    private void OnDrawGizmos()
    {
        Vector2 gridbound = new Vector2(gridboundx,gridboundy);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(player.position, gridbound);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(wallCheckPos.position, wallCheckSize);
    }
}
