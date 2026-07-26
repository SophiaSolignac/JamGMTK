using System.Collections.Generic;
using UnBocal.Utilities;
using UnityEngine;

public class Gun : Weapon<SOGun>
{
    List<Transform> _myBullets = new();

    protected override void Fire()
    {
        switch (_settings.Type)
        {
            case SOGun.HitType.Bullet:
                FireProjectile();
                break;

            case SOGun.HitType.Raycast:
                FireScan();
                break;
        }
    }

    private void FireScan()
    {
        Transform aim = _owner.Aim ? _owner.Aim : _aim;
        if (!aim) return;

        Ray ray = new(aim.position, aim.forward);
        I_BulletOrRaycastTarget[] targets;

        // Can Touch Multiple Target
        if (_settings.Perssing)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, _settings.LayerMask);

            foreach (RaycastHit currentHit in hits)
            {
                // Exclude Owner
                if (currentHit.collider == _owner.Collider) continue;

                targets = currentHit.transform.GetComponents<I_BulletOrRaycastTarget>();
                if (targets.Length <= 0) continue;

                foreach (I_BulletOrRaycastTarget target in targets)
                {
                    target.OnHit(_settings.Damages);
                }
            }
            return;
        }

        // Can Only Have One Target
        if (!Physics.Raycast(ray, out RaycastHit hit, _settings.Distance, _settings.LayerMask)) return;

        targets = hit.transform.GetComponents<I_BulletOrRaycastTarget>();
        if (targets.Length <= 0) return;

        foreach (I_BulletOrRaycastTarget target in targets)
        {
            target.OnHit(_settings.Damages);
        }

    }

    private void FireProjectile()
    {
        if (!_settings.Bullet) return;

        int bulletCount = 0;

        Transform aim = GetAim();
        if (!aim) return;

        Quaternion baseRotation = GetBaseRotation(aim);

        do
        {
            // Init Bullet
            UBPool<Bullet> bullet = UBPool<Bullet>.GetInstancePrefab(_settings.Bullet);
            _myBullets.Add(bullet.transform);
            bullet.stored += OnBulletStored;

            // Init Direction And Position
            bullet.transform.position = aim.position;

            bullet.transform.rotation =
                Quaternion.AngleAxis(_settings.AngleOpening * Random.Range(-1f, 1f), aim.right)
                * Quaternion.AngleAxis(_settings.AngleOpening * Random.Range(-1f, 1f), aim.up)
                * baseRotation;

            // Init Bullet
            bullet.instance.Init(_settings, _owner.Collider, this);

            bulletCount++;

        } while (_settings.BulletPerShot > bulletCount);
    }

    private Transform GetAim()
    => _settings.Aim switch
    {
        SOGun.AimType.Auto => _owner && _owner.Aim ? _owner.Aim : _aim,
        SOGun.AimType.Self => _aim,
        SOGun.AimType.Owner => _owner.Aim,
        _ => null
    };

    private Quaternion GetBaseRotation(Transform aim)
    {
        if (!_settings.CorrectAimWithHolder || !_aim || !(_owner && _owner.Aim)) return aim.rotation;

        Ray ownerAimRay = new Ray(_owner.Aim.position, _owner.Aim.forward);
        Vector3 fromSelfAimToPoint = default;

        float distance = _settings.Bullet.Settings ? _settings.Bullet.Settings.DespawnDistance : SOGun.DEFAULT_CORRECTION_AIM_DISTANCE;

        RaycastHit[] hits = Physics.RaycastAll(ownerAimRay);
        if (hits.Length > 0)
        {
            foreach (RaycastHit hit in hits)
            {
                if (_myBullets.Contains(hit.transform)) continue;

                fromSelfAimToPoint = hit.point - _aim.position;
                break;
            }
        }

        if (fromSelfAimToPoint == default)
            fromSelfAimToPoint = (_owner.Aim.position + _owner.Aim.forward * distance) - _aim.position;

        return _aim.rotation * Quaternion.FromToRotation(_aim.forward, fromSelfAimToPoint.normalized);
    }

    private void OnBulletStored(UBPool<Bullet> storedBullet)
    {
        _myBullets.Remove(storedBullet.transform);

        storedBullet.stored -= OnBulletStored;
    }
}