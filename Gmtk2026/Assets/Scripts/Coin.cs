using System;
using UnityEngine;
using UnityEngine.LowLevelPhysics2D;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(PhysicsBody))]
public class Coin : MonoBehaviour
{
    [SerializeField]
    Collider physicCollider;
    [SerializeField]
    Collider triggerCollider;
    [SerializeField]
    Rigidbody physicsBody;


    GameObject target;
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
        float speed = 500f; // Adjust the speed as needed
        physicsBody.AddForce(speed * Time.deltaTime * direction, ForceMode.Force);
        return true;
    }

    private void RotateCoin()
    {
        float rotationSpeed = 500f; // Adjust the rotation speed as needed
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
