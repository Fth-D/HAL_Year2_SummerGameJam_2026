using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class UIPlayerMove : MonoBehaviour
{
    [Header("移动速度")]
    [SerializeField]
    private float moveSpeed = 3.0f;

    [Header("每一格的相对距离")]
    [SerializeField]
    private float moveDistance = 3.0f;

    private Rigidbody2D rb;

    // 游戏开始时的位置，作为中间位置
    private float centerX;

    // -1：左边，0：中间，1：右边
    private int currentIndex = 0;

    private float targetX;
    private bool isMoving;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // 记录游戏开始时的世界坐标
        centerX = rb.position.x;

        targetX = centerX;
        currentIndex = 0;
        isMoving = false;
    }

    private void Update()
    {
        ReadInput();
    }

    private void FixedUpdate()
    {
        MoveToTarget();
    }

    private void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        // Q：向左移动一格
        if (keyboard.qKey.wasPressedThisFrame)
        {
            currentIndex--;

            if (currentIndex < -1)
            {
                currentIndex = -1;
            }

            UpdateTargetPosition();
        }

        // E：向右移动一格
        if (keyboard.eKey.wasPressedThisFrame)
        {
            currentIndex++;

            if (currentIndex > 1)
            {
                currentIndex = 1;
            }

            UpdateTargetPosition();
        }
    }

    private void UpdateTargetPosition()
    {
        targetX =
            centerX + currentIndex * moveDistance;

        isMoving = true;
    }

    private void MoveToTarget()
    {
        if (!isMoving)
        {
            rb.linearVelocityX = 0.0f;
            return;
        }

        float distance =
            targetX - rb.position.x;

        float oneFrameDistance =
            moveSpeed * Time.fixedDeltaTime;

        // 下一物理帧会超过目标点时，直接放到目标位置
        if (Mathf.Abs(distance) <= oneFrameDistance)
        {
            rb.position = new Vector2(
                targetX,
                rb.position.y
            );

            rb.linearVelocityX = 0.0f;
            isMoving = false;

            return;
        }

        // 保持固定速度移动
        rb.linearVelocityX =
            Mathf.Sign(distance) * moveSpeed;
    }
}