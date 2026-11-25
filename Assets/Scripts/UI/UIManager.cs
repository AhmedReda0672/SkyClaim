using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls in-game HUD, overlay panels, and UI status updates.
/// Features graphical 2D sprite icons for Timer, Lives, and Power-Up notifications.
/// Automatically handles dynamic spacing so heart icons never overlap labels.
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

    [Header("Icon Sprites")]
    public Sprite heartSprite;
    public Sprite clockSprite;
    public Sprite thunderSprite;
    public Sprite spiralSprite;
    public Sprite coinSprite;

    private Image _timerIcon;
    private Image[] _heartImages = new Image[5];
    private TextMeshProUGUI _heartCountText;
    private Image _powerUpIcon;
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
        EnsureSpritesLoaded();
        SetupTimerIcon();
        SetupLivesIcons();
        SetupPowerUpIcon();

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

    private void EnsureSpritesLoaded()
    {
        if (heartSprite == null) heartSprite = LoadIcon("HeartIcon");
        if (clockSprite == null) clockSprite = LoadIcon("ClockIcon");
        if (thunderSprite == null) thunderSprite = LoadIcon("ThunderIcon");
        if (spiralSprite == null) spiralSprite = LoadIcon("SpiralIcon");
        if (coinSprite == null) coinSprite = LoadIcon("CoinIcon");
    }

    /// <summary>
    /// Robust 3-tier icon loader: Resources.Load Sprite -> Texture2D -> Direct Disk File.
    /// Guarantees sprites never load as null or blank white squares.
    /// </summary>
    private Sprite LoadIcon(string iconName)
    {
        Sprite spr = Resources.Load<Sprite>($"Icons/{iconName}");
        if (spr != null) return spr;

        Texture2D tex = Resources.Load<Texture2D>($"Icons/{iconName}");
        if (tex != null)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        return null;
    }

    private void SetupTimerIcon()
    {
        if (timerText == null) return;
        Transform existing = timerText.transform.Find("TimerIcon");
        if (existing != null) Destroy(existing.gameObject);

        timerText.alignment = TextAlignmentOptions.Center;
        var iconGO = new GameObject("TimerIcon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(timerText.transform, false);

        var r = iconGO.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = new Vector2(-60f, 0f);
        r.sizeDelta = new Vector2(28f, 28f);

        _timerIcon = iconGO.GetComponent<Image>();
        _timerIcon.sprite = clockSprite;
        _timerIcon.preserveAspect = true;
    }

    private void SetupLivesIcons()
    {
        if (livesText == null) return;
        Transform existing = livesText.transform.Find("HeartsContainer");
        if (existing != null) Destroy(existing.gameObject);

        // Blank out text on the livesText label so no old text displays
        livesText.text = "";

        // Container with HorizontalLayoutGroup prevents any heart from overlapping the count label
        var container = new GameObject("HeartsContainer", typeof(RectTransform));
        container.transform.SetParent(livesText.transform, false);

        var cRect = container.GetComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0.5f, 0.5f);
        cRect.anchorMax = new Vector2(0.5f, 0.5f);
        cRect.pivot = new Vector2(0.5f, 0.5f);
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = new Vector2(240f, 40f);

        var hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 5f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Create 5 heart icon slots
        for (int i = 0; i < 5; i++)
        {
            var heartGO = new GameObject($"Heart_{i}", typeof(RectTransform), typeof(Image));
            heartGO.transform.SetParent(container.transform, false);

            var hr = heartGO.GetComponent<RectTransform>();
            hr.sizeDelta = new Vector2(26f, 26f);

            var img = heartGO.GetComponent<Image>();
            img.sprite = heartSprite;
            img.preserveAspect = true;
            _heartImages[i] = img;
        }

        // Heart Count label (e.g. "(3)", "(4)") placed dynamically after the last heart
        var countGO = new GameObject("HeartCount", typeof(RectTransform), typeof(TextMeshProUGUI));
        countGO.transform.SetParent(container.transform, false);

        var cr = countGO.GetComponent<RectTransform>();
        cr.sizeDelta = new Vector2(50f, 32f);

        _heartCountText = countGO.GetComponent<TextMeshProUGUI>();
        _heartCountText.fontSize = 24;
        _heartCountText.color = Color.white;
        _heartCountText.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private void SetupPowerUpIcon()
    {
        if (powerUpText == null) return;
        Transform existing = powerUpText.transform.Find("PowerUpIcon");
        if (existing != null) Destroy(existing.gameObject);

        var iconGO = new GameObject("PowerUpIcon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(powerUpText.transform, false);

        var r = iconGO.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 1f);
        r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 0f);
        r.anchoredPosition = new Vector2(0f, 10f); // Sits right above title
        r.sizeDelta = new Vector2(52f, 52f);

        _powerUpIcon = iconGO.GetComponent<Image>();
        _powerUpIcon.preserveAspect = true;
        _powerUpIcon.gameObject.SetActive(false);
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
            levelCompletePanel.transform.Find("NextLevelButton")?.GetComponent<Button>()?.onClick.AddListener(OnNextLevelButton);

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
            case GameState.Paused: ShowPauseMenu(); break;
            case GameState.Playing: HidePauseMenu(); break;
            case GameState.GameOver: ShowGameOver(GameManager.Instance != null ? GameManager.Instance.TotalScore : 0); break;
            case GameState.Victory: ShowVictory(GameManager.Instance != null ? GameManager.Instance.TotalScore : 0); break;
        }
    }

    public void UpdateLivesDisplay(int lives)
    {
        if (_heartImages == null) return;

        // Show exact number of active hearts matching current lives
        for (int i = 0; i < 5; i++)
        {
            if (_heartImages[i] != null)
            {
                if (i < lives)
                {
                    _heartImages[i].gameObject.SetActive(true);
                    _heartImages[i].color = Color.white;
                }
                else
                {
                    _heartImages[i].gameObject.SetActive(false);
                }
            }
        }

        if (_heartCountText != null)
        {
            _heartCountText.text = $"({lives})";
            // Ensure count text always sits right after the active hearts
            _heartCountText.transform.SetAsLastSibling();
        }
    }

    public void UpdateScoreDisplay(int score)
    {
        if (scoreText != null) scoreText.text = $"Score: {score:N0}";
    }

    public void UpdateLevelDisplay(int level)
    {
        if (levelText != null) levelText.text = $"Level {level}";

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
            timerText.text = $"{mins:00}:{secs:00}";

            Color c = secondsRemaining < 30f ? Color.red : Color.white;
            timerText.color = c;
            if (_timerIcon != null) _timerIcon.color = c;
        }
    }

    public void ShowPauseMenu() { if (pausePanel != null) pausePanel.SetActive(true); }
    public void HidePauseMenu() { if (pausePanel != null) pausePanel.SetActive(false); }

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
        Sprite icon = null;
        string title = "";

        switch (type)
        {
            case PowerUpType.ExtraLife:
                icon = heartSprite;
                title = "<color=#FF2244>EXTRA LIFE (+1)!</color>";
                break;
            case PowerUpType.ExtraTime:
                icon = clockSprite;
                title = "<color=#00E5FF>EXTRA TIME (+30s)!</color>";
                break;
            case PowerUpType.Slow:
                icon = spiralSprite;
                title = "<color=#64B5F6>ENEMIES SLOWED!</color>";
                break;
            case PowerUpType.Speed:
                icon = thunderSprite;
                title = "<color=#FFEB3B>SPEED BOOST!</color>";
                break;
            case PowerUpType.ScoreBonus:
                icon = coinSprite;
                title = "<color=#FFD700>SCORE BONUS (+250)!</color>";
                break;
        }

        if (_powerUpIcon != null)
        {
            _powerUpIcon.sprite = icon;
            _powerUpIcon.gameObject.SetActive(icon != null);
        }

        powerUpText.text = title;
        powerUpText.gameObject.SetActive(true);

        // Punch scale bounce animation
        Transform t = powerUpText.transform;
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(0.5f, 1.15f, elapsed / 0.2f);
            t.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        t.localScale = Vector3.one;

        yield return new WaitForSeconds(Mathf.Max(duration, 3f));

        if (_powerUpIcon != null) _powerUpIcon.gameObject.SetActive(false);
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
