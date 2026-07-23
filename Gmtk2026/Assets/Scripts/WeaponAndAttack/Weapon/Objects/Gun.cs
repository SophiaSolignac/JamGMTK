using UnBocal.CookingProject.Utilities;
using UnityEngine;

public class Gun : Weapon<SOGun>
{
    protected override void Fire(ItemHolder owner)
    {
        switch (_settings.Type)
        {
            case SOGun.HitType.Bullet:
                FireProjectile(owner);
                break;

            case SOGun.HitType.Raycast:
                FireScan(owner);
                break;
        }
    }

    private void FireScan(ItemHolder owner)
    {
        Transform aim = owner.Aim ? owner.Aim : _aim;
        if (!aim) return;

        Ray ray = new(aim.position, aim.forward);
        I_BulletOrRaycastTarget target;

        // Can Touch Multiple Target
        if (_settings.Perssing)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, _settings.LayerMask);

            foreach (RaycastHit currentHit in hits)
            {
                // Exclude Owner
                if (currentHit.collider == owner.Collider) continue;

                if (currentHit.transform.TryGetComponent(out target)) continue;

                target.OnHit();
            }
            return;
        }


        // Can Only Have One Target

        if (!Physics.Raycast(ray, out RaycastHit hit, _settings.Distance, _settings.LayerMask)) return;

        if (!hit.transform.TryGetComponent(out target)) return;

        target.OnHit();
    }

    private void FireProjectile(ItemHolder owner)
    {
        if (!_settings.Bullet) return;

        int bulletCount = 0;

        Transform aim = owner && owner.Aim ? owner.Aim : _aim;

        do
        {
            // Init Bullet
            UBPool<Bullet> bullet = UBPool<Bullet>.GetInstancePrefab(_settings.Bullet, transform);

            // Init Direction And Position
            bullet.transform.position = aim.position;
            bullet.transform.rotation =
                Quaternion.AngleAxis(_settings.AngleOpening * Random.Range(-1f, 1f), aim.right)
                * Quaternion.AngleAxis(_settings.AngleOpening * Random.Range(-1f, 1f), aim.up)
                * aim.rotation;

            // Init Bullet
            bullet.instance.Init(_settings, owner.Collider, this);

            bulletCount++;

        } while (_settings.BulletPerShot > bulletCount);

    }
}