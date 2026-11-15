using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;


// Class which deals with player movement and movement abilities
[RequireComponent(typeof(Rigidbody2D), typeof(PlayerAnimController))]
public class PlayerMovement : MonoBehaviour
{
    // Only changed through SetPlayerState
    [SerializeField] private PlayerState currentState = PlayerState.Falling;
    public PlayerState CurrentState => currentState;

    // Previous then new state
    public event Action<PlayerState, PlayerState> StateChanged;
    public bool logStateChanges = false;

    public GameManager gameManager;

    [Header("General Settings")]
    private float currMultIncrease = 1f;
    public float baseMultIncrease = 1f;
    public Transform player;
    public Rigidbody2D rb;

    // Animation and facing
    public PlayerAnimController Anim { get; private set; }

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    float horizontalMovement;

    [Header("Jump Settings")]
    public float jumpPower = 5f;
    public int jumpCount = 0;
    public int maxJumps = 2;
    bool isJumping = false;

    private Vector2 moveInput;

    // Ability which currently owns the players state, null when no ability is charging or active
    public AbstractAbility activeAbility { get; set; }
    private PlayerDash playerDash;
    private PlayerGlide playerGlide;

    // True while gliding, glide changes gravity and move speed but does not take over movement
    public bool IsGliding => playerGlide != null && playerGlide.isGliding;

    // True while an ability controls velocity and gravity
    public bool MovementLocked => activeAbility != null && activeAbility.OverridesMovement;

    // Used by enemies, true while an ability is hitting them and the player can not be hurt
    public bool IsAttacking => activeAbility != null && activeAbility.IsAttacking;
    public int AttackDamage => activeAbility != null ? activeAbility.Damage : 0;

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

    // Last ability to end, while its after ability window runs falling gravity eases back to normal
    public AbstractAbility lastEndedAbility;

    // True while the last ability to end is still in its after ability window
    public bool InAbilityFallWindow => lastEndedAbility != null && lastEndedAbility.InAfterAbilityWindow;

    // How quickly the last abilitys speed fades, horizontal eases toward normal movement and upward eases toward 0
    // Both use the same rate so the path arcs naturally with gravity, 0 means that direction is done easing
    public float abilityExitDeceleration = 0f;
    public float abilityUpwardDeceleration = 0f;

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

    // Own components
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Anim = GetComponent<PlayerAnimController>();
        playerDash = GetComponent<PlayerDash>();
        playerGlide = GetComponent<PlayerGlide>();

        // Smooth movement between physics steps
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    // Other objects
    void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();

        // Add mult while in the air
        StartCoroutine(IncrementMult());
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

            currMultIncrease = 1f;
            gameManager.DisplayAndResetScore();

            // End any glide and refill its charge
            if (playerGlide != null)
                playerGlide.OnLanded();
        }

        // Let the active ability control velocity and gravity without fighting it
        if (MovementLocked)
            return;

        ApplyGravity();

        // Jump ends once falling
        if (rb.linearVelocity.y <= 0 && isJumping)
            isJumping = false;

        ProcessWallSlide();
        ProcessWallJump();

        // If not walljumping handle movement and flip if needed
        if (!isWallJumping)
        {
            float speed = IsGliding ? playerGlide.CurrentGlideMoveSpeed : moveSpeed;
            float targetVelocityX = horizontalMovement * speed;

            // After an ability ease from its speed toward normal movement instead of snapping to it
            // Runs until caught up rather than until the window ends, so a long slide never snaps
            float velocityX = targetVelocityX;
            if (abilityExitDeceleration > 0f)
            {
                velocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetVelocityX, abilityExitDeceleration * Time.deltaTime);

                if (Mathf.Approximately(velocityX, targetVelocityX))
                    abilityExitDeceleration = 0f;
            }

            rb.linearVelocity = new Vector2(velocityX, rb.linearVelocity.y);
            Anim.FaceDirection(horizontalMovement);
        }

        // Abilities set their own state, so only update it when none are active
        if (activeAbility == null)
            UpdateState();
    }

    // Movement state when no ability owns it
    void UpdateState()
    {
        // Dead is final
        if (currentState == PlayerState.Dead)
            return;

        SetPlayerState(GetMovementState());
    }

    // Works out the state from movement alone, also used by abilities when they finish
    public PlayerState GetMovementState()
    {
        // isJumping covers frames the ground check still overlaps
        if (!isGrounded || isJumping)
            return rb.linearVelocity.y > 0.1f ? PlayerState.Jumping : PlayerState.Falling;

        if (Mathf.Abs(horizontalMovement) > 0.01f)
            return PlayerState.Running;

        return PlayerState.Idle;
    }

    // Applies Gravity
    void ApplyGravity()
    {
        // Glide handles its own gravity while active
        if (IsGliding)
        {
            playerGlide.ApplyGlideGravity();
            return;
        }

        // Fade upward speed left over from an ability at the same rate as horizontal, gravity also pulls it down, stops once it peaks
        if (abilityUpwardDeceleration > 0f)
        {
            if (rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.MoveTowards(rb.linearVelocity.y, 0f, abilityUpwardDeceleration * Time.deltaTime));
            else
                abilityUpwardDeceleration = 0f;
        }

        // After an ability use the eased gravity until its window ends, rising or falling, so gravity starts gentle
        if (InAbilityFallWindow)
        {
            rb.gravityScale = GetAbilityFallGravity();
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -maxFallSpeed));
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

    // Called by abilities when they end so the fall starts gentle like the peak of a jump
    public void OnAbilityEnded(AbstractAbility ability)
    {
        lastEndedAbility = ability;

        // Abilities choose to ease out after this, so a cancelled one just stops
        abilityExitDeceleration = 0f;
        abilityUpwardDeceleration = 0f;

        // Apply the starting gravity straight away so there is no frame at another gravity
        if (InAbilityFallWindow)
            rb.gravityScale = GetAbilityFallGravity();
        else
            ResetGravityScale();
    }

    // Eases gravity from the abilities starting gravity up to full fall gravity, synced to its after ability timer
    private float GetAbilityFallGravity()
    {
        float startGravity = baseGravity * lastEndedAbility.gravityRampStart;
        float fullFallGravity = baseGravity * fallSpeedMultiplier;

        return Mathf.Lerp(startGravity, fullFallGravity, lastEndedAbility.AfterAbilityProgress);
    }

    // Called by abilities after they end so their speed fades out instead of stopping dead
    // deceleration fades horizontal speed, upwardDeceleration fades upward speed, gravity is applied on top so it arcs
    public void EaseOutOfAbility(float deceleration, float upwardDeceleration)
    {
        abilityExitDeceleration = deceleration;
        abilityUpwardDeceleration = upwardDeceleration;
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
        // Can not jump while an ability controls movement or paused
        if (context.performed && !MovementLocked && !PauseController.IsPaused)
        {
            if (wallJumpTimer > 0f)
            {
                WallJump();
            }
            else if (jumpCount < maxJumps)
            {
                NormalJump();
            }
            // Out of jumps, holding jump glides instead
            else if (playerGlide != null)
            {
                playerGlide.Execute(context);
            }
        }

        // Releasing jump ends a glide
        if (context.canceled && playerGlide != null)
        {
            playerGlide.Execute(context);
        }
    }

    // Handles normal jumping
    private void NormalJump()
    {
        jumpCount++;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
        isJumping = true;
    }

    // Handles WallJumps
    private void WallJump()
    {
        isWallJumping = true;
        isJumping = true;
        rb.linearVelocity = new Vector2(wallJumpDirection * wallJumpPower.x, wallJumpPower.y);
        wallJumpTimer = 0;

        // Face away from wall
        Anim.FaceDirection(wallJumpDirection);

        Invoke(nameof(CancelWallJump), wallJumpTime + 0.1f);
    }

    public void ReduceDashCooldown()
    {
        Debug.Log("Killed enemy, reset dash cooldown");

        if (playerDash != null)
            playerDash.ResetCooldown();
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
            wallJumpDirection = -Anim.FacingSign;
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

    // The only place state changes, listeners follow it
    public void SetPlayerState(PlayerState state)
    {
        if (state == currentState)
            return;

        PlayerState prevState = currentState;
        currentState = state;

        if (logStateChanges)
            Debug.Log($"PlayerState changed from {prevState} to {currentState}");

        StateChanged?.Invoke(prevState, currentState);
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
