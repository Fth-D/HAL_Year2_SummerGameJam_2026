using UnityEngine;

public class PlayerDirectionLine : MonoBehaviour
{
    [Header("引用")]
    [SerializeField]
    private Rigidbody2D playerBody;

    [SerializeField]
    private LineRenderer directionLine;

    [SerializeField]
    private ChainBallController2D chainBall;

    [Header("方向线长度")]
    [SerializeField]
    private float minimumDirectionLineLength = 0.5f;

    [SerializeField]
    private float maximumDirectionLineLength = 30.0f;

    [Header("速度设置")]
    [SerializeField]
    private float minimumDirectionSpeed = 0.2f;

    [SerializeField]
    private float maximumPlayerSpeed = 30.0f;

    private void Awake()
    {
        // 如果没有手动设置，就从自己身上找
        if (playerBody == null)
        {
            playerBody = GetComponent<Rigidbody2D>();
        }

        ConfigureDirectionLine();
    }

    private void LateUpdate()
    {
        DrawDirectionLine();
    }

    private void ConfigureDirectionLine()
    {
        if (directionLine == null)
        {
            Debug.LogError(
                "PlayerDirectionLine：没有设置Direction Line。"
            );

            return;
        }

        directionLine.positionCount = 2;
        directionLine.useWorldSpace = true;

        directionLine.startWidth = 0.15f;
        directionLine.endWidth = 0.15f;

        directionLine.sortingOrder = 100;

        directionLine.enabled = false;
    }

    private void DrawDirectionLine()
    {
        if (directionLine == null ||
            playerBody == null ||
            chainBall == null)
        {
            return;
        }

        /*
         * 只有：
         *
         * 1. Ball已经吸附Socket
         * 2. 正在按Shift进入专注模式
         *
         * 才显示Player箭头。
         */
        if (!chainBall.IsAttachedToSocket ||
            !chainBall.IsFocusing)
        {
            directionLine.enabled = false;
            return;
        }

        Vector2 currentVelocity =
            playerBody.linearVelocity;

        float currentSpeed =
            currentVelocity.magnitude;

        // 速度太低时不显示
        if (currentSpeed < minimumDirectionSpeed)
        {
            directionLine.enabled = false;
            return;
        }

        // Player当前运动方向
        Vector2 direction =
            currentVelocity.normalized;

        // 根据速度决定箭头长度
        float speedRatio =
            Mathf.Clamp01(
                currentSpeed /
                maximumPlayerSpeed
            );

        float currentLineLength =
            Mathf.Lerp(
                minimumDirectionLineLength,
                maximumDirectionLineLength,
                speedRatio
            );

        Vector2 startPosition =
            playerBody.position;

        Vector2 endPosition =
            startPosition +
            direction *
            currentLineLength;

        directionLine.enabled = true;

        directionLine.SetPosition(
            0,
            startPosition
        );

        directionLine.SetPosition(
            1,
            endPosition
        );
    }
}