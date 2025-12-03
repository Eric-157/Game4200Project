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
        // Show/hide pause menu based on pause state
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(PauseManager.Instance.IsPaused);
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
