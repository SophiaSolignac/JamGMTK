using UnityEngine;

public abstract class Weapon<WeaponType> : Item where WeaponType : SOWeapon
{
    protected const string ANIM_USE = "Use";
    protected const string ANIM_RELOAD = "Reload";

    public override InputType Input => _settings ? _settings.Input : InputType.Tap;
    [SerializeField] Animator _animator;
    [SerializeField] protected WeaponType _settings;

    protected float _lastTimeUse = float.NegativeInfinity;

    private void Awake()
    {
        if (_settings) return;

        _settings = Instantiate(_settings);
    }

    public override void Use(bool started, ItemHolder aim)
    {
        base.Use(started, aim);
        // if (!started) _lastTimeUse = float.NegativeInfinity;
    }

    protected override void ApplyUse(ItemHolder aim)
    {
        // Check Null
        if (!_settings) return;

        // Check If Cooldown If Finished
        float time = Time.time;
        if (time - _lastTimeUse <= _settings.WaitBetweenInput) return;
        
        // Fire
        Fire(aim);
        _animator?.SetTrigger(ANIM_USE);
        _lastTimeUse = Time.time;
    }

    protected abstract void Fire(ItemHolder aim);
}