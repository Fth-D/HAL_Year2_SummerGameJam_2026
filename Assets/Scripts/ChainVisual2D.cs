using UnityEngine;

public class ChainVisual2D : MonoBehaviour
{
    [Header("链条两端")]
    [SerializeField]
    private Transform startPoint;

    [SerializeField]
    private Transform endPoint;

    [Header("链节")]
    [SerializeField]
    private Transform[] links;

    [Header("链条弯曲")]
    [SerializeField]
    private float sagAmount = 0.5f;

    private void LateUpdate()
    {
        UpdateChainVisual();
    }

    private void UpdateChainVisual()
    {
        if (startPoint == null ||
            endPoint == null ||
            links == null ||
            links.Length == 0)
        {
            return;
        }

        Vector2 start =
            startPoint.position;

        Vector2 end =
            endPoint.position;

        /*
         * 中间控制点。
         *
         * 往下偏一点，让链条有自然下垂。
         */
        Vector2 middle =
            (start + end) * 0.5f;

        Vector2 controlPoint =
            middle +
            Vector2.down * sagAmount;

        for (int i = 0; i < links.Length; i++)
        {
            if (links[i] == null)
            {
                continue;
            }

            /*
             * 让每一节平均分布。
             */
            float t =
                (i + 0.5f) /
                links.Length;

            Vector2 position =
                GetBezierPosition(
                    start,
                    controlPoint,
                    end,
                    t
                );

            Vector2 direction =
                GetBezierDirection(
                    start,
                    controlPoint,
                    end,
                    t
                );

            links[i].position =
                position;

            /*
             * 如果你的链条图片默认是竖着的，
             * 所以这里减90度。
             *
             * 如果发现方向横了，
             * 就把 -90 删掉。
             */
            float angle =
                Mathf.Atan2(
                    direction.y,
                    direction.x
                )
                * Mathf.Rad2Deg
                - 90.0f;

            links[i].rotation =
                Quaternion.Euler(
                    0.0f,
                    0.0f,
                    angle
                );
        }
    }

    private Vector2 GetBezierPosition(
        Vector2 start,
        Vector2 control,
        Vector2 end,
        float t
    )
    {
        float oneMinusT =
            1.0f - t;

        return
            oneMinusT * oneMinusT * start
            +
            2.0f * oneMinusT * t * control
            +
            t * t * end;
    }

    private Vector2 GetBezierDirection(
        Vector2 start,
        Vector2 control,
        Vector2 end,
        float t
    )
    {
        Vector2 direction =
            2.0f * (1.0f - t)
            * (control - start)
            +
            2.0f * t
            * (end - control);

        return direction.normalized;
    }
}