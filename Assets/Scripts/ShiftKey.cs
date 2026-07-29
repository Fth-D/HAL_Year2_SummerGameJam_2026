using UnityEngine;
using UnityEngine.InputSystem;


public class PressShiftKey : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        bool isPressed =
            keyboard != null &&
            keyboard.leftShiftKey.isPressed;

        animator.SetBool("IsPress", isPressed);
    }
}