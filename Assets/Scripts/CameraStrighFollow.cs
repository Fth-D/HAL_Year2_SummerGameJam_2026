using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public float smoothSpeed = 5f;

    public float minY = 0f;
    public float maxY = 21f;

    void LateUpdate()
    {
        Vector3 targetPos = new Vector3(
        transform.position.x,
        player.position.y,
        transform.position.z
        );

        transform.position = Vector3.Lerp(
        transform.position,
        targetPos,
        smoothSpeed * Time.deltaTime
        );
    }
}