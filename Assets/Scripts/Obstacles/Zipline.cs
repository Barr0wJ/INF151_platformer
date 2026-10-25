using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D), typeof(LineRenderer))]
public class Zipline : MonoBehaviour
{
    [SerializeField] private Vector2 startPoint = new Vector2(-4f, 0f);
    [SerializeField] private Vector2 endPoint = new Vector2(4f, 0f);
    [SerializeField, Min(0.01f)] private float lineThickness = 0.12f;
    [SerializeField, Min(0f)] private float startingSpeed = 6.5f;
    [SerializeField, Min(0.01f)] private float minimumSpeed = 3f;
    [SerializeField] private Color lineColor = Color.yellow;

    protected virtual bool RideOnUpperSide => true;

    private EdgeCollider2D lineCollider;
    private LineRenderer lineRenderer;
    private MovementController rider;
    private Rigidbody2D riderBody;
    private Vector2 worldStart;
    private Vector2 travelDirection;
    private Vector2 surfaceNormal;
    private float lineLength;
    private float distanceTravelled;
    private float normalOffset;
    private Vector2 bodyCenterOffset;
    private float riderSpeed;

    protected virtual void Awake()
    {
        lineCollider = GetComponent<EdgeCollider2D>();
        lineRenderer = GetComponent<LineRenderer>();
        UpdateLineGeometry();

        if (lineLength <= Mathf.Epsilon)
        {
            Debug.LogError("Zipline start and end points must be different.", this);
            enabled = false;
        }
    }

    protected virtual void OnValidate()
    {
        lineThickness = Mathf.Max(0.01f, lineThickness);
        startingSpeed = Mathf.Max(0f, startingSpeed);
        minimumSpeed = Mathf.Max(0.01f, minimumSpeed);
        lineCollider = GetComponent<EdgeCollider2D>();
        lineRenderer = GetComponent<LineRenderer>();
        UpdateLineGeometry();
    }

    private void UpdateLineGeometry()
    {
        if (lineCollider == null || lineRenderer == null)
        {
            return;
        }

        lineLength = Vector2.Distance(startPoint, endPoint);
        lineCollider.points = new[] { startPoint, endPoint };
        lineCollider.edgeRadius = lineThickness * 0.5f;
        lineCollider.isTrigger = true;

        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);
        lineRenderer.startWidth = lineThickness;
        lineRenderer.endWidth = lineThickness;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (rider != null)
        {
            return;
        }

        MovementController movement = other.GetComponentInParent<MovementController>();
        if (movement == null)
        {
            return;
        }

        Rigidbody2D body = movement.GetComponent<Rigidbody2D>();
        if (body == null)
        {
            return;
        }

        Vector2 worldEnd = transform.TransformPoint(endPoint);
        worldStart = transform.TransformPoint(startPoint);
        Vector2 segment = worldEnd - worldStart;
        lineLength = segment.magnitude;
        if (lineLength <= Mathf.Epsilon)
        {
            return;
        }

        travelDirection = segment / lineLength;
        surfaceNormal = new Vector2(-travelDirection.y, travelDirection.x);
        if (surfaceNormal.y < 0f)
        {
            surfaceNormal = -surfaceNormal;
        }

        Vector2 playerCenter = other.bounds.center;
        float progress = Mathf.Clamp(
            Vector2.Dot(playerCenter - worldStart, travelDirection),
            0f,
            lineLength);
        Bounds playerBounds = other.bounds;
        float playerRadius = Mathf.Abs(surfaceNormal.x) * playerBounds.extents.x
            + Mathf.Abs(surfaceNormal.y) * playerBounds.extents.y;
        float side = RideOnUpperSide ? 1f : -1f;
        normalOffset = side * (playerRadius + lineThickness * 0.5f);
        Vector2 bodyToColliderOffset = playerCenter - body.position;

        rider = movement;
        riderBody = body;
        distanceTravelled = progress;
        bodyCenterOffset = bodyToColliderOffset;
        riderSpeed = Mathf.Max(
            minimumSpeed,
            startingSpeed,
            Mathf.Abs(Vector2.Dot(body.linearVelocity, travelDirection)));
        if (movement.IsDashing)
        {
            riderSpeed = Mathf.Max(riderSpeed, movement.DashSpeed);
        }
        rider.AttachToZipline(this);
        riderBody.linearVelocity = travelDirection * riderSpeed;
    }

    public bool TryDetachRider(MovementController movement, out Vector2 exitVelocity)
    {
        if (rider != movement)
        {
            exitVelocity = Vector2.zero;
            return false;
        }

        exitVelocity = travelDirection * riderSpeed;
        rider = null;
        riderBody = null;
        return true;
    }

    protected virtual void FixedUpdate()
    {
        if (rider == null || riderBody == null)
        {
            return;
        }

        riderBody.gravityScale = 0f;
        float gravityAlongLine = Vector2.Dot(
            Physics2D.gravity * rider.NormalGravityScale,
            travelDirection) * 0.5f;

        if (riderSpeed < rider.DashSpeed || gravityAlongLine <= 0f)
        {
            riderSpeed = Mathf.Max(minimumSpeed, riderSpeed + gravityAlongLine * Time.fixedDeltaTime);
        }

        distanceTravelled = Mathf.Min(
            lineLength,
            distanceTravelled + riderSpeed * Time.fixedDeltaTime);
        riderBody.MovePosition(GetRiderPosition(distanceTravelled));

        if (distanceTravelled >= lineLength)
        {
            FinishRide();
        }
    }

    private void FinishRide()
    {
        MovementController finishedRider = rider;
        Rigidbody2D finishedBody = riderBody;
        rider = null;
        riderBody = null;

        finishedBody.position = GetRiderPosition(lineLength);
        finishedBody.linearVelocity = travelDirection * riderSpeed;
        finishedRider.SetZiplineActive(false);
    }

    private Vector2 GetRiderPosition(float distance)
    {
        return worldStart
            + travelDirection * distance
            + surfaceNormal * normalOffset
            - bodyCenterOffset;
    }
}
