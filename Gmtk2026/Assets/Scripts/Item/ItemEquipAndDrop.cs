using UnityEngine;

public class ItemEquipAndDrop : MonoBehaviour, I_Interactable
{
    // Compoenents
    [SerializeField] Collider _collider;

    // Ground 
    [SerializeField] LayerMask _whatIsGround = Physics.AllLayers;
    [SerializeField] float _distanceFromGround = 1f;
    ItemHolder _owner;

    // Interation
    public bool IsInteractable { get; set; } = true;

    public bool CanInteract() => IsInteractable;

    public void Interact(I_Interactor interactor)
    {
        if (interactor == null || !interactor.From) return;
        if (!interactor.From.TryGetComponent(out ItemHolder holder)) return;

        gameObject.SendMessage(Item.ASK_EQUIP, holder, SendMessageOptions.DontRequireReceiver);
    }

    public void BridgeOnEquip(ItemHolder owner)
    {
        if (_collider) _collider.enabled = false;
        _owner = owner;
    }

    public void BridgeOnDrop()
    {
        if (_collider) _collider.enabled = true;
        _owner = null;

        // Get Ground
        Ray groundRay = new Ray(transform.position, Vector3.down);
        if (!Physics.Raycast(groundRay, out RaycastHit info, float.PositiveInfinity, _whatIsGround)) return;

        // Put Self On Ground
        transform.position = info.point + Vector3.up * _distanceFromGround;
    }
}