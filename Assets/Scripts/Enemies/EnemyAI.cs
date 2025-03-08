using UnityEngine;
using VInspector;

public class EnemyAI : MonoBehaviour
{
    [Tab("Detection")]
    [SerializeField]
    private float _hearingThreshold = 5f;

    [SerializeField]
    private float _investigationTime = 10f;

    [Tab("State")]
    [SerializeField, ReadOnly]
    private bool _isInvestigating = false;

    [SerializeField, ReadOnly]
    private Vector3 _lastHeardPosition;

    [SerializeField, ReadOnly]
    private float _investigationTimeRemaining = 0f;

    private enum EnemyState
    {
        Patrolling,
        Investigating,
        Chasing
    }

    [SerializeField, ReadOnly]
    private EnemyState _currentState = EnemyState.Patrolling;

    void Update()
    {
        // Handle investigation timer
        if (_isInvestigating)
        {
            _investigationTimeRemaining -= Time.deltaTime;
            if (_investigationTimeRemaining <= 0)
            {
                _isInvestigating = false;
                _currentState = EnemyState.Patrolling;
            }
        }

        // State machine logic would go here
        switch (_currentState)
        {
            case EnemyState.Patrolling:
                // Regular patrol behavior
                break;
            case EnemyState.Investigating:
                // Move towards noise source
                break;
            case EnemyState.Chasing:
                // Chase player
                break;
        }
    }

    public void AlertToNoise(Vector3 noisePosition, float noiseLevel)
    {
        // If noise is loud enough to hear
        if (noiseLevel > _hearingThreshold)
        {
            float distanceToNoise = Vector3.Distance(transform.position, noisePosition);
            float adjustedNoiseLevel = noiseLevel - (distanceToNoise * 0.1f); // Noise decreases with distance

            // If noise is still audible after distance adjustment
            if (adjustedNoiseLevel > _hearingThreshold)
            {
                _isInvestigating = true;
                _lastHeardPosition = noisePosition;
                _investigationTimeRemaining = _investigationTime;
                _currentState = EnemyState.Investigating;

                Debug.Log($"Enemy heard noise of level {adjustedNoiseLevel} at {noisePosition}");
            }
        }
    }
}
