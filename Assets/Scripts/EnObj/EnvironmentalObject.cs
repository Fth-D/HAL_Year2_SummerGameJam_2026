using UnityEngine;

public class EnvironmentObject : MonoBehaviour
{
    [Header("Object Settings")]
    [SerializeField] protected bool isActive = true;
    [SerializeField] protected bool isInteractable;

    public virtual void Activate()
    {
        isActive = true;
    }

    public virtual void Deactivate()
    {
        isActive = false;
    }

    public virtual void Interact(GameObject interactor)
    {
        if (!isInteractable)
            return;

        Debug.Log($"{name} was interacted with by {interactor.name}");
    }
}