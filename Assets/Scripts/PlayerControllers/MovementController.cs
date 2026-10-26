using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class MovementController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private AnimationController animationController;
    [SerializeField] private float baseMoveSpeed = 6.5f;
    [SerializeField] private float maxMoveSpeed = 16f;
    [SerializeField] private float movementAcceleration = 8f;
    [SerializeField] private float movementDeceleration = 4f;

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
    [SerializeField] private float dashCooldown = 0.8f;
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


    public bool movementEnabled = true;

    private PlayerBehaviour playerBehaviour;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isGrounded;
    private bool isObstructed;
    private int remainingJumps;
    private int lastJumpDir = 0; // 0 = none, -1 = left, 1 = right

    private bool isWallSliding;
    private bool isWallJumping;

    private bool isDashing;
    private bool isSliding;
    private bool isZiplining;
    private Zipline activeZipline;
    private bool canDash = true;
    private float gravityScale;
    private float movementMultiplier = 1f;

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
        if (isDashing) return;
        if (isZiplining)
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
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

        Vector2 input = Vector2.zero;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            input.x -= 1f;
        }

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            input.x += 1f;
        }
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            input.y += 1f;
        }
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
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

        float xSpeed = Math.Abs(rb.linearVelocityX);
        float ySpeed = Math.Abs(rb.linearVelocityY);
        animationController.SetSpeed(xSpeed/maxRunningSpeed);
        // Debug.Log(xSpeed);

        if (isGrounded)
        {
            coyoteCounter = coyoteDur;
            remainingJumps = maxExtraJumps;

            if (xSpeed > 1f)
            {
                groundParticles.Play();
                
                if (ySpeed <= 0.5f)
                {
                    animationController.SetState(AnimationState.Run);
                }
            } 
            else
            {
                groundParticles.Stop();
                
                if (ySpeed <= 0.5f)
                {
                    animationController.SetState(AnimationState.Idle);
                }
            }
        }
        else
        {
            // animationController.SetState(AnimationState.Fall);
            coyoteCounter -= Time.deltaTime;
            groundParticles.Stop();
            runParticles.Stop();
        }

        if (xSpeed >= runningVelocity) // probably inefficient
        {
            runParticles.Play();
        }
        else
        {
            runParticles.Stop();
        }

        // Jump (max height)
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
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

        //slide on ctrl press
        if (Keyboard.current.ctrlKey.wasPressedThisFrame && isGrounded){
            isSliding = true;
        }
        if (Keyboard.current.ctrlKey.wasReleasedThisFrame && isGrounded){
            isSliding = false;
        }


        // Jump (soft jump)
        if (Keyboard.current.spaceKey.wasReleasedThisFrame && rb.linearVelocity.y > 0)
        {
            animationController.SetState(AnimationState.Jump);
            float jumpCutImpulse = rb.linearVelocity.y * (jumpCutMultiplier - 1f) * rb.mass;
            rb.AddForce(Vector2.up * jumpCutImpulse, ForceMode2D.Impulse);
        }

        // Input.GetKeyDown
        if (Keyboard.current.leftShiftKey.wasPressedThisFrame && canDash)
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
        // Removed "IsTouchingWall() && " since its already checked in update
        isWallSliding = rb.linearVelocity.y <= 0f;
        if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(0, -wallSlideSpeed);
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
            Debug.Log("Wall Jump Right Wall");
            lastJumpDir = 1;

            Vector2 jumpDirection = direction.normalized;

            Vector2 targetVelocity = new Vector2(-1 * wallJumpForce, wallJumpForce);
            rb.AddForce((targetVelocity - rb.linearVelocity) * rb.mass, ForceMode2D.Impulse);
            SFXManager.instance.PlaySFX(wallJumpSFX, transform, 1f, UnityEngine.Random.Range(2.5f, 3));
            animationController.SetState(AnimationState.Jump);

            yield return new WaitForSeconds(wallJumpDuration);
        }
        else if (Physics2D.OverlapBox(wallCheckLeft.position, wallCheckLeftSize, 0f, wallLayer) && lastJumpDir != -1)
        {
            Debug.Log("Wall Jump Left Wall");
            lastJumpDir = -1;

            Vector2 jumpDirection = direction.normalized;
            Vector2 targetVelocity = new Vector2(1 * wallJumpForce, wallJumpForce);
            rb.AddForce((targetVelocity - rb.linearVelocity) * rb.mass, ForceMode2D.Impulse);
            SFXManager.instance.PlaySFX(wallJumpSFX, transform, 1f, UnityEngine.Random.Range(2.5f, 3));
            animationController.SetState(AnimationState.Jump);

            yield return new WaitForSeconds(wallJumpDuration);
        }
        canDash = true; // Allow dashing after wall jump
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

        rb.gravityScale = gravityScale;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerBehaviour = GetComponent<PlayerBehaviour>();
        gravityScale = rb.gravityScale;
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
            rb.freezeRotation = false;
            SFXManager.instance.PlaySFX(landSFX, transform, 0.1f, UnityEngine.Random.Range(1, 1.3f));
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
        float targetMoveSpeed = Mathf.Min(baseMoveSpeed * movementMultiplier, maxMoveSpeed);

        if (moveInput.x != 0f)
        {
            float inputDirection = Mathf.Sign(moveInput.x);
            float speedInInputDirection = horizontalSpeed * inputDirection;
            if (horizontalSpeed < baseMoveSpeed)
            {
                rb.AddForce(Vector2.right * inputDirection * baseMoveSpeed);
            }
            if ((speedInInputDirection > baseMoveSpeed) && !isSliding)
            {
                rb.AddForce(Vector2.right * -inputDirection * movementDeceleration * rb.mass);
            }
            else if (speedInInputDirection < targetMoveSpeed)
            {
                float acceleration = Mathf.Min(
                    movementAcceleration,
                    (targetMoveSpeed - speedInInputDirection) / Time.fixedDeltaTime);// Calculate the required acceleration to reach the target speed
                rb.AddForce(Vector2.right * inputDirection * acceleration * rb.mass);
            }
        }
        //Debug.Log($"Horizontal Speed: {horizontalSpeed}, Target Move Speed: {targetMoveSpeed}");
    }
}
