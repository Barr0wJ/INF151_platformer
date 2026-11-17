using UnityEngine;

//text next to the player eg respawn indicator
public class PlayerText : MonoBehaviour
{
    private Transform playerTransform;
    private DialogueHandler dialogueHandler;
    private Vector3 offset = new Vector3(0.7f, 0, 0);

    private void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        dialogueHandler = GetComponent<DialogueHandler>();
    }

    private void LateUpdate()
    {
        transform.position = playerTransform.position + offset;
    }
}
