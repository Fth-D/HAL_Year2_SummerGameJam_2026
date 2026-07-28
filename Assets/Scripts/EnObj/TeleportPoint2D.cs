using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class TeleportPoint2D : MonoBehaviour, ITriggerable
{
    [Header("Teleport Destination")]
    [SerializeField]
    private Transform destinationPoint;

    [Header("Activation")]
    [SerializeField]
    private bool startActive = true;

    [Header("Target Filtering")]
    [SerializeField]
    private LayerMask playerLayer;

    [SerializeField]
    private string playerTag = "Player";

    [Header("Connected Objects")]
    [Tooltip(
        "Automatically teleports Rigidbody2D objects connected " +
        "to the player through Joint2D components."
    )]
    [SerializeField]
    private bool teleportJointConnectedBodies = true;

    [Tooltip(
        "Additional Rigidbody2D objects that should always " +
        "teleport together with the player."
    )]
    [SerializeField]
    private Rigidbody2D[] additionalBodies;

    [Header("Velocity")]
    [Tooltip("Stops all teleported objects after teleporting.")]
    [SerializeField]
    private bool resetVelocity = true;

    [Tooltip("Velocity applied after teleporting.")]
    [SerializeField]
    private Vector2 exitVelocity;

    [Tooltip("Treat Exit Velocity as local to the destination.")]
    [SerializeField]
    private bool useDestinationDirection;

    [Tooltip(
        "Apply Exit Velocity to the ball and other connected " +
        "objects as well as the player."
    )]
    [SerializeField]
    private bool applyExitVelocityToAllBodies = true;

    [Header("Repeat Prevention")]
    [SerializeField, Min(0f)]
    private float teleportCooldown = 0.25f;

    [Tooltip(
        "Prevents this teleport point from activating again " +
        "until the player exits its trigger."
    )]
    [SerializeField]
    private bool requireExitBeforeReuse = true;

    private CircleCollider2D triggerCollider;

    private bool isActive;
    private bool canTeleport = true;

    public bool IsActive => isActive;

    private void Awake()
    {
        triggerCollider = GetComponent<CircleCollider2D>();
        triggerCollider.isTrigger = true;

        SetActive(startActive);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive || !canTeleport)
            return;

        if (!IsPlayer(other))
            return;

        Teleport(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!requireExitBeforeReuse)
            return;

        if (IsPlayer(other))
            canTeleport = true;
    }

    private bool IsPlayer(Collider2D other)
    {
        Rigidbody2D body = other.attachedRigidbody;

        if (body == null)
            return false;

        /*
         * Check the Rigidbody GameObject instead of only the
         * touched child collider.
         */
        if (!body.CompareTag(playerTag))
            return false;

        int bodyLayerMask =
            1 << body.gameObject.layer;

        return
            (playerLayer.value & bodyLayerMask) != 0;
    }

    private void Teleport(Collider2D playerCollider)
    {
        if (destinationPoint == null)
        {
            Debug.LogWarning(
                "TeleportPoint2D: Destination Point is not assigned.",
                this
            );

            return;
        }

        Rigidbody2D playerBody =
            playerCollider.attachedRigidbody;

        if (playerBody == null)
        {
            Debug.LogWarning(
                "TeleportPoint2D: Player has no Rigidbody2D.",
                playerCollider
            );

            return;
        }

        canTeleport = false;

        List<Rigidbody2D> bodiesToTeleport =
            CollectBodiesToTeleport(playerBody);

        Vector2 destinationPosition =
            destinationPoint.position;

        /*
         * Every body receives the same offset.
         *
         * This preserves the original player-to-ball distance.
         */
        Vector2 teleportOffset =
            destinationPosition - playerBody.position;

        MoveAllBodies(
            bodiesToTeleport,
            teleportOffset
        );

        HandleVelocities(
            bodiesToTeleport,
            playerBody
        );

        Physics2D.SyncTransforms();

        if (!requireExitBeforeReuse)
        {
            StartCoroutine(
                RestoreTeleportAfterCooldown()
            );
        }
    }

    private List<Rigidbody2D> CollectBodiesToTeleport(
        Rigidbody2D playerBody)
    {
        HashSet<Rigidbody2D> collectedBodies =
            new HashSet<Rigidbody2D>();

        Queue<Rigidbody2D> bodiesToSearch =
            new Queue<Rigidbody2D>();

        collectedBodies.Add(playerBody);
        bodiesToSearch.Enqueue(playerBody);

        /*
         * Search the complete joint connection graph.
         *
         * This works even when the DistanceJoint2D is located
         * on the ball instead of the player.
         */
        if (teleportJointConnectedBodies)
        {
            Joint2D[] allJoints =
                FindObjectsByType<Joint2D>(
                    FindObjectsSortMode.None
                );

            while (bodiesToSearch.Count > 0)
            {
                Rigidbody2D currentBody =
                    bodiesToSearch.Dequeue();

                foreach (Joint2D joint in allJoints)
                {
                    if (joint == null || !joint.enabled)
                        continue;

                    Rigidbody2D jointOwner =
                        joint.GetComponent<Rigidbody2D>();

                    Rigidbody2D connectedBody =
                        joint.connectedBody;

                    /*
                     * Current body owns the joint:
                     *
                     * Current → Connected
                     */
                    if (jointOwner == currentBody &&
                        connectedBody != null)
                    {
                        AddBodyToCollection(
                            connectedBody,
                            collectedBodies,
                            bodiesToSearch
                        );
                    }

                    /*
                     * Another object has a joint connected
                     * to the current body:
                     *
                     * Joint owner → Current
                     */
                    if (connectedBody == currentBody &&
                        jointOwner != null)
                    {
                        AddBodyToCollection(
                            jointOwner,
                            collectedBodies,
                            bodiesToSearch
                        );
                    }
                }
            }
        }

        if (additionalBodies != null)
        {
            foreach (Rigidbody2D additionalBody
                     in additionalBodies)
            {
                if (additionalBody != null)
                    collectedBodies.Add(additionalBody);
            }
        }

        return new List<Rigidbody2D>(
            collectedBodies
        );
    }

    private void AddBodyToCollection(
        Rigidbody2D body,
        HashSet<Rigidbody2D> collectedBodies,
        Queue<Rigidbody2D> bodiesToSearch)
    {
        if (body == null)
            return;

        if (collectedBodies.Add(body))
            bodiesToSearch.Enqueue(body);
    }

    private void MoveAllBodies(
        List<Rigidbody2D> bodies,
        Vector2 teleportOffset)
    {
        foreach (Rigidbody2D body in bodies)
        {
            if (body == null)
                continue;

            body.position += teleportOffset;
        }
    }

    private void HandleVelocities(
        List<Rigidbody2D> bodies,
        Rigidbody2D playerBody)
    {
        Vector2 finalExitVelocity =
            GetExitVelocity();

        foreach (Rigidbody2D body in bodies)
        {
            if (body == null)
                continue;

            if (resetVelocity)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }

            if (applyExitVelocityToAllBodies ||
                body == playerBody)
            {
                body.linearVelocity +=
                    finalExitVelocity;
            }
        }
    }

    private Vector2 GetExitVelocity()
    {
        if (!useDestinationDirection)
            return exitVelocity;

        return destinationPoint.TransformDirection(
            exitVelocity
        );
    }

    private IEnumerator RestoreTeleportAfterCooldown()
    {
        yield return new WaitForSeconds(
            teleportCooldown
        );

        canTeleport = true;
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

        if (triggerCollider != null)
            triggerCollider.enabled = active;
    }

    private void OnDrawGizmos()
    {
        CircleCollider2D circle =
            GetComponent<CircleCollider2D>();

        float radius =
            circle != null
                ? circle.radius *
                  Mathf.Max(
                      Mathf.Abs(transform.lossyScale.x),
                      Mathf.Abs(transform.lossyScale.y)
                  )
                : 0.5f;

        Gizmos.color = startActive
            ? new Color(0.1f, 0.8f, 1f, 0.3f)
            : new Color(0.5f, 0.5f, 0.5f, 0.2f);

        Gizmos.DrawSphere(
            transform.position,
            radius
        );

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            radius
        );

        if (destinationPoint != null)
        {
            Gizmos.DrawLine(
                transform.position,
                destinationPoint.position
            );

            Gizmos.DrawWireSphere(
                destinationPoint.position,
                0.25f
            );
        }
    }
}