using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [SerializeField] private MonoBehaviour[] targetBehaviours;
    [SerializeField] private string requiredTag = "Ball";

    private ITriggerable[] targets;
    private int objectsOnPlate;

    private void Awake()
    {
        targets = new ITriggerable[targetBehaviours.Length];

        for (int i = 0; i < targetBehaviours.Length; i++)
            targets[i] = targetBehaviours[i] as ITriggerable;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(requiredTag))
            return;

        objectsOnPlate++;

        if (objectsOnPlate == 1)
            SetTargets(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(requiredTag))
            return;

        objectsOnPlate = Mathf.Max(0, objectsOnPlate - 1);

        if (objectsOnPlate == 0)
            SetTargets(false);
    }

    private void SetTargets(bool active)
    {
        foreach (ITriggerable target in targets)
        {
            if (target == null)
                continue;

            if (active)
                target.Activate();
            else
                target.Deactivate();
        }
    }
}