using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    MainMenu,
    Loading,
    Playing,
    Paused,
    LifeLost,
    LevelComplete,
    GameOver,
    Victory
}

[System.Serializable]
public class LevelConfig
{
    public int ballCount;
    public float targetPercent;
    public float timerSeconds;
    public int mineCount;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Level configurations
    public LevelConfig[] levelConfigs = new LevelConfig[]
    {
        new LevelConfig { ballCount = 3, targetPercent = 60f, timerSeconds = 180f, mineCount = 1 },
        new LevelConfig { ballCount = 4, targetPercent = 70f, timerSeconds = 150f, mineCount = 2 },
        new LevelConfig { ballCount = 7, targetPercent = 80f, timerSeconds = 120f, mineCount = 3 }
    };

    // Game state
    private GameState _currentState = GameState.MainMenu;
    public GameState CurrentState => _currentState;

    // Player stats
    public int TotalLives { get; private set; } = 3;
    public int TotalScore { get; private set; } = 0;
    public int CurrentLevel { get; private set; } = 1; // 1-based

    // Events
    public static event Action<GameState> OnStateChanged;
    public static event Action<int> OnLivesChanged;
    public static event Action<int> OnScoreChanged;
    public static event Action<int> OnLevelChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetState(GameState newState)
    {
        _currentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    public LevelConfig GetCurrentLevelConfig()
    {
        int idx = Mathf.Clamp(CurrentLevel - 1, 0, levelConfigs.Length - 1);
        return levelConfigs[idx];
    }

    public void StartNewGame()
    {
        TotalLives = 3;
        TotalScore = 0;
        CurrentLevel = 1;
        OnLivesChanged?.Invoke(TotalLives);
        OnScoreChanged?.Invoke(TotalScore);
        OnLevelChanged?.Invoke(CurrentLevel);
        LoadLevel(CurrentLevel);
    }

    public void LoadLevel(int level)
    {
        CurrentLevel = Mathf.Clamp(level, 1, levelConfigs.Length);
        OnLevelChanged?.Invoke(CurrentLevel);
        SetState(GameState.Loading);
        if (Application.isPlaying)
        {
            SceneManager.LoadScene("GameScene");
        }
    }

    public void AddScore(int amount)
    {
        TotalScore += amount;
        OnScoreChanged?.Invoke(TotalScore);

        // Update best score
        if (SaveManager.Instance != null && TotalScore > SaveManager.Instance.GetBestScore())
        {
            SaveManager.Instance.SetBestScore(TotalScore);
        }
    }

    public void AddLife()
    {
        TotalLives = Mathf.Min(TotalLives + 1, 5);
        OnLivesChanged?.Invoke(TotalLives);
    }

    public void LoseLife()
    {
        TotalLives--;
        TotalLives = Mathf.Max(0, TotalLives);
        OnLivesChanged?.Invoke(TotalLives);

        if (TotalLives <= 0)
        {
            GameOver();
        }
        else
        {
            SetState(GameState.LifeLost);
        }
    }

    public void GameOver()
    {
        SetState(GameState.GameOver);
        AudioManager.Instance?.PlaySFX("gameover");
    }

    public void CompleteLevel()
    {
        SetState(GameState.LevelComplete);
        AudioManager.Instance?.PlaySFX("levelcomplete");

        // Unlock next level
        if (SaveManager.Instance != null)
        {
            int nextLevel = CurrentLevel + 1;
            if (nextLevel > SaveManager.Instance.GetUnlockedLevel())
            {
                SaveManager.Instance.SetUnlockedLevel(nextLevel);
            }
        }
    }

    public void ProceedToNextLevel()
    {
        int nextLevel = CurrentLevel + 1;
        if (nextLevel > levelConfigs.Length)
        {
            SetState(GameState.Victory);
            AudioManager.Instance?.PlaySFX("levelcomplete");
        }
        else
        {
            LoadLevel(nextLevel);
        }
    }

    public void PauseGame()
    {
        if (_currentState == GameState.Playing)
        {
            SetState(GameState.Paused);
            Time.timeScale = 0f;
        }
    }

    public void ResumeGame()
    {
        if (_currentState == GameState.Paused)
        {
            SetState(GameState.Playing);
            Time.timeScale = 1f;
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        TotalLives = 3;
        OnLivesChanged?.Invoke(TotalLives);
        LoadLevel(CurrentLevel);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SetState(GameState.MainMenu);
        if (Application.isPlaying)
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
