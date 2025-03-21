using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    private int _score = 0;

    public void IncreaseScore()
    {
        _score++;
        UIManager.Instance.UpdateScore(_score);
    }
}
