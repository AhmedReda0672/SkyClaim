using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>Awards 10 points per captured cell.</summary>
    public void AwardCapturedCells(int count)
    {
        int points = count * 10;
        GameManager.Instance?.AddScore(points);
    }

    /// <summary>Awards 50 points for collecting a power-up.</summary>
    public void AwardPowerUp()
    {
        GameManager.Instance?.AddScore(50);
    }

    /// <summary>Calculates time bonus at 5 points per second remaining, adds to score and returns total bonus.</summary>
    public int CalculateTimeBonus(float seconds)
    {
        int bonus = Mathf.RoundToInt(seconds * 5f);
        if (bonus > 0)
        {
            GameManager.Instance?.AddScore(bonus);
        }
        return bonus;
    }
}
