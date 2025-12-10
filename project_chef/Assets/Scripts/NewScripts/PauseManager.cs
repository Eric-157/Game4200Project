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
    // time (unscaled) when pause was last toggled
    public float lastToggleTime = -999f;
    // minimum time between toggles to avoid immediate double-toggle (seconds, unscaled)
    public float toggleDebounce = 0.15f;

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
            // Debounce repeated toggles (use unscaled time so it's independent of Time.timeScale)
            if (Time.unscaledTime - lastToggleTime < toggleDebounce) return;

            // Do not allow pausing/resuming if the main menu is open
            var mm = FindObjectOfType<MainMenu>();
            if (mm != null && mm.mainMenuPanel != null && mm.mainMenuPanel.activeSelf) return;

            // Do not allow pausing/resuming if the player is dead
            var ps = FindObjectOfType<PlayerStats>();
            if (ps != null && ps.currentHP <= 0) return;

            if (IsPaused)
                Resume();
            else
                Pause();

            lastToggleTime = Time.unscaledTime;
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
        lastToggleTime = Time.unscaledTime;
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
        lastToggleTime = Time.unscaledTime;
        Debug.Log("[PauseManager] Game resumed.");
    }
}
