using UnityEngine;

public class FoodPickUp : MonoBehaviour
{
    void OnTriggerEnter(Collider other) 
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.IncreaseScore();
            Destroy(gameObject);
        }
     }
}
