using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class TimerHealth : PersistentSingleton<TimerHealth>, I_Resettable, I_BulletOrRaycastTarget
{
    public float maxTime = 100f; //in seconds
    private float currentTime;   //in milliseconds
    public bool isTimerActive = true;

    public float CurrentTime { 
        get => currentTime; 
        set 
        {
            currentTime = value;
            OnTimeChanged.Invoke(currentTime);
        }
    }
    // Events (static)
    public static UnityEvent OnPlayerDeath = new UnityEvent();
    public static UnityEvent OnPlayerRagdoll = new UnityEvent();
    // Events (instance)
    public UnityEvent<float> OnTimeChanged = new UnityEvent<float>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Awake()
    { 
        base.Awake();
        GameManager.OnReset.AddListener(ResetObj);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isTimerActive) return;
        // Count down the timer
        CountDown();
    }
    private void CountDown()
    {
        CurrentTime -= Time.deltaTime * 1000;
        if (CurrentTime <= 0)
        {
            Ragdoll();
        }
    }
    public void AddMaxTime(float timeToAdd)
    {
        maxTime += timeToAdd ; // Convert seconds to milliseconds
    }
    public void Die()
    {
        CurrentTime = 0;
        isTimerActive = false;
        OnPlayerDeath.Invoke();
    }
    public void ResetObj()
    {
        transform.position = Vector3.zero + Vector3.up * 5; //temporary tp
        CurrentTime = maxTime * 1000;
        isTimerActive = true;
    }

    public void OnHit()
    {
        if (!isTimerActive) return;
        Ragdoll();
    }

    private void Ragdoll()
    {
        if (!isTimerActive)
            return;
        CurrentTime = 0;
        isTimerActive = false;
        OnPlayerRagdoll.Invoke();
        Invoke(nameof(Die), 5f);
    }
}
