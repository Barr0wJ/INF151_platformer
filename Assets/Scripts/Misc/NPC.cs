using UnityEngine;

public class NPC : MonoBehaviour
{
    private Transform playerTransform;
    private bool facingRight = false;

    private void Flip()
    {
        facingRight = !facingRight;

        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (playerTransform == null) return;

        if (playerTransform.position.x > transform.position.x && !facingRight)
        {
            Flip();
        }
        else if (playerTransform.position.x < transform.position.x && facingRight)
        {
            Flip();
        }
    }
}
