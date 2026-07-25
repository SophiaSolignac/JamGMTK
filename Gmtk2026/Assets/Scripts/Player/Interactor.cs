using GMTK.Inputs;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class PlayerInteractor : MonoBehaviour, I_Interactor
{
    [SerializeField]
    private Camera _camera;

    public float interactDistance = 3f;

    public GameObject From => gameObject;

    private void Start()
    {
        InputManager.onInteract.AddListener(CheckForInteractables);
    }
    public void CheckForInteractables()
    {
        // Check for interactable objects in front of the player
        RaycastHit hit;
        Debug.DrawRay(_camera.transform.position, _camera.transform.forward * interactDistance, Color.red, 1f);

        if (!Physics.Raycast(_camera.transform.position, _camera.transform.forward, out hit, interactDistance))
        {
            return;
        }
        I_Interactable interactable = hit.collider.GetComponent<I_Interactable>();

        if (interactable == null)
        {
            return;
        }
        Debug.Log($"Interacting with {interactable}");
        interactable.Interact(this);
    }
}

