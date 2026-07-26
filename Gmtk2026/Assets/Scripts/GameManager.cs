using UnBocal.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : PersistentSingleton<GameManager>
{
    [SerializeField] GameState _gameState = GameState.Shop;

    public static UnityEvent OnReset = new UnityEvent();
    override protected void Awake()
    {
        base.Awake();
        TimerHealth.OnPlayerDeath.AddListener(OnPlayerDeath);
        EventBus<GameState>.Connect(EventGame.AskChangeState, SwitchState);
        EventBus.Connect(EventGame.End, OnEnd);
    }

    private void Start()
    {
        OnReset.Invoke();

        EventBus<GameState>.Invoke(EventGame.OnStateChanged, _gameState);
    }

    private void OnEnd()
    {
        Destroy(gameObject);
    }

    private void OnPlayerDeath() => SwitchState(GameState.Shop);

    private void SwitchState(GameState state)
    {
        if (state == GameState.Shop)
        {
            LoadShopScene();
            return;
        }

        _gameState = state;
        EventBus<GameState>.Invoke(EventGame.OnStateChanged, _gameState);
    }

    private async void LoadShopScene()
    {
        _gameState = GameState.Shop;

        await SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
        OnReset.Invoke();

        EventBus<GameState>.Invoke(EventGame.OnStateChanged, _gameState);

    }
}

public enum GameState { Shop, Game }

public enum EventGame
{
    AskChangeState,
    OnStateChanged,
    NewCheckPoint,
    GoToLastCheckPoint,
    GoToCheckPoint,
    End,
    Upgrade
}