using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class WindBlower2D : MonoBehaviour, ITriggerable
{
    public enum ForceMode
    {
        Continuous,
        InstantImpulse
    }

    [Header("Wind Area")]
    [SerializeField, Min(0.1f)]
    private float blowingLength = 5f;

    [SerializeField, Min(0.1f)]
    private float blowingWidth = 2f;

    [Header("Wind Force")]
    [SerializeField, Min(0f)]
    private float blowingStrength = 10f;

    [SerializeField]
    private ForceMode forceMode = ForceMode.Continuous;

    [Header("Activation")]
    [SerializeField]
    private bool startActive = true;

    [Header("Target Filtering")]
    [SerializeField]
    private LayerMask targetLayers;

    [SerializeField]
    private bool affectTriggerColliders = false;

    private BoxCollider2D windCollider;
    private bool isActive;

    // Used to prevent repeated impulses while the same object stays inside.
    private readonly HashSet<Rigidbody2D> impulseTargets = new();

    public bool IsActive => isActive;

    /// <summary>
    /// Wind blows along the object's local right direction.
    /// Rotate the GameObject to change the direction.
    /// </summary>
    public Vector2 BlowingDirection => transform.right.normalized;

    private void Awake()
    {
        windCollider = GetComponent<BoxCollider2D>();

        ConfigureCollider();
        SetActive(startActive);
    }

    private void OnValidate()
    {
        blowingLength = Mathf.Max(0.1f, blowingLength);
        blowingWidth = Mathf.Max(0.1f, blowingWidth);
        blowingStrength = Mathf.Max(0f, blowingStrength);

        windCollider = GetComponent<BoxCollider2D>();

        if (windCollider != null)
            ConfigureCollider();
    }

    private void ConfigureCollider()
    {
        windCollider.isTrigger = true;

        // The wind begins at the blower and extends forward.
        windCollider.size = new Vector2(blowingLength, blowingWidth);
        windCollider.offset = new Vector2(blowingLength * 0.5f, 0f);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isActive)
            return;

        if (forceMode != ForceMode.Continuous)
            return;

        TryApplyWind(other, ForceMode2D.Force);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive)
            return;

        if (forceMode != ForceMode.InstantImpulse)
            return;

        Rigidbody2D targetBody = other.attachedRigidbody;

        if (targetBody == null || impulseTargets.Contains(targetBody))
            return;

        if (TryApplyWind(other, ForceMode2D.Impulse))
            impulseTargets.Add(targetBody);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Rigidbody2D targetBody = other.attachedRigidbody;

        if (targetBody != null)
            impulseTargets.Remove(targetBody);
    }

    private bool TryApplyWind(Collider2D target, ForceMode2D mode)
    {
        if (!IsTargetAllowed(target))
            return false;

        Rigidbody2D targetBody = target.attachedRigidbody;

        if (targetBody == null)
            return false;

        if (targetBody.bodyType != RigidbodyType2D.Dynamic)
            return false;

        Vector2 windForce = BlowingDirection * blowingStrength;
        targetBody.AddForce(windForce, mode);

        return true;
    }

    private bool IsTargetAllowed(Collider2D target)
    {
        if (!affectTriggerColliders && target.isTrigger)
            return false;

        int targetLayerMask = 1 << target.gameObject.layer;

        return (targetLayers.value & targetLayerMask) != 0;
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

        if (windCollider != null)
            windCollider.enabled = active;

        if (!active)
            impulseTargets.Clear();
    }

    public void SetBlowingStrength(float newStrength)
    {
        blowingStrength = Mathf.Max(0f, newStrength);
    }

    public void SetBlowingLength(float newLength)
    {
        blowingLength = Mathf.Max(0.1f, newLength);
        ConfigureCollider();
    }

    public void SetBlowingWidth(float newWidth)
    {
        blowingWidth = Mathf.Max(0.1f, newWidth);
        ConfigureCollider();
    }

    public void SetBlowingDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnDrawGizmos()
    {
        Vector3 center =
            transform.position +
            transform.right * (blowingLength * 0.5f);

        Matrix4x4 previousMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(
            center,
            transform.rotation,
            Vector3.one
        );

        Gizmos.color = isActive
            ? new Color(0.2f, 0.8f, 1f, 0.25f)
            : new Color(0.5f, 0.5f, 0.5f, 0.15f);

        Gizmos.DrawCube(
            Vector3.zero,
            new Vector3(blowingLength, blowingWidth, 0.1f)
        );

        Gizmos.color = isActive
            ? new Color(0.2f, 0.8f, 1f, 1f)
            : Color.gray;

        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(blowingLength, blowingWidth, 0.1f)
        );

        Gizmos.matrix = previousMatrix;

        Vector3 arrowStart = transform.position;
        Vector3 arrowEnd =
            arrowStart + transform.right * blowingLength;

        Gizmos.DrawLine(arrowStart, arrowEnd);

        Vector3 arrowDirection = transform.right;
        Vector3 arrowSide = transform.up;

        Gizmos.DrawLine(
            arrowEnd,
            arrowEnd - arrowDirection * 0.4f + arrowSide * 0.25f
        );

        Gizmos.DrawLine(
            arrowEnd,
            arrowEnd - arrowDirection * 0.4f - arrowSide * 0.25f
        );
    }
}