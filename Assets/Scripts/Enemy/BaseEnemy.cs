using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using VInspector;

public class BaseEnemy : MonoBehaviour
{
    [Tab("Movement")]
    [SerializeField]
    protected float Speed = 2f;
    protected Rigidbody RB;
    protected NavMeshAgent Agent;
    protected Transform PatrolPointsParent;

    [Tab("Patrol")]
    [SerializeField]
    public float WanderTime = 5f;

    [SerializeField, ReadOnly]
    private bool _isWandering = false;

    [SerializeField, ReadOnly]
    private Coroutine _wanderCoroutine = null;

    [Tab("Vision")]
    [SerializeField]
    protected bool CanSee = true;

    [SerializeField]
    protected float VisionDistance = 10f;

    [SerializeField]
    protected float VisionAngle = 45f;

    [SerializeField]
    protected float VisibilityCheckInterval = 0.5f;

    [
        SerializeField,
        Tooltip("Time in seconds the enemy will continue chasing after losing sight of player")
    ]
    protected float PlayerVisibilityTimeout = 7f;

    [SerializeField, ReadOnly]
    protected float LastPlayerVisibleTime = 0f;

    [SerializeField, Tooltip("Layers that block line of sight")]
    protected LayerMask VisionBlockingLayers;

    [
        SerializeField,
        Tooltip("How much movement affects visibility (higher = easier to spot when moving)")
    ]
    protected float MovementDetectionMultiplier = 1.5f;

    [
        SerializeField,
        Tooltip("How much light levels affect visibility (lower = harder to see in darkness)")
    ]
    protected float LightDetectionMultiplier = 1.2f;

    [SerializeField, Tooltip("Time needed to fully detect a player in optimal conditions")]
    protected float DetectionTime = 1.0f;

    [SerializeField, Range(0, 1), ReadOnly]
    public float DetectionMeter = 0f;

    [SerializeField, Tooltip("Rate at which detection meter decreases when player exits vision")]
    protected float DetectionDecayRate = 0.5f;

    [
        SerializeField,
        Tooltip(
            "Player can be detected up to this angle outside of vision cone (peripheral vision)"
        )
    ]
    protected float PeripheralVisionAngle = 90f;

    [
        SerializeField,
        Tooltip("Detection multiplier for peripheral vision (lower value = harder to detect)")
    ]
    protected float PeripheralVisionMultiplier = 0.3f;

    // Alert level enum
    public enum AlertLevel
    {
        Normal,
        Suspicious,
        Alert,
        Confirmed
    }

    [SerializeField, ReadOnly]
    public AlertLevel CurrentAlertLevel = AlertLevel.Normal;

    [Tab("Hearing")]
    [SerializeField]
    protected bool CanHear = true;

    [SerializeField]
    protected float HearingDistance = 8f;

    [SerializeField]
    private float _hearingThreshold = 3f;

    [SerializeField]
    protected Color HearingGizmoColor = new Color(0, 0.5f, 1f, 0.2f);

    [SerializeField]
    protected bool AlwaysShowHearingRadius = true;
    protected Player Player;

    [SerializeField, ReadOnly]
    protected bool IsPlayerSpotted = false;

    [Tab("Investigation")]
    [SerializeField]
    protected float _investigationTime = 10f;

    [SerializeField, ReadOnly]
    private bool _isInvestigating = false;

    [SerializeField, ReadOnly]
    private Vector3 _lastHeardPosition;

    [SerializeField, ReadOnly]
    protected float _investigationTimeRemaining = 0f;

    [SerializeField, ReadOnly]
    private float _currentNoiseLevel = 0f;

    protected enum EnemyState
    {
        Patrolling,
        Investigating,
        Chasing,
        Wandering
    }

    [SerializeField, ReadOnly]
    protected EnemyState _currentState = EnemyState.Patrolling;

    protected virtual void Start()
    {
        Agent = GetComponent<NavMeshAgent>();
        RB = GetComponent<Rigidbody>();
        RB.isKinematic = true;
        Agent.speed = Speed;
        Player = FindFirstObjectByType<Player>();

        // Register with NoiseManager
        if (NoiseManager.Instance != null)
        {
            NoiseManager.Instance.RegisterEnemy(this);
        }
    }

    protected virtual void OnDestroy()
    {
        NoiseManager.Instance.UnregisterEnemy(this);
    }

    protected virtual void Update()
    {
        // If we're chasing, check if player is still visible
        if (_currentState == EnemyState.Chasing)
        {
            if (Time.time - LastPlayerVisibleTime > PlayerVisibilityTimeout)
            {
                LosePlayerVisibility();
            }
            else
            {
                // Check visibility periodically instead of every frame
                if (Time.frameCount % (int)(VisibilityCheckInterval * 60) == 0)
                {
<<<<<<< Updated upstream
                    CheckPlayerVisibility();
=======
                    if (IsPlayerVisible())
                    {
                        // Update last visible time since we can still see the player
                        LastPlayerVisibleTime = Time.time;
                        Agent.SetDestination(Player.transform.position); // Actively chase player
                    }
                    // If not visible, the timeout will eventually trigger LosePlayerVisibility()
>>>>>>> Stashed changes
                }
            }
        }
        else
        {
            // Regular vision check for non-chase states
            CheckVision();
        }

        CheckHearing();

        if (_isInvestigating)
        {
            _investigationTimeRemaining -= Time.deltaTime;
            if (_investigationTimeRemaining <= 0)
            {
                _isInvestigating = false;
<<<<<<< Updated upstream
                _currentState = EnemyState.Patrolling;
                CurrentAlertLevel = AlertLevel.Normal; // Reset alert level
=======
                // Only return to patrolling if not in another higher priority state
                if (_currentState == EnemyState.Investigating)
                {
                    _currentState = EnemyState.Patrolling;
                }
>>>>>>> Stashed changes
            }
        }

        // State machine logic
        switch (_currentState)
        {
            case EnemyState.Patrolling:
                // Regular patrol behavior
                break;
            case EnemyState.Investigating:
                // Move towards noise source
                Agent.SetDestination(_lastHeardPosition);
                break;
            case EnemyState.Chasing:
                // Chase player - keep updating destination
                Agent.SetDestination(Player.transform.position);
                break;
            case EnemyState.Wandering:
                // Wandering is handled by the coroutine
                break;
        }
    }

    protected virtual void OnDrawGizmos()
    {
        // Always draw hearing radius if enabled
        if (AlwaysShowHearingRadius && CanHear)
        {
            Gizmos.color = HearingGizmoColor;
            Gizmos.DrawWireSphere(transform.position, HearingDistance);

            // If investigating, draw a line to the noise source
            if (_isInvestigating && Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, _lastHeardPosition);
                Gizmos.DrawSphere(_lastHeardPosition, 0.3f);
            }
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        // Vision Gizmos
        Gizmos.color = Color.red;
        Vector3 forward = transform.forward * VisionDistance;
        Vector3 leftBoundary = Quaternion.Euler(0, -VisionAngle / 2, 0) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, VisionAngle / 2, 0) * forward;

        // Draw main vision cone
        Gizmos.DrawRay(transform.position, forward);
        Gizmos.DrawRay(transform.position, leftBoundary);
        Gizmos.DrawRay(transform.position, rightBoundary);

        // Draw peripheral vision (lighter color)
        if (PeripheralVisionAngle > VisionAngle)
        {
            Gizmos.color = new Color(1f, 0.5f, 0.5f, 0.3f); // Light red with transparency
            Vector3 leftPeripheral = Quaternion.Euler(0, -PeripheralVisionAngle / 2, 0) * forward;
            Vector3 rightPeripheral = Quaternion.Euler(0, PeripheralVisionAngle / 2, 0) * forward;
            Gizmos.DrawRay(transform.position, leftPeripheral);
            Gizmos.DrawRay(transform.position, rightPeripheral);
        }

        // Draw detection status in game
        if (Application.isPlaying)
        {
            // Different colors based on alert level
            Color alertColor = Color.green;

            switch (CurrentAlertLevel)
            {
                case AlertLevel.Normal:
                    alertColor = Color.green;
                    break;
                case AlertLevel.Suspicious:
                    alertColor = Color.yellow;
                    break;
                case AlertLevel.Alert:
                    alertColor = new Color(1f, 0.5f, 0f); // Orange
                    break;
                case AlertLevel.Confirmed:
                    alertColor = Color.red;
                    break;
            }

            Gizmos.color = alertColor;
            Gizmos.DrawSphere(transform.position + Vector3.up * 2.5f, 0.3f);

            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 3,
                $"Detection: {DetectionMeter * 100:F0}%"
            );
        }

        // Hearing Gizmos when selected
        Gizmos.color = new Color(0, 0.5f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, HearingDistance);

        if (Application.isPlaying && _currentNoiseLevel > 0)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2,
                $"Noise: {_currentNoiseLevel:F1}/{_hearingThreshold:F1}"
            );
        }
    }

    private void CheckVision()
    {
        if (!CanSee || Player == null)
            return;

<<<<<<< Updated upstream
=======
        if (Time.frameCount % (int)(VisibilityCheckInterval * 30) != 0)
            return; // Only check periodically for performance

>>>>>>> Stashed changes
        Vector3 directionToPlayer = (Player.transform.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        float distanceToPlayer = Vector3.Distance(transform.position, Player.transform.position);

<<<<<<< Updated upstream
        // Check if player is within maximum detection range
        if (distanceToPlayer <= VisionDistance)
        {
            float visionMultiplier = 1.0f;

            // Check if in main vision cone or peripheral vision
            if (angleToPlayer <= VisionAngle / 2)
            {
                // In main vision cone - full detection
                visionMultiplier = 1.0f;

                // Boost detection rate further if player is directly in front
                if (angleToPlayer <= 15f)
                {
                    visionMultiplier = 1.5f;
                }
            }
            else if (angleToPlayer <= PeripheralVisionAngle / 2)
            {
                // In peripheral vision - reduced detection
                visionMultiplier = PeripheralVisionMultiplier;
            }
            else
            {
                // Outside vision cones, reduce detection meter
                DetectionMeter = Mathf.Max(
                    0,
                    DetectionMeter - (Time.deltaTime * DetectionDecayRate)
                );
                UpdateAlertLevel();
                return;
            }

            // Line of sight check
            RaycastHit hit;
            if (
                Physics.Raycast(
                    transform.position + Vector3.up * 0.5f, // Eye level
                    directionToPlayer,
                    out hit,
                    VisionDistance,
                    VisionBlockingLayers
=======
        if (angleToPlayer < VisionAngle / 2 && distanceToPlayer <= VisionDistance)
        {
            RaycastHit hit;
            if (
                Physics.Raycast(
                    transform.position + Vector3.up * 0.5f,
                    directionToPlayer,
                    out hit,
                    VisionDistance
>>>>>>> Stashed changes
                )
            )
            {
                if (hit.collider.GetComponent<Player>() != null)
                {
<<<<<<< Updated upstream
                    // Calculate detection rate based on factors
                    float detectionRate = CalculateDetectionRate() * visionMultiplier;

                    // Direct spot - immediate partial detection when player is very close and directly visible
                    if (distanceToPlayer < VisionDistance * 0.3f && angleToPlayer < 20f)
                    {
                        // Give an immediate boost to detection when player is very close and visible
                        DetectionMeter = Mathf.Max(DetectionMeter, 0.5f);
                    }

                    // Increase detection meter based on factors
                    DetectionMeter += detectionRate * Time.deltaTime;
                    DetectionMeter = Mathf.Clamp01(DetectionMeter);

                    // Update alert level based on detection meter
                    UpdateAlertLevel();

                    // If detection is complete, spot the player
                    if (DetectionMeter >= 1.0f && !IsPlayerSpotted)
                    {
                        StartCoroutine(OnPlayerSpotted());
                    }

                    // Update last visible time since we can see the player
                    LastPlayerVisibleTime = Time.time;
                    return;
=======
                    // If player wasn't spotted before, trigger the spotted event
                    if (!IsPlayerSpotted)
                    {
                        StartCoroutine(OnPlayerSpotted());
                    }

                    // Update last visible time since we can see the player
                    LastPlayerVisibleTime = Time.time;
>>>>>>> Stashed changes
                }
            }
        }

        // Player not in sight or blocked by obstacle, gradually decrease detection
        DetectionMeter = Mathf.Max(0, DetectionMeter - (Time.deltaTime * DetectionDecayRate));
        UpdateAlertLevel();
    }

    // Check visibility of already spotted player
    private void CheckPlayerVisibility()
    {
        if (IsPlayerVisible())
        {
            // Update last visible time since we can still see the player
            LastPlayerVisibleTime = Time.time;
            DetectionMeter = 1.0f; // Keep detection full while actively chasing
        }
        else
        {
            // Player temporarily out of sight during chase
            // The timeout in Update will eventually trigger LosePlayerVisibility
            DetectionMeter = Mathf.Max(
                0.75f,
                DetectionMeter - (Time.deltaTime * DetectionDecayRate * 0.5f)
            );
        }
        UpdateAlertLevel();
    }

    // Calculate how quickly the player is detected based on various factors
    protected float CalculateDetectionRate()
    {
        float baseDetectionRate = 1.0f / DetectionTime;
        float distanceModifier = 1.0f;
        float movementModifier = 1.0f;
        float lightModifier = 1.0f;
        float noiseModifier = 1.0f;
        float directVisibilityModifier = 1.0f;

        // Distance factor - closer is detected faster
        float distanceToPlayer = Vector3.Distance(transform.position, Player.transform.position);
        distanceModifier = 1.0f - (distanceToPlayer / VisionDistance) * 0.6f;

        // Movement factor - check if player is moving
        Rigidbody playerRB = Player.GetComponent<Rigidbody>();
        if (playerRB != null)
        {
            float playerSpeed = playerRB.linearVelocity.magnitude;
            // Exponential increase in detection based on speed
            movementModifier =
                1.0f + Mathf.Pow(playerSpeed * 0.25f, 1.5f) * MovementDetectionMultiplier;
        }

        // Light factor - check ambient light or if player is in shadow
        // Check if the player has a light component we can use to determine light level
        Light[] playerLights = Player.GetComponentsInChildren<Light>();
        if (playerLights.Length > 0)
        {
            // Player has a light source - easier to spot
            lightModifier = LightDetectionMultiplier * 1.5f;
        }
        else
        {
            // Get render settings ambient light as a fallback
            lightModifier = LightDetectionMultiplier;
        }

        // Noise factor - higher noise makes detection easier
        if (Player != null)
        {
            float playerNoiseLevel = Player._noiseLevel;
            noiseModifier = 1.0f + (playerNoiseLevel * 0.1f); // Adjust the multiplier as needed

            // If player is very quiet (below 2.0f), make them harder to detect
            if (playerNoiseLevel < 2.0f)
            {
                noiseModifier *= 0.25f;
            }
        }

        // Direct visibility - if player is directly in front and close, significantly boost detection
        Vector3 directionToPlayer = (Player.transform.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        // Very direct line of sight - facing the player directly
        if (angleToPlayer < 15f)
        {
            directVisibilityModifier = 2.0f; // Double detection rate when looking directly at player
        }
        // Looking at player clearly within main vision cone
        else if (angleToPlayer < VisionAngle / 3f)
        {
            directVisibilityModifier = 1.5f;
        }

        // Even more immediate detection when very close and in direct sight
        if (distanceToPlayer < VisionDistance * 0.3f && angleToPlayer < 30f)
        {
            directVisibilityModifier *= 1.5f;
        }

        // Combine all modifiers
        return baseDetectionRate
            * distanceModifier
            * movementModifier
            * lightModifier
            * noiseModifier
            * directVisibilityModifier;
    }

    private void CheckHearing()
    {
        if (CanHear)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, HearingDistance);
            foreach (Collider collider in colliders)
            {
                if (collider.GetComponent<Player>() != null)
                {
                    RaycastHit hit;
                    if (
                        Physics.SphereCast(
                            transform.position,
                            0.5f,
                            collider.transform.position - transform.position,
                            out hit,
                            HearingDistance
                        )
                    )
                    {
                        if (hit.collider.GetComponent<Player>() != null)
                        {
                            OnNoiseHeard(collider.transform.position, _hearingThreshold + 1f);
                        }
                    }
                }
            }
        }
    }

    public void OnNoiseHeard(Vector3 noisePosition, float noiseLevel)
    {
        if (!CanHear)
            return;

        float distanceToNoise = Vector3.Distance(transform.position, noisePosition);

        // Only process if within hearing range
        if (distanceToNoise <= HearingDistance)
        {
            // Calculate distance falloff - noise gets weaker with distance
            float distanceFactor = 1f - (distanceToNoise / HearingDistance);
            float adjustedNoiseLevel = noiseLevel * distanceFactor;

            _currentNoiseLevel = adjustedNoiseLevel;

            if (adjustedNoiseLevel > _hearingThreshold)
            {
                _isInvestigating = true;
                _lastHeardPosition = noisePosition;
                _investigationTimeRemaining = _investigationTime;
                _currentState = EnemyState.Investigating;

                StartCoroutine(OnSoundHeard(noisePosition));

                Debug.Log($"Enemy heard noise of level {adjustedNoiseLevel} at {noisePosition}");
            }
        }
    }

    // Public method to start wandering
    public void StartWandering(Vector3 confinedAreaCenter, float radius)
    {
        StopWandering(); // Stop any existing wandering
        _currentState = EnemyState.Wandering;
        _wanderCoroutine = StartCoroutine(WanderCoroutine(WanderTime, confinedAreaCenter, radius));
    }

    // Public method to stop wandering
    public void StopWandering()
    {
        if (_wanderCoroutine != null)
        {
            StopCoroutine(_wanderCoroutine);
            _wanderCoroutine = null;
        }

        _isWandering = false;

        // Only reset state if we were wandering
        if (_currentState == EnemyState.Wandering)
        {
            _currentState = EnemyState.Patrolling;
        }
    }

    // Coroutine for wandering behavior
    private IEnumerator WanderCoroutine(float wanderTime, Vector3 confinedAreaCenter, float radius)
    {
        _isWandering = true;
        float wanderCounter = 0f;
        float nextWanderTime = 0f;

        while (wanderCounter < wanderTime)
        {
            wanderCounter += Time.deltaTime;

            // Only pick a new destination every few seconds
            if (Time.time >= nextWanderTime)
            {
                Vector3 randomPoint =
                    confinedAreaCenter
                    + new Vector3(Random.Range(-radius, radius), 0, Random.Range(-radius, radius));

                // Make sure the point is on the NavMesh
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomPoint, out hit, radius, NavMesh.AllAreas))
                {
                    Agent.SetDestination(hit.position);
                }

                // Set the time for the next destination change
                nextWanderTime = Time.time + Random.Range(1.5f, 3.5f);
            }

            // Wait until we reach the destination or get close enough
            if (!Agent.pathPending && Agent.remainingDistance < 0.5f)
            {
                // If we've reached our destination, wait a moment before moving again
                yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
                nextWanderTime = 0; // Force picking a new destination
            }

            yield return null;
        }

        // Wandering complete
        _isWandering = false;
        _currentState = EnemyState.Patrolling;
        _wanderCoroutine = null;
    }

    // Legacy method for compatibility - starts the coroutine version
    public void Wander(float wanderTime, Vector3 confinedAreaCenter, float radius)
    {
        StartWandering(confinedAreaCenter, radius);
    }

    protected virtual IEnumerator OnPlayerSpotted()
    {
        Debug.Log("Player spotted! " + gameObject.name);
        IsPlayerSpotted = true;
        _currentState = EnemyState.Chasing;
        LastPlayerVisibleTime = Time.time; // Initialize the last visible time
        yield return null;
    }

    protected virtual IEnumerator OnSoundHeard(Vector3 soundPosition)
    {
        Debug.Log("Sound heard! " + gameObject.name);
        Agent.SetDestination(soundPosition);

        // Wait until we're close to the investigation point
        while (Vector3.Distance(transform.position, soundPosition) > 0.5f && _isInvestigating)
        {
            // Keep updating destination in case player moves
            if (_currentState == EnemyState.Chasing && Player != null)
            {
                break; // Exit if we've switched to chasing
            }

            yield return null;
        }

        // Look around once we reach the point (only if still investigating)
        if (_isInvestigating && _currentState == EnemyState.Investigating)
        {
            // Look around by rotating
            float originalRotation = transform.eulerAngles.y;
            float timer = 0;
            while (timer < 2.0f && _isInvestigating)
            {
                transform.rotation = Quaternion.Euler(
                    0,
                    originalRotation + Mathf.Sin(timer * 3) * 90,
                    0
                );
                timer += Time.deltaTime;
                yield return null;
            }

            // Reset rotation
            transform.rotation = Quaternion.Euler(0, originalRotation, 0);
        }
    }

    // Check if the player is currently visible to the enemy - improved version
    protected virtual bool IsPlayerVisible()
    {
        if (!CanSee || Player == null)
            return false;

        Vector3 directionToPlayer = (Player.transform.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        float distanceToPlayer = Vector3.Distance(transform.position, Player.transform.position);

<<<<<<< Updated upstream
        // Within vision angle and distance
        if (angleToPlayer <= VisionAngle / 2 && distanceToPlayer <= VisionDistance)
        {
            RaycastHit hit;
            // Cast from eye level (a bit above the origin point)
=======
        if (angleToPlayer < VisionAngle / 2 && distanceToPlayer <= VisionDistance)
        {
            RaycastHit hit;
            // Offset raycast start position slightly up to avoid terrain issues
>>>>>>> Stashed changes
            if (
                Physics.Raycast(
                    transform.position + Vector3.up * 0.5f,
                    directionToPlayer,
                    out hit,
<<<<<<< Updated upstream
                    VisionDistance,
                    VisionBlockingLayers
=======
                    VisionDistance
>>>>>>> Stashed changes
                )
            )
            {
                if (hit.collider.GetComponent<Player>() != null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Called when player visibility is lost
    protected virtual void LosePlayerVisibility()
    {
        if (_currentState == EnemyState.Chasing)
        {
            Debug.Log(gameObject.name + " lost sight of player");

            // Only reset IsPlayerSpotted if detection is very low
            if (DetectionMeter < 0.1f)
            {
                IsPlayerSpotted = false;
            }

            // Investigate the player's last known position
            _lastHeardPosition = Player.transform.position;
            _isInvestigating = true;
            _investigationTimeRemaining = _investigationTime;
            _currentState = EnemyState.Investigating;

<<<<<<< Updated upstream
            // Start wandering at player's last known position
            StartWandering(Player.transform.position, 5f);

            // Gradually reduce detection
            StartCoroutine(GraduallyReduceDetection());
        }
    }

    private IEnumerator GraduallyReduceDetection()
    {
        float initialDetection = DetectionMeter;
        float elapsed = 0f;
        float duration = 3f; // Time to completely forget

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            DetectionMeter = Mathf.Lerp(initialDetection, 0f, elapsed / duration);
            UpdateAlertLevel();
            yield return null;
        }

        DetectionMeter = 0f;
        UpdateAlertLevel();
    }

    // Make this accessible to derived classes
    protected void UpdateAlertLevel()
    {
        if (DetectionMeter >= 1.0f)
        {
            CurrentAlertLevel = AlertLevel.Confirmed;
        }
        else if (DetectionMeter >= 0.75f)
        {
            CurrentAlertLevel = AlertLevel.Alert;
        }
        else if (DetectionMeter >= 0.5f)
        {
            CurrentAlertLevel = AlertLevel.Suspicious;
        }
        else
        {
            CurrentAlertLevel = AlertLevel.Normal;
=======
            // Ensure we move to the last known position
            Agent.SetDestination(_lastHeardPosition);

            // Start wandering at player's last known position after reaching the location
            StartCoroutine(StartWanderingAfterReachingPosition());
>>>>>>> Stashed changes
        }
    }

    private IEnumerator StartWanderingAfterReachingPosition()
    {
        // Wait until we're close to the investigation point
        while (Vector3.Distance(transform.position, _lastHeardPosition) > 1.5f)
        {
            yield return null;
        }

        // Start wandering around the area
        StartWandering(_lastHeardPosition, 5f);
    }
}
