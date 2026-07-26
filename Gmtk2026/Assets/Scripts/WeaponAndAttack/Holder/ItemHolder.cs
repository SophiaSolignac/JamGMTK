using System.Collections;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;

public abstract class ItemHolder : MonoBehaviour
{
    public const string ANIM_EQUIP = "Equip";

    [SerializeField] protected Transform _aim;
    [SerializeField] protected Transform _container;
    [SerializeField] protected Transform _secondaryContainer;
    [SerializeField] protected Collider _collider;
    [SerializeField] protected Item _item;
    public Item _secondaryItem;

    public Item SecondaryItem => _secondaryItem;
    public Item Item => _item;
    public Transform Aim => _aim;
    public Collider Collider => _collider;

    Coroutine _holdLoop;
    bool _isHolding;

    protected virtual void Awake()
    {
        if (!_container) _container = transform;
        if (!_aim) _aim = _container;

        Equip(_item);
    }

    public virtual void Equip(Item item)
    {
        if (!item) return;

        if (_item)
        {
            // Drop Last Item
            if (!_secondaryContainer || _secondaryItem) _item.Drop();

            // Put Item In Secondary Slot
            else
            {
                _secondaryItem = _item;
                _item.Equip(_secondaryContainer, this);
            }
        }

        // Get New
        _item = item;
        _item.Equip(_container, this);
    }

    protected virtual void Drop()
    {
        if (!_item) return;
        _item.Drop();
        
        if (!_secondaryItem)
        {
            _item = null;
            return;
        }

        Equip(_secondaryItem);
        _secondaryItem = null;
    }

    protected void Switch()
    {
        if (!_secondaryItem || !_item) return;

        Item item = _item;
        _item = _secondaryItem;
        _secondaryItem = item;

        _item.Equip(_container, this);
        _secondaryItem.Equip(_secondaryContainer, this);
    }

    protected virtual void TryUseItem(bool started)
    {
        if (!UseItem(started)) return;

        // Check If Item Input Can Be Held
        if (_item.Input == Item.InputType.Hold)
        {
            // Update Holding
            _isHolding = started;
            if (_holdLoop != null) return;

            // Loop Holding
            _holdLoop = StartCoroutine(HoldInput());
        }
    }

    private IEnumerator HoldInput()
    {
        // loop Holding
        while (_isHolding && _item)
        {
            yield return new WaitForEndOfFrame();
            
            UseItem(false);
        }

        _holdLoop = null;
    }

    private bool UseItem(bool started)
    {
        if (_item == null) return false;
        
        _item.Use(started);

        return true;
    }
}