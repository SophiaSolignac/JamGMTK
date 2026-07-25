using UnBocal.CookingProject.Utilities;
using UnityEngine;

public class Bullet : MonoBehaviour, IUBPooledObject
{
    [SerializeField] SOBullet _settings;
    [SerializeField] Collider _collider;

    public SOBullet Settings => _settings;

    Collider _shooterCollider;
    Item _shooter;

    Vector3 _startPosition;
    float _startTime;
    float _speedMultipler;

    public IUBPoolRef PoolSelf { get; set; }


    private void Update()
    {
        CheckForDestroy();

        if (!_settings) return;

        // Update Position
        transform.position += transform.forward * _settings.Speed * _speedMultipler * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Return If Bullet From Same Shooter
        if (_shooter != null && collision.transform.TryGetComponent(out Bullet ohterBullet) && ohterBullet._shooter == _shooter) return;

        // Try Hit I_BulletTarget
        if (collision.transform.TryGetComponent(out I_BulletOrRaycastTarget hitTarget))
            hitTarget.OnHit(_settings.Damages);

        StoreInPool();
    }

    public void Init(SOGun gunSettings, Collider shooterCollider, Item shooter)
    {
        // Init Properties
        _shooter = shooter;
        _startTime = Time.time;
        _startPosition = transform.position;
        _speedMultipler = Random.Range(gunSettings.SpeedMultiplyer.x, gunSettings.SpeedMultiplyer.y);

        // Ignore Collision With Shooter
        if (!_collider || !shooterCollider) return;
        _shooterCollider = shooterCollider;
        Physics.IgnoreCollision(_collider, _shooterCollider, true);
    }

    private void CheckForDestroy()
    {
        // Store In Pool Only If Too Far Or To Old
        if (!(_settings == null
            || Time.time - _startTime >= _settings.LifeTime
            || Vector3.Distance(_startPosition, transform.position) >= _settings.DespawnDistance)) return;

        StoreInPool();
    }

    private void StoreInPool()
    {
        if (_collider && _shooterCollider)
            Physics.IgnoreCollision(_collider, _shooterCollider, false);

        PoolSelf?.Store();
    }

    public void CheckForInteractables()
    {

    }
}