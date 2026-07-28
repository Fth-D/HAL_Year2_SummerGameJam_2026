using UnityEngine;

public class ActivateCamera : MonoBehaviour
{
    public GameObject cameraToActivate; // The camera to activate
    public LayerMask playerLayer; // The layer of the player

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            cameraToActivate.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            cameraToActivate.SetActive(false);
        }
    }
}
