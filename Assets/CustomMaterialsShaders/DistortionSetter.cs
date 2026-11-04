using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class DistortionSetter : MonoBehaviour
{
    private SpriteRenderer sr;
    
    private Transform[] typingBlocks;
    private Transform playerTransform;

    private int currentIndex = 0;
    
    // goal is to not make the function O(N)
    public Transform GetClosestTransform()
    {
        if (typingBlocks == null || typingBlocks.Length == 0) return null;

        int closestIndex = currentIndex;
        Transform closest = typingBlocks[closestIndex];
        float closestDist = Vector2.Distance(closest.position, playerTransform.position);

        if (closestIndex > 0)
        {
            Transform before = typingBlocks[closestIndex - 1];
            float d = Vector2.Distance(before.position, playerTransform.position);
            if (d < closestDist)
            {
                closest = before;
            }
        }

        if (closestIndex < typingBlocks.Length - 1)
        {
            Transform after = typingBlocks[closestIndex + 1];
            float d = Vector2.Distance(after.position, playerTransform.position);
            if (d < closestDist)
            {
                closest = after;
            }
        }

        return closest;
    }

    // TO DO: MAKE IT SMOOTHLY TRANSITION
    private void ProcessMaterial()
    {   
        Transform typingBlock = GetClosestTransform();

        if (typingBlock == null) {return;}

        Vector3 localPos = sr.transform.InverseTransformPoint(typingBlock.position);

        Vector2 spriteBoundsSize = sr.sprite.rect.size / sr.sprite.pixelsPerUnit;
        float u = (localPos.x / spriteBoundsSize.x) + 0.5f;
        float v = (localPos.y / spriteBoundsSize.y) + 0.5f;

        Rect spriteRect = sr.sprite.rect;
        Texture2D texture = sr.sprite.texture;

        float finalU = Mathf.Lerp(spriteRect.xMin / texture.width, spriteRect.xMax / texture.width, u);
        float finalV = Mathf.Lerp(spriteRect.yMin / texture.height, spriteRect.yMax / texture.height, v);

        Vector2 uv = new Vector2(Mathf.Clamp01(finalU), Mathf.Clamp01(finalV));

        sr.material.SetVector("_Position", uv);

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        GameObject[] blockObjects = GameObject.FindGameObjectsWithTag("TypingBlock");
        List<Transform> list = new List<Transform>();

        foreach (GameObject g in blockObjects)
        {
            list.Add(g.transform);
        }

        typingBlocks = list.OrderBy(obj => Vector3.Distance(playerTransform.position, obj.transform.position)).ToArray();
        Debug.Log(typingBlocks.Length);


        sr = GetComponent<SpriteRenderer>();

        if (blockObjects.Length == 0)
        {
            sr.material.SetVector("_Position", new Vector2(2, 2));
            Destroy(this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        ProcessMaterial();
    }
       
}
