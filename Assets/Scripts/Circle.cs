using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class Circle : MonoBehaviour
{
    private Vector2 moveInput;
    private bool CanKick;

    [Header("连接对象")]
    [SerializeField]
    private Rigidbody2D playerBody;
    private Rigidbody2D rb;

    private float kickSpeed = 5000.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        ReadInput();
    }

    private void ReadInput()
    {
        rb = GetComponent<Rigidbody2D>();
        float2 playerPos=rb.position;
        float2 ballPos=playerBody.position;
        Keyboard keyboard = Keyboard.current;
        if(CanKick)
        {
            if (keyboard.kKey.isPressed)
            {
                Vector2 kickDirection = playerPos - ballPos;
                kickDirection.Normalize();

                rb.linearVelocity = kickDirection * kickSpeed;
            }
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

    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CanKick = true;
            Debug.Log("Player进入踢球范围");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CanKick = false;
            Debug.Log("Player离开踢球范围");
        }
    }
}
