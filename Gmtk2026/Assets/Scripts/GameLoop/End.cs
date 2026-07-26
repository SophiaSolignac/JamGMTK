using UnBocal.Events;
using UnityEngine;

public class End : MonoBehaviour
{
    [SerializeField] string _endTag = "End";

    public void OnTrigger(Collider collider)
    {
        if (!collider.TryGetComponent(out PlayerItemHolder holder)) return;

        if (!holder.Item && !holder.Item.CompareTag(_endTag)
            || !holder.SecondaryItem && !holder.SecondaryItem.CompareTag(_endTag)) return;

        EventBus.Invoke(EventGame.End);
    }
}
