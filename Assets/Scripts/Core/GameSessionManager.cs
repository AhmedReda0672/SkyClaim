using System.Collections;
using UnityEngine;

public class GameSessionManager : MonoBehaviour
{
    public static GameSessionManager Instance { get; private set; }

    [Header("References")]
    public EnemySpawner enemySpawner;
    public MineSpawner mineSpawner;
    public PowerUpManager powerUpManager;
    public TerritoryGrid territoryGrid;
    public PlayerController playerController;
    public TimerManager timerManager;

    private bool _levelComplete = false;
    private bool _isRespawning = false;
    private LevelConfig _config;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(InitializeLevel());
    }

    private IEnumerator InitializeLevel()
    {
        yield return null; // Wait one frame for all Awakes to complete

        _levelComplete = false;
        _config = GameManager.Instance != null ? GameManager.Instance.GetCurrentLevelConfig() : new LevelConfig { ballCount = 3, targetPercent = 60f, timerSeconds = 180f, mineCount = 1 };

        // Initialize territory grid
        if (territoryGrid != null)
        {
            territoryGrid.Initialize();
            territoryGrid.OnCapturedPercentChanged += OnCapturedPercentChanged;
        }

        yield return null;

        // Spawn enemies
        if (enemySpawner != null)
        {
            enemySpawner.SpawnEnemies(_config.ballCount, 4f);
        }

        yield return null;

        // Spawn mines after some captured area exists
        if (mineSpawner != null)
        {
            mineSpawner.SpawnMines(_config.mineCount, 2.5f);
        }

        yield return null;

        // Schedule power-up spawns
        if (powerUpManager != null)
        {
            powerUpManager.ScheduleSpawns(3);
        }

        // Start timer
        if (timerManager != null)
        {
            timerManager.OnTimeExpired += OnTimerExpired;
            timerManager.StartTimer(_config.timerSeconds);
        }

        // Respawn player
        if (playerController != null)
        {
            Vector3 spawnPos = territoryGrid != null ? territoryGrid.GetSpawnPosition() : Vector3.zero;
            playerController.Respawn(spawnPos);
        }

        // Set game state to playing
        GameManager.Instance?.SetState(GameState.Playing);
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
            return;

        if (_levelComplete) return;

        // Check win condition
        if (territoryGrid != null && _config != null)
        {
            float captured = territoryGrid.GetCapturedPercent();
            if (captured >= _config.targetPercent)
            {
                CompleteLevel();
            }
        }
    }

    private void OnCapturedPercentChanged(float percent)
    {
        if (!_levelComplete && _config != null && percent >= _config.targetPercent)
        {
            CompleteLevel();
        }
    }

    private void CompleteLevel()
    {
        if (_levelComplete) return;
        _levelComplete = true;

        // Award time bonus once
        int bonus = 0;
        if (timerManager != null && ScoreManager.Instance != null)
        {
            float remaining = timerManager.GetRemainingTime();
            timerManager.StopTimer();
            bonus = ScoreManager.Instance.CalculateTimeBonus(remaining);
        }

        GameManager.Instance?.CompleteLevel();

        int currentLvl = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1;
        float captured = territoryGrid != null ? territoryGrid.GetCapturedPercent() : 0f;
        int totalScore = GameManager.Instance != null ? GameManager.Instance.TotalScore : 0;

        if (currentLvl >= 3)
        {
            GameManager.Instance?.SetState(GameState.Victory);
            UIManager.Instance?.ShowVictory(totalScore);
        }
        else
        {
            UIManager.Instance?.ShowLevelComplete(currentLvl, captured, totalScore, bonus);
        }
    }

    public void OnPlayerHit()
    {
        if (_isRespawning) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState == GameState.LifeLost ||
            GameManager.Instance.CurrentState == GameState.GameOver) return;

        StartCoroutine(HandlePlayerHit());
    }

    private IEnumerator HandlePlayerHit()
    {
        _isRespawning = true;

        // Clear trail
        if (territoryGrid != null)
            territoryGrid.ClearTrail();

        if (CameraController.Instance != null)
            CameraController.Instance.TriggerShake(0.35f, 0.25f);

        // Play death animation
        if (playerController != null)
            playerController.PlayDeathAnimation();

        GameManager.Instance?.LoseLife();

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GameOver)
        {
            _isRespawning = false;
            yield break;
        }

        yield return new WaitForSeconds(1.5f);

        // Respawn
        if (playerController != null && territoryGrid != null)
        {
            Vector3 spawnPos = territoryGrid.GetSpawnPosition();
            playerController.Respawn(spawnPos);
        }

        GameManager.Instance?.SetState(GameState.Playing);
        _isRespawning = false;
    }

    public void OnTimerExpired()
    {
        if (_levelComplete) return;
        GameManager.Instance?.GameOver();
    }

    private void OnDestroy()
    {
        if (territoryGrid != null)
            territoryGrid.OnCapturedPercentChanged -= OnCapturedPercentChanged;

        if (timerManager != null)
            timerManager.OnTimeExpired -= OnTimerExpired;
    }
}
