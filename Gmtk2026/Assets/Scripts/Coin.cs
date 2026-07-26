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
    private int coinValue = 20;
    public IUBPoolRef PoolSelf 
    { 
        get ; 
        set ;
    }
    public float AttractionSpeed = 2;

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
        transform.position = Vector3.MoveTowards(transform.position, target.transform.position, AttractionSpeed * Time.deltaTime);
        return true;
    }

    private void RotateCoin()
    {
        float rotationSpeed = 5f; // Adjust the rotation speed as needed
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
    protected override void OnCollisionWithPlayer()
    {
        OnAddMoneyToPlayer.Invoke(coinValue);
        PoolSelf.Store();
    }
    /// <summary>
    /// Add a force to the coin's rigidbody
    /// Default mode is Impulse
    /// </summary>
    public void AddForce(Vector3 pForce, ForceMode pMode = ForceMode.Impulse)
    {
        physicsBody.AddForce(pForce, pMode);
    }
}
