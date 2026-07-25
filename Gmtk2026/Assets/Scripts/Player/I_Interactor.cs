using UnityEngine;

public interface I_Interactor
{
    public GameObject From { get; }

    public void CheckForInteractables();
}