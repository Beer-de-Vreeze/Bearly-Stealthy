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
    private float lastInputTime;
    private bool isResetting = false;
    private Vector3 currentOffset;

    void Start()
    {
        if (target == null)
        {
            target = GameObject.Find("Player").transform;
        }

        // Start with the editor-defined offset
        currentOffset = offset;

        // Initialize rotation values
        Vector3 direction = target.position - transform.position;
        horizontalRotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

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
            // Reset camera rotation gradually
            HandleCameraReset();

            // Simply use the original offset for the reset position
            targetPosition = target.position + offset;
        }
        else
        {
            // Calculate position based on the current rotation and the editor offset magnitude
            float horizontalAngle = horizontalRotation * Mathf.Deg2Rad;

            // Maintain the same height and distance as the editor offset
            float offsetDistance = new Vector2(offset.x, offset.z).magnitude;

            // Create rotated offset
            currentOffset = new Vector3(
                Mathf.Sin(horizontalAngle) * offsetDistance,
                offset.y, // Keep the same height
                Mathf.Cos(horizontalAngle) * offsetDistance
            );

            targetPosition = target.position + currentOffset;
        }

        // Always look at the target
        Vector3 lookDirection = target.position - transform.position;
        if (lookDirection != Vector3.zero)
        {
            targetRotation = Quaternion.LookRotation(lookDirection);
        }
        else
        {
            targetRotation = transform.rotation;
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
            // We have input, update rotation
            horizontalRotation += mouseX * mouseSensitivity;

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
        // Gradually interpolate rotation to return to the default offset angle
        float targetAngle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;

        // Gradually interpolate rotation to return to the original offset angle
        horizontalRotation = Mathf.LerpAngle(
            horizontalRotation,
            targetAngle,
            rotationSpeed * 0.5f * Time.deltaTime
        );

        // Check if we're close enough to target rotation to finish reset
        if (Mathf.Abs(Mathf.DeltaAngle(horizontalRotation, targetAngle)) < 0.1f)
        {
            horizontalRotation = targetAngle;
            isResetting = false; // Reset is complete
        }
    }
}
