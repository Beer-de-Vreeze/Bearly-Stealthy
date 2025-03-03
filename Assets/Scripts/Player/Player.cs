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

    [Tab("References")]
    [SerializeField, ReadOnly]
    private Rigidbody _rb;

    [SerializeField, ReadOnly]
    private Animator _animator;

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
