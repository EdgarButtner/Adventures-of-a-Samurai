using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDash : AbstractAbility
{
    [Header("Dash Settings")]
    public float minDashPower = 2f;
    public float maxDashPower = 10f;
    public int minDashDamage = 15;
    public int maxDashDamage = 50;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    // Decel based on charge amount / time
    [SerializeField] private AnimationCurve dashStopByCharge = new AnimationCurve(
        new Keyframe(0f, 0.58f),
        new Keyframe(1f, 0.58f));

    private const float MinExitDeceleration = 0.5f;
    private const float MaxExitDeceleration = 1000f;

    // How much momentum a fully charged dash keeps compared to a short one, 1 keeps it all and 0.5 halves the slide
    // Scales in with charge so short dashes are untouched
    [SerializeField, Range(0.05f, 5f)] private float longDashMomentumScale = 0.5f;

    // Upward cone, how much upward momentum is kept based on how steep the dash was
    // X is degrees above horizontal from 0 to 90, Y is the amount kept, 1 keeps it all and 0.5 roughly halves the rise
    [SerializeField] private AnimationCurve upwardMomentumByAngle = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(45f, 1f),
        new Keyframe(90f, 0.25f));

    [Header("Charge Settings")]
    public float maxChargeTime = 2f;
    public bool freezePlayerWhileCharging = true;

    [Header("Aim Line")]
    // Line shown while charging, it points at the mouse and its length is how far the dash will go
    public LineRenderer dashLine;

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

    // PlayerMovement stands aside while dashing, and while charging if the player is frozen
    public override bool OverridesMovement => isDashing || (isCharging && freezePlayerWhileCharging);
    public override bool IsAttacking => isDashing;
    public override int Damage => currentDashDamage;

    protected override void Awake()
    {
        base.Awake();

        mainCamera = Camera.main;

        if(mainCamera == null)
        {
            Debug.Log("Camera could not be found");
        }

        SetDashLineVisible(false);
    }

    protected override void Update()
    {
        // Keeps the after ability timer ticking
        base.Update();

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

            // Stretch the aim line toward the mouse as the charge builds
            UpdateDashLine();

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
        // On press, begin charging if the player is allowed to dash
        if (context.started && StateCheck())
        {
            BeginAbility(PlayerState.Charging);

            isCharging = true;
            chargeTimer = 0f;

            // Reset to minimum in case the button is released before the next Update
            currentDashPower = minDashPower;
            currentDashDamage = minDashDamage;

            if (freezePlayerWhileCharging)
            {
                rb.linearVelocity = Vector2.zero;
                rb.gravityScale = 0f;
            }
            // If not frozen, drop any upward jump momentum so the player falls while charging
            else if (rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            }

            UpdateDashLine();
            SetDashLineVisible(true);
        }
        // On release, launch only if this ability owns the current charge (charge may have been cancelled)
        else if (context.canceled && isCharging && IsActiveAbility)
        {
            StartDash();
        }
    }

    // Checks if the player can enter / use this ability
    protected override bool StateCheck()
    {
        return BaseStateCheck() && !isDashing && !isCharging && Time.time >= lastDashTime + dashCooldown;
    }

    // Lets the dash be used again straight away (e.g. after killing an enemy)
    public void ResetCooldown()
    {
        lastDashTime = -Mathf.Infinity;
    }

    private void StartDash()
    {
        SetDashLineVisible(false);

        // Get direction from player to mouse.
        dashDirection = GetMouseDirection();

        if (dashDirection == Vector2.zero)
        {
            Cancel();
            return;
        }

        // Set fields properly to handle state etc
        player.SetPlayerState(PlayerState.Dashing);
        isCharging = false;
        isDashing = true;
        dashTimer = dashDuration;
        lastDashTime = Time.time;

        // Replace all velocity with the dash velocity, this dumps any vertical momentum from a jump
        rb.gravityScale = 0f;
        rb.linearVelocity = dashDirection * currentDashPower;

        // Override blend tree with dash animation
        animator.SetBool("isDashing", true);
        animator.Play("DashAttack");

        Debug.Log($"Dashing with power: {currentDashPower}, damage: {currentDashDamage}");
    }

    private void EndDash()
    {
        isCharging = false;
        isDashing = false;
        animator.SetBool("isDashing", false);

        // Carry on with exactly the dash velocity so nothing snaps, and nothing from before the dash leaks in
        rb.linearVelocity = dashDirection * currentDashPower;

        EndAbility();

        // Longer dashes fade faster, dividing by the scale shrinks the slide distance by that same scale
        float chargePercent = maxChargeTime > 0f ? chargeTimer / maxChargeTime : 1f;
        float momentumScale = Mathf.Lerp(1f, longDashMomentumScale, chargePercent);
        float exitDeceleration = GetExitDeceleration(chargePercent) / momentumScale;

        // Steep upward dashes lose their upward momentum faster so the player does not go flying
        // Fading faster instead of cutting the speed keeps it smooth
        float dashAngle = Mathf.Asin(Mathf.Clamp(dashDirection.y, -1f, 1f)) * Mathf.Rad2Deg;
        float upwardScale = dashAngle > 0f ? Mathf.Max(upwardMomentumByAngle.Evaluate(dashAngle), 0.05f) : 1f;

        player.EaseOutOfAbility(exitDeceleration, exitDeceleration / upwardScale);
    }

    // Turns the 0 to 1 stop curve into a deceleration, 0 gives MinExitDeceleration and 1 gives MaxExitDeceleration
    private float GetExitDeceleration(float chargePercent)
    {
        float stopStrength = Mathf.Clamp01(dashStopByCharge.Evaluate(chargePercent));
        return MinExitDeceleration * Mathf.Pow(MaxExitDeceleration / MinExitDeceleration, stopStrength);
    }

    // Drops the dash momentum so the player falls from a standstill instead of carrying on
    private void ResetDashMomentum()
    {
        rb.linearVelocity = Vector2.zero;
        player.ResetGravityScale();
    }

    // Cancels a charge or dash in progress (e.g. player took damage or pressed a cancel input)
    public override void Cancel()
    {
        if (!isCharging && !isDashing)
            return;

        // A dash cut short should not keep its speed either
        if (isDashing)
            ResetDashMomentum();

        isCharging = false;
        isDashing = false;
        animator.SetBool("isDashing", false);
        SetDashLineVisible(false);
        EndAbility();
        Debug.Log("Dash Cancelled");
    }

    // Points the aim line at the mouse with the length the dash would travel right now
    private void UpdateDashLine()
    {
        if (dashLine == null)
            return;

        // No gravity during the dash so distance is just speed times time
        float dashDistance = currentDashPower * dashDuration;
        Vector2 start = transform.position;
        Vector2 end = start + GetMouseDirection() * dashDistance;

        dashLine.positionCount = 2;
        dashLine.SetPosition(0, start);
        dashLine.SetPosition(1, end);
    }

    private void SetDashLineVisible(bool visible)
    {
        if (dashLine != null)
            dashLine.enabled = visible;
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
