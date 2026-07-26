using System;
using UnityEngine;

public class ComponentLinker : MonoBehaviour
{
    [SerializeField]
    private NewPlayerMovement playerMovement;
    [SerializeField]
    private PlayerInteractor interactor;
    [SerializeField]
    private TimerHealth timerHealth;
    [SerializeField]
    private PlayerHUD playerHUD;
    [SerializeField]
    private RessourceSystem ressourceSystem;

    void Awake()
    {
        //static Events subscription
        ShopStand.OnTrySpendMoney += (ressourceSystem.TrySpendCoins);
        ShopStand.OnAddMaxTime.AddListener(timerHealth.AddMaxTime);
        Deathzone.OnPlayerEnterDeathZone.AddListener(timerHealth.Die);
        Coin.OnAddMoneyToPlayer.AddListener(ressourceSystem.AddCoins);

        //instance Events subscription
        timerHealth.OnTimeChanged.AddListener(playerHUD.UpdateHealthUI);
        ressourceSystem.OnCoinsChanged.AddListener(playerHUD.UpdateCoinUI);
    }

    private void OnDestroy()
    {
        //static Events unsubscription
        Deathzone.OnPlayerEnterDeathZone.RemoveListener(timerHealth.Die);
        ShopStand.OnTrySpendMoney -= (ressourceSystem.TrySpendCoins);
        ShopStand.OnAddMaxTime.RemoveListener(timerHealth.AddMaxTime);
        Coin.OnAddMoneyToPlayer.RemoveListener(ressourceSystem.AddCoins);

        //instance Events unsubscription
        timerHealth.OnTimeChanged.RemoveListener(playerHUD.UpdateHealthUI);
        ressourceSystem.OnCoinsChanged.RemoveListener(playerHUD.UpdateCoinUI);
    }

}
