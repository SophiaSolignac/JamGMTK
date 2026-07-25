using UnityEngine;
using UnityEngine.Events;

public class ListenTriggerExit : MonoBehaviour
{
    public UnityEvent<Collider> onTriggerExitCollider;
    public UnityEvent ontriggerExit;

    private void OnTriggerExit(Collider other)
    {
        onTriggerExitCollider.Invoke(other);
        ontriggerExit.Invoke();
    }
}
