using System;
using UnityEngine;

public interface I_BulletOrRaycastTarget
{
    public void OnHit(Damage damage);
}

[Serializable]
public struct Damage
{
    public int Point;
    public float Time;

    public Damage(int point, float time)
    {
        Point = point;
        Time = time;
    }

    public static explicit operator Damage(Vector2 vector)
        => new Damage(Mathf.RoundToInt(vector.x), vector.y) ;
}