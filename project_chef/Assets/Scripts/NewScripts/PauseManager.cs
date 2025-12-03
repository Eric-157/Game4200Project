using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Singleton that manages the pause state of the game.
/// When paused, sets Time.timeScale to 0 to freeze all physics/animations.
/// Can be extended to track additional pause-related state (e.g., pause menu open).
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    public bool IsPaused { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        // Toggle pause on Esc press
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }
    }

    /// <summary>
    /// Pause the game by setting Time.timeScale to 0.
    /// This freezes all physics, animations, and coroutines that use WaitForSeconds.
    /// </summary>
    public void Pause()
    {
        if (IsPaused) return;

        IsPaused = true;
        Time.timeScale = 0f;
        Debug.Log("[PauseManager] Game paused.");
    }

    /// <summary>
    /// Resume the game by restoring Time.timeScale to 1.
    /// </summary>
    public void Resume()
    {
        if (!IsPaused) return;

        IsPaused = false;
        Time.timeScale = 1f;
        Debug.Log("[PauseManager] Game resumed.");
    }
}
