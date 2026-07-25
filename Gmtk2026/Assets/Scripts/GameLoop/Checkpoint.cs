using UnBocal.Events;
using UnityEngine;
using UnityEngine.Events;

public class Checkpoint : MonoBehaviour
{
    UnityEvent HasBeenSet = new();

    [SerializeField] bool _canBeDefault = true;
    [SerializeField] Transform _point;
    public Transform Point => _point;

    private void Awake()
    {
        if (_point == null) _point = transform;

        // TryAutoSetDefault();
    }


    public void Register()
    {
        Debug.Log("hey");
        HasBeenSet.Invoke();
        EventBus<Checkpoint>.Invoke(EventGame.NewCheckPoint, this);
    }

    public void Trigger()
        => EventBus<Checkpoint>.Invoke(EventGame.GoToCheckPoint, this);

    private void TryAutoSetDefault()
    {
        return;
        if (!_canBeDefault) return;

        CheckPointTarget target = FindObjectOfType<CheckPointTarget>();
        if (!target || target.Checkpoint != null) return;

        target.Checkpoint = this;
    }

    [ContextMenu("Set As Defaut Checkpoint")]
    public void SetAsDefault()
    {
        return;
        if (!_canBeDefault) return;

        CheckPointTarget target = FindObjectOfType<CheckPointTarget>();
        if (!target) return;

        target.Checkpoint = this;
    }
}
