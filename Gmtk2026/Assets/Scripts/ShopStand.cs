using System;
using UnityEngine;
using UnityEngine.Events;

public class ShopStand : MonoBehaviour, I_Interactable
{
    private bool isInteractable = true;
    #region I_Interactable implementation
    bool I_Interactable.IsInteractable
    {
        get
        {
            return isInteractable;
        }
        set
        {
            isInteractable = value;
        }
    }

    public void Interact()
    {
        if (!CanInteract())
        {
            return;
        }
        // Implement the interaction logic here
        Debug.Log("Interacting with the Shop Stand.");
        OnAddMaxTime?.Invoke(10f); // Add 10 seconds to the timer
    }
    public bool CanInteract()
    {
        print("CanInteract called");
        return true; // Implement your logic to determine if interaction is possible
    }

    #endregion

    public UnityEvent<float> OnAddMaxTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
