using UnityEngine;

public class CheckpointBlock : MonoBehaviour
{
    public Vector2 teleportPosition;

    public void OnTriggerEnter2D(Collider2D other) {
        if (other.transform.parent.CompareTag("Player"))
        {
            other.transform.parent.position = teleportPosition;
        }
    }
}
