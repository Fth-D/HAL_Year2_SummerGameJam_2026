using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField]
    public bool onlyX = false;
    [SerializeField]
    public bool onlyY = false;

    public Transform player;
    public float smoothSpeed = 5f;

    public float minX = 0f;
    public float maxX = 0f;

    public float minY = 0f;
    public float maxY = 21f;

    void LateUpdate()
    {
        // 取得玩家X座標
        float targetX = player.position.x;

        // 限制Camera X範圍
        targetX = Mathf.Clamp(targetX, minX, maxX);

        // 取得玩家Y座標
        float targetY = player.position.y;

        // 限制Camera Y範圍
        targetY = Mathf.Clamp(targetY, minY, maxY);

        if (onlyX)
        {
            // 保持X和Z不變，只移動Y
            Vector3 targetPosition = new Vector3(
            targetX,
            transform.position.y,
            transform.position.z
            );

            // 平滑移動
            transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
            );
        }
        else if (onlyY)
        {
            // 保持X和Z不變，只移動Y
            Vector3 targetPosition = new Vector3(
            transform.position.x,
            targetY,
            transform.position.z
            );

            // 平滑移動
            transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
            );
        }
        else
        {
            // 同時移動X和Y
            Vector3 targetPosition = new Vector3(
            targetX,
            targetY,
            transform.position.z
            );
            // 平滑移動
            transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
            );
        }

    }
}