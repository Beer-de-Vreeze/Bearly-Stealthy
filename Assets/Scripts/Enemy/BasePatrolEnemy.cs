using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using VInspector;

/// <summary>
/// Represents a patrolling enemy (Ranger) that follows defined patrol points and can protect citizens.
/// Rangers are more vigilant enemies that respond to alerts and search for intruders.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class BasePatrolEnemy : BaseEnemy
{
    #region Patrol System Variables
    [Tab("Patrol")]
    [SerializeField]
    private Transform[] _patrolPoints;

    [SerializeField]
    private float _waitTime = 2f;

    [SerializeField]
    private float _startWaitTime = 2f;

    [SerializeField]
    private bool _patrolLoop = true;

    [SerializeField]
    private bool _showGizmos = true;

    private int _currentPointIndex = 0;
    private bool _isReversing = false;
    private float _waitCounter;
    #endregion

    #region Alert System Variables
    [Tab("Alert System")]
    [SerializeField]
    private bool _canAlert = true;

    [SerializeField]
    private float _alertRadius = 15f;

    [SerializeField]
    private float _alertCooldown = 8f;

    [SerializeField, ReadOnly]
    private float _lastAlertTime = -10f;

    [SerializeField]
    private bool _showAlertRadius = true;

    private List<BaseEnemy> _alertedEnemies = new List<BaseEnemy>();
    #endregion

    #region Protection System Variables
    [Tab("Protection")]
    [SerializeField]
    private bool _protectCitizens = true;

    [SerializeField]
    private float _protectionRadius = 10f;

    [SerializeField]
    private float _escortSpeed = 3f;

    [SerializeField]
    private bool _showProtectionRadius = true;

    [SerializeField, ReadOnly]
    private BaseWanderingEnemy _citizenToProtect;

    [SerializeField]
    private float _searchDuration = 30f;

    [SerializeField]
    private Color _protectionGizmoColor = new Color(0f, 0.7f, 0f, 0.3f);

    private Vector3 _lastKnownPlayerPosition;
    private bool _isProtectingCitizen = false;
    private bool _isSearching = false;
    #endregion

    #region Suspicion System Variables
    [Tab("Suspicion")]
    [SerializeField]
    private float _maxSuspicion = 10f;

    [SerializeField]
    private float _suspicionIncreaseRate = 2.5f;

    [SerializeField]
    private float _suspicionDecayRate = 0.5f;

    [SerializeField, ReadOnly]
    private float _currentSuspicion = 0f;

    [SerializeField]
    private float _suspiciousThreshold = 5f;

    [SerializeField]
    private Color _suspicionGizmoColor = new Color(1f, 1f, 0f, 0.6f);

    private bool _isSuspicious = false;
    #endregion

    #region Initialization and Updates
    protected override void Start()
    {
        base.Start();
        if (_patrolPoints.Length > 0)
        {
            Agent.SetDestination(_patrolPoints[_currentPointIndex].position);
        }
        _waitCounter = _startWaitTime;
    }

    protected override void Update()
    {
        base.Update();

        // Handle suspicion decay
        UpdateSuspicionState();
    }

    /// <summary>
    /// Updates the suspicion level and related behavior
    /// </summary>
    private void UpdateSuspicionState()
    {
        // Handle suspicion decay
        if (_currentSuspicion > 0 && !IsPlayerSpotted)
        {
            _currentSuspicion -= _suspicionDecayRate * Time.deltaTime;
            if (_currentSuspicion <= 0)
            {
                _currentSuspicion = 0;
                _isSuspicious = false;
            }
        }

        // Display suspicion state if suspicious but not fully alerted
        if (_isSuspicious && !IsPlayerSpotted)
        {
            // Look around more actively when suspicious
            transform.Rotate(0, Time.deltaTime * 45f * (_currentSuspicion / _maxSuspicion), 0);
        }
    }

    void Reset()
    {
        // Component setup
        RB = GetComponent<Rigidbody>();
        Agent = GetComponent<NavMeshAgent>();

        // Configure NavMeshAgent
        if (Agent != null)
        {
            Agent.speed = Speed;
            Agent.angularSpeed = 120f;
            Agent.acceleration = 8f;
            Agent.stoppingDistance = 0.1f;
        }

        // State machine initialization
        _currentState = EnemyState.Patrolling;
        _waitCounter = _startWaitTime;
        _currentPointIndex = 0;
        IsPlayerSpotted = false;
        _isWandering = false;

        SetupPatrolPointsHierarchy();
    }

    /// <summary>
    /// Sets up the patrol points organization hierarchy
    /// </summary>
    private void SetupPatrolPointsHierarchy()
    {
        // Setup patrol points hierarchy
        GameObject globalPatrolPointsObject = GameObject.Find("GlobalPatrolPoints");
        if (globalPatrolPointsObject == null)
        {
            globalPatrolPointsObject = new GameObject("GlobalPatrolPoints");
        }

        // Create a child parent for this enemy under the global parent
        string enemyParentName = gameObject.name + "Patrol";
        GameObject enemyParentObject = GameObject.Find(enemyParentName);
        if (enemyParentObject == null)
        {
            enemyParentObject = new GameObject(enemyParentName);
            enemyParentObject.transform.SetParent(globalPatrolPointsObject.transform);
        }

        // Create a separate GameObject for this enemy's PatrolPoints
        string patrolPointsName = gameObject.name + "PatrolPoints";
        GameObject patrolPointsObject = GameObject.Find(patrolPointsName);
        if (patrolPointsObject == null)
        {
            patrolPointsObject = new GameObject(patrolPointsName);
            patrolPointsObject.transform.SetParent(enemyParentObject.transform);
        }

        PatrolPointsParent = patrolPointsObject.transform;
    }
    #endregion

    #region Patrol Behavior
    protected override void Patrol()
    {
        if (_patrolPoints == null || _patrolPoints.Length == 0)
        {
            Debug.LogWarning("No patrol points set for " + gameObject.name);
            return;
        }

        if (_patrolLoop)
        {
            PatrolLoop();
        }
        else
        {
            PatrolBackAndForth();
        }

        if (_waitCounter <= 0)
        {
            _waitCounter = _waitTime;
        }
        else
        {
            _waitCounter -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Patrols in a continuous loop through all patrol points
    /// </summary>
    private void PatrolLoop()
    {
        if (Agent.remainingDistance < 0.1f)
        {
            if (_waitCounter <= 0)
            {
                _currentPointIndex = (_currentPointIndex + 1) % _patrolPoints.Length;
                Agent.SetDestination(_patrolPoints[_currentPointIndex].position);
                _waitCounter = _waitTime;
            }
        }
    }

    /// <summary>
    /// Patrols back and forth between the first and last patrol points
    /// </summary>
    private void PatrolBackAndForth()
    {
        if (Agent.remainingDistance < 0.1f)
        {
            if (_waitCounter <= 0)
            {
                if (!_isReversing)
                {
                    _currentPointIndex++;
                    if (_currentPointIndex >= _patrolPoints.Length)
                    {
                        _currentPointIndex = _patrolPoints.Length - 2;
                        _isReversing = true;
                    }
                }
                else
                {
                    _currentPointIndex--;
                    if (_currentPointIndex < 0)
                    {
                        _currentPointIndex = 1;
                        _isReversing = false;
                    }
                }

                Agent.SetDestination(_patrolPoints[_currentPointIndex].position);
                _waitCounter = _waitTime;
            }
        }
    }

    /// <summary>
    /// Resets the patrol behavior to the current patrol point
    /// </summary>
    public void ResetPatrol()
    {
        // Stop any wandering when resetting patrol
        StopWandering();

        // Set state explicitly to patrolling using the enhanced method
        SetState(EnemyState.Patrolling);

        if (_patrolPoints != null && _patrolPoints.Length > 0)
        {
            Agent.SetDestination(_patrolPoints[_currentPointIndex].position);
            Debug.Log($"{gameObject.name} resetting to patrol state");
        }
    }
    #endregion

    #region Alert System
    /// <summary>
    /// Alerts nearby enemies of a potential threat
    /// </summary>
    private void AlertNearbyEnemies(Vector3 dangerPosition)
    {
        if (!_canAlert || Time.time - _lastAlertTime < _alertCooldown)
            return;

        _lastAlertTime = Time.time;
        _alertedEnemies.Clear();

        Collider[] colliders = Physics.OverlapSphere(transform.position, _alertRadius);
        foreach (Collider col in colliders)
        {
            BaseEnemy enemy = col.GetComponent<BaseEnemy>();
            if (enemy != null && enemy != this && !_alertedEnemies.Contains(enemy))
            {
                _alertedEnemies.Add(enemy);

                // Different alert handling for different enemy types
                if (enemy is BaseWanderingEnemy wanderingEnemy)
                {
                    wanderingEnemy.RespondToAlert(dangerPosition, 10f);
                }
                else
                {
                    enemy.OnNoiseHeard(dangerPosition, 10f);
                }
            }
        }

        if (_alertedEnemies.Count > 0)
        {
            Debug.Log($"{gameObject.name} alerted {_alertedEnemies.Count} nearby enemies!");
        }
    }

    /// <summary>
    /// Increases suspicion level when partial detection occurs
    /// </summary>
    public void IncreaseSuspicion(float amount)
    {
        _currentSuspicion = Mathf.Min(_currentSuspicion + amount, _maxSuspicion);

        if (_currentSuspicion >= _suspiciousThreshold && !_isSuspicious && !IsPlayerSpotted)
        {
            _isSuspicious = true;
            SetState(EnemyState.Investigating);
            Debug.Log($"{gameObject.name} is now suspicious!");
        }

        if (_currentSuspicion >= _maxSuspicion && !IsPlayerSpotted)
        {
            // Full detection
            StartCoroutine(OnPlayerSpotted());
        }
    }
    #endregion

    #region Protection Behavior
    /// <summary>
    /// Initiates protection of a citizen
    /// </summary>
    public void ProtectCitizen(BaseWanderingEnemy citizen)
    {
        if (!_protectCitizens || _isProtectingCitizen)
            return;

        _citizenToProtect = citizen;
        _isProtectingCitizen = true;
        SetState(EnemyState.Protecting);
        StartCoroutine(EscortCitizen());
    }

    /// <summary>
    /// Follows a citizen to provide protection
    /// </summary>
    public void FollowCitizen(GameObject citizen)
    {
        // Stop any current wandering when following a citizen
        StopWandering();
        Agent.SetDestination(citizen.transform.position);
    }

    /// <summary>
    /// Escorts a citizen away from danger
    /// </summary>
    private IEnumerator EscortCitizen()
    {
        if (_citizenToProtect == null)
        {
            _isProtectingCitizen = false;
            yield break;
        }

        Debug.Log($"{gameObject.name} is now protecting {_citizenToProtect.name}");

        // Store original speed and increase speed during escort
        float originalSpeed = Agent.speed;
        Agent.speed = _escortSpeed;

        while (_isProtectingCitizen && _citizenToProtect != null)
        {
            // Move to position slightly behind the citizen (relative to danger)
            Vector3 citizenPos = _citizenToProtect.transform.position;
            Vector3 dangerDirection = (_lastKnownPlayerPosition - citizenPos).normalized;
            Vector3 protectPosition = citizenPos - dangerDirection * 2f;

            Agent.SetDestination(protectPosition);

            // Check if we've reached a safe distance or max escort time has elapsed
            float distanceToDanger = Vector3.Distance(citizenPos, _lastKnownPlayerPosition);
            if (distanceToDanger > _protectionRadius * 2)
            {
                Debug.Log($"Citizen {_citizenToProtect.name} is now safe");
                break;
            }

            yield return null;
        }

        // Restore original speed
        Agent.speed = originalSpeed;
        _isProtectingCitizen = false;
        _citizenToProtect = null;

        // Start searching after escorting
        StartCoroutine(SearchForPlayer());
    }
    #endregion

    #region Detection and Response
    protected override IEnumerator OnPlayerSpotted()
    {
        // Record the player's position for search patterns
        _lastKnownPlayerPosition = Player.transform.position;

        // Stop any current wandering
        StopWandering();

        // Alert other enemies before starting chase
        AlertNearbyEnemies(Player.transform.position);

        yield return base.OnPlayerSpotted();

        IsPlayerSpotted = true;

        // Chase after the player if the player is spotted
        while (Vector3.Distance(transform.position, Player.transform.position) > 1f)
        {
            // Update last known position while in sight
            _lastKnownPlayerPosition = Player.transform.position;

            // Re-alert periodically during chase
            if (Time.time - _lastAlertTime > _alertCooldown)
            {
                AlertNearbyEnemies(Player.transform.position);
            }

            Agent.SetDestination(Player.transform.position);
            yield return null;
        }

        // If they lose the player, they will search the last known position
        Agent.SetDestination(_lastKnownPlayerPosition);

        if (Vector3.Distance(transform.position, _lastKnownPlayerPosition) < 1f)
        {
            // More thorough search behavior
            StartCoroutine(SearchForPlayer());
        }
    }

    protected override IEnumerator OnSoundHeard(Vector3 soundPosition)
    {
        // Stop any current wandering
        StopWandering();

        yield return base.OnSoundHeard(soundPosition);

        Agent.SetDestination(soundPosition);
        while (Vector3.Distance(transform.position, soundPosition) > 0.1f)
        {
            yield return null;
        }

        // Use new wandering system
        StartWandering(soundPosition, 5f);

        yield return new WaitForSeconds(WanderTime);
        ResetPatrol();
    }

    /// <summary>
    /// Performs a detailed search for the player around their last known position
    /// </summary>
    private IEnumerator SearchForPlayer()
    {
        _isSearching = true;
        SetState(EnemyState.Investigating);

        float searchTimeRemaining = _searchDuration;
        Vector3 searchCenter = _lastKnownPlayerPosition;

        // We'll do a more thorough search pattern around last known position
        while (searchTimeRemaining > 0 && !IsPlayerSpotted)
        {
            // Pick random points around the last known position
            Vector3 searchPoint =
                searchCenter
                + new Vector3(
                    UnityEngine.Random.Range(-_alertRadius / 2, _alertRadius / 2),
                    0,
                    UnityEngine.Random.Range(-_alertRadius / 2, _alertRadius / 2)
                );

            NavMeshHit hit;
            if (NavMesh.SamplePosition(searchPoint, out hit, _alertRadius, NavMesh.AllAreas))
            {
                Agent.SetDestination(hit.position);

                // Wait until arrived at the search point
                while (Agent.pathPending || Agent.remainingDistance > 1f)
                {
                    searchTimeRemaining -= Time.deltaTime;
                    if (searchTimeRemaining <= 0 || IsPlayerSpotted)
                        break;
                    yield return null;
                }

                // Look around at the search point
                float lookAroundTime = 2f;
                float timer = 0;
                float originalRotation = transform.eulerAngles.y;

                while (timer < lookAroundTime)
                {
                    transform.rotation = Quaternion.Euler(
                        0,
                        originalRotation + Mathf.Sin(timer * 3) * 120,
                        0
                    );
                    timer += Time.deltaTime;
                    searchTimeRemaining -= Time.deltaTime;
                    if (searchTimeRemaining <= 0 || IsPlayerSpotted)
                        break;
                    yield return null;
                }
            }

            if (searchTimeRemaining <= 0 || IsPlayerSpotted)
                break;
        }

        _isSearching = false;
        ResetPatrol();
    }
    #endregion

    #region Visualization
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        DrawAlertRadiusGizmos();
        DrawProtectionGizmos();
        DrawSuspicionGizmos();
        DrawSearchGizmos();
        DrawPatrolPathGizmos();
    }

    /// <summary>
    /// Draws the alert radius visualization
    /// </summary>
    private void DrawAlertRadiusGizmos()
    {
        if (_canAlert && _showAlertRadius)
        {
            Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.3f); // Red
            Gizmos.DrawWireSphere(transform.position, _alertRadius);

            if (_alertedEnemies != null && _alertedEnemies.Count > 0)
            {
                Gizmos.color = Color.red;
                foreach (var enemy in _alertedEnemies)
                {
                    if (enemy != null)
                    {
                        Gizmos.DrawLine(transform.position, enemy.transform.position);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Draws protection radius and connections
    /// </summary>
    private void DrawProtectionGizmos()
    {
        // Show protection radius
        if (_protectCitizens && _showProtectionRadius)
        {
            Gizmos.color = _protectionGizmoColor;
            Gizmos.DrawWireSphere(transform.position, _protectionRadius);

            // Draw line to citizen being protected
            if (_isProtectingCitizen && _citizenToProtect != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _citizenToProtect.transform.position);
            }
        }
    }

    /// <summary>
    /// Draws suspicion level visualization
    /// </summary>
    private void DrawSuspicionGizmos()
    {
        // Show suspicion level in play mode
        if (Application.isPlaying && _currentSuspicion > 0)
        {
            // Draw suspicion indicator
            float suspicionRatio = _currentSuspicion / _maxSuspicion;
            Vector3 suspicionBarPosition = transform.position + Vector3.up * 2.2f;
            Vector3 suspicionBarStart = suspicionBarPosition - Vector3.right * 1f;
            Vector3 suspicionBarEnd = suspicionBarStart + Vector3.right * 2f * suspicionRatio;

            Gizmos.color = Color.Lerp(Color.yellow, Color.red, suspicionRatio);
            Gizmos.DrawLine(suspicionBarStart, suspicionBarEnd);

            // Draw suspicion state text
            if (_isSuspicious)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, "Suspicious");
            }
        }
    }

    /// <summary>
    /// Draws search visualization
    /// </summary>
    private void DrawSearchGizmos()
    {
        // Show last known player position when searching
        if (_isSearching)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_lastKnownPlayerPosition, 1f);
            Gizmos.DrawLine(transform.position, _lastKnownPlayerPosition);
        }
    }

    /// <summary>
    /// Draws patrol path visualization
    /// </summary>
    private void DrawPatrolPathGizmos()
    {
        // Draw patrol path visualization
        if (!_showGizmos || _patrolPoints == null || _patrolPoints.Length == 0)
            return;

        for (int i = 0; i < _patrolPoints.Length; i++)
        {
            if (_patrolPoints[i] != null)
            {
                if (i == 0)
                {
                    Gizmos.color = Color.green; // Start point
                }
                else if (i == _patrolPoints.Length - 1)
                {
                    Gizmos.color = Color.red; // End point
                }
                else
                {
                    Gizmos.color = Color.blue; // Intermediate points
                }

                Gizmos.DrawSphere(_patrolPoints[i].position, 0.3f);
                Gizmos.color = Color.yellow;
                if (i < _patrolPoints.Length - 1 && _patrolPoints[i + 1] != null)
                {
                    Gizmos.DrawLine(_patrolPoints[i].position, _patrolPoints[i + 1].position);
                }
            }
        }

        if (
            _patrolLoop
            && _patrolPoints.Length > 1
            && _patrolPoints[0] != null
            && _patrolPoints[_patrolPoints.Length - 1] != null
        )
        {
            Gizmos.DrawLine(
                _patrolPoints[_patrolPoints.Length - 1].position,
                _patrolPoints[0].position
            );
        }
    }
    #endregion

    #region Editor Tools
#if UNITY_EDITOR
    private bool isPlacingPoints = false;

    [Button("Place Patrol Points"), Tab("Patrol")]
    public void TogglePlacePatrolPoints()
    {
        isPlacingPoints = !isPlacingPoints;
        SceneView.duringSceneGui -= OnSceneGUI;

        if (isPlacingPoints)
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        SceneView.RepaintAll();
    }

    [Button("Clear Patrol Points"), Tab("Patrol")]
    public void ClearPatrolPoints()
    {
        if (_patrolPoints == null || _patrolPoints.Length == 0)
            return;

        foreach (Transform point in _patrolPoints)
        {
            if (point != null)
            {
                DestroyImmediate(point.gameObject);
            }
        }

        _patrolPoints = new Transform[0];
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && isPlacingPoints)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Add Patrol Point");

                GameObject point = new GameObject("PatrolPoint" + (_patrolPoints.Length + 1));
                Undo.RegisterCreatedObjectUndo(point, "Create Patrol Point");

                point.transform.position = hit.point;
                point.transform.SetParent(PatrolPointsParent); // Set parent to PatrolPoints

                Undo.RecordObject(this, "Add Patrol Point");

                Array.Resize(ref _patrolPoints, _patrolPoints.Length + 1);
                _patrolPoints[_patrolPoints.Length - 1] = point.transform;

                Selection.activeGameObject = point;
                EditorUtility.SetDirty(this);
            }

            e.Use();
        }
    }
#endif
    #endregion
}
