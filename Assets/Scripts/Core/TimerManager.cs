using System;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
    public static TimerManager Instance { get; private set; }

    public event Action<float> OnTimeChanged;
    public event Action OnTimeExpired;

    private float _totalTime = 180f;
    private float _remainingTime = 180f;
    private bool _isRunning = false;
    private bool _isPaused = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (!_isRunning || _isPaused) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

        _remainingTime -= Time.deltaTime;

        if (_remainingTime <= 0f)
        {
            _remainingTime = 0f;
            _isRunning = false;
            OnTimeChanged?.Invoke(_remainingTime);
            OnTimeExpired?.Invoke();
            return;
        }

        OnTimeChanged?.Invoke(_remainingTime);
    }

    public void StartTimer(float seconds)
    {
        _totalTime = seconds;
        _remainingTime = seconds;
        _isRunning = true;
        _isPaused = false;
        OnTimeChanged?.Invoke(_remainingTime);
    }

    public void StopTimer()
    {
        _isRunning = false;
    }

    public void PauseTimer()
    {
        _isPaused = true;
    }

    public void ResumeTimer()
    {
        _isPaused = false;
    }

    public void AddTime(float seconds)
    {
        _remainingTime = Mathf.Clamp(_remainingTime + seconds, 0f, _totalTime + seconds);
        OnTimeChanged?.Invoke(_remainingTime);
    }

    public float GetRemainingTime()
    {
        return _remainingTime;
    }

    /// <summary>Returns time bonus points at 5 pts per second remaining.</summary>
    public int GetTimeBonusPoints()
    {
        return Mathf.RoundToInt(_remainingTime * 5f);
    }
}
