using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using VInspector;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class BasePatrolEnemy : MonoBehaviour
{
    [Tab("Movement")]
    [SerializeField]
    private float _speed = 2f;

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
    private Transform _patrolPointsParent;
    private Rigidbody _rb;
    private NavMeshAgent _agent;

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _agent.speed = _speed;

        if (_patrolPoints.Length > 0)
        {
            _agent.SetDestination(_patrolPoints[_currentPointIndex].position);
        }
        _waitCounter = _startWaitTime;
    }

    private void FixedUpdate()
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

    private void PatrolLoopMethod()
    {
        if (!_agent.pathPending && _agent.remainingDistance < 0.2f)
        {
            _currentPointIndex = (_currentPointIndex + 1) % _patrolPoints.Length;
            _agent.SetDestination(_patrolPoints[_currentPointIndex].position);
            _waitCounter = _waitTime;
        }
    }

    private void PatrolBackAndForth()
    {
        if (!_agent.pathPending && _agent.remainingDistance < 0.2f)
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
            _agent.SetDestination(_patrolPoints[_currentPointIndex].position);
            _waitCounter = _waitTime;
        }
    }

    void Reset()
    {
        _rb = GetComponent<Rigidbody>();
        _agent = GetComponent<NavMeshAgent>();

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

        _patrolPointsParent = patrolPointsObject.transform;
    }

    private void OnDrawGizmos()
    {
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
                point.transform.SetParent(_patrolPointsParent); // Set parent to PatrolPoints

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
