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

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        ProcessInput();
        GenerateNoise();
        HandleObjectInteraction();
        if (Input.GetKeyDown(KeyCode.V)) { }
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    private void ProcessInput()
    {
        Vector2 movementInput = InputManager.Instance.PlayerInput.Player.Move.ReadValue<Vector2>();
        _movementDirection = new Vector3(movementInput.x, 0, movementInput.y);
        _movementDirection = Vector3.ClampMagnitude(_movementDirection, 1f);
        _movementDirection = transform.TransformDirection(_movementDirection); // Convert to local space

        float speed;
        bool isMovingBackwards = _movementDirection.z < 0;
        bool isMovingRight = _movementDirection.x > 0;
        bool isMovingLeft = _movementDirection.x < 0;

        if (InputManager.Instance.PlayerInput.Player.Sprint.ReadValue<float>() > 0)
        {
            speed = _runSpeed;
            //SetAnimationState(true, false, false, isMovingBackwards, isMovingLeft, isMovingRight);
        }
        else if (InputManager.Instance.PlayerInput.Player.Stealth.ReadValue<float>() > 0)
        {
            speed = _stealthSpeed;
            //SetAnimationState(false, true, false, isMovingBackwards, isMovingLeft, isMovingRight);
        }
        else
        {
            speed = _walkSpeed;
            //SetAnimationState(false, false, true, isMovingBackwards, isMovingLeft, isMovingRight);
        }

        _movementDirection *= speed;
    }

    private void MovePlayer()
    {
        Vector3 targetPosition = _rb.position + _movementDirection * Time.fixedDeltaTime;
        _rb.MovePosition(targetPosition);

        if (_movementDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_movementDirection);
            _rb.rotation = Quaternion.Slerp(
                _rb.rotation,
                targetRotation,
                Time.fixedDeltaTime * _turnSpeed
            );
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

    // private void SetAnimationState(
    //     bool running,
    //     bool stealth,
    //     bool walking,
    //     bool backward,
    //     bool left,
    //     bool right
    // )
    // {
    //     _animator.SetBool("Run Forward", running && !backward && !left && !right);
    // {
    //     _animator.SetBool("Run Forward", running && !backward && !left && !right);
    //     _animator.SetBool("Run Backward", running && backward);
    //     _animator.SetBool("Running Left", running && left);
    //     _animator.SetBool("Running Right", running && right);

    //     _animator.SetBool("WalkingForward", walking && !backward && !left && !right);
    //     _animator.SetBool("WalkingBackward", walking && backward);
    //     _animator.SetBool("Run Forward Left", walking && left);
    //     _animator.SetBool("Run Forward Right", walking && right);
    //     _animator.SetBool("Run Backward Left", walking && backward && left);
    //     _animator.SetBool("Run Backward Right", walking && backward && right);
    // }

    void Reset()
    {
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
    }
}
