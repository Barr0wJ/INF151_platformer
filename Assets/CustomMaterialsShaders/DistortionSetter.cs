using UnityEngine;

public class DistortionSetter : MonoBehaviour
{
    [SerializeField] private Material distortionMaterial;


    private GameObject[] typingBlocks;
    private Transform player_Transform;

    private void ProcessMaterial(Transform typingBlock)
    {  
        Vector2 blockPosition = typingBlock.position; 
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        typingBlocks = GameObject.FindGameObjectsWithTag("TypingBlock");

        player_Transform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    // Update is called once per frame
    void Update()
    {
        // ProcessMaterial();
    }
}
