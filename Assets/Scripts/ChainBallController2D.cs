using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(DistanceJoint2D))]

public class ChainBallController2D : MonoBehaviour
{
    [Header("Socket State")]
    [SerializeField]
    private bool isAttachedToSocket;

    private Transform currentSocketPoint;
    private RigidbodyType2D bodyTypeBeforeSocket;
    private float gravityScaleBeforeSocket;

    public bool IsAttachedToSocket => isAttachedToSocket;
    public bool IsFocusing => isFocusing;

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

    [Header("挥人设置")]
    [SerializeField]
    private float playerSwingForce = 35.0f;

    [SerializeField]
    private float maximumBallSpeed = 30.0f;

    private Rigidbody2D ballBody;
    private DistanceJoint2D distanceJoint;


    // 转球时使用的隐藏支点
    private GameObject spinPivotObject;
    private Rigidbody2D spinPivotBody;

    // Player当前是否正在输入移动
    private bool hasPlayerMoveInput;

    // 上一帧是否连接隐藏支点
    private bool wasUsingSpinPivot;

    // 转人模式：固定球使用的隐藏支点
    private GameObject ballPivotObject;
    private Rigidbody2D ballPivotBody;

    // 转人模式：把球锁在隐藏支点上的Joint
    private FixedJoint2D ballLockJoint;
    //是否转球
    private float swingInput;
    //上一帧是否转球
    private bool wasSpinning;
    // I/P控制Player旋转
    private float playerSwingInput;

    // 记录上一帧是否处于转人状态
    private bool wasPlayerSwinging;

    [Header("专注模式")]
    [SerializeField]
    private LineRenderer directionLine;

    [Header("方向线长度")]
    [SerializeField]
    private float minimumDirectionLineLength = 0.5f;

    [SerializeField]
    private float maximumDirectionLineLength = 30.0f;

    [Header("方向显示最低速度")]
    [SerializeField]
    private float minimumDirectionSpeed = 0.2f;

    [Header("慢动作倍率")]
    [SerializeField]
    [Range(0.05f, 1.0f)]
    private float focusTimeScale = 0.2f;

    // 当前是否处于Shift专注模式
    private bool isFocusing;

    // 用来检测Shift的按下和松开
    private bool wasFocusHeld;

    // 用于恢复进入慢动作前的时间设置
    private float previousTimeScale;
    private float previousFixedDeltaTime;

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
        CreateBallPivot();
        IgnorePlayerCollision();
        ConnectJointToPlayer();
        ConfigureLineRenderer();
        ConfigureDirectionLine();
    }

    private void Update()
    {
        ReadInput();
        UpdateFocusInput();
    }

    private void FixedUpdate()
    {
        // 当前是否正在主动转球
        bool isSpinning =
            !Mathf.Approximately(swingInput, 0.0f);

        bool isPlayerSwinging =
        !Mathf.Approximately(
        playerSwingInput,
        0.0f
    );
        /*
         * 满足任意条件时，球不影响Player：
         *
         * 1. 正在按Q/E转球
         * 2. 正在按A/D移动
         * 3. 正在按W/Space跳跃
         */
        bool shouldUseSpinPivot =
    !isPlayerSwinging &&
    (
        isSpinning ||
        hasPlayerMoveInput
    );

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

        // I/P状态发生变化时，固定或解除固定Ball
        if (isPlayerSwinging != wasPlayerSwinging)
        {
            if (isPlayerSwinging)
            {
                BeginPlayerSwing();
            }
            else
            {
                EndPlayerSwing();
            }

            wasPlayerSwinging =
                isPlayerSwinging;
        }
        // 转人模式优先，防止两套系统同时运行
        if (isPlayerSwinging)
        {
            SwingPlayer();
        }
        else if (isSpinning)
        {
            SwingBall();
        }

        LimitBallSpeed();
    }

    private void LateUpdate()
    {
        DrawChain();
        DrawDirectionLine();
    }

    private void ReadInput()
    {
        swingInput = 0.0f;
        playerSwingInput = 0.0f;
        hasPlayerMoveInput = false;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        // Q/E控制链球旋转
        if (keyboard.qKey.isPressed)
        {
            if (isAttachedToSocket)
            {
                playerSwingInput -= 5.0f;
            }
            else swingInput -= 5.0f;
        }

        if (keyboard.eKey.isPressed)
        {
            if (isAttachedToSocket)
            {
                playerSwingInput += 5.0f;
            }
            else swingInput += 5.0f;
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
    //球固定点
    private void CreateBallPivot()
    {
        // 创建转人模式使用的隐藏固定点
        ballPivotObject =
            new GameObject("[Runtime] Ball Fixed Pivot");

        ballPivotObject.transform.position =
            ballBody.position;

        // 隐藏固定点使用Kinematic刚体
        ballPivotBody =
            ballPivotObject.AddComponent<Rigidbody2D>();

        ballPivotBody.bodyType =
            RigidbodyType2D.Kinematic;

        ballPivotBody.gravityScale =
            0.0f;

        ballPivotBody.linearVelocity =
            Vector2.zero;

        ballPivotBody.angularVelocity =
            0.0f;

        ballPivotBody.interpolation =
            RigidbodyInterpolation2D.Interpolate;

        // 在Ball上创建FixedJoint2D
        ballLockJoint =
            gameObject.AddComponent<FixedJoint2D>();

        ballLockJoint.autoConfigureConnectedAnchor =
            false;

        ballLockJoint.anchor =
            Vector2.zero;

        ballLockJoint.connectedAnchor =
            Vector2.zero;

        ballLockJoint.connectedBody =
            ballPivotBody;

        ballLockJoint.enableCollision =
            false;

        // 初始关闭，按I/P时才打开
        ballLockJoint.enabled =
            false;
    }
    //固定球的函数
    private void BeginPlayerSwing()
    {
        if (ballPivotBody == null ||
       ballLockJoint == null)
        {
            return;
        }

        // 把固定支点放到球当前的位置
        ballPivotBody.position =
            ballBody.position;

        ballPivotBody.linearVelocity =
            Vector2.zero;

        ballPivotBody.angularVelocity =
            0.0f;

        // 球进入固定状态前先停止
        ballBody.linearVelocity =
            Vector2.zero;

        ballBody.angularVelocity =
            0.0f;

        // 保持Ball和Player之间原来的DistanceJoint
        ConnectJointToPlayer();

        // 固定球
        ballLockJoint.connectedBody =
            ballPivotBody;

        ballLockJoint.enabled =
            true;
    }

    //解除固定球
    private void EndPlayerSwing()
    {
        DetachFromSocket();
        if (ballLockJoint == null)
        {
            return;
        }

        ballLockJoint.enabled =
            false;

        ConnectJointToPlayer();
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

    private void SwingPlayer()
    {

        if (!isAttachedToSocket)
        {
            return;
        }

        if (ballPivotBody == null)
        {
            return;
        }

        Vector2 centerPosition =
            ballPivotBody.position;

        // 从固定球指向Player
        Vector2 radiusDirection =
            playerBody.position - centerPosition;

        if (radiusDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        radiusDirection.Normalize();

        // 半径方向旋转90度，得到切线方向
        Vector2 tangentDirection =
            new Vector2(
                -radiusDirection.y,
                radiusDirection.x
            );

        Vector2 swingVector =
            tangentDirection
            * playerSwingInput
            * playerSwingForce;

        // 每个FixedUpdate持续加力
        playerBody.AddForce(
            swingVector,
            ForceMode2D.Force
        );
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
      * 如果脚本在慢动作期间被关闭，
      * 必须恢复正常时间。
      */
        if (isFocusing)
        {
            EndFocus();
        }

        wasFocusHeld = false;

        /*
         * 原来已有的Joint恢复逻辑。
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

        if (ballPivotObject != null)
        {
            Destroy(ballPivotObject);
        }
    }

    private void UpdateFocusInput()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        bool focusHeld =
            keyboard.leftShiftKey.isPressed ||
            keyboard.rightShiftKey.isPressed;

        // 刚刚按下Shift
        if (focusHeld && !wasFocusHeld)
        {
            BeginFocus();
        }

        // 刚刚松开Shift
        if (!focusHeld && wasFocusHeld)
        {
            EndFocus();
        }

        wasFocusHeld = focusHeld;
    }

    private void BeginFocus()
    {
        if (isFocusing)
        {
            return;
        }

        isFocusing = true;

        Debug.Log(
       $"进入专注模式，球速={ballBody.linearVelocity.magnitude}，" +
       $"方向线={directionLine}"
   );


        // 保存进入慢动作前的设置
        previousTimeScale =
            Time.timeScale;

        previousFixedDeltaTime =
            Time.fixedDeltaTime;

        /*
         * 把整个游戏减速。
         * 例如focusTimeScale为0.2，
         * 游戏就以五分之一速度运行。
         */
        Time.timeScale =
            focusTimeScale;

        /*
         * 根据timeScale调整物理更新时间，
         * 避免慢动作时物理表现过于跳跃。
         */
        float timeScaleRatio =
            focusTimeScale /
            Mathf.Max(previousTimeScale, 0.001f);

        Time.fixedDeltaTime =
            previousFixedDeltaTime *
            timeScaleRatio;
    }

    private void EndFocus()
    {
        if (!isFocusing)
        {
            return;
        }

        isFocusing = false;

        // 恢复进入慢动作前的设置
        Time.timeScale =
            previousTimeScale;

        Time.fixedDeltaTime =
            previousFixedDeltaTime;

        if (directionLine != null)
        {
            directionLine.enabled = false;
        }
    }

    //初始化方向线
    private void ConfigureDirectionLine()
    {
        if (directionLine == null)
        {
            Debug.LogError("Direction Line没有设置");
            return;
        }

        directionLine.positionCount = 2;
        directionLine.useWorldSpace = true;

        // 测试时设置得明显一点
        directionLine.startWidth = 0.15f;
        directionLine.endWidth = 0.15f;

        directionLine.startColor = Color.red;
        directionLine.endColor = Color.red;

        // 保证显示在其他Sprite上面
        directionLine.sortingOrder = 100;

        directionLine.enabled = false;
    }

    //绘制当前前进方向
    private void DrawDirectionLine()
    {
        if (directionLine == null)
        {
            return;
        }

        // 只有按住Shift时才显示
        if (!isFocusing)
        {
            directionLine.enabled = false;
            return;
        }

        Vector2 currentVelocity =
            ballBody.linearVelocity;

        float currentSpeed =
            currentVelocity.magnitude;

        // 球速度太低时，不显示方向
        if (currentSpeed < minimumDirectionSpeed)
        {
            directionLine.enabled = false;
            return;
        }

        // 当前真实前进方向
        Vector2 direction =
            currentVelocity.normalized;

        /*
         * 计算当前速度占最高速度的比例。
         *
         * 例如：
         * 当前速度7.5
         * 最大速度15
         * 比例就是0.5
         */
        float speedRatio =
            Mathf.Clamp01(
                currentSpeed /
                maximumBallSpeed
            );

        /*
         * 根据速度比例计算箭头长度。
         *
         * 慢时接近最短长度，
         * 快时接近最长长度。
         */
        float currentLineLength =
            Mathf.Lerp(
                minimumDirectionLineLength,
                maximumDirectionLineLength,
                speedRatio
            );

        Vector2 startPosition =
            ballBody.position;

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

    //Socket Functions
    /// <summary>
    /// Attempts to attach the chain ball to a socket point.
    /// </summary>
    public bool AttachToSocket(Transform socketPoint)
    {
        if (socketPoint == null)
        {
            Debug.LogWarning(
                "ChainBallController2D: Socket point is null.",
                this
            );

            return false;
        }

        if (isAttachedToSocket)
            return false;

        if (isFocusing)
            EndFocus();

        currentSocketPoint = socketPoint;
        isAttachedToSocket = true;

        bodyTypeBeforeSocket = ballBody.bodyType;
        gravityScaleBeforeSocket = ballBody.gravityScale; 

        ballBody.linearVelocity = Vector2.zero;
        ballBody.angularVelocity = 0f;

        // A kinematic body will remain locked in the socket.
        ballBody.bodyType = RigidbodyType2D.Kinematic;
        ballBody.gravityScale = 0f;

        ballBody.position = currentSocketPoint.position;
        ballBody.rotation = currentSocketPoint.eulerAngles.z;

        swingInput = 0f;
        hasPlayerMoveInput = false;

        if (directionLine != null)
            directionLine.enabled = false;

        return true;
    }

    /// <summary>
    /// Detaches the ball and reconnects it to the player.
    /// </summary>
    public bool DetachFromSocket()
    {
        return DetachFromSocket(Vector2.zero);
    }

    /// <summary>
    /// Detaches the ball with an optional release velocity.
    /// </summary>
    public bool DetachFromSocket(Vector2 releaseVelocity)
    {
        if (!isAttachedToSocket)
            return false;

        isAttachedToSocket = false;
        currentSocketPoint = null;

        ballBody.bodyType = bodyTypeBeforeSocket;
        ballBody.gravityScale = gravityScaleBeforeSocket;

        ballBody.linearVelocity = releaseVelocity;
        ballBody.angularVelocity = 0f;

        distanceJoint.enabled = true;
        ConnectJointToPlayer();

        wasUsingSpinPivot = false;

        return true;
    }

    private void HoldBallAtSocket()
    {
        if (currentSocketPoint == null)
        {
            DetachFromSocket();
            return;
        }

        ballBody.MovePosition(currentSocketPoint.position);
        ballBody.MoveRotation(currentSocketPoint.eulerAngles.z);

        ballBody.linearVelocity = Vector2.zero;
        ballBody.angularVelocity = 0f;
    }
}