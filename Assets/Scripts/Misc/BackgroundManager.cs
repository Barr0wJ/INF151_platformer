using System;
using UnityEngine;

[Serializable]
public class BackgroundLayer
{
    [SerializeField] private Transform baseLayer; // follows the camera's x position

    [SerializeField] private GameObject[] spawnableElements; // prefabs like trees
    [SerializeField] private Range ySpawnRange; // the y spawn range relative to the baseLayers position
    [SerializeField] private Range scaleRange;
    [SerializeField] private Range spawnAmount; // how many to spawn
    [SerializeField] private int orderInLayer;
    
    public void UpdateBaseLayer(float cameraXPos)
    {
        // move base layer based on camera's x position, but keeps the y value set initially in the scene
        if (baseLayer)
        {
            baseLayer.position = new Vector2(cameraXPos, baseLayer.position.y);
        }
    }

    public void SpawnElements(Range xBounds)
    {
        if (spawnableElements.Length == 0) return;

        // spawns elements randomly across the y-axis of baseLayer or 0
        float y = 0;

        if (baseLayer)
        {
            y = baseLayer.position.y;
        }

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
    [Tooltip("X bounds of the level, restricts the elements within these bounds")]
    [SerializeField] private Range xBounds;
    [SerializeField] private float lowestY;

    [Header("Objects")]
    [SerializeField] private BackgroundLayer[] layers;

    private Camera camera;

    private void SpawnAll()
    {
        foreach (BackgroundLayer layer in layers)
        {
            layer.SpawnElements(xBounds);
        }
    }

    private void ProcessBackground()
    {
        float xPos = camera.transform.position.x;

        foreach (BackgroundLayer layer in layers)
        {
            layer.UpdateBaseLayer(xPos);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        camera = Camera.main;

        SpawnAll();
    }

    // Update is called once per frame
    void Update()
    {
        ProcessBackground();
    }
}
