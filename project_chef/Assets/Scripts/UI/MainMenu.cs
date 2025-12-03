using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main menu UI controller for the single-scene setup.
/// Attach to a GameObject (for example `MainMenuCanvas`) and assign `mainMenuPanel`.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Tooltip("The panel that contains the main menu UI. Enable/disable to show/hide main menu.")]
    public GameObject mainMenuPanel;

    // [Tooltip("If using scene-based loading this is the name of the gameplay scene. Not required for single-scene setup.")]
    // public string gameplaySceneName = "GameScene";

    private void Start()
    {
        // Show the main menu UI by default; Hide it when StartGame() is called
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    /// <summary>
    /// Called by the Start Game button. In the single-scene setup this triggers GameManager.StartGame().
    /// </summary>
    public void StartGame()
    {
        Time.timeScale = 1f;

        var gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            // Hide main menu UI and start the game via GameManager
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            gm.StartGame();
            return;
        }

        // Fallback: if no GameManager found, attempt to load a scene by name
        // if (!string.IsNullOrEmpty(gameplaySceneName))
        // {
        //     var eventSys = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        //     if (eventSys != null) Destroy(eventSys.gameObject);
        //     SceneManager.LoadScene(gameplaySceneName);
        // }
        // else
        // {
        //     Debug.LogWarning("MainMenu.StartGame: No GameManager found and no gameplaySceneName set.");
        // }
    }

    /// <summary>
    /// Show the main menu UI. Called when returning from pause.
    /// </summary>
    public void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    /// <summary>
    /// Hide the main menu UI.
    /// </summary>
    public void HideMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
    }

    /// <summary>
    /// Quit the application or stop Play Mode in the editor.
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
