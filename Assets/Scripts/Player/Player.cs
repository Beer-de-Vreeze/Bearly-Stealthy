using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;

[
    RequireComponent(typeof(Rigidbody)),
    RequireComponent(typeof(PlayerInput)),
    RequireComponent(typeof(Animator))
]
public class Player : MonoBehaviour
{
    [Tab("Movement")]
    [SerializeField]
    private float _walkSpeed = 10f;

    [SerializeField]
    private float _runSpeed = 20f;

    [SerializeField]
    private float _turnSpeed = 10f;

    [Tab("Stealth")]
    [SerializeField]
    private float _stealthSpeed = 5f;

    [Tab("Noise")]
    [SerializeField, ReadOnly]
    public float _noiseLevel = 0f;

    [SerializeField]
    private float _runNoiseLevel = 10f;

    [SerializeField]
    private float _walkNoiseLevel = 5f;

    [SerializeField]
    private float _stealthNoiseLevel = 2f;

    [Tab("References")]
    [SerializeField, ReadOnly]
    private Rigidbody _rb;

    [SerializeField, ReadOnly]
    private Animator _animator;

    [Tab("Interaction")]
    [SerializeField]
    private float _throwForce = 10f;

    [SerializeField]
    private float _pickupDistance = 2f;

    [SerializeField]
    private Transform _holdPosition;

    [SerializeField]
    private LayerMask _interactableLayers;

    private DistractionObject _heldObject;
    private bool _isHoldingObject = false;

    private Vector3 _movementDirection = Vector3.zero;
    private bool _isMovingBackwards = false;

    public bool IsStealthActive =>
        InputManager.Instance.PlayerInput.Player.Stealth.ReadValue<float>() > 0;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetUp();
    }

    void Update()
    {
        ProcessInput();
        GenerateNoise();
        HandleObjectInteraction();
        if (Input.GetKeyDown(KeyCode.V)) { }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    private void ProcessInput()
    {
        // Get movement input values
        Vector2 inputVector = InputManager.Instance.PlayerInput.Player.Move.ReadValue<Vector2>();

        // Convert input to world space direction
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;

        // Ensure movement is on the horizontal plane
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        // Calculate movement direction
        _movementDirection = forward * inputVector.y + right * inputVector.x;

        // Check if moving backwards
        _isMovingBackwards = inputVector.y < 0;

        // Set speed based on movement state
        float currentSpeed;

        if (inputVector.magnitude > 0.1f)
        {
<<<<<<< Updated upstream
            speed = _runSpeed;
            SetAnimationState(true, false, false, _isMovingBackwards, isMovingLeft, isMovingRight);
        }
        else if (InputManager.Instance.PlayerInput.Player.Stealth.ReadValue<float>() > 0)
        {
            speed = _stealthSpeed;
            SetAnimationState(false, true, false, _isMovingBackwards, isMovingLeft, isMovingRight);
        }
        else
        {
            speed = _walkSpeed;
            SetAnimationState(false, false, true, _isMovingBackwards, isMovingLeft, isMovingRight);
=======
            // Check sprint
            if (InputManager.Instance.PlayerInput.Player.Sprint.ReadValue<float>() > 0)
            {
                currentSpeed = _runSpeed;
                UpdateAnimationState(true, false, false);
            }
            // Check stealth
            else if (InputManager.Instance.PlayerInput.Player.Stealth.ReadValue<float>() > 0)
            {
                currentSpeed = _stealthSpeed;
                UpdateAnimationState(false, true, false);
            }
            // Normal walking
            else
            {
                currentSpeed = _walkSpeed;
                UpdateAnimationState(false, false, true);
            }
        }
        else
        {
            // Not moving
            currentSpeed = 0f;
            UpdateAnimationState(false, false, false);
>>>>>>> Stashed changes
        }

        // Apply speed to movement direction
        _movementDirection = _movementDirection.normalized * currentSpeed;
    }

    private void MovePlayer()
    {
        // Preserve vertical velocity for gravity
        Vector3 velocity = _movementDirection;
        velocity.y = _rb.linearVelocity.y;
        _rb.linearVelocity = velocity;

        // Rotate player to face movement direction
        if (_movementDirection.magnitude > 0.1f)
        {
            Quaternion toRotation = Quaternion.LookRotation(_movementDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                toRotation,
                _turnSpeed * Time.deltaTime
            );
        }
    }

    private void UpdateAnimationState(bool running, bool stealth, bool walking)
    {
        if (_animator == null)
            return;

        // Reset all animation states
        _animator.SetBool("Run Forward", false);
        _animator.SetBool("Run Backward", false);
        _animator.SetBool("Running Left", false);
        _animator.SetBool("Running Right", false);
        _animator.SetBool("WalkingForward", false);
        _animator.SetBool("WalkingBackward", false);

        // Get directional input
        Vector2 input = InputManager.Instance.PlayerInput.Player.Move.ReadValue<Vector2>();
        bool isMovingLeft = input.x < -0.1f;
        bool isMovingRight = input.x > 0.1f;

        // Set appropriate animation state based on movement type
        if (running)
        {
            if (_isMovingBackwards)
            {
<<<<<<< Updated upstream
                Vector3 lookDirection = transform.forward;
                lookDirection.x = horizontalDirection.x;
                lookDirection = lookDirection.normalized;

                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                _rb.rotation = Quaternion.Slerp(
                    _rb.rotation,
                    targetRotation,
                    Time.fixedDeltaTime * _turnSpeed
                );
=======
                _animator.SetBool("Run Backward", true);
                if (isMovingLeft)
                    _animator.SetBool("Run Backward Left", true);
                if (isMovingRight)
                    _animator.SetBool("Run Backward Right", true);
            }
            else
            {
                _animator.SetBool("Run Forward", true);
                if (isMovingLeft)
                    _animator.SetBool("Running Left", true);
                if (isMovingRight)
                    _animator.SetBool("Running Right", true);
            }
        }
        else if (walking || stealth)
        {
            float speedMultiplier = stealth ? 0.5f : 1.0f;
            _animator.SetFloat("SpeedMultiplier", speedMultiplier);

            if (_isMovingBackwards)
            {
                _animator.SetBool("WalkingBackward", true);
            }
            else
            {
                _animator.SetBool("WalkingForward", true);
                if (isMovingLeft)
                    _animator.SetBool("Run Forward Left", true);
                if (isMovingRight)
                    _animator.SetBool("Run Forward Right", true);
>>>>>>> Stashed changes
            }
        }
    }

    private void GenerateNoise()
    {
        if (_movementDirection.magnitude > 0)
        {
            if (InputManager.Instance.PlayerInput.Player.Sprint.ReadValue<float>() > 0)
            {
                _noiseLevel = _runNoiseLevel;
            }
            else if (InputManager.Instance.PlayerInput.Player.Stealth.ReadValue<float>() > 0)
            {
                _noiseLevel = _stealthNoiseLevel;
            }
            else
            {
                _noiseLevel = _walkNoiseLevel;
            }
            NoiseManager.Instance.GenerateNoise(transform.position, _noiseLevel);
        }
        else
        {
            _noiseLevel = 0f;
        }

        // Noise level decay over time
        if (_noiseLevel > 0)
        {
            _noiseLevel = Mathf.Max(0, _noiseLevel - Time.deltaTime * 5f);
        }
    }

    private void GenerateNoise(float noiseLevel)
    {
        _noiseLevel = noiseLevel;
        NoiseManager.Instance.GenerateNoise(transform.position, noiseLevel);
    }

    private void HandleObjectInteraction()
    {
        // Pickup/Drop objects
        if (InputManager.Instance.PlayerInput.Player.Interact.triggered)
        {
            if (_isHoldingObject)
            {
                DropObject();
            }
            else
            {
                TryPickupObject();
            }
        }

        // Throw objects
        if (_isHoldingObject && InputManager.Instance.PlayerInput.Player.Throw.triggered)
        {
            ThrowObject();
        }
    }

    private void TryPickupObject()
    {
        RaycastHit hit;
        if (
            Physics.Raycast(
                Camera.main.transform.position,
                Camera.main.transform.forward,
                out hit,
                _pickupDistance,
                _interactableLayers
            )
        )
        {
            DistractionObject distractionObject = hit.collider.GetComponent<DistractionObject>();
            if (distractionObject != null && distractionObject.CanBePickedUp)
            {
                _heldObject = distractionObject;
                _heldObject.PickUp(_holdPosition);
                _isHoldingObject = true;
            }
        }
    }

    private void DropObject()
    {
        if (_heldObject != null)
        {
            _heldObject.Drop();
            // Generate noise based on object weight when dropped
            GenerateNoise(_heldObject.NoiseOnDrop);
            _heldObject = null;
            _isHoldingObject = false;
        }
    }

    private void ThrowObject()
    {
        if (_heldObject != null)
        {
            // Generate even more noise when throwing
            GenerateNoise(_heldObject.NoiseOnThrow);
            _heldObject.Throw(Camera.main.transform.forward * _throwForce);
            _heldObject = null;
            _isHoldingObject = false;
        }
    }

    private void SetAnimationState(
        bool running,
        bool stealth,
        bool walking,
        bool backward,
        bool left,
        bool right
    )
    {
        _animator.SetBool("Run Forward", running && !backward && !left && !right);
        _animator.SetBool("Run Backward", running && backward);
        _animator.SetBool("Running Left", running && left);
        _animator.SetBool("Running Right", running && right);

        _animator.SetBool("WalkingForward", walking && !backward && !left && !right);
        _animator.SetBool("WalkingBackward", walking && backward);
        _animator.SetBool("Run Forward Left", walking && left);
        _animator.SetBool("Run Forward Right", walking && right);
        _animator.SetBool("Run Backward Left", walking && backward && left);
        _animator.SetBool("Run Backward Right", walking && backward && right);
    }

    private void BearRoar()
    {
        GenerateNoise(100f);
    }

    private void Reset()
    {
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
    }

    private void SetUp()
    {
        InputManager.Instance.PlayerInput.Player.Roar.performed += ctx => BearRoar();
    }
}
