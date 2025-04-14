using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    private int _score = 0;

    public void IncreaseScore(int score)
    {
        _score += score;
        UIManager.Instance.UpdateScore(_score);
    }
}
