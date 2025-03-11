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

        IsPlayerSpotted = true;

        // Chase after the player if the player is spotted
        while (Vector3.Distance(transform.position, Player.transform.position) > 1f)
        {
            Agent.SetDestination(Player.transform.position);
            yield return null;
        }

        // If they lose the player, they will wander a bit
        Vector3 playerLastKnownPosition = Player.transform.position;
        Agent.SetDestination(playerLastKnownPosition);

        if (Vector3.Distance(transform.position, playerLastKnownPosition) < 1f)
        {
            // Use new wandering system
            StartWandering(playerLastKnownPosition, 5f);

            // Wait for the wandering to complete
            yield return new WaitForSeconds(WanderTime);
        }

        // Then go back to patrolling
        IsPlayerSpotted = false;
        ResetPatrol();
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

        if (_patrolPoints.Length > 0)
        {
            Agent.SetDestination(_patrolPoints[_currentPointIndex].position);
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

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
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
}
