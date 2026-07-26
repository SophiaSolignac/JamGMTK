using System;
using UnBocal.Utilities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Coin : Collectible, IUBPooledObject
{
    [SerializeField]
    Collider physicCollider;
    [SerializeField]
    SphereCollider triggerCollider;
    [SerializeField]
    Rigidbody physicsBody;

    public static UnityEvent<int> OnAddMoneyToPlayer = new();

    GameObject target;
    public int coinValue = 20;
    public IUBPoolRef PoolSelf 
    { 
        get ; 
        set ;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            target = other.gameObject;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            target = null;
        }
    }
    private void Update()
    {
        RotateCoin();
        Attract();
    }

    private bool Attract()
    {
        if (target == null)
        {
            return false;
        }
        Vector3 direction = (target.transform.position - transform.position).normalized;
        float speed = 5000f; // Adjust the speed as needed
        float vec = Vector3.Distance(target.transform.position,transform.position);
        float ratio = 1- Mathf.Clamp01(vec / triggerCollider.radius);
        physicsBody.AddForce(speed * ratio * Time.deltaTime * direction, ForceMode.Force);
        return true;
    }

    private void RotateCoin()
    {
        float rotationSpeed = 500f; // Adjust the rotation speed as needed
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
    protected override void OnCollisionWithPlayer()
    {
        OnAddMoneyToPlayer.Invoke(coinValue);
        PoolSelf.Store();
    }
}
