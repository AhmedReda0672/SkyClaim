using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central audio manager for SkyClaim.
/// Includes procedural audio waveform synthesis to ensure 100% self-contained audio
/// without requiring external media asset files.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Music Tracks")]
    public AudioClip menuMusic;
    public AudioClip[] levelMusic;

    [Header("Sound Effects")]
    public AudioClip captureSound;
    public AudioClip deathSound;
    public AudioClip powerupSound;
    public AudioClip mineSound;
    public AudioClip levelCompleteSound;
    public AudioClip gameOverSound;
    public AudioClip trailSound;

    private readonly Dictionary<string, AudioClip> _sfxMap = new Dictionary<string, AudioClip>();
    private float _musicVolume = 0.75f;
    private float _sfxVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure audio sources exist
        if (musicSource == null)
        {
            var mGO = new GameObject("MusicSource");
            mGO.transform.SetParent(transform);
            musicSource = mGO.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            var sGO = new GameObject("SFXSource");
            sGO.transform.SetParent(transform);
            sfxSource = sGO.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        // Generate procedural audio clips if not assigned
        GenerateProceduralClips();
        BuildSFXMap();
        LoadVolumes();
    }

    private void BuildSFXMap()
    {
        _sfxMap.Clear();
        if (captureSound)       _sfxMap["capture"]       = captureSound;
        if (deathSound)         _sfxMap["death"]         = deathSound;
        if (powerupSound)       _sfxMap["powerup"]       = powerupSound;
        if (mineSound)          _sfxMap["mine"]          = mineSound;
        if (levelCompleteSound) _sfxMap["levelcomplete"] = levelCompleteSound;
        if (gameOverSound)      _sfxMap["gameover"]      = gameOverSound;
        if (trailSound)         _sfxMap["trail"]         = trailSound;
    }

    private void LoadVolumes()
    {
        if (SaveManager.Instance != null)
        {
            _musicVolume = SaveManager.Instance.GetMusicVolume();
            _sfxVolume = SaveManager.Instance.GetSFXVolume();
        }

        if (musicSource) musicSource.volume = _musicVolume;
        if (sfxSource) sfxSource.volume = _sfxVolume;
    }

    public void PlayMusic(int levelIndex)
    {
        if (musicSource == null) return;

        AudioClip clipToPlay = null;
        if (levelIndex < 0)
        {
            clipToPlay = menuMusic;
        }
        else if (levelMusic != null && levelIndex < levelMusic.Length)
        {
            clipToPlay = levelMusic[levelIndex];
        }

        if (clipToPlay != null && musicSource.clip != clipToPlay)
        {
            musicSource.clip = clipToPlay;
            musicSource.volume = _musicVolume;
            musicSource.Play();
        }
    }

    public void PlaySFX(string key)
    {
        if (sfxSource == null) return;
        if (_sfxMap.TryGetValue(key, out AudioClip clip) && clip != null)
        {
            sfxSource.PlayOneShot(clip, _sfxVolume);
        }
    }

    public void SetMusicVolume(float volume)
    {
        _musicVolume = Mathf.Clamp01(volume);
        if (musicSource) musicSource.volume = _musicVolume;
    }

    public void SetSFXVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource) sfxSource.volume = _sfxVolume;
    }

    // ─── Procedural Audio Synthesis ──────────────────────────────────────────

    private void GenerateProceduralClips()
    {
        int sampleRate = 44100;

        if (trailSound == null)
            trailSound = CreateBeepClip("SFX_Trail", 0.04f, 880f, sampleRate);

        if (captureSound == null)
            captureSound = CreateChimeClip("SFX_Capture", sampleRate);

        if (powerupSound == null)
            powerupSound = CreateArpeggioClip("SFX_PowerUp", sampleRate);

        if (deathSound == null)
            deathSound = CreateNoiseSweepClip("SFX_Death", 0.6f, sampleRate);

        if (mineSound == null)
            mineSound = CreateMineExplosionClip("SFX_Mine", sampleRate);

        if (levelCompleteSound == null)
            levelCompleteSound = CreateFanfareClip("SFX_LevelComplete", sampleRate);

        if (gameOverSound == null)
            gameOverSound = CreateSadFanfareClip("SFX_GameOver", sampleRate);

        if (menuMusic == null)
            menuMusic = CreateMusicLoop("Music_Menu", 65.4f, 4.0f, sampleRate);

        if (levelMusic == null || levelMusic.Length < 3)
        {
            levelMusic = new AudioClip[3];
            levelMusic[0] = CreateMusicLoop("Music_Level1", 130.8f, 3.2f, sampleRate);
            levelMusic[1] = CreateMusicLoop("Music_Level2", 146.8f, 2.8f, sampleRate);
            levelMusic[2] = CreateMusicLoop("Music_Level3", 164.8f, 2.4f, sampleRate);
        }
    }

    private AudioClip CreateBeepClip(string name, float duration, float freq, int sampleRate)
    {
        int samples = Mathf.CeilToInt(duration * sampleRate);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float env = 1f - ((float)i / samples);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.3f;
        }
        var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateChimeClip(string name, int sampleRate)
    {
        float duration = 0.35f;
        int samples = Mathf.CeilToInt(duration * sampleRate);
        float[] data = new float[samples];
        float[] freqs = { 523.25f, 659.25f, 783.99f, 1046.50f }; // C5, E5, G5, C6

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Exp(-t * 8f);
            float s = 0f;
            foreach (var f in freqs)
                s += Mathf.Sin(2f * Mathf.PI * f * t);
            data[i] = (s / freqs.Length) * env * 0.45f;
        }
        var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateArpeggioClip(string name, int sampleRate)
    {
        float duration = 0.4f;
        int samples = Mathf.CeilToInt(duration * sampleRate);
        float[] data = new float[samples];
        float[] notes = { 440f, 554.37f, 659.25f, 880f }; // A4, C#5, E5, A5

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            int noteIdx = Mathf.Clamp(Mathf.FloorToInt(t / (duration / notes.Length)), 0, notes.Length - 1);
            float freq = notes[noteIdx];
            float noteT = t % (duration / notes.Length);
            float env = 1f - (noteT / (duration / notes.Length));
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.4f;
        }
        var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateNoiseSweepClip(string name, float duration, int sampleRate)
    {
        int samples = Mathf.CeilToInt(duration * sampleRate);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float progress = (float)i / samples;
            float noise = (Random.value * 2f - 1f);
            float pitch = Mathf.Lerp(400f, 60f, progress);
            float tone = Mathf.Sin(2f * Mathf.PI * pitch * t);
            float env = (1f - progress);
            data[i] = (noise * 0.4f + tone * 0.6f) * env * 0.5f;
        }
        var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateMineExplosionClip(string name, int sampleRate)
    {
        float duration = 0.5f;
        int samples = Mathf.CeilToInt(duration * sampleRate);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float progress = (float)i / samples;
            float noise = (Random.value * 2f - 1f);
            float env = Mathf.Exp(-progress * 7f);
            data[i] = noise * env * 0.6f;
        }
        var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateFanfareClip(string name, int sampleRate)
    {
        float duration = 1.0f;
        int samples = Mathf.CeilToInt(duration * sampleRate);
        float[] data = new float[samples];
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f };

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            int idx = Mathf.Clamp(Mathf.FloorToInt(t / 0.25f), 0, notes.Length - 1);
            float freq = notes[idx];
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.35f;
        }
        var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateSadFanfareClip(string name, int sampleRate)
    {
        float duration = 1.2f;
        int samples = Mathf.CeilToInt(duration * sampleRate);
        float[] data = new float[samples];
        float[] notes = { 440f, 415.3f, 392f, 349.23f }; // A, G#, G, F

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            int idx = Mathf.Clamp(Mathf.FloorToInt(t / 0.3f), 0, notes.Length - 1);
            float freq = notes[idx];
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.35f;
        }
        var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateMusicLoop(string name, float baseFreq, float duration, int sampleRate)
    {
        int samples = Mathf.CeilToInt(duration * sampleRate);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float bass = Mathf.Sin(2f * Mathf.PI * baseFreq * t) * 0.2f;
            float harm = Mathf.Sin(2f * Mathf.PI * (baseFreq * 1.5f) * t) * 0.1f;
            float pulse = Mathf.Sin(2f * Mathf.PI * 4f * t) > 0 ? 0.05f : 0f;
            data[i] = (bass + harm + pulse) * 0.4f;
        }
        var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
