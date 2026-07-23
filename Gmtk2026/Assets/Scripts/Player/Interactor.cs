using GMTK.Inputs;
using System;
using UnityEngine;

public class Interactor : MonoBehaviour
{
    [SerializeField]
    private Camera _camera;

    public float interactDistance = 3f;
    private void Start()
    {
        InputManager.onInteract.AddListener(OnInteract);
    }

    private void OnInteract()
    {
        // Check for interactable objects in front of the player
        RaycastHit hit;
        Debug.DrawRay(_camera.transform.position, _camera.transform.forward * interactDistance, Color.red, 1f);

        if (Physics.Raycast(_camera.transform.position, _camera.transform.forward, out hit, interactDistance))
        {
            I_Interactable interactable = hit.collider.GetComponent<I_Interactable>();
            if (interactable != null && interactable.CanInteract())
            {
                interactable.Interact();
            }
        }
    }

    private void Update()
    {
        
    }
}
