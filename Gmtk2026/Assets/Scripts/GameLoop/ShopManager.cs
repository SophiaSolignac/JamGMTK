using UnityEngine;
public class ShopManager : GameStateManager
{
    protected override GameState _state => GameState.Shop;

    [SerializeField] Checkpoint _checkpoint;

    protected override void GameStateEnter()
    {
        _checkpoint.Trigger();
    }

    protected override void GameStateExit()
    {

    }
}