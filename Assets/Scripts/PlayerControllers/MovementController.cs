using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class MovementController : MonoBehaviour
{
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
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.2f);
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header ("Wall Check")]
    [SerializeField] private Transform wallCheckRight;
    [SerializeField] private Transform wallCheckLeft;
    [SerializeField] private Vector2 wallCheckRightSize = new Vector2(0.1f, 1.6f);
    [SerializeField] private Vector2 wallCheckLeftSize = new Vector2(0.1f, 1.6f);
    [SerializeField] private LayerMask wallLayer;


    public bool movementEnabled = true;

    private PlayerBehaviour playerBehaviour;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isGrounded;
    private bool isObstructed;
    private int remainingJumps;

    private bool isWallSliding;
    private bool isWallJumping;

    private bool isDashing;
    private bool canDash = true;
    private float gravityScale;
    private float movementMultiplier = 1f;

    private Vector2 direction = Vector2.right;

    private void ProcessInput()
    {
        if (isDashing) return;
        if (!movementEnabled)
        {
            moveInput = Vector2.zero;
            return;
        }

        // so apparently this method is outdated in this unity version
        // HOWEVER, there is an option to re-enable it, although it may cause issues
        // moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // There are two methods, but im too lazy to learn how to do the second despite it being recommended
        // method 1: (works)

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
        if (input.x != 0f)
        {
            direction = new Vector2(Mathf.Sign(input.x), 0f);
        }

        // method 2 requires using something called an InputActionEvent. If someone wants to take the time to
        // learn how to link that up then cool
        // sorry for the technical debt

        if (isGrounded)
        {
            coyoteCounter = coyoteDur;
            remainingJumps = maxExtraJumps;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        // Input.GetKeyDown
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (IsTouchingWall())
            {
                Debug.Log(isWallSliding);
                isWallJumping = true;
                StartCoroutine(WallJump());

                isWallSliding = false;


            }
            else
            {
                HandleJump();

            }
        }

        // Input.GetKeyUp
        if (Keyboard.current.spaceKey.wasReleasedThisFrame && rb.linearVelocity.y > 0)
        {
            float jumpCutImpulse = rb.linearVelocity.y * (jumpCutMultiplier - 1f) * rb.mass;
            rb.AddForce(Vector2.up * jumpCutImpulse, ForceMode2D.Impulse);
        }

        // Input.GetKeyDown
        if (Keyboard.current.leftShiftKey.wasPressedThisFrame && canDash)
        {
            StartCoroutine(Dash());
        }
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
        float jumpImpulse = (jumpForce - rb.linearVelocity.y) * rb.mass;
        rb.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
    }

    // ---- WALL JUMP ---- \\

    private bool IsTouchingWall()
    {
        return Physics2D.OverlapBox(wallCheckRight.position, wallCheckRightSize, 0f, wallLayer) || Physics2D.OverlapBox(wallCheckLeft.position, wallCheckLeftSize, 0f, wallLayer);
    }

    private void WallSlide()
    {
        isWallSliding = IsTouchingWall() && rb.linearVelocity.y <= 0f;
        if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(0, -wallSlideSpeed);
        }
    }

    private IEnumerator WallJump()
    {
        if (!isWallSliding)
        {
            yield break; // Exit if not wall sliding
        }

        isWallJumping = true;
        if (Physics2D.OverlapBox(wallCheckRight.position, wallCheckRightSize, 0f, wallLayer))
        {
            Debug.Log("Wall Jump Right Wall");

            Vector2 jumpDirection = direction.normalized;

            Vector2 targetVelocity = new Vector2(-jumpDirection.x * wallJumpForce, wallJumpForce);
            rb.AddForce((targetVelocity - rb.linearVelocity) * rb.mass, ForceMode2D.Impulse);

            yield return new WaitForSeconds(wallJumpDuration);
        }
        else if (Physics2D.OverlapBox(wallCheckLeft.position, wallCheckLeftSize, 0f, wallLayer))
        {
            Debug.Log("Wall Jump Left Wall");

            Vector2 jumpDirection = direction.normalized;
            Vector2 targetVelocity = new Vector2(-jumpDirection.x * wallJumpForce, wallJumpForce);
            rb.AddForce((targetVelocity - rb.linearVelocity) * rb.mass, ForceMode2D.Impulse);

            yield return new WaitForSeconds(wallJumpDuration);
        }
        isWallJumping = false;

    }

    // ---- DASH ---- \\

    private IEnumerator Dash()
    {
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

        yield return new WaitForSeconds(dashDur);

        rb.gravityScale = gravityScale;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // ---- CHECKS ---- \\

    private void CheckGrounded()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
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
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        CheckObstacle();
        if (isObstructed)
        {   // Destroys the script within the player object, might want to change to something else later
            playerBehaviour.RestartGame(5f);
            GameObject.Destroy(this);
            rb.freezeRotation = false;
        }

        if (IsTouchingWall())
        {
            WallSlide();
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
            if (isGrounded&& (speedInInputDirection > baseMoveSpeed))
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
        Debug.Log($"Horizontal Speed: {horizontalSpeed}, Target Move Speed: {targetMoveSpeed}");
    }
}
