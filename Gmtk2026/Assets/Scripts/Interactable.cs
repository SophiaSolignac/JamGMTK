using UnityEngine;

public interface I_Interactable
{
    public bool IsInteractable { get; set; }
    public void Interact(I_Interactor interactor = null);
    public bool CanInteract();
}
