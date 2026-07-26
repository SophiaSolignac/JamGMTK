using UnBocal.Events;
using UnityEngine;
using UnityEngine.Events;

public class ShopStand : MonoBehaviour, I_Interactable, I_BulletOrRaycastTarget
{
    // -------~~~~~~~~~~================# // Events
    public UnityEvent NoMoreUpgrade = new();

    // -------~~~~~~~~~~================# // Money
    public delegate bool OnTrySpendMoneyHandler(int amount);
    public static event OnTrySpendMoneyHandler OnTrySpendMoney;


    // -------~~~~~~~~~~================# // Upgrade
    [SerializeField] SOUpgrade _uprade;
    bool _lock;
    int _count = 0;

    #region I_Interactable implementation
    bool I_Interactable.IsInteractable { get; set; } = true;

    public void Interact(I_Interactor interactor = null)
    {
        if (!CanInteract()) return;

        SOUpgrade.Upgrade u = _uprade[_count];

        // If No More Upgrade Then Lock
        if (u == null)
        {
            Lock();
            return;
        }

        // If the player has enough money, spend it and add time to the timer
        if (!(OnTrySpendMoney != null && OnTrySpendMoney.Invoke(Mathf.RoundToInt(u.Price.Value)))) return;
        
        // Buy Upgrade
        EventBus<SOUpgrade.Upgrade>.Invoke(EventGame.Upgrade, u);
        _count++;

        if (_uprade.CountMax < _count) Lock();
    }

    private void Lock()
    {
        _lock = true;
        NoMoreUpgrade.Invoke();
    }

    public bool CanInteract()
    {
        if (_lock) return false;

        if (_uprade != null) return true;

        Debug.Log("Cannot interact with the shop stand right now.");

        return false;
    }

    public void OnHit(Damage damage)
    {
        Interact();
    }
    #endregion

    public static UnityEvent<float> OnAddMaxTime = new UnityEvent<float>();
 
}
