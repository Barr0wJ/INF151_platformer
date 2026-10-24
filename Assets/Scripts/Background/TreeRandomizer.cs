using UnityEngine;

public class TreeRandomizer : SpriteRandomizer
{
    public SpriteRenderer trunkRenderer;
    public SpriteRenderer leafRenderer;

    public Sprite[] trunkSprites;
    public Sprite[] leafSprites;

    public override void Randomize()
    {
        if (trunkSprites.Length > 0 && trunkRenderer != null)
        {
            trunkRenderer.sprite = trunkSprites[Random.Range(0, trunkSprites.Length)];
        }

        if (leafSprites.Length > 0 && leafRenderer != null)
        {
            leafRenderer.sprite = leafSprites[Random.Range(0, leafSprites.Length)];
        }

        Destroy(this);
    }
}