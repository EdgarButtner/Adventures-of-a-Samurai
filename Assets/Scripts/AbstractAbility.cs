using System;
using UnityEngine.InputSystem;


// Abstract class for abilites to inherit from.
// Allows abilities to also inherit from PlayerMovement allowing access to players current state.
// Basic execute function which each subclass overrides. Class automatically exits the state when
// ability is over. Once over check for grounded or in air then set state.

public abstract class AbstractAbility : PlayerMovement
{
    // Executes the ability.
    public abstract void Execute(InputAction.CallbackContext context);

    // Checks for all requirements met before executing ability.
    protected abstract Boolean StateCheck();

    // A basic check which most abilities can use.
    protected Boolean BaseStateCheck()
    {
        bool baseStates = currentState switch
        {
            PlayerState.Idle => true,
            PlayerState.Running => true,
            PlayerState.Jumping => true,
            PlayerState.Falling => true,
            _ => false
        };

        return baseStates;
    }

    // After an ability is finished checks what the current PlayerState should be and sets the currentPlayerState to this
    protected PlayerState GetCurrPlayerState()
    {
        return PlayerState.Falling;
    }
}