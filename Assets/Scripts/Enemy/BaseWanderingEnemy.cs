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
<<<<<<< Updated upstream
            && _currentState != EnemyState.Chasing
=======
            && _currentState != EnemyState.Chasing // Don't wander if chasing
>>>>>>> Stashed changes
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

        // Update alert level to Confirmed
        DetectionMeter = 1.0f;
        UpdateAlertLevel();

        yield return base.OnPlayerSpotted();

        // Find the nearest ranger
        BasePatrolEnemy nearestRanger = FindNearestRanger();
        if (
            nearestRanger != null
            && Vector3.Distance(transform.position, nearestRanger.transform.position) < 30f
        )
        {
            // Only proceed with ranger logic if we're still in chase state
            if (_currentState == EnemyState.Chasing && Player != null)
            {
                // Store player position before moving to ranger
                Vector3 playerLastKnownPosition = Player.transform.position;

                // Move towards the nearest ranger
                Agent.SetDestination(nearestRanger.transform.position);

                // Wait until the wandering enemy reaches the ranger
                while (
                    _currentState == EnemyState.Chasing
                    && Vector3.Distance(transform.position, nearestRanger.transform.position) > 1.5f
                )
                {
                    Agent.SetDestination(nearestRanger.transform.position);
                    yield return null;
                }

                // If state changed, abort the ranger interaction
                if (_currentState != EnemyState.Chasing)
                {
                    yield break;
                }

                // Tell the ranger to follow the wandering enemy back to where it spotted the player
                nearestRanger.FollowCitizen(gameObject);

                // Update the ranger's alert level too
                if (nearestRanger.CurrentAlertLevel < AlertLevel.Alert)
                {
                    nearestRanger.DetectionMeter = 0.75f;
                    nearestRanger.UpdateRangerAlertLevel(AlertLevel.Alert);
                }

                // Move back to the player's last known position
                Agent.SetDestination(playerLastKnownPosition);

                // Wait until the wandering enemy reaches the player's last known position
                while (
                    _currentState == EnemyState.Chasing
                    && Vector3.Distance(transform.position, playerLastKnownPosition) > 1.5f
                )
                {
                    yield return null;
                }

                // If state changed, abort
                if (_currentState != EnemyState.Chasing)
                {
                    yield break;
                }

                // Use new wandering method for ranger
                nearestRanger.StartWandering(playerLastKnownPosition, 5f);

                StartCoroutine(ResetPatrol());
            }
        }
        else
        {
            // Just chase the player ourselves if no ranger is found nearby
            while (
                _currentState == EnemyState.Chasing
                && Player != null
                && Vector3.Distance(transform.position, Player.transform.position) > 1f
            )
            {
                Agent.SetDestination(Player.transform.position);
                yield return null;
            }

            // After losing the player or reaching them, return to patrolling
            StartCoroutine(ResetPatrol());
        }
    }

    protected override IEnumerator OnSoundHeard(Vector3 soundPosition)
    {
        // Stop current wandering
        StopWandering();

        // Update alert level to at least Suspicious when hearing a sound
        if (CurrentAlertLevel == AlertLevel.Normal)
        {
            CurrentAlertLevel = AlertLevel.Suspicious;
            DetectionMeter = 0.5f;
        }

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

    protected override void LosePlayerVisibility()
    {
        base.LosePlayerVisibility();

        // Update alert level when visibility is lost
        CurrentAlertLevel = AlertLevel.Suspicious;

        // After losing sight, return to wandering centered at original position
        StartCoroutine(ReturnToOriginalWanderArea());
    }

    private IEnumerator ReturnToOriginalWanderArea()
    {
        yield return new WaitForSeconds(_investigationTime);

        // Only return to original position if we're not chasing again
        if (_currentState != EnemyState.Chasing)
        {
            StartCoroutine(ResetPatrol());
        }
    }

    private IEnumerator ResetPatrol()
    {
        // Stop any current wandering
        StopWandering();

        // Set state to investigating while returning
        _currentState = EnemyState.Investigating;
        IsPlayerSpotted = false;

        // Return to confined area center
        Agent.SetDestination(_confinedAreaCenter);

        while (Vector3.Distance(transform.position, _confinedAreaCenter) > 2f)
        {
            // If we spot the player again during return, break out
            if (IsPlayerSpotted || _currentState == EnemyState.Chasing)
            {
                yield break;
            }

            yield return null;
        }

        // Reset alert level
        DetectionMeter = 0f;
        CurrentAlertLevel = AlertLevel.Normal;

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
