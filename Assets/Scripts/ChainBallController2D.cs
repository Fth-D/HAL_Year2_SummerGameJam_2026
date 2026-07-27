using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(DistanceJoint2D))]
public class ChainBallController2D : MonoBehaviour
{
    [Header("连接对象")]
    [SerializeField]
    private Rigidbody2D playerBody;

    [SerializeField]
    private Transform handPoint;

    [SerializeField]
    private LineRenderer chainLine;

    [Header("链条设置")]
    [SerializeField]
    private float chainLength = 3.0f;

    [Header("挥球设置")]
    [SerializeField]
    private float swingForce = 35.0f;

    [SerializeField]
    private float maximumBallSpeed = 300.0f;

    private Rigidbody2D ballBody;
    private DistanceJoint2D distanceJoint;

    // 转球时使用的隐藏支点
    private GameObject spinPivotObject;
    private Rigidbody2D spinPivotBody;

    // Player当前是否正在输入移动
    private bool hasPlayerMoveInput;

    // 上一帧是否连接隐藏支点
    private bool wasUsingSpinPivot;

    private float swingInput;
    private bool wasSpinning;

    private void Awake()
    {
        ballBody = GetComponent<Rigidbody2D>();
        distanceJoint = GetComponent<DistanceJoint2D>();

        if (playerBody == null)
        {
            Debug.LogError(
                "ChainBallController2D：没有设置Player Body。"
            );

            enabled = false;
            return;
        }

        ConfigureJoint();
        CreateSpinPivot();
        IgnorePlayerCollision();
        ConnectJointToPlayer();
        ConfigureLineRenderer();
    }

    private void Update()
    {
        ReadInput();
    }

    private void FixedUpdate()
    {
        // 当前是否正在主动转球
        bool isSpinning =
            !Mathf.Approximately(swingInput, 0.0f);

        /*
         * 满足任意条件时，球不影响Player：
         *
         * 1. 正在按Q/E转球
         * 2. 正在按A/D移动
         * 3. 正在按W/Space跳跃
         */
        bool shouldUseSpinPivot =
            isSpinning ||
            hasPlayerMoveInput;

        // 隐藏支点始终跟随Player
        MoveSpinPivotToHand();

        // 只在状态改变时切换Joint
        if (shouldUseSpinPivot != wasUsingSpinPivot)
        {
            if (shouldUseSpinPivot)
            {
                // 玩家正在操作：球连接隐藏支点
                ConnectJointToSpinPivot();
            }
            else
            {
                // 玩家停止操作：球重新连接Player
                ConnectJointToPlayer();
            }

            wasUsingSpinPivot = shouldUseSpinPivot;
        }

        // 只有按Q/E时才主动给球旋转力
        if (isSpinning)
        {
            SwingBall();
        }

        LimitBallSpeed();
    }

    private void LateUpdate()
    {
        DrawChain();
    }

    private void ReadInput()
    {
        swingInput = 0.0f;
        hasPlayerMoveInput = false;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        // Q/E控制链球旋转
        if (keyboard.qKey.isPressed)
        {
            swingInput -= 5.0f;
        }

        if (keyboard.eKey.isPressed)
        {
            swingInput += 5.0f;
        }

        // A/D表示Player正在主动移动
        //W和空格表示正在跳跃
        hasPlayerMoveInput =
            keyboard.aKey.isPressed ||
            keyboard.dKey.isPressed ||
            keyboard.wKey.isPressed ||
            keyboard.spaceKey.isPressed;
    }

    private void ConfigureJoint()
    {
        distanceJoint.autoConfigureConnectedAnchor = false;
        distanceJoint.autoConfigureDistance = false;

        // 球这一端连接在球的中心
        distanceJoint.anchor = Vector2.zero;

        distanceJoint.distance = chainLength;

        // 链条可以松弛，但不能超过最大长度
        distanceJoint.maxDistanceOnly = true;

        distanceJoint.enableCollision = true;
    }

    private void CreateSpinPivot()
    {
        spinPivotObject =
            new GameObject("[Runtime] Chain Spin Pivot");

        spinPivotObject.transform.position =
            GetHandPosition();

        spinPivotBody =
            spinPivotObject.AddComponent<Rigidbody2D>();

        spinPivotBody.bodyType =
            RigidbodyType2D.Kinematic;

        spinPivotBody.gravityScale = 0.0f;

        spinPivotBody.linearVelocity = Vector2.zero;
        spinPivotBody.angularVelocity = 0.0f;

        spinPivotBody.interpolation =
            RigidbodyInterpolation2D.Interpolate;
    }

    private void MoveSpinPivotToHand()
    {
        if (spinPivotBody == null)
        {
            return;
        }

        spinPivotBody.MovePosition(
            GetHandPosition()
        );
    }

    private void ConnectJointToSpinPivot()
    {
        if (spinPivotBody == null)
        {
            return;
        }

        /*
         * 先把支点准确放到手的位置，
         * 防止切换瞬间出现位置跳动。
         */
        spinPivotBody.position = GetHandPosition();

        distanceJoint.connectedBody =
            spinPivotBody;

        // 支点物体的中心就是连接位置
        distanceJoint.connectedAnchor =
            Vector2.zero;
    }

    private void ConnectJointToPlayer()
    {
        distanceJoint.connectedBody =
            playerBody;

        UpdatePlayerConnectedAnchor();

        /*
         * 注意：
         * 这里绝对不能清空Ball的linearVelocity。
         * 松开按键后，Ball必须保留旋转速度，
         * 才能通过链条牵引Player。
         */
    }

    private void UpdatePlayerConnectedAnchor()
    {
        if (distanceJoint.connectedBody != playerBody)
        {
            return;
        }

        if (handPoint == null)
        {
            distanceJoint.connectedAnchor =
                Vector2.zero;

            return;
        }

        /*
         * connectedAnchor需要Player的局部坐标，
         * 不能直接使用HandPoint的世界坐标。
         */
        Vector3 localHandPosition =
            playerBody.transform.InverseTransformPoint(
                handPoint.position
            );

        distanceJoint.connectedAnchor =
            new Vector2(
                localHandPosition.x,
                localHandPosition.y
            );
    }

    private void SwingBall()
    {
        Vector2 centerPosition =
            spinPivotBody.position;

        // 从旋转中心指向球
        Vector2 radiusDirection =
            ballBody.position - centerPosition;

        // 球与中心完全重合时无法计算切线
        if (radiusDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        radiusDirection.Normalize();

        // 将半径方向旋转90度，得到切线方向
        Vector2 tangentDirection =
            new Vector2(
                -radiusDirection.y,
                radiusDirection.x
            );

        Vector2 swingVector =
            tangentDirection
            * swingInput
            * swingForce;

        ballBody.AddForce(
            swingVector,
            ForceMode2D.Force
        );
    }

    private void LimitBallSpeed()
    {
        float maximumSpeedSquared =
            maximumBallSpeed * maximumBallSpeed;

        if (ballBody.linearVelocity.sqrMagnitude
            <= maximumSpeedSquared)
        {
            return;
        }

        ballBody.linearVelocity =
            ballBody.linearVelocity.normalized
            * maximumBallSpeed;
    }

    private Vector2 GetHandPosition()
    {
        if (handPoint != null)
        {
            return handPoint.position;
        }

        return playerBody.position;
    }

    private void ConfigureLineRenderer()
    {
        if (chainLine == null)
        {
            return;
        }

        chainLine.positionCount = 2;
        chainLine.useWorldSpace = true;
    }

    private void DrawChain()
    {
        if (chainLine == null)
        {
            return;
        }

        chainLine.SetPosition(
            0,
            GetHandPosition()
        );

        chainLine.SetPosition(
            1,
            ballBody.position
        );
    }

    private void IgnorePlayerCollision()
    {
        Collider2D[] ballColliders =
            GetComponentsInChildren<Collider2D>();

        Collider2D[] playerColliders =
            playerBody.GetComponentsInChildren<Collider2D>();

        foreach (Collider2D ballCollider in ballColliders)
        {
            foreach (Collider2D playerCollider in playerColliders)
            {
                /*
                 * Trigger不能Ignore。
                 * KickCollision是Trigger，需要保留检测。
                 */
                if (ballCollider.isTrigger ||
                    playerCollider.isTrigger)
                {
                    continue;
                }

                /*
                 * 只忽略：
                 * 球的普通Collider × Player的普通Collider
                 */
                Physics2D.IgnoreCollision(
                    ballCollider,
                    playerCollider,
                    true
                );
            }
        }
    }

    private void OnDisable()
    {
        /*
         * 脚本被关闭时恢复Player连接，
         * 防止Joint永久留在隐藏支点上。
         */
        if (distanceJoint != null &&
            playerBody != null)
        {
            ConnectJointToPlayer();
        }
    }

    private void OnDestroy()
    {
        if (spinPivotObject != null)
        {
            Destroy(spinPivotObject);
        }
    }
}