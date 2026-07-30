using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class BallSocket2D : MonoBehaviour, ITriggerable
{
    [Header("Socket Area")]
    [SerializeField, Min(0.1f)]
    private float attachmentRadius = 1f;

    [SerializeField]
    private Transform attachmentPoint;

    [Header("Activation")]
    [SerializeField]
    private bool startActive = true;

    [SerializeField]
    private bool automaticallyAttach = true;

    [Header("Target Filtering")]
    [SerializeField]
    private LayerMask ballLayers;

    [Header("Release")]
    [SerializeField]
    private Vector2 releaseVelocity;

    [Tooltip(
        "The ball cannot immediately reconnect after release. " +
        "It must leave the socket area first."
    )]
    [SerializeField]
    private bool requireExitBeforeReattaching = true;

    private CircleCollider2D detectionCollider;
    private ChainBallController2D attachedBall;
    private ChainBallController2D nearbyBall;

    private bool isActive;
    private bool canAttach = true;

    public bool IsActive => isActive;
    public bool HasAttachedBall => attachedBall != null;

    private void Awake()
    {
        detectionCollider = GetComponent<CircleCollider2D>();

        ConfigureCollider();

        if (attachmentPoint == null)
            attachmentPoint = transform;

        SetActive(startActive);
    }

    private void OnValidate()
    {
        attachmentRadius = Mathf.Max(
            0.1f,
            attachmentRadius
        );

        detectionCollider =
            GetComponent<CircleCollider2D>();

        if (detectionCollider != null)
            ConfigureCollider();
    }

    private void ConfigureCollider()
    {
        detectionCollider.isTrigger = true;
        detectionCollider.radius = attachmentRadius;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive || attachedBall != null)
            return;

        ChainBallController2D ball =
            FindChainBall(other);

        if (ball == null)
            return;

        nearbyBall = ball;

        if (automaticallyAttach && canAttach)
            TryAttach(ball);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isActive || attachedBall != null)
            return;

        ChainBallController2D ball =
            FindChainBall(other);

        if (ball == null)
            return;

        nearbyBall = ball;

        if (automaticallyAttach && canAttach)
            TryAttach(ball);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        ChainBallController2D ball =
            FindChainBall(other);

        if (ball == null)
            return;

        if (nearbyBall == ball)
            nearbyBall = null;

        // Prevents instant recapture after detaching.
        if (requireExitBeforeReattaching)
            canAttach = true;
    }

    private ChainBallController2D FindChainBall(
        Collider2D other)
    {
        if (!IsLayerAllowed(other.gameObject.layer))
            return null;

        Rigidbody2D body = other.attachedRigidbody;

        if (body == null)
            return null;

        return body.GetComponent<ChainBallController2D>();
    }

    private bool IsLayerAllowed(int layer)
    {
        int layerMask = 1 << layer;

        return
            (ballLayers.value & layerMask) != 0;
    }

    private bool TryAttach(
        ChainBallController2D ball)
    {
        if (ball == null ||
            attachedBall != null ||
            !canAttach ||
            ball.IsAttachedToSocket)
        {
            return false;
        }

        bool attached =
    ball.AttachToSocket(
        this,
        attachmentPoint
    );

        if (!attached)
            return false;

        attachedBall = ball;
        nearbyBall = null;

        return true;
    }

    /// <summary>
    /// Manually attaches a nearby ball.
    /// Useful when Automatically Attach is disabled.
    /// </summary>
    public void AttachNearbyBall()
    {
        if (!isActive ||
            attachedBall != null ||
            nearbyBall == null)
        {
            return;
        }

        TryAttach(nearbyBall);
    }

    /// <summary>
    /// Releases the currently attached ball.
    /// Can be called from a switch or UnityEvent.
    /// </summary>
    public void DetachBall()
    {
        if (attachedBall == null)
            return;

        ChainBallController2D ballToRelease =
            attachedBall;

        /*
         * Do not clear attachedBall here.
         * DetachFromSocket will call NotifyBallDetached().
         */
        ballToRelease.DetachFromSocket(
            GetWorldReleaseVelocity()
        );
    }
    /// <summary>
    /// Called by the ball when it detaches itself.
    /// This clears the socket's internal occupied state.
    /// </summary>
    public void NotifyBallDetached(
        ChainBallController2D ball)
    {
        if (attachedBall != ball)
            return;

        attachedBall = null;
        nearbyBall = null;

        /*
         * The ball must leave the trigger before it can attach again.
         * This prevents it from instantly snapping back into the socket.
         */
        if (requireExitBeforeReattaching)
        {
            canAttach = false;
        }
        else
        {
            canAttach = true;
        }
    }

    private Vector2 GetWorldReleaseVelocity()
    {
        // The Inspector value is treated as local direction.
        return transform.TransformDirection(
            releaseVelocity
        );
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

        if (detectionCollider != null)
            detectionCollider.enabled = active;

        if (!active && attachedBall != null)
            DetachBall();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isActive
            ? new Color(0.2f, 1f, 0.6f, 0.25f)
            : new Color(0.5f, 0.5f, 0.5f, 0.15f);

        Gizmos.DrawSphere(
            transform.position,
            attachmentRadius
        );

        Gizmos.color = isActive
            ? new Color(0.2f, 1f, 0.6f, 1f)
            : Color.gray;

        Gizmos.DrawWireSphere(
            transform.position,
            attachmentRadius
        );

        Transform point =
            attachmentPoint != null
                ? attachmentPoint
                : transform;

        Gizmos.DrawWireSphere(
            point.position,
            0.15f
        );
    }
}