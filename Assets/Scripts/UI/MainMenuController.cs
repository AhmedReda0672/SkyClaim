using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the main menu, level selection (3 levels), and audio settings.
/// Features fully functional and visually rendered Music & SFX sliders with live percentage feedback,
/// reliable Exit handling for both Editor and Standalone builds, and ambient sky integration.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject levelSelectPanel;
    public GameObject settingsPanel;

    [Header("Level Select Buttons")]
    public Button[] levelButtons;
    public GameObject[] levelLocks;
    public TextMeshProUGUI[] difficultyLabels;

    [Header("Settings Controls")]
    public Slider musicSlider;
    public Slider sfxSlider;
    private TextMeshProUGUI _musicPercentText;
    private TextMeshProUGUI _sfxPercentText;

    [Header("Stats")]
    public TextMeshProUGUI bestScoreText;

    private static readonly string[] DifficultyNames = {
        "Beginner (60% Target, 3 Balls)",
        "Medium (70% Target, 4 Balls)",
        "Hard (80% Target, 7 Balls)"
    };

    private void Start()
    {
        // Ensure sky background exists in main menu
        if (FindObjectOfType<SkyEnvironment>() == null)
        {
            var skyGO = new GameObject("SkyEnvironment");
            skyGO.AddComponent<SkyEnvironment>();
        }

        WireButtons();
        ShowMainMenu();

        if (bestScoreText != null && SaveManager.Instance != null)
        {
            bestScoreText.text = $"Best Score: {SaveManager.Instance.GetBestScore():N0}";
        }

        UpdateLevelSelectUI();
        InitializeSettings();

        AudioManager.Instance?.PlayMusic(-1);
    }

    private void WireButtons()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.transform.Find("PlayButton")?.GetComponent<Button>()?.onClick.AddListener(OnPlayButton);
            mainMenuPanel.transform.Find("LevelSelectButton")?.GetComponent<Button>()?.onClick.AddListener(ShowLevelSelect);
            mainMenuPanel.transform.Find("SettingsButton")?.GetComponent<Button>()?.onClick.AddListener(ShowSettings);
            mainMenuPanel.transform.Find("ExitButton")?.GetComponent<Button>()?.onClick.AddListener(OnExitButton);
        }

        if (levelSelectPanel != null)
        {
            levelSelectPanel.transform.Find("BackFromLevelSelectButton")?.GetComponent<Button>()?.onClick.AddListener(ShowMainMenu);
        }

        if (settingsPanel != null)
        {
            settingsPanel.transform.Find("BackFromSettingsButton")?.GetComponent<Button>()?.onClick.AddListener(ShowMainMenu);
        }

        if (levelButtons != null)
        {
            for (int i = 0; i < levelButtons.Length; i++)
            {
                int lvl = i + 1;
                levelButtons[i]?.onClick.AddListener(() => OnLevelButton(lvl));
            }
        }
    }

    private void InitializeSettings()
    {
        // Build and wire Music Slider
        if (musicSlider != null)
        {
            BuildSliderVisuals(musicSlider, new Color(0.1f, 0.8f, 1f, 1f), out _musicPercentText);
            float musicVol = SaveManager.Instance != null ? SaveManager.Instance.GetMusicVolume() : 0.75f;
            musicSlider.value = musicVol;
            if (_musicPercentText != null) _musicPercentText.text = $"{Mathf.RoundToInt(musicVol * 100f)}%";
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        // Build and wire SFX Slider
        if (sfxSlider != null)
        {
            BuildSliderVisuals(sfxSlider, new Color(1f, 0.8f, 0.2f, 1f), out _sfxPercentText);
            float sfxVol = SaveManager.Instance != null ? SaveManager.Instance.GetSFXVolume() : 1f;
            sfxSlider.value = sfxVol;
            if (_sfxPercentText != null) _sfxPercentText.text = $"{Mathf.RoundToInt(sfxVol * 100f)}%";
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }

    /// <summary>
    /// Programmatically generates full visual hierarchy for a Slider if components are missing.
    /// Creates Background, Fill Area, Fill, Handle Slide Area, Handle Knob, and Percentage Text.
    /// </summary>
    private void BuildSliderVisuals(Slider slider, Color fillColor, out TextMeshProUGUI percentLabel)
    {
        percentLabel = null;
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        if (sliderRect == null) sliderRect = slider.gameObject.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(260f, 24f);

        // 1. Background Track
        Transform bgTrans = slider.transform.Find("Background");
        GameObject bgGO = bgTrans != null ? bgTrans.gameObject : new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(slider.transform, false);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.25f);
        bgRect.anchorMax = new Vector2(1f, 0.75f);
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        Image bgImg = bgGO.GetComponent<Image>();
        bgImg.color = new Color(0.15f, 0.18f, 0.25f, 0.95f);

        // 2. Fill Area & Fill
        Transform fillAreaTrans = slider.transform.Find("Fill Area");
        GameObject fillAreaGO = fillAreaTrans != null ? fillAreaTrans.gameObject : new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGO.transform.SetParent(slider.transform, false);
        RectTransform fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.sizeDelta = new Vector2(-12f, 0f);
        fillAreaRect.anchoredPosition = Vector2.zero;

        Transform fillTrans = fillAreaGO.transform.Find("Fill");
        GameObject fillGO = fillTrans != null ? fillTrans.gameObject : new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.sizeDelta = Vector2.zero;
        Image fillImg = fillGO.GetComponent<Image>();
        fillImg.color = fillColor;
        slider.fillRect = fillRect;

        // 3. Handle Slide Area & Handle Knob
        Transform handleAreaTrans = slider.transform.Find("Handle Slide Area");
        GameObject handleAreaGO = handleAreaTrans != null ? handleAreaTrans.gameObject : new GameObject("Handle Slide Area", typeof(RectTransform));
        handleAreaGO.transform.SetParent(slider.transform, false);
        RectTransform handleAreaRect = handleAreaGO.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = new Vector2(-20f, 0f);
        handleAreaRect.anchoredPosition = Vector2.zero;

        Transform handleTrans = handleAreaGO.transform.Find("Handle");
        GameObject handleGO = handleTrans != null ? handleTrans.gameObject : new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGO.transform.SetParent(handleAreaGO.transform, false);
        RectTransform handleRect = handleGO.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(26f, 26f);
        Image handleImg = handleGO.GetComponent<Image>();
        handleImg.color = Color.white;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;

        // 4. Value Percentage Text next to slider
        Transform valTrans = slider.transform.Find("PercentValueText");
        GameObject valGO = valTrans != null ? valTrans.gameObject : new GameObject("PercentValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        valGO.transform.SetParent(slider.transform, false);
        RectTransform valRect = valGO.GetComponent<RectTransform>();
        valRect.anchorMin = new Vector2(1f, 0.5f);
        valRect.anchorMax = new Vector2(1f, 0.5f);
        valRect.anchoredPosition = new Vector2(40f, 0f);
        valRect.sizeDelta = new Vector2(65f, 30f);

        percentLabel = valGO.GetComponent<TextMeshProUGUI>();
        percentLabel.fontSize = 20;
        percentLabel.alignment = TextAlignmentOptions.MidlineLeft;
        percentLabel.color = Color.white;
    }

    public void UpdateLevelSelectUI()
    {
        int unlocked = SaveManager.Instance != null ? SaveManager.Instance.GetUnlockedLevel() : 1;

        if (levelButtons != null)
        {
            for (int i = 0; i < levelButtons.Length && i < 3; i++)
            {
                if (levelButtons[i] == null) continue;
                bool isUnlocked = (i + 1) <= unlocked;
                levelButtons[i].interactable = isUnlocked;

                if (levelLocks != null && i < levelLocks.Length && levelLocks[i] != null)
                    levelLocks[i].SetActive(!isUnlocked);

                if (difficultyLabels != null && i < difficultyLabels.Length && difficultyLabels[i] != null)
                    difficultyLabels[i].text = DifficultyNames[i];
            }
        }
    }

    // ─── Panel Navigation ───────────────────────────────────────────────────

    public void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void ShowLevelSelect()
    {
        UpdateLevelSelectUI();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void ShowSettings()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    // ─── Button Actions ─────────────────────────────────────────────────────

    public void OnPlayButton()
    {
        GameManager.Instance?.StartNewGame();
    }

    public void OnLevelButton(int level)
    {
        int unlocked = SaveManager.Instance != null ? SaveManager.Instance.GetUnlockedLevel() : 1;
        if (level > unlocked) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadLevel(level);
        }
    }

    public void OnExitButton()
    {
        Debug.Log("[MainMenu] Exit requested.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnMusicVolumeChanged(float v)
    {
        if (_musicPercentText != null)
            _musicPercentText.text = $"{Mathf.RoundToInt(v * 100f)}%";

        SaveManager.Instance?.SetMusicVolume(v);
        AudioManager.Instance?.SetMusicVolume(v);
    }

    private void OnSFXVolumeChanged(float v)
    {
        if (_sfxPercentText != null)
            _sfxPercentText.text = $"{Mathf.RoundToInt(v * 100f)}%";

        SaveManager.Instance?.SetSFXVolume(v);
        AudioManager.Instance?.SetSFXVolume(v);
    }
}
