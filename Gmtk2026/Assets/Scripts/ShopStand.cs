using TMPro;
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

    // -------~~~~~~~~~~================# // Interaction
    bool I_Interactable.IsInteractable { get; set; } = true;

    // -------~~~~~~~~~~================# // Component
    [SerializeField] TextMeshProUGUI _nameRenderer;
    [SerializeField] TextMeshProUGUI _priceRenderer;

    // -------~~~~~~~~~~================# // Upgrade
    [SerializeField] SOUpgrade _uprade;
    SOUpgrade.Upgrade _current;
    bool _lock;
    int _count = 0;

    private void Start()
    {
        _current = _uprade[_count];

        if (_uprade && _nameRenderer) _nameRenderer.text = _uprade.Title;

        UpdateDisplay();
    }


    public void Interact(I_Interactor interactor = null)
    {
        if (!CanInteract()) return;

        _current = _uprade[_count];

        // If No More Upgrade Then Lock
        if (_current == null)
        {
            Lock();
            return;
        }

        // If the player has enough money, spend it and add time to the timer
        if (!(OnTrySpendMoney != null && OnTrySpendMoney.Invoke(Mathf.RoundToInt(_current.Price.Value)))) return;
        
        // Buy Upgrade
        EventBus<SOUpgrade.Upgrade>.Invoke(EventGame.Upgrade, _current);
        
        _current = _uprade[++_count];
        UpdateDisplay();

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

    public void UpdateDisplay()
    {
        if (_current == null || !_priceRenderer) return;

        _priceRenderer.text = $"${Mathf.Round(_current.Price.Value)}";
    }

    public static UnityEvent<float> OnAddMaxTime = new UnityEvent<float>();
 
}
