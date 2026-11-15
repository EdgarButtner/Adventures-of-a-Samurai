using UnityEngine;

// Drives the animator and facing from PlayerState
// State lives in PlayerMovement, this only reacts to it
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class PlayerAnimController : MonoBehaviour
{
    // Found automatically if empty
    public PlayerMovement playerMovement;

    private Animator animator;
    private Rigidbody2D rb;

    // State driven params
    private static readonly int IsJumpingHash = Animator.StringToHash("isJumping");
    private static readonly int IsDashingHash = Animator.StringToHash("isDashing");
    private static readonly int IsRunningHash = Animator.StringToHash("isRunning");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private const string DashAttackState = "DashAttack";

    // Per frame params
    private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");
    private static readonly int XVelocityHash = Animator.StringToHash("xVelocity");
    private static readonly int YVelocityHash = Animator.StringToHash("yVelocity");
    private static readonly int IsWallSlidingHash = Animator.StringToHash("isWallSliding");
    private static readonly int IsGlidingHash = Animator.StringToHash("isGliding");

    // Read only view of the real state
    public PlayerState State => playerMovement.CurrentState;

    public bool IsFacingRight { get; private set; } = true;

    // Positive right, negative left
    public float FacingSign => IsFacingRight ? 1f : -1f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        // Match scene facing
        IsFacingRight = transform.localScale.x >= 0f;
    }

    void OnEnable()
    {
        playerMovement.StateChanged += OnStateChanged;
    }

    void OnDisable()
    {
        playerMovement.StateChanged -= OnStateChanged;
    }

    void Start()
    {
        // Sync to starting state
        ApplyState(State);
    }

    // After movement and physics
    void LateUpdate()
    {
        animator.SetBool(IsGroundedHash, playerMovement.isGrounded);
        animator.SetFloat(XVelocityHash, Mathf.Abs(rb.linearVelocity.x));
        animator.SetFloat(YVelocityHash, rb.linearVelocity.y);
        animator.SetBool(IsWallSlidingHash, playerMovement.isWallSliding);
        animator.SetBool(IsGlidingHash, playerMovement.IsGliding);
    }

    // State

    private void OnStateChanged(PlayerState previous, PlayerState next)
    {
        ApplyState(next);

        // One shot animations on enter
        if (next == PlayerState.Dashing)
            animator.Play(DashAttackState);
        else if (next == PlayerState.Dead && HasParameter(DieHash))
            animator.SetTrigger(DieHash);
    }

    // Every state bool comes from here
    private void ApplyState(PlayerState state)
    {
        animator.SetBool(IsJumpingHash, state == PlayerState.Jumping);
        animator.SetBool(IsDashingHash, state == PlayerState.Dashing);
        animator.SetBool(IsRunningHash, state == PlayerState.Running);
    }

    // Facing

    public void SetFacing(bool faceRight)
    {
        if (IsFacingRight == faceRight)
            return;

        IsFacingRight = faceRight;
        Vector3 ls = transform.localScale;
        ls.x = Mathf.Abs(ls.x) * FacingSign;
        transform.localScale = ls;
    }

    // Ignores near zero
    public void FaceDirection(float x)
    {
        if (x > 0.01f)
            SetFacing(true);
        else if (x < -0.01f)
            SetFacing(false);
    }

    private bool HasParameter(int hash)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == hash)
                return true;
        }

        return false;
    }
}
