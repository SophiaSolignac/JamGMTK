using UnityEngine;

public class TiltOnVelocityChange : MonoBehaviour
{
    Vector3 _lastPosition;
    Quaternion _baseRotation;
    Quaternion _target;

    [SerializeField] float _maxAngle = 30f;
    [SerializeField] float _maxDelta = 30f;
    [SerializeField] float _applySpeed = 30f;
    [SerializeField] float _targetSpeed = 1f;

    private void Start()
    {
        _lastPosition = transform.position;
        _baseRotation = transform.localRotation;
    }

    void Update()
    {
        Vector3 current = transform.position;
        Vector3 localDelta = Quaternion.Inverse(transform.rotation) * (current - _lastPosition);

        Quaternion horizontal = Quaternion.AngleAxis(Mathf.Clamp(localDelta.z, -_maxDelta, _maxDelta) / _maxDelta * _maxAngle, Vector3.right);
        Quaternion forward = Quaternion.AngleAxis(Mathf.Clamp(-localDelta.x, -_maxDelta, _maxDelta) / _maxDelta * _maxAngle, Vector3.up);

        _target = Quaternion.Lerp(_target, _baseRotation * horizontal * forward, Time.deltaTime * _targetSpeed);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, _target, Time.deltaTime * _applySpeed);

        _lastPosition = current;
    }
}
