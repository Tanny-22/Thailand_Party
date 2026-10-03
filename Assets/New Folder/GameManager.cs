using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int score = 0;
    public int totalCoins = 0;

    public TextMeshProUGUI scoreText;
    public GameObject winPanel;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UpdateUI();
        winPanel.SetActive(false);
    }

    public void AddScore()
    {
        score++;
        UpdateUI();

        if (score >= totalCoins)
        {
            winPanel.SetActive(true);
        }
    }

    void UpdateUI()
    {
        scoreText.text = "Coins: " + score + " / " + totalCoins;
    }
}