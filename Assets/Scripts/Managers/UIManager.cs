using TMPro;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    TextMeshProUGUI scoreText;

    void Start()
    {
        scoreText = GameObject.Find("SCORE").GetComponent<TextMeshProUGUI>();
    }

    public void UpdateScore(int score)
    {
        scoreText.text = "SCORE: " + score;
    }
}
