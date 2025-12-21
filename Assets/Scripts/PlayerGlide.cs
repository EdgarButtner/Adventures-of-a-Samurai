using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerGlide : PlayerMovement
{
    public Transform playerPos;
    public PlayerMovement playerMove;
    public int glideCharge = 100;
    public int maxGlideCharge = 100;
    public float glideMoveSpeed = 1.5f;
    public float glideGravityScale = 0.5f;
    public float minGlideFallSpeed = -2f;
    private bool isGliding = false;

    void Awake() 
    {
        playerPos = GameObject.FindGameObjectWithTag("Player").transform;
        playerMove = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
    }

    private void Glide(InputAction.CallbackContext context)
    {
        if(context.performed 
            && currentState == PlayerState.Falling
            && playerMove.glideCharge > 0
            && jumpCount < maxJumps
        ) {
            
        }
    }
}