using UnityEngine;
using UnityEngine.Events;

public class ListenTriggerStay : MonoBehaviour
{
    public UnityEvent<Collider> onTriggerStayCollider;
    public UnityEvent onTriggerStay;

    private void OnTriggerStay(Collider other)
    {
        onTriggerStayCollider.Invoke(other);
        onTriggerStay.Invoke();
    }
}
