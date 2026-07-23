using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Events;

public class Deathzone : MonoBehaviour
{
    public static UnityEvent OnPlayerEnterDeathZone = new();
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            OnPlayerEnterDeathZone.Invoke();
        }
    }
}
