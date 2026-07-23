using UnityEngine;

public class ItemRenderer : MonoBehaviour
{
    protected const string ANIM_NAME_USE = "Use";

    [SerializeField] Animator _animator;

    public virtual void Use()
    {
        if (!_animator) return;

        _animator.SetTrigger(ANIM_NAME_USE);
    }    
}