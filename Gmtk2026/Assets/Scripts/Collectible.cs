using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public abstract class Collectible : MonoBehaviour
{
    protected void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            OnCollisionWithPlayer();
        }
    }

    protected abstract void OnCollisionWithPlayer();
}