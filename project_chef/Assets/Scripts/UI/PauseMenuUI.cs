using UnityEngine;

/// <summary>
/// Manages the pause menu UI.
/// Shows/hides the pause menu panel when the game is paused/resumed.
/// Extend this script or add child Button handlers to implement menu actions (e.g., Resume, Return to Menu, Exit).
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Tooltip("Reference to the pause menu panel (Canvas child) that will be shown/hidden on pause.")]
    public GameObject pauseMenuPanel;

    [Tooltip("Optional: button to resume the game")]
    public UnityEngine.UI.Button resumeButton;

    [Tooltip("Optional: button to return to main menu")]
    public UnityEngine.UI.Button returnToMenuButton;

    [Tooltip("Optional: button to exit the game")]
    public UnityEngine.UI.Button exitGameButton;

    private void Start()
    {
        // Ensure pause menu is hidden at start
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }

    private void Update()
    {
        // Only show the pause menu when the game is paused AND the player is alive.
        if (pauseMenuPanel != null && PauseManager.Instance != null)
        {
            bool isPaused = PauseManager.Instance.IsPaused;

            // determine whether the player is alive
            var ps = FindObjectOfType<PlayerStats>();
            bool playerAlive = ps.currentHP > 0;

            pauseMenuPanel.SetActive(isPaused && playerAlive);

            // Only accept pause-menu related hotkeys when the game is paused and the player is alive
            if (isPaused && playerAlive)
            {
                // Respect PauseManager's toggle debounce to avoid immediate resume from the same keypress
                bool allowedByDebounce = Time.unscaledTime - PauseManager.Instance.lastToggleTime >= PauseManager.Instance.toggleDebounce;
                if (!allowedByDebounce) return;

                if (Input.GetKeyDown(KeyCode.Q))
                {
                    OnReturnToMenuClicked();
                }
                else if (Input.GetKeyDown(KeyCode.M))
                {
                    OnExitGameClicked();
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    OnResumeClicked();
                }
            }
        }
    }

    public void OnResumeClicked()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.Resume();
    }

    public void OnReturnToMenuClicked()
    {
        // Resume the game first (so scene loading works at normal speed)
        if (PauseManager.Instance != null)
            PauseManager.Instance.Resume();
        // If we have an in-scene MainMenu controller, use it. Otherwise fall back to loading the MainMenu scene.
        var mm = FindObjectOfType<MainMenu>();
        if (mm != null)
        {
            // Destroy current room and show the main menu UI
            var gm = FindObjectOfType<GameManager>();
            if (gm != null) gm.ReturnToMainMenu();
            mm.ShowMainMenu();
        }
    }

    public void OnExitGameClicked()
    {
        // Resume the game first
        if (PauseManager.Instance != null)
            PauseManager.Instance.Resume();

        // Exit
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}
