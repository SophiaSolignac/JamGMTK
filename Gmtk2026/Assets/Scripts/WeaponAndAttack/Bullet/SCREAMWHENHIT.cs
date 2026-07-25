using UnityEngine;

public class SCREAMWHENHIT : MonoBehaviour, I_BulletOrRaycastTarget
{
    public void OnHit(Damage damage)
    {
        Debug.Log($"AAAAAAAAAAAAAAAAAAAAAAAA");
    }
}
