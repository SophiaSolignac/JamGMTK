using System.Collections;
using UnityEngine;

public abstract class ItemHolder : MonoBehaviour
{
    public const string ANIM_EQUIP = "Equip";

    [SerializeField] protected Transform _aim;
    [SerializeField] protected Animator _animator;
    [SerializeField] protected Transform _container;
    [SerializeField] protected Collider _collider;
    [SerializeField] protected Item _item;

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

    protected virtual void Equip(Item item)
    {
        if (!item) return;

        item.Parent(_container);
        _animator?.SetTrigger(ANIM_EQUIP);
    }

    protected virtual void Drop()
    {
        if (!_item) return;
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
        while (_isHolding)
        {
            yield return new WaitForEndOfFrame();
            
            UseItem(false);
        }

        _holdLoop = null;
    }

    private bool UseItem(bool started)
    {
        if (_item == null) return false;
        
        _item.Use(started, this);

        return true;
    }
}