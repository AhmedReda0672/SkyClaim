using UnityEngine;

/// <summary>
/// Ensures persistent singletons (GameManager, SaveManager, AudioManager) exist
/// regardless of which scene is loaded first.
/// </summary>
public class SceneBootstrapper : MonoBehaviour
{
    private void Awake()
    {
        EnsureManager<SaveManager>("SaveManager");
        EnsureManager<AudioManager>("AudioManager");
        EnsureManager<GameManager>("GameManager");
    }

    private void EnsureManager<T>(string objectName) where T : Component
    {
        if (FindObjectOfType<T>() == null)
        {
            GameObject go = new GameObject(objectName);
            go.AddComponent<T>();
        }
    }
}
