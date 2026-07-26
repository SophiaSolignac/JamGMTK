using UnityEngine;
using UnityEngine.Events;

public class KeyListener : MonoBehaviour
{
    public UnityEvent Down = new();
    public UnityEvent Hold = new();
    public UnityEvent Up = new();

    [SerializeField] KeyCode key = KeyCode.None;

    private void Update()
    {
        if (Input.GetKeyDown(key)) Down.Invoke();
        if (Input.GetKeyUp(key)) Up.Invoke();
        if (Input.GetKey(key)) Hold.Invoke();
    }
}
