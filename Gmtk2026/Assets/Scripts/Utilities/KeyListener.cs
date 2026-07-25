using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class KeyListener : MonoBehaviour
{
    public UnityEvent Down = new();
    public UnityEvent Hold = new();
    public UnityEvent Up = new();

    [SerializeField] KeyCode key = KeyCode.None;

    private void Update()
    {
        if (Input.GetKeyDown(key)) Down.Invoke();
        else if (Input.GetKeyUp(key)) Up.Invoke();
        else if (Input.GetKey(key)) Hold.Invoke();
    }

}
