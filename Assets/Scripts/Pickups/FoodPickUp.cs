using UnityEngine;

public class FoodPickUp : MonoBehaviour
{
    [SerializeField]
    private int _scoreValue = 1;
    private float _rotationSpeed = 50f;
    private float _bobSpeed = 2f;
    private float _bobHeight = 0.5f;
    private Vector3 _startPosition;

    void Start()
    {
        _startPosition = transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.IncreaseScore(_scoreValue);
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Rotate the food pickup
        transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);

        // Bob up and down
        float newY = _startPosition.y + Mathf.Sin(Time.time * _bobSpeed) * _bobHeight;
        transform.position = new Vector3(_startPosition.x, newY, _startPosition.z);
    }
}
