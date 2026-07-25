using GMTK.Inputs;
using UnityEngine;

public class CameraTilt : MonoBehaviour
{
    [SerializeField] float _tilt = 10f;
    [SerializeField] float _speed = 10f;

    float _target;
    float _current;

    private void Awake()
    {
        InputManager.onMove.AddListener(OnMove);
    }

    private void OnDestroy()
    {
        InputManager.onMove.RemoveListener(OnMove);
    }

    private void Update()
    {
        _current = Mathf.Lerp(_current, _target, _speed * Time.deltaTime);
        transform.localRotation = Quaternion.AngleAxis(-_current * _tilt, Vector3.forward);
    }

    private void OnMove(Vector3 direction)
        => _target = direction.x;
}
