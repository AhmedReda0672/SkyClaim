using UnityEngine;

/// <summary>
/// Scene-level listener for game state events. Handles audio and HUD transitions.
/// </summary>
public class GameStateListener : MonoBehaviour
{
    private void Start()
    {
        GameManager.OnStateChanged += HandleStateChanged;

        // Play level background music
        if (AudioManager.Instance != null && GameManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(GameManager.Instance.CurrentLevel - 1);
        }
    }

    private void HandleStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Paused:
                UIManager.Instance?.ShowPauseMenu();
                TimerManager.Instance?.PauseTimer();
                break;

            case GameState.Playing:
                UIManager.Instance?.HidePauseMenu();
                TimerManager.Instance?.ResumeTimer();
                break;
        }
    }

    private void OnDestroy()
    {
        GameManager.OnStateChanged -= HandleStateChanged;
    }
}
