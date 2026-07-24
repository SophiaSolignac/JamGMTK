using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : PersistentSingleton<GameManager>
{
    public static UnityEvent OnReset = new UnityEvent();
    override protected void Awake()
    {
        base.Awake();
        TimerHealth.OnPlayerDeath.AddListener(LoadShopScene);
    }
    private void Start()
    {
        OnReset.Invoke(); // Reset all resettable objects when the game starts
    }

    private async void LoadShopScene()
    {
        await SceneManager.LoadSceneAsync("Game");
        OnReset.Invoke();
    }
}
