using GMTK.Inputs;
using System;
using System.Threading.Tasks;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInteractor : MonoBehaviour, I_Interactor
{
    [SerializeField]
    private Camera _camera;

    public float interactDistance = 3f;
    private void Start()
    {
        InputManager.onInteract.AddListener(CheckForInteractables);
    }
    public void CheckForInteractables() 
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
                LoadMap();
                Debug.Log($"Interacted with {hit.collider.name}");
            }
        }
    }

    public async Task LoadMap()
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("Shop", LoadSceneMode.Single);
        while (!asyncOperation.isDone)
        {
            await Task.Yield();
        }
    }

    private void Update()
    {
        
    }

}
