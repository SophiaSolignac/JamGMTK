using UnBocal.Events;
using UnityEngine;
using UnityEngine.Events;

public class TriggerOnEvent : MonoBehaviour
{
    public UnityEvent Trigger = new();

    [SerializeField] EventGame _event;

    private void Awake()
    {
        GlobalEventBus.Connect(_event, Trigger.Invoke);
    }

    private void OnDestroy()
    {
        GlobalEventBus.Disconnect(_event, Trigger.Invoke);
    }
}
