using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] PlayerSound playerSound;

    private Vector2 moveInput;
    private Rigidbody2D rb;
    private bool OldJumpKey = false;
    private bool NewJumpKey = false;
    private bool IsGround = false;
    public bool IsStart;
    public bool IsQuit;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            IsGround = true;
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
            playerSound?.PlayPlayerHitWall();
        }

        if (collision.gameObject.CompareTag("Teleport"))
        {
            playerSound?.PlayTeleport();
        }

        if (collision.gameObject.CompareTag("Laser"))
        {
            playerSound?.PlayPlayerHitLaser();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            IsGround = false;
        }
    }


    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("StartArea"))
        {
            IsStart=true;
        }
        else if (other.CompareTag("QuitArea"))
        {
            IsQuit=true;
        }   
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("StartArea"))
        {
            IsStart = false;
        }
        else if (other.CompareTag("QuitArea"))
        {
            IsQuit = false;
        }
    }

}
