using UnityEngine;

public enum AnimationState
{
    Idle,
    Run,
    Jump,
    WallSlide,
    Fall,
    Dead
}

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class AnimationController : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    private AnimationState currentState;

    public void SetState(AnimationState newState)
    {
        if (currentState == newState) return;

        animator.Play(newState.ToString());
        currentState = newState;
        Debug.Log("Setting State: " + newState.ToString());
    }

    public void SetSpeed(float speedMultiplier)
    {
        animator.speed = Mathf.Max(0.1f, speedMultiplier);
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponentInParent<Rigidbody2D>();
    }

    void Update()
    {
        if (rb.linearVelocity.y < 0) 
        {
            animator.SetTrigger("Falling");
        }
    }
}
