using System;
using System.Collections;
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
        if (
            !IsPlayerSpotted
            && _currentState != EnemyState.Wandering
            && _currentState != EnemyState.Investigating
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
        if (!Agent.pathPending && Agent.remainingDistance < 0.2f)
        {
            _currentPointIndex = (_currentPointIndex + 1) % _patrolPoints.Length;
            Agent.SetDestination(_patrolPoints[_currentPointIndex].position);
            _waitCounter = _waitTime;
        }
    }

    private void PatrolBackAndForth()
    {
        if (!Agent.pathPending && Agent.remainingDistance < 0.2f)
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
            Agent.SetDestination(_patrolPoints[_currentPointIndex].position);
            _waitCounter = _waitTime;
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
            && Vector3.Distance(transform.position, Player.transform.position) > 1f
        )
        {
            Agent.SetDestination(Player.transform.position);
            yield return null;

            // If we've lost sight for too long, this will be handled in Update()
        }

        // If we've caught the player or state has changed, handle accordingly
        if (_currentState != EnemyState.Chasing)
        {
            // State was changed elsewhere (likely in Update due to losing visibility)
            // Let that logic handle the transition
            yield break;
        }

        // If we reached the player
        if (Vector3.Distance(transform.position, Player.transform.position) <= 1f)
        {
            // Handle reaching the player (attack, game over, etc.)
            Debug.Log("Caught the player!");
        }
    }

    protected override void LosePlayerVisibility()
    {
        base.LosePlayerVisibility();

        // After investigating, return to patrol
        StartCoroutine(ReturnToPatrolAfterDelay(_investigationTime));
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

        if (_patrolPoints.Length > 0)
        {
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
    private bool _isPlacingPatrolPoints = false; // New boolean for toggling patrol points placement

    [Button("Place Patrol Points"), Tab("Patrol")]
    public void TogglePlacePatrolPoints()
    {
        _isPlacingPatrolPoints = !_isPlacingPatrolPoints; // Toggle the boolean
        SceneView.duringSceneGui -= OnSceneGUI;

        if (_isPlacingPatrolPoints)
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
        if (e.type == EventType.MouseDown && e.button == 0 && _isPlacingPatrolPoints) // Use the new boolean
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
