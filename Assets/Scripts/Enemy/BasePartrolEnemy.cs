using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using VInspector;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class BasePatrolEnemy : BaseEnemy
{
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

    [SerializeField]
    private Transform PatrolPointsParent;

    private int _currentPointIndex = 0;
    private bool _isReversing = false;
    private float _waitCounter;

    protected override void Start()
    {
        base.Start();
        if (_patrolPoints.Length > 0)
        {
            Agent.SetDestination(_patrolPoints[_currentPointIndex].position);
        }
        _waitCounter = _startWaitTime;
    }

    private void FixedUpdate()
    {
        // Only do patrol logic when we're actually in the patrolling state
        // and not spotted, investigating, or wandering
        if (
            _currentState == EnemyState.Patrolling
            && !IsPlayerSpotted
            && _patrolPoints != null
            && _patrolPoints.Length > 0
        )
        {
            if (_waitCounter > 0)
            {
                _waitCounter -= Time.deltaTime;
                return;
            }

            if (_patrolLoop)
            {
                PatrolLoopMethod();
            }
            else
            {
                PatrolBackAndForth();
            }
        }
    }

    private void PatrolLoopMethod()
    {
        if (_patrolPoints.Length == 0)
            return;

        if (!Agent.pathPending && Agent.remainingDistance < 0.5f && Agent.enabled)
        {
            _currentPointIndex = (_currentPointIndex + 1) % _patrolPoints.Length;
            if (_patrolPoints[_currentPointIndex] != null)
            {
                Agent.SetDestination(_patrolPoints[_currentPointIndex].position);
                _waitCounter = _waitTime;
            }
        }
    }

    private void PatrolBackAndForth()
    {
        if (_patrolPoints.Length == 0)
            return;

        if (!Agent.pathPending && Agent.remainingDistance < 0.5f && Agent.enabled)
        {
            if (_currentPointIndex == 0)
            {
                _isReversing = false;
            }
            else if (_currentPointIndex == _patrolPoints.Length - 1)
            {
                _isReversing = true;
            }

            _currentPointIndex = _isReversing ? _currentPointIndex - 1 : _currentPointIndex + 1;
            if (_patrolPoints[_currentPointIndex] != null)
            {
                Agent.SetDestination(_patrolPoints[_currentPointIndex].position);
                _waitCounter = _waitTime;
            }
        }
    }

    protected override IEnumerator OnPlayerSpotted()
    {
        // Stop any current wandering
        StopWandering();

        yield return base.OnPlayerSpotted();

        // Chase after the player if the player is spotted
        while (
            _currentState == EnemyState.Chasing
            && Player != null
            && Vector3.Distance(transform.position, Player.transform.position) > 1f
        )
        {
            Agent.SetDestination(Player.transform.position);
            yield return null;
        }

        // If we've caught the player or state has changed, handle accordingly
        if (_currentState != EnemyState.Chasing)
        {
            yield break;
        }

        // If we reached the player
        if (Player != null && Vector3.Distance(transform.position, Player.transform.position) <= 1f)
        {
            Debug.Log("Caught the player!");
            // Handle player caught logic here (game over, damage player, etc.)
        }
    }

    protected override void LosePlayerVisibility()
    {
        base.LosePlayerVisibility();
    }

    private IEnumerator ReturnToPatrolAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Only return to patrol if we're not chasing again
        if (_currentState != EnemyState.Chasing)
        {
            ResetPatrol();
        }
    }

    protected override IEnumerator OnSoundHeard(Vector3 soundPosition)
    {
        // Stop any current wandering
        StopWandering();

        yield return base.OnSoundHeard(soundPosition);

        // Move to sound position
        Agent.SetDestination(soundPosition);

        // Wait until we reach the position or get close enough
        while (
            Vector3.Distance(transform.position, soundPosition) > 0.5f
            && _currentState == EnemyState.Investigating
        )
        {
            yield return null;
        }

        // Only start wandering if we're still investigating (not chasing)
        if (_currentState == EnemyState.Investigating)
        {
            // Use new wandering system
            StartWandering(soundPosition, 5f);
        }

        // Wait for wandering to complete before returning to patrol
        yield return new WaitForSeconds(WanderTime);

        // Only return to patrol if not in another higher priority state (like chasing)
        if (_currentState == EnemyState.Wandering || _currentState == EnemyState.Investigating)
        {
            ResetPatrol();
        }
    }

    public void UpdateRangerAlertLevel(AlertLevel alertLevel)
    {
        // Update the alert level of the ranger
        if (alertLevel == AlertLevel.Suspicious)
        {
            CurrentAlertLevel = AlertLevel.Suspicious;
            DetectionMeter = 0.5f;
        }
        else if (alertLevel == AlertLevel.Alert)
        {
            CurrentAlertLevel = AlertLevel.Alert;
            DetectionMeter = 1f;
        }
    }

    public void FollowCitizen(GameObject citizen)
    {
        // Stop any current wandering when following a citizen
        StopWandering();
        Agent.SetDestination(citizen.transform.position);
    }

    public void ResetPatrol()
    {
        // Stop any wandering when resetting patrol
        StopWandering();

        _currentState = EnemyState.Patrolling;
        IsPlayerSpotted = false;

        if (_patrolPoints != null && _patrolPoints.Length > 0)
        {
            // Ensure _currentPointIndex is within bounds
            if (_currentPointIndex >= _patrolPoints.Length)
            {
                _currentPointIndex = 0; // Reset to first point if out of bounds
            }

            // Check if the patrol point exists and if we're far from it
            if (
                _patrolPoints[_currentPointIndex] != null
                && Vector3.Distance(transform.position, _patrolPoints[_currentPointIndex].position)
                    > 10f
            )
            {
                int closestPointIndex = 0;
                float closestDistance = Mathf.Infinity;

                for (int i = 0; i < _patrolPoints.Length; i++)
                {
                    if (_patrolPoints[i] == null)
                        continue;

                    float dist = Vector3.Distance(transform.position, _patrolPoints[i].position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestPointIndex = i;
                    }
                }

                _currentPointIndex = closestPointIndex;
            }

            if (
                _currentPointIndex < _patrolPoints.Length
                && _patrolPoints[_currentPointIndex] != null
            )
            {
                Agent.SetDestination(_patrolPoints[_currentPointIndex].position);
            }
        }
    }

    void Reset()
    {
        RB = GetComponent<Rigidbody>();
        Agent = GetComponent<NavMeshAgent>();

        // Create or find a global GameObject for all PatrolPoints
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

        // Create a separate GameObject for this enemy's PatrolPoints with the name GameObject.name + "PatrolPoints"
        string patrolPointsName = gameObject.name + "PatrolPoints";
        GameObject patrolPointsObject = GameObject.Find(patrolPointsName);
        if (patrolPointsObject == null)
        {
            patrolPointsObject = new GameObject(patrolPointsName);
            patrolPointsObject.transform.SetParent(enemyParentObject.transform);
        }

        PatrolPointsParent = patrolPointsObject.transform;
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
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

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        // Patrol point visualization moved to OnDrawGizmos
    }

#if UNITY_EDITOR
    private bool isPlacingPoints = false;

    [Button("Place Patrol Points"), Tab("Patrol")]
    public void TogglePlacePatrolPoints()
    {
        isPlacingPoints = !isPlacingPoints;
        SceneView.duringSceneGui -= OnSceneGUI;

        if (isPlacingPoints)
        {
            // Ensure we have a valid parent for patrol points
            if (PatrolPointsParent == null)
            {
                // Create parent structure if it doesn't exist
                GameObject globalPatrolPointsObject = GameObject.Find("GlobalPatrolPoints");
                if (globalPatrolPointsObject == null)
                {
                    globalPatrolPointsObject = new GameObject("GlobalPatrolPoints");
                }

                string enemyParentName = gameObject.name + "Patrol";
                GameObject enemyParentObject = GameObject.Find(enemyParentName);
                if (enemyParentObject == null)
                {
                    enemyParentObject = new GameObject(enemyParentName);
                    enemyParentObject.transform.SetParent(globalPatrolPointsObject.transform);
                }

                string patrolPointsName = gameObject.name + "PatrolPoints";
                GameObject patrolPointsObject = GameObject.Find(patrolPointsName);
                if (patrolPointsObject == null)
                {
                    patrolPointsObject = new GameObject(patrolPointsName);
                    patrolPointsObject.transform.SetParent(enemyParentObject.transform);
                }

                PatrolPointsParent = patrolPointsObject.transform;
            }

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
}
