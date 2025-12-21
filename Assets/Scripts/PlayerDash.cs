using UnityEngine;
using UnityEngine.InputSystem;
using System.Diagnostics;
using System;

public class PlayerDash : AbstractAbility
{
    [Header("Dash Settings")]
    public float minDashPower = 2f;
    public float maxDashPower = 10f;
    public int minDashDamage = 15;
    public int maxDashDamage = 50;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    
    [Header("Charge Settings")]
    public float maxChargeTime = 2f;
    public bool freezePlayerWhileCharging = true;

    // Private fields
    private Vector2 dashDirection;
    private float currentDashPower;
    private int currentDashDamage;
    private float dashTimer = 0f;
    private float lastDashTime = -Mathf.Infinity;
    private float chargeTimer = 0f;
    private bool isCharging = false;
    private bool isDashing = false;
    private Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;

        if(mainCamera == null)
        {
            UnityEngine.Debug.Log("Camera could not be found");
        }
    }

    void Update()
    {
        // Still need to add animation stuff and UI stuff here.

        // If is charging increase charge timer
        if(isCharging)
        {
            // Increment charge timer
            chargeTimer += Time.deltaTime;
            chargeTimer = Mathf.Min(chargeTimer, maxChargeTime);

            // Calculate current dash power based on charge
            float chargePercent = chargeTimer / maxChargeTime;
            currentDashPower = Mathf.Lerp(minDashPower, maxDashPower, chargePercent);
            currentDashDamage = Mathf.RoundToInt(Mathf.Lerp(minDashDamage, maxDashDamage, chargePercent));

            // Update dash direction to follow mouse
            // dashDirection = GetMouseDirection();

            // Keep player frozen if enabled
            if (freezePlayerWhileCharging)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }       
        // If currently dashing reduce dash timer and if dash is done end the dash
        else if (isDashing)
        {
            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0f)
            {
                EndDash();
            }
        }
    }

    // Starts ability, only check that button was pressed rest of the script handles everything else 
    public override void Execute(InputAction.CallbackContext context)
    {
        if(StateCheck())
        {   
            // If the player holds the mouse down 
            if (context.started)
            {
                SetPlayerState(PlayerState.Dashing);

                isCharging = true;
                chargeTimer = 0f;

                if (freezePlayerWhileCharging)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.gravityScale = 0f;
                }
            }
            else if (context.canceled && isCharging)
            {
                StartDash();
            }

            // Pause player while dash charging 

            // Launch player toward mouse direction increased by dash charge 
            // 

        }
    }

    // Checks if the player can enter / use this ability
    protected override bool StateCheck()
    {
        return BaseStateCheck() && !isDashing && !isCharging && Time.time >= lastDashTime + dashCooldown;
    }

    private void StartDash()
    {
        // Get mouse position.
        Vector2 mousePos = GetMouseDirection();

        if (dashDirection == Vector2.zero || dashDirection == null)
        {
            CancelDash();
            return;
        }

        // Set fields properly to handle state etc
        isCharging = false;
        isDashing = true;
        dashTimer = dashDuration;
        lastDashTime = Time.time;

        // Apply dash velocity
        rb.gravityScale = 0f;
        rb.linearVelocity = dashDirection * currentDashPower;

        UnityEngine.Debug.Log($"Dashing with power: {currentDashPower}, damage: {currentDashDamage}"); 
    }

    private void EndDash()
    {
        isCharging = false;
        isDashing = false;
        ResetGravityScale();
    }

    // If the player cancels a dash
    private void CancelDash()
    {
        isCharging = false;
        isDashing = false;
        ResetGravityScale();
        UnityEngine.Debug.Log("Dash Cancelled");
    }

    private Vector2 GetMouseDirection()
    {
        // Get mouse position in screen space
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        
        // Convert to world space
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        
        // Calculate direction from player to mouse
        Vector2 direction = ((Vector2)mouseWorldPos - (Vector2)transform.position).normalized;
        
        return direction;
    }
}