using UnityEngine;

public class SCREAMWHENHIT : MonoBehaviour, I_BulletOrRaycastTarget
{
    public void OnHit(int damage)
    {
        Debug.Log($"AAAAAAAAAAAAAAAAAAAAAAAA");
    }
}
