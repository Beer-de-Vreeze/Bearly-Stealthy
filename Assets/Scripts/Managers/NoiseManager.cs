using System.Collections.Generic;
using UnityEngine;

public class NoiseManager : MonoBehaviour
{
    private static NoiseManager _instance;

    public static NoiseManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<NoiseManager>();
            }
            return _instance;
        }
    }

    [System.Serializable]
    public struct NoiseEvent
    {
        public Vector3 position;
        public float intensity;
        public float maxRadius;
        public float startTime;
        public float duration;
    }

    [Header("Debug Settings")]
    [SerializeField]
    private bool _showNoiseGizmos = true;

    [SerializeField]
    private float _defaultNoiseDuration = 3.0f;

    [SerializeField]
    private Color _lowNoiseColor = new Color(0, 1, 0, 0.3f);

    [SerializeField]
    private Color _highNoiseColor = new Color(1, 0, 0, 0.3f);

    [SerializeField]
    private float _highNoiseThreshold = 10.0f;

    [SerializeField]
    private float _maxNoiseDistance = 20f;

    private List<BaseEnemy> _enemies = new List<BaseEnemy>();
    private List<NoiseEvent> _activeNoiseEvents = new List<NoiseEvent>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Find and register all enemies in the scene at start
        BaseEnemy[] allEnemies = FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None);
        foreach (BaseEnemy enemy in allEnemies)
        {
            RegisterEnemy(enemy);
        }
    }

    private void Update()
    {
        // Update active noise events list - remove expired events
        for (int i = _activeNoiseEvents.Count - 1; i >= 0; i--)
        {
            if (Time.time > _activeNoiseEvents[i].startTime + _activeNoiseEvents[i].duration)
            {
                _activeNoiseEvents.RemoveAt(i);
            }
        }
    }

    public void RegisterEnemy(BaseEnemy enemy)
    {
        if (!_enemies.Contains(enemy))
        {
            _enemies.Add(enemy);
        }
    }

    public void UnregisterEnemy(BaseEnemy enemy)
    {
        if (_enemies.Contains(enemy))
        {
            _enemies.Remove(enemy);
        }
    }

    public void GenerateNoise(Vector3 position, float noiseLevel)
    {
        // Notify enemies
        foreach (var enemy in _enemies)
        {
            enemy.OnNoiseHeard(position, noiseLevel);
        }

        // Add to active noise events for visualization
        if (_showNoiseGizmos)
        {
            NoiseEvent newEvent = new NoiseEvent
            {
                position = position,
                intensity = noiseLevel,
                maxRadius = Mathf.Min(noiseLevel * 2.0f, _maxNoiseDistance),
                startTime = Time.time,
                duration = _defaultNoiseDuration
            };
            _activeNoiseEvents.Add(newEvent);
        }
    }

    private void OnDrawGizmos()
    {
        if (!_showNoiseGizmos || Application.isPlaying == false)
            return;

        foreach (var noiseEvent in _activeNoiseEvents)
        {
            // Calculate time progression (0 to 1)
            float timeProgress = (Time.time - noiseEvent.startTime) / noiseEvent.duration;
            if (timeProgress > 1)
                continue;

            // Calculate current radius based on time (grows over time)
            float currentRadius = Mathf.Lerp(0.5f, noiseEvent.maxRadius, timeProgress);

            // Calculate color based on noise intensity
            float t = Mathf.Clamp01(noiseEvent.intensity / _highNoiseThreshold);
            Color gizmoColor = Color.Lerp(_lowNoiseColor, _highNoiseColor, t);

            // Fade opacity over time
            gizmoColor.a *= (1 - timeProgress);

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(noiseEvent.position, currentRadius);

            // Draw a smaller solid sphere at the center
            Gizmos.DrawSphere(noiseEvent.position, 0.2f);
        }
    }
}
