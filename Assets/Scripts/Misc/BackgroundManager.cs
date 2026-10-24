using System;
using UnityEngine;

[Serializable]
public class BackgroundLayer
{
    [SerializeField] private Transform baseLayer; // The layer parent transform
    [SerializeField] private bool followY;

    [SerializeField] private GameObject[] spawnableElements; // prefabs like trees
    [SerializeField] private Range ySpawnRange; // the y spawn range relative to the baseLayers position
    [SerializeField] private Range scaleRange;
    [SerializeField] private Range spawnAmount; // how many to spawn
    [SerializeField] private int orderInLayer;

    private Camera cam;
    private SpriteRenderer spriteRenderer;
    private float halfHeight;
    private float halfWidth;

    public void Initialize(Camera cam)
    {
        this.cam = cam;
        halfHeight = cam.orthographicSize;
        halfWidth = halfHeight * cam.aspect;

        if (!baseLayer)
            return;

        spriteRenderer = baseLayer.GetComponent<SpriteRenderer>();

        if (!spriteRenderer)
            spriteRenderer = baseLayer.GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer)
            spriteRenderer.sortingOrder = orderInLayer;
    }

    public void UpdateBaseLayer(Range xBounds)
    {
        if (!baseLayer || !cam || !spriteRenderer)
            return;

        float minX = xBounds.lowerBound;
        float maxX = xBounds.upperBound;

        Bounds layerBounds = spriteRenderer.bounds;
        float cameraTop = cam.transform.position.y + halfHeight;
        float cameraBottom = cam.transform.position.y - halfHeight;

        // if (cameraTop < layerBounds.min.y || cameraBottom > layerBounds.max.y)
        //     return;

        // progress til end of level
        float progress = Mathf.InverseLerp(
            minX,
            maxX,
            cam.transform.position.x
        );


        float layerWidth = layerBounds.size.x;

        float leftCenter = minX - halfWidth + layerWidth * 0.5f;
        float rightCenter = maxX + halfWidth - layerWidth * 0.5f;

        float targetCenterX = Mathf.Lerp(
            leftCenter,
            rightCenter,
            progress
        );

        float offset = layerBounds.center.x - baseLayer.position.x;
        Vector3 position = followY ? cam.transform.position : baseLayer.position;
        position.x = targetCenterX - offset;
        position.z = baseLayer.position.z;

        baseLayer.position = position;
    }

    public void SpawnElements()
    {
        if (spawnableElements == null || spawnableElements.Length == 0)
            return;

        float baseY = baseLayer ? baseLayer.position.y : 0f;
        float baseZ = baseLayer ? baseLayer.position.z : 0f;

        int spawns = spawnAmount.GetRandomInt();
        Bounds layerBounds = spriteRenderer.bounds;
        Range range = new Range(layerBounds.min.x, layerBounds.max.x);

        for (int i = 0; i < spawns; i++)
        {
            float x = range.GetRandomFloat();
            float yOffset = ySpawnRange.GetRandomFloat();
            float scale = scaleRange.GetRandomFloat();

            Vector3 spawnPos = new Vector3(
                x,
                baseY + yOffset,
                baseZ
            );

            int prefabIndex = UnityEngine.Random.Range(
                0,
                spawnableElements.Length
            );

            GameObject prefab = spawnableElements[prefabIndex];

            if (!prefab)
                continue;
            
            GameObject obj = GameObject.Instantiate(
                prefab,
                spawnPos,
                Quaternion.identity,
                baseLayer
            );

            obj.transform.localScale = Vector3.one * scale;
            SpriteRandomizer randomizer;

            if (obj.TryGetComponent<SpriteRandomizer>(out randomizer))
            {
                randomizer.Randomize();
            }
            

            // SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);

            // foreach (SpriteRenderer renderer in renderers)
            // {
            //     renderer.sortingOrder = orderInLayer + 1;
            // }
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
            layer.Initialize(cam);
            layer.SpawnElements();
        }
    }

    private void ProcessBackground()
    {
        float xPos = cam.transform.position.x;

        foreach (BackgroundLayer layer in layers)
        {
            layer.UpdateBaseLayer(xBounds);
        }
    }

    void Update()
    {
        ProcessBackground();
    }
}