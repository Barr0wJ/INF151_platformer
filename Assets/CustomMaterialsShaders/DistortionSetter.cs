using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DistortionSetter : MonoBehaviour
{
    private SpriteRenderer sr;
    private Transform[] typingBlocks;
    private Transform playerTransform;

    private int currentIndex = 0;

    // goal is to not make it O(n)
    public Transform GetClosestTransform()
    {
        if (typingBlocks == null || typingBlocks.Length == 0) return null;

        Vector2 playerPos = playerTransform.position;
        float closestSqrDist = ((Vector2)typingBlocks[currentIndex].position - playerPos).sqrMagnitude;

        while (currentIndex > 0)
        {
            float prevSqrDist = ((Vector2)typingBlocks[currentIndex - 1].position - playerPos).sqrMagnitude;
            if (prevSqrDist < closestSqrDist)
            {
                closestSqrDist = prevSqrDist;
                currentIndex--;
            }
            else
            {
                break;
            }
        }

        while (currentIndex < typingBlocks.Length - 1)
        {
            float nextSqrDist = ((Vector2)typingBlocks[currentIndex + 1].position - playerPos).sqrMagnitude;
            if (nextSqrDist < closestSqrDist)
            {
                closestSqrDist = nextSqrDist;
                currentIndex++;
            }
            else
            {
                break;
            }
        }

        return typingBlocks[currentIndex];
    }

    private void ProcessMaterial()
    {
        Transform typingBlock = GetClosestTransform();
        if (typingBlock == null || sr.sprite == null) return;

        Vector3 localPos = sr.transform.InverseTransformPoint(typingBlock.position);

        Rect spriteRect = sr.sprite.rect;
        Vector2 pivotNormalized = sr.sprite.pivot / spriteRect.size;
        Vector2 spriteBoundsSize = spriteRect.size / sr.sprite.pixelsPerUnit;

        float u = (localPos.x / spriteBoundsSize.x) + pivotNormalized.x;
        float v = (localPos.y / spriteBoundsSize.y) + pivotNormalized.y;

        Texture2D texture = sr.sprite.texture;
        float finalU = Mathf.Lerp(spriteRect.xMin / texture.width, spriteRect.xMax / texture.width, u);
        float finalV = Mathf.Lerp(spriteRect.yMin / texture.height, spriteRect.yMax / texture.height, v);

        Vector2 uv = new Vector2(Mathf.Clamp01(finalU), Mathf.Clamp01(finalV));
        sr.material.SetVector("_Position", uv);
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        GameObject[] blockObjects = GameObject.FindGameObjectsWithTag("TypingBlock");

        if (blockObjects.Length == 0 || playerTransform == null)
        {
            sr.material.SetVector("_Position", new Vector2(2, 2));
            Destroy(this);
            return;
        }

        typingBlocks = blockObjects
            .Select(g => g.transform)
            .OrderBy(t => t.position.x) 
            .ToArray();

        Vector2 playerPos = playerTransform.position;
        float bestDist = float.MaxValue;
        for (int i = 0; i < typingBlocks.Length; i++)
        {
            float d = ((Vector2)typingBlocks[i].position - playerPos).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                currentIndex = i;
            }
        }
    }

    void Update()
    {
        ProcessMaterial();
    }
}