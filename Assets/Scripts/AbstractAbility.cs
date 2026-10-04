using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;


// Abstract class for abilites to inherit from.
// Abilities sit on the player next to PlayerMovement and hold a reference to it, so every ability
// shares the one player state. While an ability is charging or active it registers itself as the
// players activeAbility, which tells PlayerMovement and other abilities who owns the current state.
// Once the ability is over it releases ownership and the state goes back to grounded or in air.

[RequireComponent(typeof(PlayerMovement), typeof(Rigidbody2D), typeof(Animator))]
public abstract class AbstractAbility : MonoBehaviour
{
    [Header("After Ability")]
    // How long the after ability window lasts once the ability ends, gravity eases back to normal over this time, 0 turns it off
    [FormerlySerializedAs("gravityRampTime")]
    public float afterAbilityDuration = 0.75f;

    // Time left in the after ability window, counts down to 0
    public float afterAbilityTimer = 0f;

    // Where gravity starts once the ability ends, as a multiple of base gravity (1 is base gravity, 0.5 is half)
    public float gravityRampStart = 1f;

    protected PlayerMovement player;
    protected Rigidbody2D rb;
    protected Animator animator;

    // True while the ability has recently ended
    public bool InAfterAbilityWindow => afterAbilityTimer > 0f;

    // How far through the after ability window we are, 0 just ended and 1 finished
    public float AfterAbilityProgress => afterAbilityDuration > 0f ? 1f - (afterAbilityTimer / afterAbilityDuration) : 1f;

    // True while the ability controls velocity and gravity, PlayerMovement stands aside while this is true.
    public virtual bool OverridesMovement => false;

    // True while the ability damages enemies it touches, the player can not be hurt while this is true.
    public virtual bool IsAttacking => false;

    // Damage dealt to enemies while IsAttacking is true.
    public virtual int Damage => 0;

    // True if this ability currently owns the players state.
    protected bool IsActiveAbility => player.activeAbility == this;

    protected virtual void Awake()
    {
        player = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    // Abilities that need their own Update must override this and call base.Update() so the timer keeps ticking
    protected virtual void Update()
    {
        // Tick down the after ability window
        if (afterAbilityTimer > 0f)
            afterAbilityTimer = Mathf.Max(afterAbilityTimer - Time.deltaTime, 0f);
    }

    // Executes the ability.
    public abstract void Execute(InputAction.CallbackContext context);

    // Cancels the ability if it is charging or active (e.g. player took damage or pressed a cancel input).
    public abstract void Cancel();

    // Checks for all requirements met before executing ability.
    protected abstract bool StateCheck();

    // Restarts the after ability window
    public void ResetAfterAbilityTimer()
    {
        afterAbilityTimer = afterAbilityDuration;
    }

    // A basic check which most abilities can use.
    protected bool BaseStateCheck()
    {
        bool baseStates = player.currentState switch
        {
            PlayerState.Idle => true,
            PlayerState.Running => true,
            PlayerState.Jumping => true,
            PlayerState.Falling => true,
            _ => false
        };

        return baseStates;
    }

    // Claims the players state for this ability.
    protected void BeginAbility(PlayerState state)
    {
        player.activeAbility = this;
        player.SetPlayerState(state);
    }

    // Releases ownership, starts the after ability window and sets the state back to normal movement.
    protected void EndAbility()
    {
        ResetAfterAbilityTimer();
        player.OnAbilityEnded(this);

        if (IsActiveAbility)
        {
            player.activeAbility = null;
            player.SetPlayerState(GetCurrPlayerState());
        }
    }

    // After an ability is finished checks what the current PlayerState should be.
    protected PlayerState GetCurrPlayerState()
    {
        return player.GetMovementState();
    }
}
