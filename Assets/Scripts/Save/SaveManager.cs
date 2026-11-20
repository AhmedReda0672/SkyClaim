using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string KEY_BEST_SCORE = "BestScore";
    private const string KEY_UNLOCKED_LEVEL = "UnlockedLevel";
    private const string KEY_BEST_PERCENT = "BestPercent";
    private const string KEY_MUSIC_VOLUME = "MusicVolume";
    private const string KEY_SFX_VOLUME = "SFXVolume";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize defaults if not set
        if (!PlayerPrefs.HasKey(KEY_UNLOCKED_LEVEL))
            PlayerPrefs.SetInt(KEY_UNLOCKED_LEVEL, 1);
        if (!PlayerPrefs.HasKey(KEY_MUSIC_VOLUME))
            PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, 0.75f);
        if (!PlayerPrefs.HasKey(KEY_SFX_VOLUME))
            PlayerPrefs.SetFloat(KEY_SFX_VOLUME, 1f);
    }

    // Best Score
    public int GetBestScore() => PlayerPrefs.GetInt(KEY_BEST_SCORE, 0);
    public void SetBestScore(int score)
    {
        PlayerPrefs.SetInt(KEY_BEST_SCORE, score);
        PlayerPrefs.Save();
    }

    // Unlocked Level (1-3)
    public int GetUnlockedLevel() => PlayerPrefs.GetInt(KEY_UNLOCKED_LEVEL, 1);
    public void SetUnlockedLevel(int level)
    {
        int clamped = Mathf.Clamp(level, 1, 3);
        int current = GetUnlockedLevel();
        if (clamped > current)
        {
            PlayerPrefs.SetInt(KEY_UNLOCKED_LEVEL, clamped);
            PlayerPrefs.Save();
        }
    }

    // Best Percent
    public float GetBestPercent() => PlayerPrefs.GetFloat(KEY_BEST_PERCENT, 0f);
    public void SetBestPercent(float percent)
    {
        if (percent > GetBestPercent())
        {
            PlayerPrefs.SetFloat(KEY_BEST_PERCENT, percent);
            PlayerPrefs.Save();
        }
    }

    // Music Volume
    public float GetMusicVolume() => PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME, 0.75f);
    public void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, Mathf.Clamp01(volume));
        PlayerPrefs.Save();
        AudioManager.Instance?.SetMusicVolume(volume);
    }

    // SFX Volume
    public float GetSFXVolume() => PlayerPrefs.GetFloat(KEY_SFX_VOLUME, 1f);
    public void SetSFXVolume(float volume)
    {
        PlayerPrefs.SetFloat(KEY_SFX_VOLUME, Mathf.Clamp01(volume));
        PlayerPrefs.Save();
        AudioManager.Instance?.SetSFXVolume(volume);
    }

    public void ResetAll()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
