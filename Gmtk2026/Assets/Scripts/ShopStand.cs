using System;
using UnityEngine;

public class ShopStand : MonoBehaviour, I_Interactable
{
    #region I_Interactable implementation
    public void Interact()
    {
        if (CanInteract())
        {
            // Implement the interaction logic here
            Debug.Log("Interacting with the Shop Stand.");
        }
    }
    public bool CanInteract()
    {
        print("CanInteract called");
        return true; // Implement your logic to determine if interaction is possible
    }

    #endregion

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
