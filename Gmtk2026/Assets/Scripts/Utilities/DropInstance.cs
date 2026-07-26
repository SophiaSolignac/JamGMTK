using UnBocal.Utilities;
using UnityEngine;

public class DropInstance : MonoBehaviour
{
    [SerializeField] Coin _objectToDrop;
    [SerializeField] int _count = 1;

    public void Drop() => Drop(_count);

    public void Drop(int Count)
    {
        UBPool<Coin> coin;
        for (int i = 0; i < Count; i++)
        {
            coin = UBPool<Coin>.GetInstancePrefab(_objectToDrop);
            coin.transform.position = transform.position;

            if (!coin.instance.TryGetComponent(out Rigidbody body)) continue;
            
            Vector3 direction = UnityEngine.Random.insideUnitSphere + Vector3.up;
            coin.transform.position += direction.normalized;
            body.AddForce(direction * 3,ForceMode.Impulse);
        }
    }
}
