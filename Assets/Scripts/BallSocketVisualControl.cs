using UnityEngine;

[RequireComponent(typeof(BallSocket2D))]
public class BallSocketVisual2D : MonoBehaviour
{
    [Header("外观切换")]
    [SerializeField] private SpriteRenderer socketSpriteRenderer;
    [SerializeField] private Sprite emptySprite;    // 没吸住球的时候
    [SerializeField] private Sprite attachedSprite; // 吸住球的时候

    private BallSocket2D socket;
    private bool wasAttached;

    private void Awake()
    {
        socket = GetComponent<BallSocket2D>();
    }

    private void Update()
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (socketSpriteRenderer == null ||
            emptySprite == null ||
            attachedSprite == null)
        {
            return;
        }

        bool isAttached = socket.HasAttachedBall;

        if (isAttached == wasAttached)
        {
            return;
        }

        socketSpriteRenderer.sprite =
            isAttached ? attachedSprite : emptySprite;

        wasAttached = isAttached;
    }
}