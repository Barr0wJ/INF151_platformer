using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class MovementController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7.5f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float jumpCutMultiplier = 0.5f; // Short jump on early button release
    [SerializeField] private int maxExtraJumps = 1; // double jump

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDur = 0.2f;
    [SerializeField] private float dashCooldown = 0.8f;

    [Header("Coyote Time")]
    [SerializeField] private float coyoteDur = 0.15f;
    private float coyoteCounter;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.2f);
    [SerializeField] private LayerMask groundLayer;

    public bool movementEnabled = true;

    private PlayerBehaviour playerBehaviour;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isGrounded;
    private int remainingJumps;

    private bool isDashing;
    private bool canDash = true;
    private float gravityScale;

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
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
        moveInput = input;

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
            HandleJump();
        }

        // Input.GetKeyUp
        if (Keyboard.current.spaceKey.wasReleasedThisFrame && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
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
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    // ---- DASH ---- \\

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        rb.gravityScale = 0f; // temporarily disable gravity

        Vector2 dashDir = moveInput.normalized;

        if (dashDir == Vector2.zero)
        {
            dashDir = new Vector2(moveInput.x > 0 ? 1f : -1f, 0f);
        }

        rb.linearVelocity = dashDir * dashSpeed;

        yield return new WaitForSeconds(dashDur);

        rb.gravityScale = gravityScale;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void CheckGrounded()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        }
    }

    // For testing visualization
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }

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

        if (isDashing) return;
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }
}