using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

public class ShopStand : MonoBehaviour, I_Interactable, I_BulletOrRaycastTarget
{
    private bool isInteractable = true;
    public int cost = 100;

    public delegate bool OnTrySpendMoneyHandler(int amount);
    public static event OnTrySpendMoneyHandler OnTrySpendMoney;

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

    public void Interact(I_Interactor interactor = null)
    {
        if (!CanInteract())
        {
            Debug.Log("Cannot interact with the shop stand right now.");
            return;
        }
        // If the player has enough money, spend it and add time to the timer
        if (OnTrySpendMoney != null && OnTrySpendMoney.Invoke(cost))
        {
            OnAddMaxTime?.Invoke(10f); // Add 10 seconds to the timer
        }
    }
    public bool CanInteract()
    {
        return true; // Implement your logic to determine if interaction is possible
    }

    public void OnHit(int damage)
    {
        Interact();
    }
    #endregion

    public static UnityEvent<float> OnAddMaxTime = new UnityEvent<float>();
 
}
