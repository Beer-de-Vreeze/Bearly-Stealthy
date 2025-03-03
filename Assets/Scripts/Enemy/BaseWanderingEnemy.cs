using UnityEngine;
using UnityEngine.AI;
using VInspector;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class BaseWanderingEnemy : MonoBehaviour
{
    /// <summary>
    /// Enemy that wanders from point to point in a random direction in a circular area
    /// </summary>

    [Tab("Movement")]
    [SerializeField]
    private float _speed = 2f;

    [Tab("Patrol")]
    [SerializeField]
    private float _wanderRadius = 5f;

    [SerializeField]
    private float _wanderTimer = 5f;

    [SerializeField]
    private bool _showGizmos = true;

    private float _timer;
    private Vector3 _newPosition;
    private NavMeshAgent _agent;
    private Transform _patrolPointsParent;
    private Vector3 _confinedAreaCenter;

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _speed;
        _timer = _wanderTimer;
        _confinedAreaCenter = transform.position; // Set the confined area center to the enemy's initial position
    }

    private void FixedUpdate()
    {
        MoveToPos();
    }

    private void MoveToPos()
    {
        _timer += Time.deltaTime;

        if (_timer >= _wanderTimer)
        {
            _newPosition = RandomNavSphere(_confinedAreaCenter, _wanderRadius, -1); // Use the confined area center
            _agent.SetDestination(_newPosition);
            _timer = 0;
        }
    }

    private Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
        return navHit.position;
    }

    private void OnDrawGizmos()
    {
        if (_showGizmos)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_confinedAreaCenter, _wanderRadius);
        }
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_newPosition, 1);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, _newPosition);
    }

    private void Reset()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _speed;
        _timer = _wanderTimer;
        _confinedAreaCenter = transform.position;

        GameObject globalPatrolPointsObject = GameObject.Find("GlobalPatrolPoints");
        if (globalPatrolPointsObject == null)
        {
            globalPatrolPointsObject = new GameObject("GlobalPatrolPoints");
        }

        // Create a child parent for this enemy under the global parent
        string enemyParentName = gameObject.name + "ConfinedArea";
        GameObject enemyParentObject = GameObject.Find(enemyParentName);
        if (enemyParentObject == null)
        {
            enemyParentObject = new GameObject(enemyParentName);
            enemyParentObject.transform.SetParent(globalPatrolPointsObject.transform);
        }

        // Create a separate GameObject for this enemy's PatrolPoints with the name GameObject.name + "PatrolPoints"
        string patrolPointsName = gameObject.name + "ConfinedAreaPoint";
        GameObject patrolPointsObject = GameObject.Find(patrolPointsName);
        if (patrolPointsObject == null)
        {
            patrolPointsObject = new GameObject(patrolPointsName);
            patrolPointsObject.transform.SetParent(enemyParentObject.transform);
        }

        _patrolPointsParent = patrolPointsObject.transform;
    }
}
