using UnityEngine;
using UnityEngine.InputSystem;

public class EscUIController : MonoBehaviour
{
    [SerializeField]
    private GameObject escUI;

    private bool isOpen = false;

    private void Start()
    {
        if (escUI != null)
        {
            escUI.SetActive(false);
        }

        isOpen = false;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            Debug.Log("没有输入");
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            ToggleEscUI();
        }
    }

    private void ToggleEscUI()
    {
        if (escUI == null)
        {

            return;
        }

        isOpen = !isOpen;

        escUI.SetActive(isOpen);
    }
}