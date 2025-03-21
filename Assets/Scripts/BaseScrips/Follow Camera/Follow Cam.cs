using UnityEngine;

[ExecuteInEditMode]
public class FollowCam : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private Transform target;

    [SerializeField]
    private Vector3 offset;

    [SerializeField]
    private float distance = 10f;

    [SerializeField]
    private float followSpeed = 5f;

    [SerializeField]
    private float rotationSpeed = 5f;

    [Header("Freelook Settings")]
    [SerializeField]
    private bool enableFreelook = true;

    [SerializeField]
    private float mouseSensitivity = 2f;

    [SerializeField]
    private float resetTime = 3f;

    // Variables to track rotation
    private float horizontalRotation = 0f;
    private float verticalRotation = 0f;
    private float defaultHorizontalRotation = 0f;
    private float defaultVerticalRotation = 0f;
    private float lastInputTime;
    private bool isResetting = false;

    void Start()
    {
        if (target == null)
        {
            target = GameObject.Find("Player").transform;
        }
        offset = transform.position - target.position;

        // Initialize default rotation values
        Vector3 direction = target.position - transform.position;
        defaultHorizontalRotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        defaultVerticalRotation = Mathf.Asin(direction.y / direction.magnitude) * Mathf.Rad2Deg;

        horizontalRotation = defaultHorizontalRotation;
        verticalRotation = defaultVerticalRotation;

        lastInputTime = Time.time;
    }

    void Update()
    {
        if (enableFreelook)
        {
            HandleFreelookInput();
        }

        Vector3 targetPosition;
        Quaternion targetRotation;

        if (isResetting)
        {
            // Calculate position and rotation for the reset state
            HandleCameraReset();

            // Calculate the camera position based on the reset rotation
            float horizontalAngle = defaultHorizontalRotation * Mathf.Deg2Rad;
            float verticalAngle = defaultVerticalRotation * Mathf.Deg2Rad;

            Vector3 direction = new Vector3(
                Mathf.Sin(horizontalAngle) * Mathf.Cos(verticalAngle),
                Mathf.Sin(verticalAngle),
                Mathf.Cos(horizontalAngle) * Mathf.Cos(verticalAngle)
            );

            targetPosition = target.position - direction * distance;
            targetRotation = Quaternion.LookRotation(direction);
        }
        else
        {
            // Calculate the camera position based on current rotation
            float horizontalAngle = horizontalRotation * Mathf.Deg2Rad;
            float verticalAngle = verticalRotation * Mathf.Deg2Rad;

            Vector3 direction = new Vector3(
                Mathf.Sin(horizontalAngle) * Mathf.Cos(verticalAngle),
                Mathf.Sin(verticalAngle),
                Mathf.Cos(horizontalAngle) * Mathf.Cos(verticalAngle)
            );

            targetPosition = target.position - direction * distance;
            targetRotation = Quaternion.LookRotation(direction);
        }

        // Smoothly interpolate the camera's position and rotation
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void HandleFreelookInput()
    {
        // Check for mouse input - only use X axis (horizontal)
        float mouseX = Input.GetAxis("Mouse X");

        if (Mathf.Abs(mouseX) > 0.01f)
        {
            // We have input, update rotation and reset timer
            horizontalRotation += mouseX * mouseSensitivity;

            // Keep vertical rotation fixed at default value
            verticalRotation = defaultVerticalRotation;

            lastInputTime = Time.time;
            isResetting = false;
        }
        else if (Time.time - lastInputTime > resetTime)
        {
            // No input for the reset time, start resetting
            isResetting = true;
        }
    }

    private void HandleCameraReset()
    {
        // Gradually interpolate rotation back to default values
        horizontalRotation = Mathf.LerpAngle(
            horizontalRotation,
            defaultHorizontalRotation,
            rotationSpeed * 0.5f * Time.deltaTime
        );

        // Vertical rotation is always set to default
        verticalRotation = defaultVerticalRotation;

        // Check if we're close enough to default to finish reset
        if (Mathf.Abs(Mathf.DeltaAngle(horizontalRotation, defaultHorizontalRotation)) < 0.1f)
        {
            horizontalRotation = defaultHorizontalRotation;
        }
    }
}
