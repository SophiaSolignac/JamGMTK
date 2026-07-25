using UnityEngine;

public class ItemAnimator : MonoBehaviour
{
    [SerializeField] Animator _animator;

    bool _isDropped = true;

    public void BridgeOnUse()
    {
        if (!_animator) return;

        _animator.SetTrigger("Use");
    }

    public void BridgeOnEquip(ItemHolder owner)
    {
        if (!_animator) return;

        _isDropped = false;
        _animator.SetBool("IsDropped", _isDropped);
    }

    public void BridgeOnDrop()
    {
        if (!_animator) return;

        _isDropped = true;
        _animator.SetTrigger("Drop");
        _animator.SetBool("IsDropped", _isDropped);
    }
}