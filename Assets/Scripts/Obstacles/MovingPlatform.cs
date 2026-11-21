using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    public enum PathSource
    {
        ChildTransforms,
        LineRenderer
    }

    public enum LoopMode
    {
        PingPong,
        Loop
    }

    [SerializeField] private PathSource pathSource = PathSource.ChildTransforms;
    [SerializeField] private LoopMode loopMode = LoopMode.PingPong;
    [SerializeField] private List<Transform> waypointTransforms = new List<Transform>();
    [SerializeField] private LineRenderer lineRenderer;

    [SerializeField] private float speed = 3f;
    [SerializeField] private float pointDelay = 0.5f;
    [SerializeField] private float endPointDelay = 2f;

    public Vector2 PlatformDelta { get; private set; }

    private List<Vector3> worldWaypoints = new List<Vector3>();
    private int currentIndex = 0;
    private int direction = 1;
    private float waitTimer = 0f;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        StoreWaypoints();

        if (worldWaypoints.Count > 0)
        {
            rb.position = worldWaypoints[0];
            transform.position = worldWaypoints[0];

            if (worldWaypoints.Count > 1)
            {
                currentIndex = 1;
                waitTimer = endPointDelay;
            }
        }
    }

    void FixedUpdate()
    {
        if (worldWaypoints.Count < 2)
        {
            PlatformDelta = Vector2.zero;
            return;
        }

        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            PlatformDelta = Vector2.zero;
            return;
        }

        Vector2 targetPosition = worldWaypoints[currentIndex];
        Vector2 nextPosition = Vector2.MoveTowards(rb.position, targetPosition, speed * Time.fixedDeltaTime);
        
        PlatformDelta = nextPosition - rb.position;
        rb.MovePosition(nextPosition);

        if (Vector2.Distance(nextPosition, targetPosition) < 0.001f)
        {
            rb.MovePosition(targetPosition);
            AdvanceToNextWaypoint();
        }
    }

    private void StoreWaypoints()
    {
        worldWaypoints.Clear();

        if (pathSource == PathSource.ChildTransforms)
        {
            if (waypointTransforms.Count == 0)
            {
                foreach (Transform child in transform)
                {
                    waypointTransforms.Add(child);
                }
            }

            foreach (Transform t in waypointTransforms)
            {
                if (t != null)
                {
                    worldWaypoints.Add(t.position);
                }
            }
        }
        else if (pathSource == PathSource.LineRenderer && lineRenderer != null)
        {
            Vector3[] positions = new Vector3[lineRenderer.positionCount];
            lineRenderer.GetPositions(positions);

            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 worldPos = lineRenderer.useWorldSpace
                    ? positions[i]
                    : lineRenderer.transform.TransformPoint(positions[i]);

                worldWaypoints.Add(worldPos);
            }
        }
    }

    private void AdvanceToNextWaypoint()
    {
        bool isAtEndPoint = false;

        if (loopMode == LoopMode.PingPong)
        {
            if (currentIndex == worldWaypoints.Count - 1 && direction == 1)
            {
                direction = -1;
                isAtEndPoint = true;
            }
            else if (currentIndex == 0 && direction == -1)
            {
                direction = 1;
                isAtEndPoint = true;
            }

            currentIndex += direction;
        }
        else if (loopMode == LoopMode.Loop)
        {
            if (currentIndex == worldWaypoints.Count - 1)
            {
                isAtEndPoint = true;
                currentIndex = 0;
            }
            else
            {
                currentIndex++;
            }
        }

        waitTimer = isAtEndPoint ? endPointDelay : pointDelay;
    }

    private void OnDrawGizmos()
    {
        List<Vector3> previewPoints = new List<Vector3>();

        if (Application.isPlaying && worldWaypoints.Count > 0)
        {
            previewPoints = worldWaypoints;
        }
        else
        {
            if (pathSource == PathSource.ChildTransforms)
            {
                if (waypointTransforms.Count > 0)
                {
                    foreach (var t in waypointTransforms)
                        if (t != null) previewPoints.Add(t.position);
                }
                else
                {
                    foreach (Transform child in transform)
                        previewPoints.Add(child.position);
                }
            }
            else if (pathSource == PathSource.LineRenderer && lineRenderer != null)
            {
                for (int i = 0; i < lineRenderer.positionCount; i++)
                {
                    Vector3 pos = lineRenderer.GetPosition(i);
                    previewPoints.Add(lineRenderer.useWorldSpace ? pos : lineRenderer.transform.TransformPoint(pos));
                }
            }
        }

        if (previewPoints.Count < 2) return;

        for (int i = 0; i < previewPoints.Count; i++)
        {
            Gizmos.DrawSphere(previewPoints[i], 0.15f);

            if (i < previewPoints.Count - 1)
            {
                Gizmos.DrawLine(previewPoints[i], previewPoints[i + 1]);
            }
            else if (loopMode == LoopMode.Loop)
            {
                Gizmos.DrawLine(previewPoints[i], previewPoints[0]);
            }
        }
    }
}