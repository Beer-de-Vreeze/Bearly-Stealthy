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

    [SerializeField]
    protected float PlayerVisibilityTimeout = 3f;

    [SerializeField, ReadOnly]
    protected float LastPlayerVisibleTime = 0f;

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

    [SerializeField]
    private BoxCollider hitTrigger;

    [SerializeField]
    private bool _detectPlayerWithTrigger = false; // Added flag to control trigger detection

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

        // Make sure the hitTrigger is set as a trigger
        if (hitTrigger != null)
        {
            hitTrigger.isTrigger = true;
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
                Debug.Log("Player lost sight of enemy: " + gameObject.name);
            }
            else
            {
                // Check visibility periodically instead of every frame
                if (Time.frameCount % (int)(VisibilityCheckInterval * 60) == 0)
                {
                    if (!IsPlayerVisible())
                    {
                        // Player not visible, but we'll wait for timeout before giving up
                        // LastPlayerVisibleTime remains unchanged
                    }
                    else
                    {
                        // Update last visible time since we can still see the player
                        LastPlayerVisibleTime = Time.time;
                    }
                }
            }
        }

        CheckVision();
        // Remove excessive debug log
        CheckHearing();

        if (_isInvestigating)
        {
            _investigationTimeRemaining -= Time.deltaTime;
            if (_investigationTimeRemaining <= 0)
            {
                _isInvestigating = false;
                _currentState = EnemyState.Patrolling;
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
                // Chase player
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
        Gizmos.DrawRay(transform.position, forward);
        Gizmos.DrawRay(transform.position, leftBoundary);
        Gizmos.DrawRay(transform.position, rightBoundary);

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
        if (CanSee)
        {
            Vector3 directionToPlayer = (Player.transform.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

            if (
                angleToPlayer < VisionAngle / 2
                && Vector3.Distance(transform.position, Player.transform.position) <= VisionDistance
            )
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, directionToPlayer, out hit, VisionDistance))
                {
                    if (hit.collider.GetComponent<Player>() != null)
                    {
                        // Player is visible
                        if (!IsPlayerSpotted)
                        {
                            StartCoroutine(OnPlayerSpotted());
                            Debug.Log("Player spotted by " + gameObject.name);
                        }

                        // Update last visible time since we can see the player
                        LastPlayerVisibleTime = Time.time;
                        return; // Exit after spotting the player
                    }
                }
            }

            // If we reach here, player is not currently visible
            if (IsPlayerSpotted && _currentState == EnemyState.Chasing)
            {
                // Only handle visibility loss if sufficient time has passed since last seeing the player
                if (Time.time - LastPlayerVisibleTime > PlayerVisibilityTimeout)
                {
                    LosePlayerVisibility();
                }
            }
        }
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

    // Check if the player is currently visible to the enemy
    protected virtual bool IsPlayerVisible()
    {
        if (!CanSee || Player == null)
            return false;

        Vector3 directionToPlayer = (Player.transform.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        if (
            angleToPlayer < VisionAngle / 2
            && Vector3.Distance(transform.position, Player.transform.position) <= VisionDistance
        )
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, directionToPlayer, out hit, VisionDistance))
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
            IsPlayerSpotted = false;

            // Investigate the player's last known position
            _lastHeardPosition = Player.transform.position;
            _isInvestigating = true;
            _investigationTimeRemaining = _investigationTime;
            _currentState = EnemyState.Investigating;

            // Start wandering at player's last known position
            StartWandering(Player.transform.position, 5f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_detectPlayerWithTrigger && other.CompareTag("Player"))
        {
            // Only detect player if the flag is enabled
            AlertEnemy(other.transform);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (_detectPlayerWithTrigger && other.CompareTag("Player"))
        {
            // Only track player if the flag is enabled
            TrackTarget(other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_detectPlayerWithTrigger && other.CompareTag("Player"))
        {
            // Only handle player exit if the flag is enabled
            LoseTarget();
        }
    }

    private void AlertEnemy(Transform target)
    {
        // Alert logic
        // ...existing code...
    }

    private void TrackTarget(Transform target)
    {
        // Tracking logic
        // ...existing code...
    }

    private void LoseTarget()
    {
        // Logic for when player is no longer detected
        // ...existing code...
    }
}
