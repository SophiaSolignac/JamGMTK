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

    protected override void ApplyUse(ItemHolder owner)
    {
        // Check Null
        if (!_settings) return;

        // Check If Cooldown If Finished
        float time = Time.time;
        if (time - _lastTimeUse <= _settings.WaitBetweenInput) return;
        
        // Fire
        Fire(owner);
        _animator?.SetTrigger(ANIM_USE);
        _lastTimeUse = Time.time;

        base.ApplyUse(owner);
    }

    protected abstract void Fire(ItemHolder owner);
}