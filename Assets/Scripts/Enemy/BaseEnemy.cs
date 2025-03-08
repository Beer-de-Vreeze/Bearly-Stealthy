using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using VInspector;

/// <summary>
/// Base class for all enemy types that handles core enemy behaviors and states
/// </summary>
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
    public bool _isWandering = false;

    [SerializeField, ReadOnly]
    private Coroutine _wanderCoroutine = null;

    [Tab("Vision")]
    [SerializeField]
    protected bool CanSee = true;

    [SerializeField]
    protected float VisionDistance = 10f;

    [SerializeField]
    protected float VisionAngle = 45f;

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
    private float _investigationTime = 10f;

    [SerializeField, ReadOnly]
    private bool _isInvestigating = false;

    [SerializeField, ReadOnly]
    private Vector3 _lastHeardPosition;

    [SerializeField, ReadOnly]
    private float _investigationTimeRemaining = 0f;

    [SerializeField, ReadOnly]
    private float _currentNoiseLevel = 0f;

    [Tab("Debug")]
    [SerializeField]
    protected bool _showStateDebug = false;

    [SerializeField]
    protected bool _PerceptionUpdate = true;

    [SerializeField]
    private float _perceptionUpdateRate = 0.25f;

    private float _lastPerceptionCheck = 0f;


    private EnemyState _previousState;
    private float _stateChangeTime;

    /// <summary>
    /// Available states for the enemy AI state machine
    /// </summary>
    protected enum EnemyState
    {
        Patrolling,
        Investigating,
        Chasing,
        Wandering,
        RunningAway,
        Protecting,
        Hiding,
    }

    [SerializeField, ReadOnly]
    protected EnemyState _currentState = EnemyState.Patrolling;


    #region Unity Lifecycle Methods
    protected virtual void Start()
    {
        Agent = GetComponent<NavMeshAgent>();
        RB = GetComponent<Rigidbody>();
        RB.isKinematic = true;
        Agent.speed = Speed;
        Player = FindFirstObjectByType<Player>();

        // Set initial state
        _currentState = EnemyState.Patrolling;

        // Register with NoiseManager
        if (NoiseManager.Instance != null)
        {
            NoiseManager.Instance.RegisterEnemy(this);
        }
    }

    protected virtual void OnDestroy()
    {
        if (NoiseManager.Instance != null)
        {
            NoiseManager.Instance.UnregisterEnemy(this);
        }
    }

    protected virtual void Update()
    {
        // Handle perception checks
        UpdatePerception();

        // Process investigation timer
        UpdateInvestigation();

        // State machine logic
        UpdateStateMachine();
    }

    /// <summary>
    /// Handles perception checks (vision and hearing) based on configuration
    /// </summary>
    private void UpdatePerception()
    {
        if (_PerceptionUpdate)
        {
            if (Time.time > _lastPerceptionCheck + _perceptionUpdateRate)
            {
                _lastPerceptionCheck = Time.time;
                CheckVision();
                CheckHearing();
            }
        }
        else
        {
            CheckVision();
            CheckHearing();
        }
    }

    /// <summary>
    /// Updates the investigation timer and state transitions
    /// </summary>
    private void UpdateInvestigation()
    {
        if (_isInvestigating)
        {
            _investigationTimeRemaining -= Time.deltaTime;
            if (_investigationTimeRemaining <= 0)
            {
                _isInvestigating = false;

                // Transition to patrolling state when investigation is complete
                if (_currentState == EnemyState.Investigating)
                {
                    SetState(EnemyState.Patrolling);
                    Debug.Log($"{gameObject.name} finished investigating, returning to patrol");
                }
            }
        }
    }

    /// <summary>
    /// Updates the enemy behavior based on the current state
    /// </summary>
    private void UpdateStateMachine()
    {
        switch (_currentState)
        {
            case EnemyState.Patrolling:
                Patrol();
                break;
            case EnemyState.Investigating:
                Investigate();
                break;
            case EnemyState.Chasing:
                Chase();
                break;
            case EnemyState.Wandering:
                Wander(WanderTime, transform.position, 5f);
                break;
        }
    }
    #endregion

    #region State Management
    /// <summary>
    /// Changes the enemy state and handles related events and debugging
    /// </summary>
    /// <param name="newState">The new state to transition to</param>
    protected virtual void SetState(EnemyState newState)
    {
        if (_currentState != newState)
        {
            _previousState = _currentState;
            _currentState = newState;
            _stateChangeTime = Time.time;

            if (_showStateDebug)
            {
                Debug.Log(
                    $"{gameObject.name} state changed from {_previousState} to {_currentState}"
                );
            }

            // Reset agent path when changing states
            if (Agent != null && Agent.isOnNavMesh)
            {
                Agent.ResetPath();
            }
        }
    }
    #endregion

    #region Behavior Methods
    /// <summary>
    /// Handles patrol behavior - to be implemented by derived classes
    /// </summary>
    protected virtual void Patrol() { }

    /// <summary>
    /// Handles investigation behavior - moves to last heard position
    /// </summary>
    protected virtual void Investigate()
    {
        Agent.SetDestination(_lastHeardPosition);
    }

    /// <summary>
    /// Handles chase behavior - pursues the player
    /// </summary>
    protected virtual void Chase()
    {
        if (Player != null)
        {
            Agent.SetDestination(Player.transform.position);
        }
    }
    #endregion

    #region Gizmo Visualization
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

    /// <summary>
    /// Draws debug gizmos when the object is selected
    /// </summary>
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

        if (Application.isPlaying)
        {
            // Show noise level
            if (_currentNoiseLevel > 0)
            {
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 2,
                    $"Noise: {_currentNoiseLevel:F1}/{_hearingThreshold:F1}"
                );
            }

            // Show current state
            if (_showStateDebug)
            {
                string stateInfo = $"State: {_currentState}";
                if (Time.time - _stateChangeTime < 3f)
                {
                    stateInfo += $" (was {_previousState})";
                }
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, stateInfo);
            }
        }
    }
    #endregion

    #region Perception Methods
    /// <summary>
    /// Checks if the enemy can see the player
    /// </summary>
    private void CheckVision()
    {
        if (!CanSee || Player == null)
            return;

        Vector3 directionToPlayer = (Player.transform.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        float distanceToPlayer = Vector3.Distance(transform.position, Player.transform.position);

        if (angleToPlayer < VisionAngle / 2 && distanceToPlayer <= VisionDistance)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, directionToPlayer, out hit, VisionDistance))
            {
                if (hit.collider.GetComponent<Player>() != null && !IsPlayerSpotted)
                {
                    StartCoroutine(OnPlayerSpotted());
                }
            }
        }
    }

    /// <summary>
    /// Checks if the enemy can hear nearby objects, especially the player
    /// </summary>
    private void CheckHearing()
    {
        if (CanHear)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, HearingDistance);
            foreach (Collider collider in colliders)
            {
                if (collider.GetComponent<Player>() != null && !IsPlayerSpotted)
                {
                    StartCoroutine(OnPlayerSpotted());
                }
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
    #endregion

    #region Event Handlers
    /// <summary>
    /// Handles noise events detected by the enemy
    /// </summary>
    /// <param name="noisePosition">Position of the noise source</param>
    /// <param name="noiseLevel">Intensity level of the noise</param>
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

                // Only change to investigating if not currently chasing
                if (_currentState != EnemyState.Chasing)
                {
                    SetState(EnemyState.Investigating);
                    StartCoroutine(OnSoundHeard(noisePosition));
                }

                Debug.Log($"Enemy heard noise of level {adjustedNoiseLevel} at {noisePosition}");
            }
        }
    }

    /// <summary>
    /// Handles the event when the player is spotted
    /// </summary>
    protected virtual IEnumerator OnPlayerSpotted()
    {
        Debug.Log("Player spotted! " + gameObject.name);
        IsPlayerSpotted = true;
        SetState(EnemyState.Chasing);
        yield return null;
    }

    /// <summary>
    /// Handles the event when a sound is heard
    /// </summary>
    /// <param name="soundPosition">Position where the sound originated</param>
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
    #endregion

    #region Wandering Methods
    /// <summary>
    /// Starts the wandering behavior in a confined area
    /// </summary>
    /// <param name="confinedAreaCenter">Center point of the wandering area</param>
    /// <param name="radius">Radius of the wandering area</param>
    public void StartWandering(Vector3 confinedAreaCenter, float radius)
    {
        StopWandering(); // Stop any existing wandering
        SetState(EnemyState.Wandering);
        _wanderCoroutine = StartCoroutine(WanderCoroutine(WanderTime, confinedAreaCenter, radius));
    }

    /// <summary>
    /// Stops the current wandering behavior
    /// </summary>
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

    /// <summary>
    /// Coroutine that handles the wandering behavior
    /// </summary>
    /// <param name="wanderTime">Total time to wander</param>
    /// <param name="confinedAreaCenter">Center of the wander area</param>
    /// <param name="radius">Radius of the wander area</param>
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

    /// <summary>
    /// Legacy method for compatibility - starts the coroutine version
    /// </summary>
    public void Wander(float wanderTime, Vector3 confinedAreaCenter, float radius)
    {
        StartWandering(confinedAreaCenter, radius);
    }
    #endregion
}
