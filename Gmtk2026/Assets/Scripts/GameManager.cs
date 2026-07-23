using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : PersistentSingleton<GameManager>
{
    override protected void Awake()
    {
        base.Awake();
        TimerHealth.OnPlayerDeath.AddListener(LoadShopScene);
    }

    private void LoadShopScene()
    {
        SceneManager.LoadScene("Shop");
    }
}
