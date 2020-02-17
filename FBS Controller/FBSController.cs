using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Enumerations
public enum PlayerMoveStatus { NotMoving, Crouching, Walking, Running, NotGrounded, Landing }
public enum CurveControlledBobCallbackType { Horizontal, Vertical }

// Delegates
public delegate void CurveControlledBobCallback();

[System.Serializable]
public class CurveControlledBobEvent
{
    public float Time = 0.0f;
    public CurveControlledBobCallback Function = null;
    public CurveControlledBobCallbackType Type = CurveControlledBobCallbackType.Vertical;
}

[System.Serializable]
public class CurveControlledBob
{
    [SerializeField]
    AnimationCurve _bobcurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f),
                                                                    new Keyframe(1f, 0f), new Keyframe(1.5f, -1f),
                                                                    new Keyframe(2f, 0f));

    // Inspector Assigned Bob Control Variables
    [SerializeField] float _horizontalMultiplier = 0.01f;
    [SerializeField] float _verticalMultiplier = 0.02f;
    [SerializeField] float _verticaltoHorizontalSpeedRatio = 2.0f;
    [SerializeField] float _baseInterval = 1.0f;

    // Internals
    float _prevXPlayHead;
    float _prevYPlayHead;
    float _xPlayHead;
    float _yPlayHead;
    float _curveEndTime;
    List<CurveControlledBobEvent> _events = new List<CurveControlledBobEvent>();

    public void Initialize()
    {
        // Record time length of bob curve
        _curveEndTime = _bobcurve[_bobcurve.length - 1].time;
        _xPlayHead = 0.0f;
        _yPlayHead = 0.0f;
        _prevXPlayHead = 0.0f;
        _prevYPlayHead = 0.0f;
    }

    public void RegisterEventCallback(float time, CurveControlledBobCallback function, CurveControlledBobCallbackType type)
    {
        CurveControlledBobEvent ccbeEvent = new CurveControlledBobEvent();
        ccbeEvent.Time = time;
        ccbeEvent.Function = function;
        ccbeEvent.Type = type;
        _events.Add(ccbeEvent);
        _events.Sort(
            delegate (CurveControlledBobEvent t1, CurveControlledBobEvent t2)
            {
                return (t1.Time.CompareTo(t2.Time));
            }
        );
    }

    public Vector3 GetVectorOffset(float speed)
    {
        _xPlayHead += (speed * Time.deltaTime) / _baseInterval;
        _yPlayHead += ((speed * Time.deltaTime) / _baseInterval) * _verticaltoHorizontalSpeedRatio;

        if (_xPlayHead > _curveEndTime)
            _xPlayHead = 0f;

        if (_yPlayHead > _curveEndTime)
            _yPlayHead = 0f;

        // Process Events
        for (int i = 0; i < _events.Count; i++)
        {
            CurveControlledBobEvent ev = _events[i];
            if (ev != null)
            {
                if (ev.Type == CurveControlledBobCallbackType.Vertical)
                {
                    if ((_prevYPlayHead < ev.Time && _yPlayHead >= ev.Time) ||
                        (_prevYPlayHead > _yPlayHead && (ev.Time > _prevYPlayHead || ev.Time <= _yPlayHead)))
                    {
                        ev.Function();
                    }
                }
                else
                {
                    if ((_prevXPlayHead < ev.Time && _xPlayHead >= ev.Time) ||
                        (_prevXPlayHead > _xPlayHead && (ev.Time > _prevXPlayHead || ev.Time <= _xPlayHead)))
                    {
                        ev.Function();
                    }
                }
            }
        }

        float xPos = _bobcurve.Evaluate(_xPlayHead) * _horizontalMultiplier;
        float yPos = _bobcurve.Evaluate(_yPlayHead) * _verticalMultiplier;

        _prevXPlayHead = _xPlayHead;
        _prevYPlayHead = _yPlayHead;

        return new Vector3(xPos, yPos, 0f);
    }
}


[RequireComponent(typeof(CharacterController))]
public class FBSController : MonoBehaviour
{
    [SerializeField] AudioCollection _footsteps = null;
    [SerializeField] float _crouchAttenuation = .2f;
    [SerializeField] float _crouchSpeed = 1f;
    [SerializeField] float _staminaDepletion = 5f;
    [SerializeField] float _staminaRecovery = 10f;
    [SerializeField] float _walkSpeed           = 2f;
    [SerializeField] float _runSpeed            = 5.5f;
    [SerializeField] float _jumpSpeed           = 7.5f;
    [SerializeField] float _gravityMultiplier   = 2.5f;
    [SerializeField] CurveControlledBob _headBob = new CurveControlledBob();
    [SerializeField] float _runStepLengthen = .75f;
    [SerializeField] GameObject _flashLight = null;
    [SerializeField] bool _flashLightOnAtStart = true;
    [SerializeField][Range(0f, 1f)] float _npcStickiness = .5f;

    // Code imported from unity standard assets 
    [SerializeField] MouseLook _mouseLook = null;

    [Header("Shared variables")]
    [SerializeField] SharedFloat _stamina = null;

    [Header("Shared variables - Broadcasters")]
    [SerializeField] protected SharedVector3 _broadcastPosition = null;
    [SerializeField] protected SharedVector3 _broadcastDirection = null;

    // Private internals
    Camera _camera = null;
    bool _jumpButtonPressed = false;
    Vector2 _inputVector = Vector2.zero;
    Vector3 _moveDirection = Vector3.zero;
    bool _previouslyGrounded = false;
    bool _isWalking = false;
    bool _isJumping = false;
    bool _isCrouching = false;
    float _characterControllerHeight = 0f;
    int _footStepIndex = 0;
    bool _freezeMovement = false;
    float _dragMultiplier = 1f;
    float _dragMultiplierLimit = 1f;

    // Timers 
    float _fallingTimer = 0f;
    PlayerMoveStatus _movementStatus = PlayerMoveStatus.NotMoving;
    Vector3 _localSpaceCameraPos = Vector3.zero;

    // Cache
    CharacterController _characterController = null;

    // Public Properties
    public PlayerMoveStatus movementStatus { get { return _movementStatus; }}
    public float walkSpeed { get { return _walkSpeed; } }
    public float runSpeed { get { return _runSpeed; }}
    public float dragMultiplierLimit { get { return _dragMultiplierLimit; } set { _dragMultiplierLimit = Mathf.Clamp01(value); }}
    public float dragMultiplier { get { return _dragMultiplier; } set { _dragMultiplier = Mathf.Min(value, _dragMultiplierLimit); }}
    public bool freezeMovement { get { return _freezeMovement; } set { _freezeMovement = value; } }


    void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _camera = Camera.main;

        // Setup mouse look script
        _mouseLook.Init(transform, _camera.transform);

        _localSpaceCameraPos = _camera.transform.localPosition;
        _headBob.Initialize();
        _headBob.RegisterEventCallback(1.5f, PlayerFootStepSound, CurveControlledBobCallbackType.Vertical);

        if (_flashLight)
            _flashLight.SetActive(_flashLightOnAtStart);

        _characterControllerHeight = _characterController.height;
    }

    void Update()
    {
        if (_characterController.isGrounded) _fallingTimer = 0f;
        else _fallingTimer += Time.deltaTime;

        // If the game is not paused
        if (Time.timeScale > Mathf.Epsilon) {
            _mouseLook.LookRotation(transform, _camera.transform);
        }

        if (Input.GetButtonDown("Flashlight")) {
            if (_flashLight) {
                _flashLight.SetActive(!_flashLight.activeSelf);
            }
        }

        if (!_jumpButtonPressed && !_isCrouching) {
            _jumpButtonPressed = Input.GetButtonDown("Jump");
        }

        if (_characterController.isGrounded && Input.GetButtonDown("Crouch")) {
            _isCrouching = !_isCrouching;
            _characterController.height = _isCrouching ? _characterControllerHeight / 2f : _characterControllerHeight;
        }

        if (!_previouslyGrounded && _characterController.isGrounded)
        {
            if (_fallingTimer > 5f)
            {
                // TODO: Landing soud effect
                _fallingTimer = 0f;
            }
            _moveDirection.y = 0f;
            _isJumping = false;
            _movementStatus = PlayerMoveStatus.Landing;
        }
        else if (!_characterController.isGrounded)
        {
            _movementStatus = PlayerMoveStatus.NotGrounded;
        }
        else if (_characterController.velocity.sqrMagnitude < .01f)
        {
            _movementStatus = PlayerMoveStatus.NotMoving;
        }
        else if (_isCrouching) 
        {
            _movementStatus = PlayerMoveStatus.Crouching;
        }
        else if (_isWalking)
        {
            _movementStatus = PlayerMoveStatus.Walking;
        }
        else _movementStatus = PlayerMoveStatus.Running;

        _previouslyGrounded = _characterController.isGrounded;

        if (_movementStatus == PlayerMoveStatus.Running) {
            _stamina.value = Mathf.Max(0f, _stamina.value - _staminaDepletion * Time.deltaTime);
        } else {
            _stamina.value = Mathf.Min(100, _stamina.value + Time.deltaTime * _staminaRecovery);
        }

        _dragMultiplier = Mathf.Min(_dragMultiplierLimit, _dragMultiplier + Time.deltaTime);
    }

    void FixedUpdate()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool wasWalking = _isWalking;

        _isWalking = !Input.GetKey(KeyCode.LeftShift);

        float speed = _isCrouching ? _crouchSpeed : _isWalking ? _walkSpeed : Mathf.Lerp(_walkSpeed, _runSpeed, _stamina.value/100);

        _inputVector = new Vector2(horizontal, vertical);

        if (_inputVector.magnitude > 1) _inputVector.Normalize();

        Vector3 desiredMove = transform.forward * _inputVector.y + transform.right * _inputVector.x;
        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position, _characterController.radius, Vector3.down, out hitInfo, _characterController.height / 2f, 1)) {
            // This means we are standing on a surface
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;
        }

        _moveDirection.x = !_freezeMovement ? desiredMove.x * speed * _dragMultiplier : 0f;
        _moveDirection.z = !_freezeMovement ? desiredMove.z * speed * _dragMultiplier : 0f;

        if (_characterController.isGrounded) {
            if (_jumpButtonPressed) {
                _moveDirection.y = _jumpSpeed;
                // We reset the jump button to fase so in another update function, it could be set to true again
                _jumpButtonPressed = false;
                // TODO: Play Jumping sound
            }
        } else {
            _moveDirection += Physics.gravity * _gravityMultiplier * Time.fixedDeltaTime;
        }

        _characterController.Move(_moveDirection * Time.fixedDeltaTime);

        // Camera bob
        // Nullifying the speed relative to the Y axis
        Vector3 speedXZ = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z);
        if (speedXZ.magnitude > 0.01f) {
            _camera.transform.localPosition = _localSpaceCameraPos + _headBob.GetVectorOffset(speedXZ.magnitude * (_isWalking || _isCrouching ? 1 : _runStepLengthen));
        } else {
            _camera.transform.localPosition = _localSpaceCameraPos;
        }

        // Update broadcasters
        if (_broadcastPosition != null)
            _broadcastPosition.value = transform.position;
        if (_broadcastDirection != null)
            _broadcastDirection.value = transform.forward;
    }

    void PlayerFootStepSound() {
        if (AudioManager.instance != null && _footsteps != null) {
            AudioClip soundToPlay;

            if (_isCrouching) {
                soundToPlay = _footsteps[1];
            } else {
                soundToPlay = _footsteps[0];
            }
            AudioManager.instance.PlayOneShotSound("Player", soundToPlay, transform.position,
                                                   _isCrouching ? _footsteps.volume * _crouchAttenuation : _footsteps.volume,
                                                   _footsteps.spatialBlend, _footsteps.priority);
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        AIStateMachine machine = GameSceneManager.instance.GetAIStateMachine(hit.collider.GetInstanceID());
        if (machine != null) {
            _dragMultiplier = 1 - _npcStickiness;
            machine.VisualThreat.Set(
                AITargetType.Visual_Player, 
                _characterController, 
                transform.position, 
                (hit.transform.position - transform.position).magnitude);

            machine.SetStateOverride(AIStateType.Attack);

        }
    }
}
