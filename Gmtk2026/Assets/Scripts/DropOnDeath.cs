using System;
using UnBocal.Utilities;
using UnityEngine;

public class DropOnDeath : MonoBehaviour, I_BulletOrRaycastTarget
{
    public Coin objectToDrop;
    private int nbCoins = 5;

    public void OnHit(Damage damage)
    {
        Debug.Log("ewq");
        UBPool<Coin> coin;
        for (int i = 0; i < nbCoins; i++)
        {
            coin = UBPool<Coin>.GetInstancePrefab(objectToDrop);
            Drop(coin.instance);
        }
    }

    private void Drop(Coin coin)
    {
        coin.transform.position = transform.position;
        Vector3 direction = UnityEngine.Random.insideUnitSphere + Vector3.up;
        coin.transform.position += direction.normalized;
        coin.AddForce(direction * 3,ForceMode.Impulse);
    }
}
