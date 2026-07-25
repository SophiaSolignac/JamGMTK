using UnityEngine;
using UnityEngine.Events;

public class ListenTriggerEnter : MonoBehaviour
{
    public UnityEvent<Collider> onTriggerEnterCollider;
    public UnityEvent onTriggerEnter;

    private void OnTriggerEnter(Collider other)
    {
        onTriggerEnterCollider.Invoke(other);
        onTriggerEnter.Invoke();
    }
}
