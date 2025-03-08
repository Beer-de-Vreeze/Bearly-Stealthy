using UnityEngine;
using VInspector;

[RequireComponent(typeof(Rigidbody))]
public class DistractionObject : MonoBehaviour
{
    [Tab("Properties")]
    [SerializeField]
    private float _weight = 1.0f;

    [SerializeField]
    private bool _canBePickedUp = true;

    [SerializeField]
    private float _noiseMultiplier = 1.0f;

    [Tab("Noise")]
    [SerializeField, ReadOnly]
    private float _currentNoiseLevel = 0f;

    [SerializeField]
    private float _noiseFalloffRate = 5.0f;

    [SerializeField]
    private float _impactNoiseThreshold = 0.2f;

    [Tab("References")]
    [SerializeField, ReadOnly]
    private Rigidbody _rb;

    private Transform _holdPosition;
    private bool _isHeld = false;
    private Vector3 _lastVelocity;
    private float _timeSinceLastNoise = 0f;
    private float _noiseRadius = 10f;

    public bool CanBePickedUp => _canBePickedUp;
    public float NoiseOnDrop => _weight * _noiseMultiplier * 2f;
    public float NoiseOnThrow => _weight * _noiseMultiplier * 4f;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.mass = _weight;
    }

    void Update()
    {
        _timeSinceLastNoise += Time.deltaTime;

        // Update hold position if being held
        if (_isHeld && _holdPosition != null)
        {
            transform.position = _holdPosition.position;
            transform.rotation = _holdPosition.rotation;
        }

        // Calculate noise decay
        if (_currentNoiseLevel > 0)
        {
            _currentNoiseLevel = Mathf.Max(
                0,
                _currentNoiseLevel - Time.deltaTime * _noiseFalloffRate
            );

            // Alert nearby enemies when making noise
            if (_currentNoiseLevel > 0)
            {
                AlertNearbyEnemies();
            }
        }

        _lastVelocity = _rb.linearVelocity;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!_isHeld)
        {
            // Calculate impact force
            float impactForce = collision.relativeVelocity.magnitude;

            // Generate noise based on impact force and weight
            if (impactForce > _impactNoiseThreshold)
            {
                GenerateNoise(impactForce * _weight * _noiseMultiplier);
            }
        }
    }

    public void PickUp(Transform holdPosition)
    {
        _isHeld = true;
        _holdPosition = holdPosition;
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        // Disable collisions with player while held
        Physics.IgnoreCollision(
            GetComponent<Collider>(),
            GameObject.FindGameObjectWithTag("Player").GetComponent<Collider>(),
            true
        );
    }

    public void Drop()
    {
        _isHeld = false;
        _rb.isKinematic = false;
        _rb.useGravity = true;

        // Re-enable collisions with player
        Physics.IgnoreCollision(
            GetComponent<Collider>(),
            GameObject.FindGameObjectWithTag("Player").GetComponent<Collider>(),
            false
        );

        // Generate noise on drop based on weight
        GenerateNoise(NoiseOnDrop);
    }

    public void Throw(Vector3 throwForce)
    {
        _isHeld = false;
        _rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.AddForce(throwForce, ForceMode.Impulse);

        // Re-enable collisions with player
        Physics.IgnoreCollision(
            GetComponent<Collider>(),
            GameObject.FindGameObjectWithTag("Player").GetComponent<Collider>(),
            false
        );

        // Generate noise on throw based on weight and force
        GenerateNoise(NoiseOnThrow);
    }

    public void Push(Vector3 pushForce)
    {
        if (!_isHeld)
        {
            _rb.AddForce(pushForce, ForceMode.Impulse);
            GenerateNoise(pushForce.magnitude * _weight * _noiseMultiplier * 0.5f);
        }
    }

    private void GenerateNoise(float noiseAmount)
    {
        if (_timeSinceLastNoise > 0.1f)
        {
            _currentNoiseLevel = Mathf.Max(_currentNoiseLevel, noiseAmount);
            _timeSinceLastNoise = 0f;

            // Alert nearby enemies immediately when noise is generated
            AlertNearbyEnemies();
        }
    }

    private void AlertNearbyEnemies()
    {
        // Find all enemies in the noise radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _noiseRadius);
        foreach (var hitCollider in hitColliders)
        {
            // Check if the collider belongs to a BaseEnemy or any class that inherits from it
            BaseEnemy enemy = hitCollider.GetComponent<BaseEnemy>();
            if (enemy != null)
            {
                enemy.OnNoiseHeard(transform.position, _currentNoiseLevel);
            }
        }

        Gizmos.DrawSphere(transform.position, _noiseRadius);
        Debug.Log($"Object made noise: {gameObject.name} - Level: {_currentNoiseLevel}");
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f); 
        Gizmos.DrawSphere(transform.position, _noiseRadius);
    }

    void Reset()
    {
        _rb = GetComponent<Rigidbody>();
    }
}
