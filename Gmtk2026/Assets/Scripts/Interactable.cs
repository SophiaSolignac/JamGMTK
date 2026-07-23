using UnityEngine;

public interface I_Interactable
{
    public bool IsInteractable { get; set; }
    public void Interact();
    public bool CanInteract();
}
