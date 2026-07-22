using GMTK.Inputs;
using System.Linq;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // -------~~~~~~~~~~================# // Components
    [Header("Components")]
    [SerializeField] Rigidbody _body;
    [SerializeField] Camera _camera;
    Transform _cameraTransform;
    private Transform _transform;

    // -------~~~~~~~~~~================# // Settings
    [Header("Settings")]
    // Look
    [SerializeField] Vector2 _lookStrength = Vector2.one;
    [SerializeField] Vector2 _lookAngle = new Vector2(-90f, 90f);
    float _currentVerticalRotation;
    float _lookMultiplyer = .01f; // For Better Lisibility In Inspector

    // Movement
    [SerializeField] float _movementSpeed = 10f;
    Vector3 _movementDirection;

    // Jump
    [SerializeField] float _jumpForce = 10f;

    // -------~~~~~~~~~~================# // Physics
    [Header("Physics")]
    [SerializeField] float _gravityForce = 1f;
    float _verticalVelocity;

    private void Start()
    {
        // Init Components
        _transform = transform;
        if (_camera)
            _cameraTransform = _camera.transform;

        // Set Cursor
        Cursor.lockState = CursorLockMode.Locked;

        // Connect Inputs
        InputManager.onLook.AddListener(OnLook);
        InputManager.onJump.AddListener(OnJump);
        InputManager.onMove.AddListener(OnMove);

    }

    private void Update()
    {
        // Update Gravity
        _verticalVelocity -= _gravityForce * Time.deltaTime;

        // Update Movement Direction
        _body.linearVelocity = _movementSpeed * (transform.rotation * _movementDirection) + Vector3.up * _verticalVelocity;
    }

    private void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal == Vector3.up)
            {
                _verticalVelocity = Mathf.Max(_verticalVelocity, 0);
                break;
            }
        }
    }

    private void OnLook(Vector2 direction)
    {
        if (!_camera || !_transform) return;

        // Update Vertical Rotation
        _currentVerticalRotation -= direction.y * _lookStrength.y * _lookMultiplyer;
        _currentVerticalRotation = Mathf.Clamp(_currentVerticalRotation, _lookAngle.x, _lookAngle.y);
        _cameraTransform.localRotation = Quaternion.AngleAxis(_currentVerticalRotation, Vector3.right);

        // Update Horizontal Rotation
        _transform.rotation = Quaternion.AngleAxis(direction.x * _lookStrength.x * _lookMultiplyer, Vector3.up) * _transform.rotation;
    }

    private void OnMove(Vector3 direction)
        => _movementDirection = direction;

    private void OnJump(bool started)
    {
        if (!started) return;

        _verticalVelocity = _jumpForce;
    }
}
