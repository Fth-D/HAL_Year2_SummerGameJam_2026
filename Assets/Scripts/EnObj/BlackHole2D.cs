using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class BlackHole2D : MonoBehaviour, ITriggerable
{
    public enum PullMode
    {
        Constant,
        StrongerNearCenter
    }

    public enum SpiralDirection
    {
        Clockwise,
        CounterClockwise
    }

    [Header("Black Hole Area")]
    [SerializeField, Min(0.1f)]
    private float pullRadius = 5f;

    [Header("Pull Settings")]
    [SerializeField, Min(0f)]
    private float pullStrength = 20f;

    [SerializeField]
    private PullMode pullMode = PullMode.StrongerNearCenter;

    [SerializeField, Min(0f)]
    private float maximumPullStrength = 50f;

    [Header("Spiral Settings")]
    [SerializeField, Min(0f)]
    private float spiralStrength = 12f;

    [SerializeField]
    private SpiralDirection spiralDirection =
        SpiralDirection.Clockwise;

    [Tooltip("Makes the spiral force stronger near the center.")]
    [SerializeField]
    private bool strongerSpiralNearCenter = true;

    [SerializeField, Min(0f)]
    private float maximumSpiralStrength = 30f;

    [Header("Activation")]
    [SerializeField]
    private bool startActive = true;

    [Header("Target Filtering")]
    [SerializeField]
    private LayerMask targetLayers;

    [SerializeField]
    private bool affectTriggerColliders = false;

    [Header("Center Behavior")]

    [SerializeField, Min(0f)]
    private float stopRadius = 0.5f;

    [SerializeField]
    private bool stopRotationAtCenter = true;

    [SerializeField]
    private bool destroyAtCenter = false;

    [SerializeField, Min(0f)]
    private float destroyRadius = 0.3f;

    [Tooltip("Stops objects from moving extremely fast.")]
    [SerializeField, Min(0f)]
    private float maximumTargetSpeed = 20f;

    private CircleCollider2D pullCollider;
    private bool isActive;

    public bool IsActive => isActive;

    private void Awake()
    {
        pullCollider = GetComponent<CircleCollider2D>();

        ConfigureCollider();
        SetActive(startActive);
    }

    private void OnValidate()
    {
        pullRadius = Mathf.Max(0.1f, pullRadius);
        pullStrength = Mathf.Max(0f, pullStrength);
        maximumPullStrength = Mathf.Max(0f, maximumPullStrength);

        spiralStrength = Mathf.Max(0f, spiralStrength);
        maximumSpiralStrength = Mathf.Max(0f, maximumSpiralStrength);

        stopRadius = Mathf.Clamp(
            stopRadius,
            0f,
            pullRadius
        );

        maximumTargetSpeed = Mathf.Max(
            0f,
            maximumTargetSpeed
        );

        pullCollider = GetComponent<CircleCollider2D>();

        if (pullCollider != null)
            ConfigureCollider();
    }

    private void ConfigureCollider()
    {
        pullCollider.isTrigger = true;
        pullCollider.radius = pullRadius;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isActive)
            return;

        if (!IsTargetAllowed(other))
            return;

        Rigidbody2D targetBody = other.attachedRigidbody;

        if (targetBody == null)
            return;

        if (targetBody.bodyType != RigidbodyType2D.Dynamic)
            return;

        ApplySpiralPull(targetBody);
    }

    private void ApplySpiralPull(Rigidbody2D targetBody)
    {
        Vector2 centerPosition = transform.position;
        Vector2 targetPosition = targetBody.worldCenterOfMass;

        Vector2 directionToCenter =
            centerPosition - targetPosition;

        float distance = directionToCenter.magnitude;

        // Stop the target when it reaches the center area.
        if (distance <= stopRadius)
        {
            StopTargetAtCenter(targetBody);
            return;
        }

        if (distance <= 0.001f)
            return;

        Vector2 inwardDirection =
            directionToCenter.normalized;

        Vector2 tangentDirection =
            GetTangentDirection(inwardDirection);

        float currentPullStrength =
            CalculatePullStrength(distance);

        float currentSpiralStrength =
            CalculateSpiralStrength(distance);

        Vector2 inwardForce =
            inwardDirection * currentPullStrength;

        Vector2 spiralForce =
            tangentDirection * currentSpiralStrength;

        Vector2 totalForce =
            inwardForce + spiralForce;

        targetBody.AddForce(
            totalForce,
            ForceMode2D.Force
        );

        LimitTargetSpeed(targetBody);
    }

    private Vector2 GetTangentDirection(
        Vector2 inwardDirection)
    {
        if (spiralDirection ==
            SpiralDirection.Clockwise)
        {
            return new Vector2(
                inwardDirection.y,
                -inwardDirection.x
            );
        }

        return new Vector2(
            -inwardDirection.y,
            inwardDirection.x
        );
    }

    private float CalculatePullStrength(float distance)
    {
        if (pullMode == PullMode.Constant)
            return pullStrength;

        float normalizedDistance =
            Mathf.Clamp01(distance / pullRadius);

        float closeness =
            1f - normalizedDistance;

        float calculatedStrength =
            pullStrength * (1f + closeness);

        return Mathf.Min(
            calculatedStrength,
            maximumPullStrength
        );
    }

    private float CalculateSpiralStrength(float distance)
    {
        if (!strongerSpiralNearCenter)
            return spiralStrength;

        float normalizedDistance =
            Mathf.Clamp01(distance / pullRadius);

        float closeness =
            1f - normalizedDistance;

        float calculatedStrength =
            spiralStrength * (1f + closeness);

        return Mathf.Min(
            calculatedStrength,
            maximumSpiralStrength
        );
    }

    private void LimitTargetSpeed(Rigidbody2D targetBody)
    {
        if (maximumTargetSpeed <= 0f)
            return;

        if (targetBody.linearVelocity.sqrMagnitude >
            maximumTargetSpeed * maximumTargetSpeed)
        {
            targetBody.linearVelocity =
                targetBody.linearVelocity.normalized *
                maximumTargetSpeed;
        }
    }

    private bool IsTargetAllowed(Collider2D target)
    {
        if (!affectTriggerColliders && target.isTrigger)
            return false;

        int targetLayer =
            1 << target.gameObject.layer;

        return
            (targetLayers.value & targetLayer) != 0;
    }

    public void Activate()
    {
        SetActive(true);
    }

    public void Deactivate()
    {
        SetActive(false);
    }

    public void Toggle()
    {
        SetActive(!isActive);
    }

    private void SetActive(bool active)
    {
        isActive = active;

        if (pullCollider != null)
            pullCollider.enabled = active;
    }

    public void SetPullRadius(float newRadius)
    {
        pullRadius = Mathf.Max(0.1f, newRadius);

        if (pullCollider != null)
            ConfigureCollider();
    }

    public void SetPullStrength(float newStrength)
    {
        pullStrength = Mathf.Max(0f, newStrength);
    }

    public void SetSpiralStrength(float newStrength)
    {
        spiralStrength = Mathf.Max(0f, newStrength);
    }

    public void ReverseSpiralDirection()
    {
        spiralDirection =
            spiralDirection ==
            SpiralDirection.Clockwise
                ? SpiralDirection.CounterClockwise
                : SpiralDirection.Clockwise;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isActive
            ? new Color(0.45f, 0.15f, 1f, 0.25f)
            : new Color(0.5f, 0.5f, 0.5f, 0.15f);

        Gizmos.DrawSphere(
            transform.position,
            pullRadius
        );

        Gizmos.color = isActive
            ? new Color(0.6f, 0.3f, 1f, 1f)
            : Color.gray;

        Gizmos.DrawWireSphere(
            transform.position,
            pullRadius
        );

        DrawSpiralGizmo();

        // Center stopping area
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            stopRadius
        );
    }

    private void DrawSpiralGizmo()
    {
        const int pointCount = 40;
        const float turns = 2.5f;

        Vector3 previousPoint = Vector3.zero;

        for (int i = 0; i < pointCount; i++)
        {
            float progress =
                i / (float)(pointCount - 1);

            float radius =
                Mathf.Lerp(pullRadius, 0.1f, progress);

            float directionMultiplier =
                spiralDirection ==
                SpiralDirection.Clockwise
                    ? -1f
                    : 1f;

            float angle =
                progress *
                turns *
                Mathf.PI *
                2f *
                directionMultiplier;

            Vector3 currentPoint =
                transform.position +
                new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f
                );

            if (i > 0)
            {
                Gizmos.DrawLine(
                    previousPoint,
                    currentPoint
                );
            }

            previousPoint = currentPoint;
        }
    }
    //Stop At Center
    [SerializeField, Min(0f)]
    private float centerSnapSpeed = 5f;

    private void StopTargetAtCenter(Rigidbody2D targetBody)
    {
        Vector2 newPosition = Vector2.MoveTowards(
            targetBody.position,
            transform.position,
            centerSnapSpeed * Time.fixedDeltaTime
        );

        targetBody.MovePosition(newPosition);
        targetBody.linearVelocity = Vector2.zero;

        if (stopRotationAtCenter)
            targetBody.angularVelocity = 0f;
    }
}

