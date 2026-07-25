using UnBocal.Events;

public  class GameLevel : GameStateManager
{
    protected override GameState _state => GameState.Game;

    protected override void GameStateEnter()
    {
        EventBus.Invoke(EventGame.GoToLastCheckPoint);
    }

    protected override void GameStateExit()
    {

    }
}