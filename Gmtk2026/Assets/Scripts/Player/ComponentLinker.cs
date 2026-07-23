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
    [SerializeField]
    private RessourceSystem ressourceSystem;

    void Awake()
    {
        timerHealth.OnTimeChanged.AddListener(playerHUD.UpdateHealthUI);
        Deathzone.OnPlayerEnterDeathZone.AddListener(timerHealth.Die);
        ressourceSystem.OnCoinsChanged.AddListener(playerHUD.UpdateCoinUI);
    }

}
