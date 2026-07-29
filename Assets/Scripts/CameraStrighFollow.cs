using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public float smoothSpeed = 5f;

    public float minY = 0f;
    public float maxY = 21f;

    void LateUpdate()
    {
        // æ“¾Šß‰ÆYÀ•W
        float targetY = player.position.y;

        // ŒÀ§Camera Y”Íš¡
        targetY = Mathf.Clamp(targetY, minY, maxY);

        // •ÛX˜aZ•sÌC‘üˆÚ“®Y
        Vector3 targetPosition = new Vector3(
        transform.position.x,
        targetY,
        transform.position.z
        );

        // •½ŠŠˆÚ“®
        transform.position = Vector3.Lerp(
        transform.position,
        targetPosition,
        smoothSpeed * Time.deltaTime
        );
    }
}