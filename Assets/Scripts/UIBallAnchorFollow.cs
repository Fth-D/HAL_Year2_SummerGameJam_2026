using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Rigidbody2D))]
public class UIBallAnchorFollow : MonoBehaviour
{
    [SerializeField]
    private Rigidbody2D playerBody;

    [SerializeField]
    private Transform followTarget;

    private Rigidbody2D anchorBody;

    private void Awake()
    {
        anchorBody = GetComponent<Rigidbody2D>();

        // 这个刚体只用来承受球和链条的力量
        anchorBody.bodyType = RigidbodyType2D.Kinematic;
        anchorBody.gravityScale = 0.0f;

        FollowPlayerImmediately();
    }

    private void FixedUpdate()
    {
        if (playerBody == null)
        {
            return;
        }

        Vector2 targetPosition;

        if (followTarget != null)
        {
            // 有手部连接点时，跟随手的位置
            targetPosition = followTarget.position;
        }
        else
        {
            // 没有连接点时，跟随Player中心
            targetPosition = playerBody.position;
        }

        // 每个物理帧修正到Player的位置
        anchorBody.position = targetPosition;

        // 同时保持和Player相同的运动速度
        anchorBody.linearVelocity =
            playerBody.linearVelocity;

        anchorBody.angularVelocity =
            playerBody.angularVelocity;
    }

    private void FollowPlayerImmediately()
    {
        if (playerBody == null)
        {
            return;
        }

        if (followTarget != null)
        {
            anchorBody.position =
                followTarget.position;
        }
        else
        {
            anchorBody.position =
                playerBody.position;
        }
    }
}