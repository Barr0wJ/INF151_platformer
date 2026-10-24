using UnityEngine;

public enum AnimationState
{
    Idle,
    Run,
    Jump,
    WallSlide
}

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class AnimationController : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;   

    private AnimationState currentState;

    public void SetState(AnimationState newState)
    {
        if (currentState == newState) return;

        animator.Play(newState.ToString());
        currentState = newState;
    }

    public void SetSpeed(float speedMultiplier)
    {
        animator.speed = Mathf.Max(0.1f, speedMultiplier);
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
}
