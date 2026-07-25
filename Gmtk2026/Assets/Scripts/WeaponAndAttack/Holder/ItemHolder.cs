using System.Collections;
using UnityEngine;

public abstract class ItemHolder : MonoBehaviour
{
    public const string ANIM_EQUIP = "Equip";

    [SerializeField] protected Transform _aim;
    [SerializeField] protected Transform _container;
    [SerializeField] protected Collider _collider;
    [SerializeField] protected Item _item;

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

        // Drop Last Item
        _item?.Drop();

        // Get New
        _item = item;
        _item.Equip(_container, this);
    }

    protected virtual void Drop()
    {
        if (!_item) return;
        _item.Drop();
        _item = null;
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