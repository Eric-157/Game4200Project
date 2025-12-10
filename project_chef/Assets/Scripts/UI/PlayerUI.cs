using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Updates the on-screen player HUD showing current HP, ingredients and rooms visited.
/// Attach this to a Canvas GameObject (HUD canvas). Assign Text fields in the inspector.
/// The script forces the Canvas to a high sorting order so it remains visible over pause/death UI.
/// </summary>
public class PlayerUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text hpText;
    public TMP_Text ingredientsText;
    public TMP_Text roomsText;

    [Header("Canvas Settings")]
    [Tooltip("Sorting order used for the HUD canvas so it appears above other UI (pause, death).")]
    public int hudSortOrder = 500;

    private Canvas hudCanvas;
    private PlayerStats playerStats;
    private GameManager gm;

    void Awake()
    {
        hudCanvas = GetComponent<Canvas>();
        if (hudCanvas == null)
            hudCanvas = GetComponentInChildren<Canvas>(true);

        if (hudCanvas != null)
        {
            // Force high sorting so HUD stays on top of pause/death UI
            hudCanvas.overrideSorting = true;
            hudCanvas.sortingOrder = hudSortOrder;
        }
    }

    void Start()
    {
        gm = GameManager.Instance;
        var pgo = GameObject.FindGameObjectWithTag("Player");
        if (pgo != null) playerStats = pgo.GetComponent<PlayerStats>();
        Refresh();
    }

    void Update()
    {
        // Update every frame so HUD reflects current values (works while timescale==0)
        Refresh();
    }

    void Refresh()
    {
        if (hpText != null && playerStats != null)
            hpText.text = $"HP: {playerStats.currentHP}/{playerStats.maxHP}";

        if (ingredientsText != null && gm != null)
            ingredientsText.text = $"Ingredients: {gm.ingredients}";

        if (roomsText != null && gm != null)
            roomsText.text = $"Rooms: {gm.roomsVisited}";
    }
}
