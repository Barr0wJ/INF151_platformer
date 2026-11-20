using System.Collections.Generic;
using UnityEngine;

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

    [SerializeField] private LoopMode loopMode = LoopMode.PingPong;
    [SerializeField] private List<Transform> waypointTransforms = new List<Transform>();

    [SerializeField] private float speed = 3f;
    [SerializeField] private float pointDelay = 0.5f;
    [SerializeField] private float endPointDelay = 2f;

    private List<Vector3> worldWaypoints = new List<Vector3>();
    private int currentIndex = 0;
    private int direction = 1;
    private float waitTimer = 0f;

    void Start()
    {
        StoreWaypoints();

        if (worldWaypoints.Count > 0)
        {
            transform.position = worldWaypoints[0];

            if (worldWaypoints.Count > 1)
            {
                currentIndex = 1;
                waitTimer = endPointDelay;
            }
        }
    }

    void Update()
    {
        if (worldWaypoints.Count < 2) return;

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        Vector3 targetPosition = worldWaypoints[currentIndex];
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.001f)
        {
            transform.position = targetPosition;
            AdvanceToNextWaypoint();
        }
    }

    private void StoreWaypoints()
    {
        worldWaypoints.Clear();

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