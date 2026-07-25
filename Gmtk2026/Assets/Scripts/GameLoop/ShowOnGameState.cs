using UnBocal.Events;
using UnityEngine;

public class ShowOnGameState : MonoBehaviour
{
    [SerializeField] GameState _state;

    private void Awake()
        => EventBus<GameState>.Connect(EventGame.OnStateChanged, OnStateChanged);

    private void OnDestroy()
        => EventBus<GameState>.Connect(EventGame.OnStateChanged, OnStateChanged);

    private void OnStateChanged(GameState state)
    {
        bool isMe = state == _state;
        gameObject.SetActive(isMe);
    }
}
