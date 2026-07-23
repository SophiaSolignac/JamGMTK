using System;
using UnityEngine;

public class ComponentLinker : MonoBehaviour
{
    [SerializeField]
    private PlayerController playerController;
    [SerializeField]
    private PlayerInteractor interactor;
    [SerializeField]
    private TimerHealth timerHealth;
    [SerializeField]
    private PlayerHUD playerHUD;

    void Start()
    {
        timerHealth.OnTimeChanged.AddListener(playerHUD.UpdateHealthUI);
        Deathzone.OnPlayerEnterDeathZone.AddListener(timerHealth.Die);
        TimerHealth.OnPlayerDeath.AddListener(ResetAll);
    }

    private void ResetAll()
    {
        timerHealth.ResetObject();
    }
}
