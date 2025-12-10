using UnityEngine;

/// <summary>
/// Simple death menu UI controller. Attach to a Canvas GameObject and assign a deathPanel.
/// The panel should contain buttons wired to `OnReturnToMenuClicked` and `OnQuitClicked`.
/// </summary>
public class DeathMenuUI : MonoBehaviour
{
    public GameObject deathPanel;

    private void Start()
    {
        if (deathPanel != null) deathPanel.SetActive(false);
    }

    public void Show()
    {
        if (deathPanel != null) deathPanel.SetActive(true);
    }

    public void Hide()
    {
        if (deathPanel != null) deathPanel.SetActive(false);
    }

    public void Update()
    {
        // if death panel is active, and Q is pressed, return to main menu, or if M is pressed, quit game
        if (!deathPanel.activeSelf && Input.GetKeyDown(KeyCode.Q))
        {
            OnReturnToMenuClicked();
        }
        else if (!deathPanel.activeSelf && Input.GetKeyDown(KeyCode.M))
        {
            OnQuitClicked();
        }
    }

    public void OnReturnToMenuClicked()
    {
        // Resume time so scene transitions behave normally
        if (PauseManager.Instance != null) PauseManager.Instance.Resume();
        var gm = GameObject.FindObjectOfType<GameManager>();
        if (gm != null) gm.ReturnToMainMenu();
        // Show main menu (if present)
        var mm = GameObject.FindObjectOfType<MainMenu>();
        if (mm != null) mm.ShowMainMenu();
        deathPanel.SetActive(false);
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
