using GMTK.Inputs;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // -------~~~~~~~~~~================# // Components
    [Header("Components")]
    [SerializeField] Rigidbody _body;
    [SerializeField] Transform _pivot;
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
    [SerializeField] float _movementSpeed;
    [SerializeField] float _groundMaxAngle = 25f;
    Vector3 _groundNormal = Vector3.up;
    Vector3 _movementDirection;
    bool _isGrouded;

    // Jump
    [SerializeField]
    float _jumpForce = 10f;
    float _jumpStart;
    bool _isJumping;
    bool _canJump;

    // -------~~~~~~~~~~================# // Physics
    [Header("Physics")]
    [SerializeField] AnimationCurve _jumpCurve;
    [SerializeField] float _jumpDuration = 2f;
    [SerializeField] float _gravityForce = 1f;
    float _verticalVelocity;

    // ----------------~~~~~~~~~~~~~~~~~~~==========================# // Unity
    private void Start()
    {
        // Init Components
        _transform = transform;
        if (_camera)
            _cameraTransform = _camera.transform;

        if (!_pivot)
            _pivot = _transform;

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
        UpdateVerticalMovement();
    }

    private void FixedUpdate()
    {
        // Update Movement Direction
        _body.linearVelocity = Quaternion.FromToRotation(Vector3.up, _groundNormal) * (_movementSpeed * (transform.rotation * _movementDirection) + Vector3.up * _verticalVelocity);
    }


    // ----------------~~~~~~~~~~~~~~~~~~~==========================# // Collisions
    private void OnCollisionStay(Collision collision)
    {
        if (CheckGroundContact(collision.contacts)) return;

        _canJump = false;
        _isGrouded = false;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (CheckGroundContact(collision.contacts)) return;

        _canJump = false;
        _isGrouded = false;
    }

    private bool CheckGroundContact(ContactPoint[] contacts)
    {
        if (contacts == null) return false;

        bool result = false;

        _groundNormal = Vector3.up;

        foreach (ContactPoint contact in contacts)
        {
            if (Vector3.Angle(contact.normal, Vector3.up) <= _groundMaxAngle)
            {
                _groundNormal += contact.normal;
                result  = true;
            }

        }

        if (result)
        {
            _verticalVelocity = Mathf.Max(_verticalVelocity, 0);
            _canJump = true;
            _isGrouded = true;
        }

        _groundNormal = _groundNormal.normalized;

        return result;
    }

    // ----------------~~~~~~~~~~~~~~~~~~~==========================# // Updates
    private void UpdateVerticalMovement()
    {
        float jumpmTime = Time.time - _jumpStart;

        // Update Jump
        if (jumpmTime <= _jumpDuration && _isJumping)
        {
            float ratio =  1 - jumpmTime / _jumpDuration;
            _verticalVelocity = _jumpCurve.Evaluate(ratio) * _jumpForce;


            return;
        }

        // Update Gravity
        _verticalVelocity -= _gravityForce * Time.deltaTime;
    }

    // ----------------~~~~~~~~~~~~~~~~~~~==========================# // Look
    private void OnLook(Vector2 direction)
    {
        if (!_camera || !_transform) return;

        // Update Vertical Rotation
        _currentVerticalRotation -= direction.y * _lookStrength.y * _lookMultiplyer;
        _currentVerticalRotation = Mathf.Clamp(_currentVerticalRotation, _lookAngle.x, _lookAngle.y);
        _cameraTransform.localRotation = Quaternion.AngleAxis(_currentVerticalRotation, Vector3.right);

        // Update Horizontal Rotation
        _pivot.rotation = Quaternion.AngleAxis(direction.x * _lookStrength.x * _lookMultiplyer, Vector3.up) * _pivot.rotation;
    }

    // ----------------~~~~~~~~~~~~~~~~~~~==========================# // Movement
    private void OnMove(Vector3 direction)
        => _movementDirection = direction;

    // ----------------~~~~~~~~~~~~~~~~~~~==========================# // Jump
    private void OnJump(bool started)
    {
        _isJumping = started;

        if (!_canJump) return;

        if (!started)
        {
            _verticalVelocity = Mathf.Max(_verticalVelocity, 0);
            return;
        }

        _jumpStart = Time.time;
        _verticalVelocity = _jumpForce;
    }
}