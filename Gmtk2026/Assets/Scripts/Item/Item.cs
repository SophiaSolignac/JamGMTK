using UnityEngine;

public class Item : MonoBehaviour, I_Interactable
{
    public enum InputType { Tap, Hold }

    [SerializeField] protected Transform _aim;

    public virtual InputType Input { get; set; }
    public bool IsInteractable { get; set; } = true;

    public void Interact()
    {
        
    }

    public bool CanInteract() => IsInteractable;

    public void Parent(Transform parent)
    {
        transform.parent = parent;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    public virtual void Use(bool started, ItemHolder owner)
    {
        if (!IsInteractable) return;

        if (Input == InputType.Hold)
        {
            ApplyUse(owner);
            return;
        }

        if (started) ApplyUse(owner);
    }

    protected virtual void ApplyUse(ItemHolder owner)
    {
        
    }
}