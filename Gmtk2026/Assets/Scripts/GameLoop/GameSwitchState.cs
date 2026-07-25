using UnBocal.Events;
using UnityEngine;

public class GameSwitchState : MonoBehaviour
{
    [SerializeField] GameState _state;

    public void Trigger() => Trigger(_state);

    public void Trigger(GameState state)
        => EventBus<GameState>.Invoke(EventGame.AskChangeState, state);
}
