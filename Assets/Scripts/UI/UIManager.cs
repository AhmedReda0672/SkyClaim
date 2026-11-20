using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls in-game HUD, overlay panels, and UI status updates.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD Labels")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI capturedPercentText;
    public TextMeshProUGUI targetPercentText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI scoreText;
    public Slider captureProgressSlider;
    public TextMeshProUGUI powerUpText;

    [Header("Panels")]
    public GameObject hudPanel;
    public GameObject pausePanel;
    public GameObject levelCompletePanel;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    [Header("Level Complete References")]
    public TextMeshProUGUI lcLevelText;
    public TextMeshProUGUI lcCapturedText;
    public TextMeshProUGUI lcScoreText;
    public TextMeshProUGUI lcTimeBonusText;

    [Header("Game Over References")]
    public TextMeshProUGUI goFinalScoreText;
    public TextMeshProUGUI goBestScoreText;

    [Header("Victory References")]
    public TextMeshProUGUI victoryScoreText;
    public TextMeshProUGUI victoryPercentText;

    private Coroutine _powerUpCoroutine;

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
        // Subscribe to game state events
        GameManager.OnLivesChanged += UpdateLivesDisplay;
        GameManager.OnScoreChanged += UpdateScoreDisplay;
        GameManager.OnLevelChanged += UpdateLevelDisplay;
        GameManager.OnStateChanged += HandleGameStateChanged;

        if (TerritoryGrid.Instance != null)
            TerritoryGrid.Instance.OnCapturedPercentChanged += UpdateCapturedPercentDisplay;

        if (TimerManager.Instance != null)
            TimerManager.Instance.OnTimeChanged += UpdateTimerDisplay;

        // Initialize display values from managers
        if (GameManager.Instance != null)
        {
            UpdateLivesDisplay(GameManager.Instance.TotalLives);
            UpdateScoreDisplay(GameManager.Instance.TotalScore);
            UpdateLevelDisplay(GameManager.Instance.CurrentLevel);

            LevelConfig cfg = GameManager.Instance.GetCurrentLevelConfig();
            if (targetPercentText != null && cfg != null)
                targetPercentText.text = $"Target: {cfg.targetPercent:0}%";

            if (captureProgressSlider != null && cfg != null)
                captureProgressSlider.maxValue = cfg.targetPercent;
        }

        HideAllOverlays();
        if (hudPanel != null) hudPanel.SetActive(true);
        WireButtons();
    }

    private void WireButtons()
    {
        if (pausePanel != null)
        {
            pausePanel.transform.Find("ResumeButton")?.GetComponent<Button>()?.onClick.AddListener(OnResumeButton);
            pausePanel.transform.Find("RestartButton")?.GetComponent<Button>()?.onClick.AddListener(OnRestartButton);
            pausePanel.transform.Find("MenuFromPauseButton")?.GetComponent<Button>()?.onClick.AddListener(OnMainMenuButton);
        }

        if (levelCompletePanel != null)
        {
            levelCompletePanel.transform.Find("NextLevelButton")?.GetComponent<Button>()?.onClick.AddListener(OnNextLevelButton);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.transform.Find("NewGameButton")?.GetComponent<Button>()?.onClick.AddListener(OnNewGameButton);
            gameOverPanel.transform.Find("MenuFromGOButton")?.GetComponent<Button>()?.onClick.AddListener(OnMainMenuButton);
        }

        if (victoryPanel != null)
        {
            victoryPanel.transform.Find("PlayAgainButton")?.GetComponent<Button>()?.onClick.AddListener(OnNewGameButton);
            victoryPanel.transform.Find("MenuFromVictoryButton")?.GetComponent<Button>()?.onClick.AddListener(OnMainMenuButton);
        }
    }

    private void HideAllOverlays()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    private void HandleGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Paused:
                ShowPauseMenu();
                break;
            case GameState.Playing:
                HidePauseMenu();
                break;
            case GameState.LevelComplete:
                // Handled in ShowLevelComplete
                break;
            case GameState.GameOver:
                ShowGameOver(GameManager.Instance != null ? GameManager.Instance.TotalScore : 0);
                break;
            case GameState.Victory:
                ShowVictory(GameManager.Instance != null ? GameManager.Instance.TotalScore : 0);
                break;
        }
    }

    public void UpdateLivesDisplay(int lives)
    {
        if (livesText != null)
        {
            string hearts = "";
            for (int i = 0; i < lives; i++) hearts += "❤ ";
            livesText.text = $"<color=#FF2244>{hearts.Trim()}</color> ({lives})";
        }
    }

    public void UpdateScoreDisplay(int score)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score:N0}";
    }

    public void UpdateLevelDisplay(int level)
    {
        if (levelText != null)
            levelText.text = $"Level {level}";

        if (GameManager.Instance != null)
        {
            LevelConfig cfg = GameManager.Instance.GetCurrentLevelConfig();
            if (targetPercentText != null && cfg != null)
                targetPercentText.text = $"Target: {cfg.targetPercent:0}%";
            if (captureProgressSlider != null && cfg != null)
                captureProgressSlider.maxValue = cfg.targetPercent;
        }
    }

    public void UpdateCapturedPercentDisplay(float percent)
    {
        if (capturedPercentText != null)
            capturedPercentText.text = $"Captured: {percent:0.0}%";

        if (captureProgressSlider != null)
            captureProgressSlider.value = percent;
    }

    public void UpdateTimerDisplay(float secondsRemaining)
    {
        if (timerText != null)
        {
            int mins = Mathf.FloorToInt(secondsRemaining / 60f);
            int secs = Mathf.FloorToInt(secondsRemaining % 60f);
            timerText.text = $"⏱ {mins:00}:{secs:00}";

            // Turn red when below 30 seconds
            timerText.color = secondsRemaining < 30f ? Color.red : Color.white;
        }
    }

    public void ShowPauseMenu()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void HidePauseMenu()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void ShowLevelComplete(int level, float capturedPercent, int score, int timeBonus)
    {
        HideAllOverlays();
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
            if (lcLevelText != null) lcLevelText.text = $"Level {level} Complete!";
            if (lcCapturedText != null) lcCapturedText.text = $"Captured: {capturedPercent:0.0}%";
            if (lcScoreText != null) lcScoreText.text = $"Score: {score:N0}";
            if (lcTimeBonusText != null) lcTimeBonusText.text = $"Time Bonus: +{timeBonus:N0}";
        }
    }

    public void ShowGameOver(int finalScore)
    {
        HideAllOverlays();
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (goFinalScoreText != null) goFinalScoreText.text = $"Final Score: {finalScore:N0}";
            int best = SaveManager.Instance != null ? SaveManager.Instance.GetBestScore() : 0;
            if (goBestScoreText != null) goBestScoreText.text = $"Best Score: {best:N0}";
        }
    }

    public void ShowVictory(int finalScore)
    {
        HideAllOverlays();
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            if (victoryScoreText != null) victoryScoreText.text = $"Final Score: {finalScore:N0}";
            float cap = TerritoryGrid.Instance != null ? TerritoryGrid.Instance.GetCapturedPercent() : 0f;
            if (victoryPercentText != null) victoryPercentText.text = $"Territory Captured: {cap:0.0}%";
        }
    }

    public void ShowPowerUpIndicator(PowerUpType type, float duration)
    {
        if (powerUpText == null) return;
        if (_powerUpCoroutine != null) StopCoroutine(_powerUpCoroutine);
        _powerUpCoroutine = StartCoroutine(PowerUpIndicatorRoutine(type, duration));
    }

    private IEnumerator PowerUpIndicatorRoutine(PowerUpType type, float duration)
    {
        string[] titles = {
            "<color=#FF2244>❤ EXTRA LIFE (+1)!</color>",
            "<color=#00E5FF>⏰ EXTRA TIME (+30s)!</color>",
            "<color=#64B5F6>⏳ ENEMIES SLOWED!</color>",
            "<color=#FFEB3B>⚡ SPEED BOOST!</color>",
            "<color=#FFD700>★ SCORE BONUS (+250)!</color>"
        };
        powerUpText.text = titles[(int)type % titles.Length];
        powerUpText.gameObject.SetActive(true);
        yield return new WaitForSeconds(Mathf.Max(duration, 3f));
        powerUpText.gameObject.SetActive(false);
    }

    // ─── Button Listeners ──────────────────────────────────────────────────

    public void OnResumeButton() => GameManager.Instance?.ResumeGame();
    public void OnRestartButton() => GameManager.Instance?.RestartLevel();
    public void OnMainMenuButton() => GameManager.Instance?.GoToMainMenu();
    public void OnNextLevelButton() => GameManager.Instance?.ProceedToNextLevel();
    public void OnNewGameButton() => GameManager.Instance?.StartNewGame();

    private void OnDestroy()
    {
        GameManager.OnLivesChanged -= UpdateLivesDisplay;
        GameManager.OnScoreChanged -= UpdateScoreDisplay;
        GameManager.OnLevelChanged -= UpdateLevelDisplay;
        GameManager.OnStateChanged -= HandleGameStateChanged;

        if (TerritoryGrid.Instance != null)
            TerritoryGrid.Instance.OnCapturedPercentChanged -= UpdateCapturedPercentDisplay;

        if (TimerManager.Instance != null)
            TimerManager.Instance.OnTimeChanged -= UpdateTimerDisplay;
    }
}
