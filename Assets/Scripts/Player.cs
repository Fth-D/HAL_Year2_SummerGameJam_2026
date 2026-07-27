using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private Vector2 moveInput;
    private Rigidbody2D rb;
    private bool OldJumpKey = false;
    private bool NewJumpKey = false;
    private bool IsGround = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        ReadInput();
        MoveUpdate();
    }

    private void MoveUpdate()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocityX += moveInput.x;
        if (rb.linearVelocityX > 20.0f) { rb.linearVelocityX = 20.0f; }
        if (rb.linearVelocityX < -20.0f) { rb.linearVelocityX = -20.0f; }
        rb.linearVelocityY += moveInput.y;


    }
    private void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        //==============================
        // 角色移动输入
        //==============================

        moveInput = Vector2.zero;

        if (keyboard.aKey.isPressed)
        {
            moveInput.x -= 1.0f;
        }

        if (keyboard.dKey.isPressed)
        {
            moveInput.x += 1.0f;
        }
        //if (keyboard.sKey.isPressed)
        //{
        //    moveInput.y -= 0.5f;
        //}

        OldJumpKey = NewJumpKey;
        NewJumpKey = (keyboard.wKey.isPressed || keyboard.spaceKey.isPressed);

        bool JumpTrigger = (!OldJumpKey && NewJumpKey); ;
        if (IsGround && JumpTrigger)
        {
            moveInput.y += 30.0f;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            IsGround = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            IsGround = false;
        }
    }
}
