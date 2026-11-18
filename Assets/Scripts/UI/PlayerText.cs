using System.Collections;
using UnityEngine;

//text next to the player eg respawn indicator
public class PlayerText : MonoBehaviour
{

    private Transform playerTransform;
    private PlayerBehaviour playerBehaviour;
    private DialogueHandler dialogueHandler;
    private Vector3 offset = new Vector3(0.9f, 0, 0);

    public IEnumerator CountdownDeath(int waitTime)
    {
        for (int i = waitTime; i > 0; i--)
        {
            dialogueHandler.WriteText("<color=\"Red\">Respawning in " + i + "...");
            yield return new WaitForSecondsRealtime(1);
        }
    }

    private void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        playerBehaviour = playerTransform.GetComponent<PlayerBehaviour>();
        dialogueHandler = GetComponent<DialogueHandler>();
    }

    private void LateUpdate()
    {
        transform.position = playerTransform.position + offset;
    }
}
