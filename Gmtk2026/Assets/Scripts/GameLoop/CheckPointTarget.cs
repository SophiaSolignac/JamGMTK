using UnBocal.Events;
using UnityEngine;

public class CheckPointTarget : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] public Checkpoint Checkpoint;

    private void Awake()
    {
        EventBus<Checkpoint>.Connect(EventGame.NewCheckPoint, OnNewCheckPoint);
        EventBus.Connect(EventGame.GoToLastCheckPoint, GoToCheckPoint);
        EventBus<Checkpoint>.Connect(EventGame.GoToCheckPoint, GoToCheckPoint);
    }

    private void OnDestroy()
    {
        EventBus<Checkpoint>.Disconnect(EventGame.NewCheckPoint, OnNewCheckPoint);
        EventBus.Disconnect(EventGame.GoToLastCheckPoint, GoToCheckPoint);
        EventBus<Checkpoint>.Disconnect(EventGame.GoToCheckPoint, GoToCheckPoint);
    }

    private void OnNewCheckPoint(Checkpoint checkPoint)
    {
        if (!checkPoint) return;
        Checkpoint = checkPoint;
    }

    private void GoToCheckPoint() => GoToCheckPoint(Checkpoint);

    private void GoToCheckPoint(Checkpoint checkpoint)
    {
        if (!checkpoint) return;

        target.position = checkpoint.Point.position;
        target.rotation = checkpoint.Point.rotation;
    }
}