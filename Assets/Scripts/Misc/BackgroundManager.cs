using System;
using UnityEngine;

[Serializable]
public class BackgroundLayer
{
    [SerializeField] private Transform baseLayer; // The layer parent transform

    [SerializeField] private GameObject[] spawnableElements; // prefabs like trees
    [SerializeField] private Range ySpawnRange; // the y spawn range relative to the baseLayers position
    [SerializeField] private Range scaleRange;
    [SerializeField] private Range spawnAmount; // how many to spawn
    [SerializeField] private int orderInLayer;

    public void UpdateBaseLayer(float cameraXPos, Range xBounds)
    {
        if (!baseLayer) return;
        // idk how to do this yet ill figure it out later
    }

    public void SpawnElements(Range xBounds)
    {
        if (spawnableElements.Length == 0) return;

        float y = baseLayer ? baseLayer.position.y : 0f;
        int spawns = spawnAmount.GetRandomInt();

        for (int i = 0; i < spawns; i++)
        {
            float yOffset = ySpawnRange.GetRandomFloat();
            Vector2 spawnPos = new Vector2(xBounds.GetRandomFloat(), y + yOffset);
            float scale = scaleRange.GetRandomFloat();
            
            GameObject.Instantiate(
                spawnableElements[(int) UnityEngine.Random.Range(0, spawnableElements.Length)],
                spawnPos,
                Quaternion.identity
            ).transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}

// handles the background
public class BackgroundManager : MonoBehaviour
{
    [Header("Bounds")]
    [SerializeField] private Range xBounds;
    [SerializeField] private float lowestY;

    [Header("Objects")]
    [SerializeField] private BackgroundLayer[] layers;

    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
        SpawnAll();
    }

    private void SpawnAll()
    {
        foreach (BackgroundLayer layer in layers)
        {
            layer.SpawnElements(xBounds);
        }
    }

    private void ProcessBackground()
    {
        float xPos = cam.transform.position.x;

        foreach (BackgroundLayer layer in layers)
        {
            layer.UpdateBaseLayer(xPos, xBounds);
        }
    }

    void Update()
    {
        ProcessBackground();
    }
}