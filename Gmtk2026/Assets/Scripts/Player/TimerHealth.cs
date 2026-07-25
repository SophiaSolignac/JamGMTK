using UnBocal.Events;
using UnityEngine;
using UnityEngine.Events;

public class TimerHealth : PersistentSingleton<TimerHealth>, I_Resettable, I_BulletOrRaycastTarget
{
    private const float MILLISECOND = 1000f;

    [SerializeField] float _hitCooldown = .1f;
    float _lastTimeHit;

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
    // Events (instance)
    public UnityEvent<float> OnTimeChanged = new UnityEvent<float>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Awake()
    { 
        base.Awake();

        GameManager.OnReset.AddListener(ResetObj);
        EventBus<GameState>.Connect(EventGame.OnStateChanged, OnStateChanged);
    }

    private void OnDestroy()
    {
        GameManager.OnReset.RemoveListener(ResetObj);
        EventBus<GameState>.Disconnect(EventGame.OnStateChanged, OnStateChanged);
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
        CurrentTime -= Time.deltaTime * MILLISECOND;
        if (CurrentTime <= 0)
        {
            Die();
        }
    }

    public void AddMaxTime(float timeToAdd)
    {
        maxTime += timeToAdd ; // Convert seconds to milliseconds

        if (!isTimerActive)
            CurrentTime = maxTime * MILLISECOND;
    }

    public void Die()
    {
        CurrentTime = 0;
        isTimerActive = false;
        OnPlayerDeath.Invoke();
    }

    private void OnStateChanged(GameState state)
    {
        if (state == GameState.Game)
        {
            isTimerActive = true;
            CurrentTime = maxTime * MILLISECOND;
            return;
        }

        isTimerActive = false;
    }

    public void ResetObj()
    {
        transform.position = Vector3.zero; //temporary tp
        CurrentTime = maxTime * MILLISECOND;
        isTimerActive = true;
    }

    public void OnHit(Damage damage)
    {
        float time = Time.time;
        if (time - _lastTimeHit < _hitCooldown) return;

        CurrentTime -= damage.Time * MILLISECOND;
        _lastTimeHit = Time.time;
    }
}