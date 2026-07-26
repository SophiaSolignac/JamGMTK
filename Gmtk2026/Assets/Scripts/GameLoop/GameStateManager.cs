using UnBocal.Events;
using UnityEngine;

public abstract class GameStateManager : MonoBehaviour
{
    protected abstract GameState _state { get; }

    protected virtual void Awake()
    {
        EventBus<GameState>.Connect(EventGame.OnStateChanged, OnStateChanged);
    }

    protected virtual void OnDestroy()
    {
        EventBus<GameState>.Connect(EventGame.OnStateChanged, OnStateChanged);
    }

    private void OnStateChanged(GameState state)
    {
        bool isMe = state == _state;
        gameObject.SetActive(isMe);

        if (isMe) GameStateEnter();
        else GameStateExit();
    }

    protected abstract void GameStateEnter();

    protected abstract void GameStateExit();
}
