using UnityEngine;

// handles... the camera
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    public bool lockX = false;
    public bool lockY = false;

    [SerializeField] private float smoothTime = 0.25f;
    private Vector3 currentVel = Vector3.zero;

    [SerializeField] private Camera cam;
    // zoom
    [SerializeField] private float minFOV = 3f;  // Zoomed in size
    [SerializeField] private float maxFOV = 8f;  // Zoomed out size (default)
    [SerializeField] private float zoomSpeed = 15f;
    [SerializeField] private float zoomXOffset = 3f;

    private float targetFOV;

    // follows player ~~unless specified otherwise~~
    private void ProcessCameraFollow()
    {
        if (!player) return;
        
        Vector3 targetPos = player.position + offset;

        if (lockX)
        {
            targetPos.x = transform.position.x;
        }

        if (lockY)
        {
            targetPos.y = transform.position.y;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPos, 
            ref currentVel, 
            smoothTime, 
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );
    }

    private void ProcessZoom()
    {
        if (!cam) return;

        cam.orthographicSize = Mathf.Lerp(
            cam.orthographicSize, 
            targetFOV, 
            zoomSpeed * Time.unscaledDeltaTime
        );
    }

    public void ZoomIn()
    {
        targetFOV = minFOV;
        offset = new Vector3(zoomXOffset, 0, -10);
    }

    public void ZoomOut()
    {
        targetFOV = maxFOV;
        offset = new Vector3(0, 0, -10);
    }

    // ---- MONOBEHAVIOUR ---- \\

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (cam == null)
        {
            cam = GetComponent<Camera>();
        }

        if (cam != null)
        {
            targetFOV = cam.orthographicSize;
        }

        if (!player)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player)
            {
                this.player = player.transform;
            }
        }
    }

    void Update()
    {
        ProcessZoom();
    }

    void LateUpdate()
    {
        ProcessCameraFollow();
    }
}