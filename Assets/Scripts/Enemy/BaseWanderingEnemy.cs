using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using VInspector;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class BaseWanderingEnemy : BaseEnemy
{
    [Tab("Patrol")]
    [SerializeField]
    private float _wanderRadius = 5f;

    [SerializeField]
    private float _wanderTimer = 5f;

    [SerializeField]
    private bool _showGizmos = true;

    [SerializeField, ReadOnly]
    private float _timer;
    private Vector3 _newPosition;
    private Vector3 _confinedAreaCenter;

    protected override void Start()
    {
        base.Start();
        _timer = _wanderTimer;
        _confinedAreaCenter = transform.position; // Initialize confined area

        // Start wandering immediately
        StartWandering(_confinedAreaCenter, _wanderRadius);
    }

    private void FixedUpdate()
    {
        // Only manage patrol if not spotted player and not already wandering
        if (
            !IsPlayerSpotted
            && _currentState != EnemyState.Wandering
            && _currentState != EnemyState.Investigating
        )
        {
            _timer += Time.deltaTime;

            if (_timer >= _wanderTimer)
            {
                // Reset timer and start wandering
                _timer = 0;
                StartWandering(_confinedAreaCenter, _wanderRadius);
            }
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
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
        Agent = GetComponent<NavMeshAgent>();
        Agent.speed = Speed;
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

        // Create a separate GameObject for this enemy's PatrolPoints with the name GameObject.name + "PatrolPoints"             e);
        string patrolPointsName = gameObject.name + "ConfinedAreaPoint";
        GameObject patrolPointsObject = GameObject.Find(patrolPointsName);
        if (patrolPointsObject == null)
        {
            patrolPointsObject = new GameObject(patrolPointsName);
            patrolPointsObject.transform.SetParent(enemyParentObject.transform);
        }

        PatrolPointsParent = patrolPointsObject.transform;
    }

    protected override IEnumerator OnPlayerSpotted()
    {
        // Stop wandering to handle the player spotting
        StopWandering();

        yield return base.OnPlayerSpotted();

        // Find the nearest ranger
        BasePatrolEnemy nearestRanger = FindNearestRanger();
        if (nearestRanger != null)
        {
            // Move towards the nearest ranger
            Agent.SetDestination(nearestRanger.transform.position);

            // Wait until the wandering enemy reaches the ranger
            while (Vector3.Distance(transform.position, nearestRanger.transform.position) > 1f)
            {
                Agent.SetDestination(nearestRanger.transform.position);
                yield return null;
            }

            // Tell the ranger to follow the wandering enemy back to where it spotted the player
            nearestRanger.FollowCitizen(gameObject);

            // Move back to the player's last known position
            Vector3 playerLastKnownPosition = Player.transform.position;
            Agent.SetDestination(playerLastKnownPosition);

            // Wait until the wandering enemy reaches the player's last known position
            while (Vector3.Distance(transform.position, playerLastKnownPosition) > 1f)
            {
                yield return null;
            }

            // Use new wandering method for ranger
            nearestRanger.StartWandering(playerLastKnownPosition, 5f);

            StartCoroutine(ResetPatrol());

            yield return new WaitForSeconds(WanderTime);

            nearestRanger.ResetPatrol();
        }
        else
        {
            Debug.LogWarning("No ranger found!");

            // Just start wandering ourselves if no ranger is found
            StartCoroutine(ResetPatrol());
        }
    }

    protected override IEnumerator OnSoundHeard(Vector3 soundPosition)
    {
        // Stop current wandering
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
        StartCoroutine(ResetPatrol());
    }

    private IEnumerator ResetPatrol()
    {
        // Stop any current wandering
        StopWandering();

        // Return to confined area center
        while (Vector3.Distance(transform.position, _confinedAreaCenter) > 1f)
        {
            Agent.SetDestination(_confinedAreaCenter);
            yield return null;
            IsPlayerSpotted = false;
        }

        // Once back at center, resume normal wandering
        StartWandering(_confinedAreaCenter, _wanderRadius);
    }

    private BasePatrolEnemy FindNearestRanger()
    {
        BasePatrolEnemy[] rangers = FindObjectsByType<BasePatrolEnemy>(FindObjectsSortMode.None);
        BasePatrolEnemy nearestRanger = null;
        float minDistance = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (BasePatrolEnemy ranger in rangers)
        {
            float distance = Vector3.Distance(ranger.transform.position, currentPosition);
            if (distance < minDistance)
            {
                nearestRanger = ranger;
                minDistance = distance;
            }
        }

        return nearestRanger;
    }
}
