using UnityEngine;

public class SCREAMWHENHIT : MonoBehaviour, I_BulletOrRaycastTarget
{
    public void OnHit()
    {
        Debug.Log($"AAAAAAAAAAAAAAAAAAAAAAAA");
    }
}
