using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using VInspector;

/// <summary>
/// Base class for wandering enemies that patrol areas and respond to alerts
/// </summary>
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



    [SerializeField]
    private float _alertResponseRadius = 10f;

    [SerializeField]
    private bool _respondToNearbyAlerts = true;

    [SerializeField]
    private float _nervousnessThreshold = 5f;

    [SerializeField, ReadOnly]
    private float _nervousness = 0f;

    [Tab("Safety")]
    [SerializeField]
    private bool _canHideWhenScared = true;

    [SerializeField]
    private float _hideSearchRadius = 15f;

    [SerializeField]
    private LayerMask _hidingSpotLayers;

    [SerializeField]
    private string[] _hidingTagOptions = { "Cover", "Bushes", "Hiding" };

    [SerializeField]
    private float _panicThreshold = 7f;

    [SerializeField]
    private float _panicSpeed = 4f;

    [SerializeField]
    private float _normalSpeed = 2f;

    [SerializeField, ReadOnly]
    private bool _isPanicking = false;

    [SerializeField, ReadOnly]
    private Transform _currentHidingSpot;

    #region Core Methods
    /// <summary>
    /// Initializes the enemy's patrol behavior
    /// </summary>
    protected override void Start()
    {
        base.Start();
        _timer = _wanderTimer;
        _confinedAreaCenter = transform.position; // Initialize confined area

        // Set normal movement speed
        Speed = _normalSpeed;
        if (Agent != null)
        {
            Agent.speed = Speed;
        }

        // Start wandering immediately
        StartWandering(_confinedAreaCenter, _wanderRadius);
    }

    /// <summary>
    /// Handles nervousness decay and wandering timer
    /// </summary>
    private void FixedUpdate()
    {
        // Decay nervousness over time
        if (_nervousness > 0)
        {
            _nervousness -= Time.deltaTime * 0.5f;
            if (_nervousness <= 0)
            {
                _nervousness = 0;

                // Return to normal speed if no longer nervous
                if (_isPanicking)
                {
                    _isPanicking = false;
                    Speed = _normalSpeed;
                    Agent.speed = Speed;
                    Debug.Log($"{gameObject.name} has calmed down");
                }
            }
        }

        // Check if should panic
        if (_nervousness >= _panicThreshold && !_isPanicking)
        {
            _isPanicking = true;
            Speed = _panicSpeed;
            Agent.speed = Speed;
            Debug.Log($"{gameObject.name} is panicking!");
        }

        // Only manage patrol if not spotted player and not already wandering
        if (
            !IsPlayerSpotted
            && _currentState != EnemyState.Wandering
            && _currentState != EnemyState.Investigating
            && _currentState != EnemyState.RunningAway
            && _currentState != EnemyState.Hiding
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
    #endregion

    #region Alert Response Methods
    /// <summary>
    /// Handles enemy response to alerts from other entities
    /// </summary>
    /// <param name="alertPosition">Position where the alert originated</param>
    /// <param name="alertLevel">Severity of the alert</param>
    public void RespondToAlert(Vector3 alertPosition, float alertLevel)
    {
        if (!_respondToNearbyAlerts)
            return;

        float distance = Vector3.Distance(transform.position, alertPosition);

        if (distance <= _alertResponseRadius)
        {
            // Increase nervousness based on alert level and distance
            float impact = alertLevel * (1f - distance / _alertResponseRadius);
            _nervousness += impact;

            // Alert other citizens nearby to create chain reactions of panic
            if (_nervousness > _nervousnessThreshold)
            {
                // Alert other citizens with reduced impact
                BaseWanderingEnemy[] nearbyCitizens = FindObjectsByType<BaseWanderingEnemy>(
                    FindObjectsSortMode.None
                );
                foreach (BaseWanderingEnemy citizen in nearbyCitizens)
                {
                    if (
                        citizen != this
                        && Vector3.Distance(transform.position, citizen.transform.position)
                            < _alertResponseRadius / 2
                    )
                    {
                        citizen.RespondToAlert(transform.position, impact * 0.5f);
                    }
                }
            }

            // Try to find a hiding spot if very nervous
            if (_nervousness > _panicThreshold && _canHideWhenScared)
            {
                StopWandering();
                SetState(EnemyState.Hiding);
                StartCoroutine(FindAndMoveToHidingSpot(alertPosition));
            }
            // Or run away if moderately nervous
            else if (_nervousness > _nervousnessThreshold)
            {
                StopWandering();
                SetState(EnemyState.RunningAway);

                // Calculate a position to run away to
                Vector3 runDirection = (transform.position - alertPosition).normalized;
                Vector3 runPosition = transform.position + runDirection * _alertResponseRadius;

                // Start running away
                StartCoroutine(RunAway(runPosition));
            }
        }
    }
    #endregion

    #region Hiding & Safety Methods
    /// <summary>
    /// Finds a suitable hiding spot and moves the enemy there
    /// </summary>
    /// <param name="dangerSource">Position of the threat to hide from</param>
    private IEnumerator FindAndMoveToHidingSpot(Vector3 dangerSource)
    {
        Debug.Log($"{gameObject.name} is looking for a hiding spot");

        // Try to find objects with hiding tags
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            _hideSearchRadius,
            _hidingSpotLayers
        );
        float bestScore = -1;
        Transform bestHidingSpot = null;

        foreach (Collider col in colliders)
        {
            // Check if the object has one of our hiding tags
            bool isHidingSpot = false;
            foreach (string tag in _hidingTagOptions)
            {
                if (col.CompareTag(tag))
                {
                    isHidingSpot = true;
                    break;
                }
            }

            if (!isHidingSpot)
                continue;

            // Score this hiding spot
            Vector3 hideDir = (col.transform.position - dangerSource).normalized;
            Vector3 hidePos = col.transform.position;

            // We want: 1) Good distance from danger 2) Not too far from current pos 3) Behind cover from danger
            float dangerDist = Vector3.Distance(hidePos, dangerSource);
            float selfDist = Vector3.Distance(hidePos, transform.position);
            float angleScore = Vector3.Dot(hideDir, (hidePos - transform.position).normalized);

            // Calculate final score - higher is better
            float score = (dangerDist * 2) - (selfDist * 0.5f) + (angleScore * 5);

            if (score > bestScore)
            {
                bestScore = score;
                bestHidingSpot = col.transform;
            }
        }

        // If found a hiding spot, move there
        if (bestHidingSpot != null)
        {
            _currentHidingSpot = bestHidingSpot;

            // Get position near the hiding spot but not inside
            Vector3 hidingPosition =
                bestHidingSpot.position + (transform.position - dangerSource).normalized * 2;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(hidingPosition, out hit, 5f, NavMesh.AllAreas))
            {
                Agent.SetDestination(hit.position);
            }
            else
            {
                Agent.SetDestination(bestHidingSpot.position);
            }

            Debug.Log($"{gameObject.name} found hiding spot at {bestHidingSpot.name}");

            // Wait until reached hiding spot or close enough
            while (Agent.remainingDistance > 1.5f)
            {
                yield return null;
            }

            // Stay hidden for a while
            float hideTime = 10f + (_nervousness / 2f);
            float hideTimer = 0;

            while (hideTimer < hideTime && _nervousness > _nervousnessThreshold / 2)
            {
                // Make citizen look away from danger while hiding
                Vector3 lookDir = (transform.position - dangerSource).normalized;
                transform.rotation = Quaternion.LookRotation(lookDir);

                hideTimer += Time.deltaTime;
                yield return null;
            }

            // Reduce nervousness after hiding
            _nervousness = Mathf.Max(_nervousness - 3f, 0);
            _currentHidingSpot = null;

            // Resume normal activities
            StartWandering(_confinedAreaCenter, _wanderRadius);
        }
        else
        {
            // If no hiding spot, run away
            Debug.Log($"{gameObject.name} couldn't find a hiding spot, running away instead");
            SetState(EnemyState.RunningAway);
            Vector3 runDirection = (transform.position - dangerSource).normalized;
            Vector3 runPosition = transform.position + runDirection * _alertResponseRadius;
            StartCoroutine(RunAway(runPosition));
        }
    }

    /// <summary>
    /// Makes the enemy flee from danger
    /// </summary>
    /// <param name="runPosition">Position to run to</param>
    private IEnumerator RunAway(Vector3 runPosition)
    {
        Agent.SetDestination(runPosition);

        while (Vector3.Distance(transform.position, runPosition) > 0.1f)
        {
            yield return null;
        }

        // Once reached the run position, resume normal wandering
        StartWandering(_confinedAreaCenter, _wanderRadius);
    }
    #endregion

    #region Debug & Visualization
    /// <summary>
    /// Draws gizmos to visualize enemy state and properties
    /// </summary>
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

        // Show alert response radius
        if (_respondToNearbyAlerts)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f); // Orange
            Gizmos.DrawWireSphere(transform.position, _alertResponseRadius);
        }

        // Show nervousness in play mode
        if (Application.isPlaying && _nervousness > 0)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 3f,
                $"Nervousness: {_nervousness:F1}/{_nervousnessThreshold:F1}"
            );
        }

        // Show hiding spot connection
        if (_currentHidingSpot != null)
        {
            Gizmos.color = new Color(0f, 0.7f, 1f, 0.6f); // Light blue
            Gizmos.DrawLine(transform.position, _currentHidingSpot.position);
            Gizmos.DrawWireSphere(_currentHidingSpot.position, 1f);
        }

        // Show panic state
        if (Application.isPlaying && _isPanicking)
        {
            Gizmos.color = Color.red;
            float pulseSize = 1f + Mathf.Sin(Time.time * 5) * 0.2f;
            Gizmos.DrawWireSphere(transform.position + Vector3.up, pulseSize);

            UnityEditor.Handles.Label(transform.position + Vector3.up * 3.5f, "PANICKING!");
        }
    }
    #endregion

    #region Setup & Initialization
    /// <summary>
    /// Initializes enemy components and generates patrol points
    /// </summary>
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
    #endregion

    #region Player Detection Response
    /// <summary>
    /// Handles enemy behavior when player is spotted
    /// </summary>
    protected override IEnumerator OnPlayerSpotted()
    {
        // Stop wandering to handle the player spotting
        StopWandering();

        // Immediately increase nervousness when spotting player
        _nervousness += 5f;

        // Call base method first to set state to chasing
        yield return base.OnPlayerSpotted();

        // Find the nearest ranger
        BasePatrolEnemy nearestRanger = FindNearestRanger();
        if (nearestRanger != null)
        {
            Debug.Log(
                $"{gameObject.name} spotted player and is seeking ranger {nearestRanger.name}"
            );

            // Alert nearby enemies
            BaseEnemy[] nearbyEnemies = FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None);
            foreach (BaseEnemy enemy in nearbyEnemies)
            {
                if (enemy != this && enemy is BaseWanderingEnemy wanderingEnemy)
                {
                    wanderingEnemy.RespondToAlert(transform.position, 5f);
                }
            }

            // Move towards the nearest ranger (with increased speed due to panic)
            Speed = _panicSpeed;
            Agent.speed = Speed;
            Agent.SetDestination(nearestRanger.transform.position);

            // Wait until the wandering enemy reaches the ranger
            while (Vector3.Distance(transform.position, nearestRanger.transform.position) > 1f)
            {
                Agent.SetDestination(nearestRanger.transform.position);
                yield return null;
            }

            // Ask ranger for protection and show it the player's position
            if (nearestRanger is BasePatrolEnemy ranger)
            {
                ranger.ProtectCitizen(this);
            }
            else
            {
                // Tell the ranger to follow the wandering enemy back to where it spotted the player
                nearestRanger.FollowCitizen(gameObject);
            }

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

            // Find a safe place to hide while ranger investigates
            if (_canHideWhenScared)
            {
                StartCoroutine(FindAndMoveToHidingSpot(playerLastKnownPosition));
            }
            else
            {
                StartCoroutine(ResetPatrol());
            }

            yield return new WaitForSeconds(WanderTime);

            nearestRanger.ResetPatrol();
        }
        else
        {
            Debug.LogWarning("No ranger found!");

            // If no ranger, try to hide
            if (_canHideWhenScared)
            {
                StartCoroutine(FindAndMoveToHidingSpot(Player.transform.position));
            }
            else
            {
                // Just start wandering ourselves if no ranger is found and can't hide
                StartCoroutine(ResetPatrol());
            }
        }
    }

    /// <summary>
    /// Finds the nearest ranger patrol enemy
    /// </summary>
    /// <returns>The closest BasePatrolEnemy or null if none found</returns>
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
    #endregion

    #region Sound Response
    /// <summary>
    /// Handles enemy behavior when a sound is heard
    /// </summary>
    /// <param name="soundPosition">Position where the sound originated</param>
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
    #endregion

    #region Patrol Management
    /// <summary>
    /// Resets the patrol behavior after an interruption
    /// </summary>
    private IEnumerator ResetPatrol()
    {
        // Stop any current wandering
        StopWandering();

        IsPlayerSpotted = false; // Reset player spotted flag

        // Set state to patrolling to ensure proper behavior
        SetState(EnemyState.Patrolling);

        // Return to confined area center
        while (Vector3.Distance(transform.position, _confinedAreaCenter) > 1f)
        {
            Agent.SetDestination(_confinedAreaCenter);
            yield return null;
        }

        // Once back at center, resume normal wandering
        StartWandering(_confinedAreaCenter, _wanderRadius);
    }
    #endregion
}
