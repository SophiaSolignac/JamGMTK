using GMTK.Inputs;
using System;
using UnityEngine;

public class NewPlayerMovement : MonoBehaviour
{
    // -------~~~~~~~~~~================# // Components
    [Header("Components")]
    [SerializeField] Rigidbody _body;
    [SerializeField] Transform _pivotY;
    [SerializeField] Transform _pivotX;
    private Transform _transform;

    // -------~~~~~~~~~~================# // Inputs
    [Header("Components")]
    [SerializeField, Range(0f, 1f)] float _thresholdInput = .01f;
    Vector3 _inputDirection;

    // -------~~~~~~~~~~================# // Look
    [Header("Look")]
    [SerializeField] Vector2 _lookStrength = Vector2.one * 5f;
    [SerializeField] Vector2 _lookAngle = new Vector2(-90f, 90f);
    float _lookMultiplyer = .01f; // For Better Lisibility In Inspector
    float _currentVerticalRotation;
    Quaternion _yRotation = Quaternion.identity;
    Quaternion _xRotation = Quaternion.identity;


    // -------~~~~~~~~~~================# // Movement
    [Header("Movement")]
    [Tooltip("How Much The Speed Will Decrease Based On The Max Movement Speed")]
    [SerializeField] AnimationCurve _cutSpeedCurve;
    [SerializeField] float _maxMovementSpeed = 10f;
    [SerializeField] float _movementForce = 10000f;
    [SerializeField] float _groundMaxAngle = 25f;
    [SerializeField, Range(0f, 1f)] float _counterMovement = .1f;
    Vector3 _groundNormal = Vector3.up;
    bool _isGrouded;

    // -------~~~~~~~~~~================# // Jump
    [Header("Jump")]
    [SerializeField] AnimationCurve _jumpCurve;
    [SerializeField] float _jumpForce = 13f;
    [SerializeField] float _jumpDuration = 2f;
    [SerializeField] float _jumpBuffer = .2f;
    [SerializeField] float _jumpCoyote = .2f;
    float _jumBufferpInputTime = float.NegativeInfinity;
    float _jumpStart;
    bool _jumpCoyoteAvailable;
    bool _isJumpInputPressed;
    bool _canJump;

    // -------~~~~~~~~~~================# // Jump
    [Header("Dash")]
    [SerializeField] float _dashForce = 100f;
    [SerializeField] float _dashCoolDown = .5f;
    float _lastDashTime = float.NegativeInfinity;

    // -------~~~~~~~~~~================# // Physics
    [Header("Physics")]
    [SerializeField] float _gravityForce = 1500f;
    float _leftGroundTime = float.NegativeInfinity;

    // ----------------~~~~~~~~~~~~~~~~~~~==========================# // Unity
    private void Start()
    {
        // Init Components
        _transform = transform;
        if (!_pivotY) _pivotY = _transform;
        if (!_pivotX) _pivotX = _transform;

        // Set Cursor
        Cursor.lockState = CursorLockMode.Locked;

        // Connect Inputs
        InputManager.onLook.AddListener(OnLook);
        InputManager.onMove.AddListener(OnMove);
        InputManager.onJump.AddListener(OnJump);
        InputManager.onDash.AddListener(OnDash);
    }

    private void OnDestroy()
    {
        // Disconnect Inputs
        InputManager.onLook.RemoveListener(OnLook);
        InputManager.onJump.RemoveListener(OnJump);
        InputManager.onJump.RemoveListener(OnJump);
        InputManager.onDash.RemoveListener(OnDash);
    }

    private void FixedUpdate()
    {
        if (!_body) return;

        UpdateVerticalMovement();

        Vector3 relativeVelocity = Quaternion.Inverse(_yRotation) * _body.linearVelocity;

        ApplyCounterForce(relativeVelocity);
        ApplyMovement(relativeVelocity);
    }

    private void ApplyCounterForce(Vector3 relativeVelocity)
    {
        Vector3 wantedDirection = _yRotation * _inputDirection;
        Vector3 velocityDirection = relativeVelocity.normalized;

        // Add Horizontal Counter
        if (CheckDirection(_inputDirection.x, velocityDirection.x))
            _body.AddForce(Time.fixedDeltaTime * _movementForce * _counterMovement * -relativeVelocity.x * _pivotY.right);

        // Add Forward Counter
        if (CheckDirection(_inputDirection.z, velocityDirection.z))
            _body.AddForce(Time.fixedDeltaTime * _movementForce * _counterMovement * -relativeVelocity.z * _pivotY.forward);


        // Add Diagonal Counter
        if (Mathf.Sqrt((Mathf.Pow(_body.linearVelocity.x, 2) + Mathf.Pow(_body.linearVelocity.z, 2))) > _maxMovementSpeed)
        {
            float inputAngle = Mathf.Atan2(velocityDirection.z, velocityDirection.x);

            relativeVelocity.y = 0f;

            // Only Add Counter If X Is Above Max Speed
            if (Mathf.Abs(relativeVelocity.x) <= _maxMovementSpeed * Mathf.Cos(inputAngle)) relativeVelocity.x = 0;
            if (Mathf.Abs(relativeVelocity.z) <= _maxMovementSpeed * Mathf.Sin(inputAngle)) relativeVelocity.z = 0;

            _body.AddForce(Time.fixedDeltaTime * _movementForce * _counterMovement * -(_yRotation * relativeVelocity));
        }
    }

    private void ApplyMovement(Vector3 relativeVelocity)
    {
        bool isSameDirectionX = relativeVelocity.x * _inputDirection.x > 0;
        bool isSameDirectionZ = relativeVelocity.z * _inputDirection.z > 0;

        Vector3 absRelVel = new Vector3(Mathf.Abs(relativeVelocity.x), 0f, Mathf.Abs(relativeVelocity.z));

        float ratioX = !isSameDirectionX ? 0f : absRelVel.x / _maxMovementSpeed;
        float ratioZ = !isSameDirectionZ ? 0f : absRelVel.z / _maxMovementSpeed;

        Vector3 movement = Vector3.zero;

        // Only Apply Movement If It Isn't In The Same Direction Or If The Max Speed Is Not Reached Yet (X)
        if (!(isSameDirectionX && absRelVel.x > _maxMovementSpeed) || absRelVel.x <= 0)
            movement += (1 - _cutSpeedCurve.Evaluate(ratioX)) * _inputDirection.x * _pivotY.right;

        // Only Apply Movement If It Isn't In The Same Direction Or If The Max Speed Is Not Reached Yet (Y)
        if (!(isSameDirectionZ && absRelVel.z > _maxMovementSpeed) || absRelVel.z <= 0)
            movement += (1 - _cutSpeedCurve.Evaluate(ratioZ)) * _inputDirection.z * _pivotY.forward;


        _body.AddForce(Time.fixedDeltaTime * _movementForce * movement);
    }

    private bool CheckDirection(float wanted, float current)
        => wanted == 0f || current > 0f && wanted < 0f || current < 0f && wanted > 0f;

    #region // ----------------~~~~~~~~~~~~~~~~~~~==========================# // Collisions
    private void OnCollisionExit(Collision collision)
         => CheckGroundContact(collision.contacts);

    private void OnCollisionStay(Collision collision)
         => CheckGroundContact(collision.contacts);

    private bool CheckGroundContact(ContactPoint[] contacts)
    {
        if (contacts == null) return false;

        bool result = false;
        _groundNormal = Vector3.zero;

        // For Every Contact Check If Ground
        foreach (ContactPoint contact in contacts)
        {
            // Continue If Angle Is Above Ground Angle
            if (Vector3.Angle(contact.normal, Vector3.up) > _groundMaxAngle) continue;

            _groundNormal += contact.normal;
            result = true;

        }

        // If At Least One Is Ground Then Update Ground Properties
        if (result)
        {
            _groundNormal = _groundNormal.normalized;
            _canJump = true;
            _isGrouded = true;
            _jumpCoyoteAvailable = true;
            CheckJumpBuffer();
        }

        // Reset Ground Normal If Nothing Has Been Founded
        else
        {
            _groundNormal = Vector3.up;
            _canJump = false;
            _isGrouded = false;
            _leftGroundTime = Time.time;
        }

        return result;
    }
    #endregion

    #region // ----------------~~~~~~~~~~~~~~~~~~~==========================# // Updates
    private void UpdateVerticalMovement()
    {
        // Update Gravity
        _body.AddForce(Time.fixedDeltaTime * _gravityForce * -_groundNormal, ForceMode.Acceleration);

        float jumpmTime = Time.time - _jumpStart;

        // Update Jump
        if (jumpmTime <= _jumpDuration && _isJumpInputPressed)
        {
            float ratio = 1 - jumpmTime / _jumpDuration;
            _body.AddForce(_jumpCurve.Evaluate(ratio) * _jumpForce * _groundNormal);

            return;
        }
    }
    #endregion

    #region // ----------------~~~~~~~~~~~~~~~~~~~==========================# // Inputs
    // -------~~~~~~~~~~================# // Look
    private void OnLook(Vector2 direction)
    {
        if (!_pivotY) return;

        // Update Horizontal Rotation
        _yRotation = Quaternion.AngleAxis(direction.x * _lookStrength.x * _lookMultiplyer, Vector3.up) * _yRotation;
        _pivotY.rotation = _yRotation;

        if (!_pivotX) return;

        // Update Vertical Rotation
        _currentVerticalRotation -= direction.y * _lookStrength.y * _lookMultiplyer;
        _currentVerticalRotation = Mathf.Clamp(_currentVerticalRotation, _lookAngle.x, _lookAngle.y);
        _pivotX.localRotation = Quaternion.AngleAxis(_currentVerticalRotation, Vector3.right);
    }

    // -------~~~~~~~~~~================# // Movement
    private void OnMove(Vector3 direction)
        // => Debug.LogWarning(_inputDirection = direction.magnitude <= _thresholdInput ? Vector3.zero : direction);
        => _inputDirection = direction.magnitude <= _thresholdInput ? Vector3.zero : direction;

    // -------~~~~~~~~~~================# // Dash
    private void OnDash()
    {
        // Cooldown
        if (Time.time - _lastDashTime < _dashCoolDown) return;

        // Dash
        _lastDashTime = Time.time;
        Vector3 dashDirection = _pivotY.forward;
        _body.linearVelocity = Vector3.ProjectOnPlane(_body.linearVelocity, dashDirection);
        _body.AddForce(dashDirection * _dashForce, ForceMode.Impulse);
    }

    // -------~~~~~~~~~~================# // Jump
    private void OnJump(bool started)
    {
        _isJumpInputPressed = started;

        if (!started) return;

        if (!(_canJump || _jumpCoyoteAvailable && Time.time - _leftGroundTime <= _jumpCoyote))
        {
            if (started) _jumBufferpInputTime = Time.time;
            return;
        }

        _jumpCoyoteAvailable = false;
        _canJump = false;

        _jumpStart = Time.time;
        _jumBufferpInputTime = float.NegativeInfinity;

        _body.linearVelocity = Vector3.ProjectOnPlane(_body.linearVelocity, _groundNormal);
        _body.AddForce(_groundNormal * _jumpForce, ForceMode.Impulse);
    }

    private void CheckJumpBuffer()
    {
        if (Time.time - _jumBufferpInputTime > _jumpBuffer) return;

        OnJump(true);
    }

    #endregion
}