using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

[RequireComponent(typeof(Rigidbody2D))]
public class MovementController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private AnimationController animationController;
    [SerializeField] private TypingSkillCheck skillCheck;

    [SerializeField] private float maxMoveSpeed = 10f;
    [SerializeField] private float movementAcceleration = 14f;
    [SerializeField] private float movementDeceleration = 60f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float jumpCutMultiplier = 0.5f; // Short jump on early button release
    [SerializeField] private int maxExtraJumps = 0;

    [Header("Wall Jump Settings")]
    [SerializeField] private float wallJumpForce = 5f;
    [SerializeField] private float wallJumpDuration = 0.2f;
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private float wallCheckDistance = 0.2f;



    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDur = 0.2f;
    [SerializeField] private float dashCooldown = 2f;
    [SerializeField] private float dashMovementBoost = 0.5f;
    [SerializeField] private float maxMovementMultiplier = 2f;
    [SerializeField] private float movementBoostRecovery = 0.75f;

    [Header("Coyote Time")]
    [SerializeField] private float coyoteDur = 0.15f;
    private float coyoteCounter;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.6f, 0.2f);
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header ("Wall Check")]
    [SerializeField] private Transform wallCheckRight;
    [SerializeField] private Transform wallCheckLeft;
    [SerializeField] private Vector2 wallCheckRightSize = new Vector2(0.1f, 1.6f);
    [SerializeField] private Vector2 wallCheckLeftSize = new Vector2(0.1f, 1.6f);
    [SerializeField] private LayerMask wallLayer;

    [Header ("Particles")]
    [SerializeField] private ParticleSet groundParticles; // activates when moving 
    [SerializeField] private ParticleSet runParticles; // activates when vel >= x
    private float runningVelocity = 4f;
    private float maxRunningSpeed = 10f; // don't change, for animations

    [Header ("SFX")]
    [SerializeField] private AudioClip jumpSFX;
    [SerializeField] private AudioClip wallJumpSFX;
    [SerializeField] private AudioClip slideSFX;
    [SerializeField] private AudioClip dashSFX;
    [SerializeField] private AudioClip landSFX;

    [SerializeField] private Stopwatch_Speed stopwatchSpeed;


    public bool movementEnabled = true;

    private PlayerBehaviour playerBehaviour;
    private Rigidbody2D rb;
    private CapsuleCollider2D collider;
    private Vector2 moveInput;
    private bool isGrounded;
    private bool isObstructed;
    private int remainingJumps;
    private int lastJumpDir = 0; // 0 = none, -1 = left, 1 = right

    private bool isWallSliding;
    private bool isWallJumping;

    private bool hasWallJumped;

    private bool isDashing;
    private bool isSliding;
    private bool isZiplining;
    private Zipline activeZipline;
    private bool canDash = true;
    private bool canMove = true;
    private float gravityScale;
    private float movementMultiplier = 1f;
    private MovingPlatform currentPlatform;

    private Vector2 direction = Vector2.right;

    public bool IsDashing => isDashing;
    public float DashSpeed => dashSpeed;
    public float NormalGravityScale => gravityScale;

    public void AttachToZipline(Zipline zipline)
    {
        activeZipline = zipline;
        isZiplining = true;
        moveInput = Vector2.zero;
        coyoteCounter = 0f;
        rb.gravityScale = 0f;
    }

    public void SetZiplineActive(bool active)
    {
        isZiplining = active;
        if (active)
        {
            moveInput = Vector2.zero;
            rb.gravityScale = 0f;
        }
        else
        {
            activeZipline = null;
            rb.gravityScale = gravityScale;
        }
    }

    private void ProcessInput()
    {
        // Debug.Log(skillCheck.GetIsActive());
        if (PlayerKeybinds.WasPressed(PlayerKeybinds.FreeCam)&&(!skillCheck.GetIsActive()))
        {
            FreeCamera.ToggleFreeCam();
        }

        if (isDashing) return;
        Keyboard keyboard = Keyboard.current;
        if (isZiplining)
        {
            if (PlayerKeybinds.WasPressed(PlayerKeybinds.Jump))
            {
                JumpOffZipline();
            }
            return;
        }

        if (!movementEnabled)
        {
            moveInput = Vector2.zero;
            return;
        }

        if (keyboard == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        Vector2 input = Vector2.zero;
        if (PlayerKeybinds.IsPressed(PlayerKeybinds.MoveLeft, PlayerKeybinds.MoveLeftAlternate)&&canMove)
        {
            input.x -= 1f;
        }

        if (PlayerKeybinds.IsPressed(PlayerKeybinds.MoveRight, PlayerKeybinds.MoveRightAlternate)&&canMove)
        {
            input.x += 1f;
        }
        if (PlayerKeybinds.IsPressed(PlayerKeybinds.MoveUp, PlayerKeybinds.MoveUpAlternate)&&canMove)  
        {
            input.y += 1f;
        }
        if (PlayerKeybinds.IsPressed(PlayerKeybinds.MoveDown, PlayerKeybinds.MoveDownAlternate)&&canMove)
        {
            input.y -= 1f;
        }

        moveInput = input;
        if (input.x != 0f && !IsTouchingWall())
        {
            direction = new Vector2(Mathf.Sign(input.x), 0f);

            if (direction.x > 0)
            {
                sprite.flipX = false;
                runParticles.Flip(true);
                groundParticles.Flip(true);
            } else
            {
                sprite.flipX = true;
                runParticles.Flip(false);
                groundParticles.Flip(false);
            }

        }

        float moveAmount = Mathf.Abs(moveInput.x);
        float xSpeed = Mathf.Abs(rb.linearVelocityX);
        animationController.SetSpeed(moveAmount);

        if (isGrounded)
        {
            coyoteCounter = coyoteDur;
            remainingJumps = maxExtraJumps;

            if (PlayerKeybinds.IsPressed(PlayerKeybinds.Slide, PlayerKeybinds.SlideAlternate))
            {
                isSliding = true;
                canMove = false;
            }

            if (PlayerKeybinds.WasReleased(PlayerKeybinds.Slide, PlayerKeybinds.SlideAlternate))
            {
                isSliding = false;
                canMove = true;
            }

            if (moveAmount > 0.01f && !IsTouchingWall())
            {
                groundParticles.Play();
                animationController.SetState(AnimationState.Run);
            } 
            else
            {
                groundParticles.Stop();
                animationController.SetState(AnimationState.Idle);
            }
        }
        else
        {
            // animationController.SetState(AnimationState.Fall);
            coyoteCounter -= Time.deltaTime;
            groundParticles.Stop();
            runParticles.Stop();
        }

        if (isGrounded && moveAmount > 0.01f && !IsTouchingWall() && xSpeed >= runningVelocity)
        {
            runParticles.Play();
        }
        else
        {
            runParticles.Stop();
        }

        // Jump (max height)
        if (PlayerKeybinds.WasPressed(PlayerKeybinds.Jump))
        {
            if (IsTouchingWall() && !isGrounded)
            {
                // Debug.Log(isWallSliding);
                StartCoroutine(WallJump());
            }
            else
            {
                HandleJump();
            }
        }

        // Jump (soft jump)
        if (PlayerKeybinds.WasReleased(PlayerKeybinds.Jump) && rb.linearVelocity.y > 0)
        {
            animationController.SetState(AnimationState.Jump);
            float jumpCutImpulse = rb.linearVelocity.y * (jumpCutMultiplier - 1f) * rb.mass;
            rb.AddForce(Vector2.up * jumpCutImpulse, ForceMode2D.Impulse);
        }

        // Input.GetKeyDown
        if (PlayerKeybinds.WasPressed(PlayerKeybinds.Dash) && canDash)
        {
            StartCoroutine(Dash());
        }
    }

    private void JumpOffZipline()
    {
        if (activeZipline == null || !activeZipline.TryDetachRider(this, out Vector2 exitVelocity))
        {
            return;
        }

        SetZiplineActive(false);
        rb.linearVelocity = new Vector2(
            exitVelocity.x,
            Mathf.Max(exitVelocity.y, jumpForce));
    }

    // ---- JUMP ---- \\

    private void HandleJump()
    {
        if (coyoteCounter > 0f)
        {
            ExecuteJump();
            coyoteCounter = 0f;
        }
        else if (remainingJumps > 0)
        {
            ExecuteJump();
            remainingJumps--;
        }
    }

    private void ExecuteJump()
    {
        animationController.SetState(AnimationState.Jump);
        float jumpImpulse = (jumpForce - rb.linearVelocity.y) * rb.mass;
        rb.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
        SFXManager.instance.PlaySFX(jumpSFX, transform, 1f, UnityEngine.Random.Range(1, 3));
    }

    // ---- WALL JUMP ---- \\

    private bool IsTouchingWall()
    {
        return Physics2D.OverlapBox(wallCheckRight.position, wallCheckRightSize, 0f, wallLayer) || Physics2D.OverlapBox(wallCheckLeft.position, wallCheckLeftSize, 0f, wallLayer);
    }

    private void WallSlide()
    {
        float input = 0f;

        if (Physics2D.OverlapBox(wallCheckRight.position, wallCheckRightSize, 0f, wallLayer)) 
        { sprite.flipX = false; }
        else if (Physics2D.OverlapBox(wallCheckLeft.position, wallCheckLeftSize, 0f, wallLayer))
        { sprite.flipX = true; }

        if (PlayerKeybinds.IsPressed(PlayerKeybinds.MoveLeft, PlayerKeybinds.MoveLeftAlternate) 
            && Physics2D.OverlapBox(wallCheckLeft.position, wallCheckLeftSize, 0f, wallLayer)) {
            input = -1f;
        }
        else if (PlayerKeybinds.IsPressed(PlayerKeybinds.MoveRight, PlayerKeybinds.MoveRightAlternate) 
            && Physics2D.OverlapBox(wallCheckRight.position, wallCheckRightSize, 0f, wallLayer)) {
            input = 1f;
        }

        else { input = 0f; }

            // Removed "IsTouchingWall() && " since its already checked in update
        isWallSliding = rb.linearVelocity.y <= 0f;
        //
        if (isWallSliding && input != 0)
        {
            rb.linearVelocity = new Vector2(input, -wallSlideSpeed);
            animationController.SetState(AnimationState.WallSlide);
            //SFXManager.instance.PlaySFX(slideSFX, transform, 0.1f, UnityEngine.Random.Range(1, 3));
        }
        // Player still has animation when on a wall going down, but the act of sliding is gone sorta.
        else if (isWallSliding)
        {
            animationController.SetState(AnimationState.WallSlide);
            SFXManager.instance.PlaySFX(slideSFX, transform, 0.1f, UnityEngine.Random.Range(1, 3));
        }
    }

    private IEnumerator WallJump()
    {
        if (!isWallSliding || isGrounded)
        {
            yield break; // Exit if not wall sliding
        }

        isWallJumping = true;
        if (Physics2D.OverlapBox(wallCheckRight.position, wallCheckRightSize, 0f, wallLayer) && lastJumpDir != 1)
        {
           // Debug.Log("Wall Jump Right Wall");
            lastJumpDir = 1;
            Vector2 jumpDirection = direction.normalized;

            Vector2 targetVelocity = new Vector2(-1 * wallJumpForce, wallJumpForce);
            rb.AddForce((targetVelocity - rb.linearVelocity) * rb.mass, ForceMode2D.Impulse);
            SFXManager.instance.PlaySFX(wallJumpSFX, transform, 1f, UnityEngine.Random.Range(2.5f, 3));
            animationController.SetState(AnimationState.Jump);

            if (canDash==false) hasWallJumped = true;
            toggleDash();
            stopwatchSpeed.ResetStopwatch();
            yield return new WaitForSeconds(wallJumpDuration);
        }
        else if (Physics2D.OverlapBox(wallCheckLeft.position, wallCheckLeftSize, 0f, wallLayer) && lastJumpDir != -1)
        {
          //  Debug.Log("Wall Jump Left Wall");
            lastJumpDir = -1;

            Vector2 jumpDirection = direction.normalized;
            Vector2 targetVelocity = new Vector2(1 * wallJumpForce, wallJumpForce);
            rb.AddForce((targetVelocity - rb.linearVelocity) * rb.mass, ForceMode2D.Impulse);
            SFXManager.instance.PlaySFX(wallJumpSFX, transform, 1f, UnityEngine.Random.Range(2.5f, 3));
            animationController.SetState(AnimationState.Jump);

            if (canDash==false) hasWallJumped = true;
            toggleDash();
            stopwatchSpeed.ResetStopwatch();
            yield return new WaitForSeconds(wallJumpDuration);
        }
        isWallJumping = false;   

    }

    // ---- DASH ---- \\

    private IEnumerator Dash()
    {
        animationController.SetState(AnimationState.Run);
        canDash = false;
        isDashing = true;
        movementMultiplier = Mathf.Min(
            movementMultiplier + dashMovementBoost,
            maxMovementMultiplier);
        rb.gravityScale = 0f; // temporarily disable gravity

        //Vector2 dashDir = moveInput.normalized;
        Vector2 dashDir = direction.normalized;
        //if (dashDir == Vector2.zero)
        //{
        //    dashDir = new Vector2(moveInput.x > 0 ? 1f : -1f, 0f);
        //}

        rb.AddForce(dashDir * dashSpeed * rb.mass, ForceMode2D.Impulse);
        SFXManager.instance.PlaySFX(dashSFX, transform, 1f, UnityEngine.Random.Range(1, 1.3f));

        yield return new WaitForSeconds(dashDur);
        StartCoroutine(stopwatchSpeed.UpdateStopWatch());

        rb.gravityScale = gravityScale;
        isDashing = false;
        if (isGrounded){  
            yield return new WaitForSeconds(dashCooldown);
            if (hasWallJumped)
            {
                hasWallJumped = false;
                yield break;
            }
            else{
                toggleDash();
            }
        }
        else
        {
            yield return new WaitForSeconds(dashCooldown);
            if (hasWallJumped)
            {
                hasWallJumped = false;
                yield break;
            }
            else{
                toggleDash();
            }
        }
    }

    private void toggleDash()
    {
        canDash = true;
        //Debug.Log("Dash is now available");
    }

    // ---- CHECKS ---- \\

    private void CheckGrounded()
    {
        // if there is ever an issue with lastJumpDir, add an extra condition that
        // checks the normal of the collider and makes sure its facing upwards
        Collider2D hit = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        isGrounded = hit != null;

        if (isGrounded)
        {
            isWallSliding = false; 
            lastJumpDir = 0;
        }
    }

    private void CheckObstacle()
    {

        if (groundCheck != null)
        {
            isObstructed = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, obstacleLayer);

        }
    }

    // ---- DEBUGGING -- \\
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
            Gizmos.DrawWireCube(wallCheckRight.position, wallCheckRightSize);
            Gizmos.DrawWireCube(wallCheckLeft.position, wallCheckLeftSize);
        }
    }

    // ---- RUNTIEME METHODS ---- \\

    private void OnCollisionStay2D(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                currentPlatform = collision.gameObject.GetComponent<MovingPlatform>();
                return;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (currentPlatform != null && collision.gameObject == currentPlatform.gameObject)
        {
            currentPlatform = null;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerBehaviour = GetComponent<PlayerBehaviour>();
        gravityScale = rb.gravityScale;
        collider = GetComponentInChildren<CapsuleCollider2D>();
    }

    private void Update()
    {
        ProcessInput();
        CheckGrounded();
        CheckObstacle();
    }

    private void FixedUpdate()
    {
        if (isObstructed)
        {   // Destroys the script within the player object, might want to change to something else later
            playerBehaviour.RestartGame(5f);
            GameObject.Destroy(this);
            SFXManager.instance.PlaySFX(landSFX, transform, 0.1f, UnityEngine.Random.Range(1, 1.3f));
            animationController.SetState(AnimationState.Dead);
            collider.size = new Vector2(0.5f, 0.5f); // ye im lazuy
            // rb.freezeRotation = false;
        }

        if (isZiplining) return;

        if (IsTouchingWall() && !isGrounded)
        {
            WallSlide();
        } else
        {
            isWallSliding = false;
        }

        if (isDashing) return;

        float horizontalSpeed = rb.linearVelocity.x;
        float inputDirection = Mathf.Sign(moveInput.x);
        float direction = Mathf.Sign(horizontalSpeed);

        //Debug.Log(inputDirection);
        if (isGrounded)
        {
            if ((Mathf.Abs(horizontalSpeed) > maxMoveSpeed) && (!isSliding))
            {
                //Debug.Log("Decel");
                rb.AddForce(Vector2.right * -direction * movementDeceleration * rb.mass);
            }
            else if ((moveInput.x!=0) && (horizontalSpeed < maxMoveSpeed))
            {
                //Debug.Log(horizontalSpeed);
                rb.AddForce(Vector2.right * inputDirection * movementAcceleration * rb.mass);
            }
        }
    }
}
