using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

// Glide ability, holding jump once all jumps are used slows the fall and movement until released or out of charge.
// Glide is a movement modifier, it does not take ownership of the player state so dashing out of a glide still works.
// PlayerMovement forwards the jump input here and asks this script for gravity and move speed while gliding.
public class PlayerGlide : AbstractAbility
{
    [Header("Glide Settings")]

    public int glideCharge = 100;

    public int maxGlideCharge = 100;

    public float glideMoveSpeed = 1.5f;

    public float glideGravityScale = 0.5f;

    public float minGlideFallSpeed = -2f;

    public Slider glideSlider;

    public bool isGliding = false;

    private float glideTimer = 0f;

    [SerializeField] private AnimationCurve glideSpeedByTime = new AnimationCurve(
        new Keyframe(0f, 1f), 
        new Keyframe(1f, 1f)
    );

    // Current curve value
    private float glideSpeedModifier => glideSpeedByTime.Evaluate(glideTimer);

    // Eased glide move speed
    public float CurrentGlideMoveSpeed => Mathf.Lerp(player.moveSpeed, glideMoveSpeed, glideSpeedModifier);

    void Start()
    {
        UpdateGlideSlider();

        // Decrease glide charge while gliding
        StartCoroutine(DecreaseGlideCharge());
    }

    protected override void Update()
    {
        base.Update();

        if (isGliding)
            glideTimer += Time.deltaTime;
    }


    // Press starts a glide if allowed, release ends it
    public override void Execute(InputAction.CallbackContext context)
    {
        if (context.performed && StateCheck())
        {
            glideTimer = 0f;
            isGliding = true;
            animator.SetBool("isGliding", true);
        }

        if (context.canceled && isGliding)
        {
            StopGlide();
        }
    }

    // Can glide while in the air with charge left and not already gliding
    protected override bool StateCheck()
    {
        return !player.isGrounded && !isGliding && glideCharge > 0;
    }

    public override void Cancel()
    {
        StopGlide();
    }

    // Called by PlayerMovement each frame while gliding
    public void ApplyGlideGravity()
    {
        float glideAmount = glideSpeedModifier;

        // Ease gravity
        float normalFallGravity = player.baseGravity * player.fallSpeedMultiplier;
        rb.gravityScale = Mathf.Lerp(normalFallGravity, glideGravityScale, glideAmount);

        // Ease fall limit
        float fallLimit = Mathf.Lerp(-player.maxFallSpeed, minGlideFallSpeed, glideAmount);
        if (rb.linearVelocity.y < fallLimit)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fallLimit);
        }
    }

    // Called by PlayerMovement on landing, ends the glide and refills the charge
    public void OnLanded()
    {
        StopGlide();

        glideCharge = maxGlideCharge;
        UpdateGlideSlider();
    }

    private void StopGlide()
    {
        isGliding = false;
        animator.SetBool("isGliding", false);
    }

    private void UpdateGlideSlider()
    {
        if (glideSlider)
        {
            glideSlider.maxValue = maxGlideCharge;
            glideSlider.value = glideCharge;
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
                UpdateGlideSlider();

                // Stop gliding if out of charge
                if (glideCharge <= 0)
                {
                    StopGlide();
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }
}
