using UnityEngine;
using UnityEngine.Events;

public partial class Item : MonoBehaviour
{
    public UnityEvent onEquip = new();
    public UnityEvent onDrop = new();
    public UnityEvent onUsed = new();

    public enum InputType { Tap, Hold }

    [SerializeField] protected Transform _aim;

    protected ItemHolder _owner;
    public virtual InputType Input { get; set; }

    private void Start()
    {
        if (_owner) return;
        Drop();
    }

    public void BridgeAskEquip(ItemHolder holder)
        => holder?.Equip(this);

    public void Equip(Transform parent, ItemHolder owner)
    {
        _owner = owner;
        transform.parent = parent;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        gameObject.SendMessage(Item.ON_EQUIP, owner, SendMessageOptions.DontRequireReceiver);
        onEquip.Invoke();
    }

    public void Drop(Transform container = null)
    {
        _owner = null;
        transform.parent = container;
        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.FromToRotation(transform.forward, Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized) * transform.rotation;

        gameObject.SendMessage(Item.ON_DROP, SendMessageOptions.DontRequireReceiver);
        onDrop.Invoke();
    }

    public virtual void Use(bool started)
    {
        if (Input == InputType.Hold)
        {
            ApplyUse();
            return;
        }

        if (started) ApplyUse();
    }

    protected virtual void ApplyUse()
    {
        gameObject.SendMessage(Item.ON_USE, SendMessageOptions.DontRequireReceiver);
        onUsed.Invoke();
    }
}