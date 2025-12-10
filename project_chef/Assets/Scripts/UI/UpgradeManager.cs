using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks UpgradeItem instances and provides a runtime reset (debug) that
/// clears purchased upgrades and restores player default stats.
/// Press 'P' at runtime to trigger a reset (debug convenience).
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    private readonly List<UpgradeItem> registeredItems = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    void Start()
    {
        // After all Awake() calls (including PlayerStats.Awake) have run, apply any previously-purchased upgrades.
        foreach (var item in registeredItems)
        {
            if (item == null) continue;
            item.ApplyIfPurchased();
        }
    }

    public void Register(UpgradeItem item)
    {
        if (item == null) return;
        if (!registeredItems.Contains(item)) registeredItems.Add(item);
    }

    public void Unregister(UpgradeItem item)
    {
        if (item == null) return;
        registeredItems.Remove(item);
    }

    void Update()
    {
        // Debug hotkey to reset all upgrades
        if (Input.GetKeyDown(KeyCode.P))
        {
            ResetAllUpgrades();
        }
    }

    public void ResetAllUpgrades()
    {
        Debug.Log("UpgradeManager: Resetting all upgrades (debug)");

        // Clear PlayerPrefs keys for registered upgrades and update visuals
        foreach (var item in registeredItems)
        {
            if (item == null) continue;
            string key = "upgrade_" + item.id;
            PlayerPrefs.DeleteKey(key);
            item.RevertPurchaseVisuals();
        }

        PlayerPrefs.Save();

        // Restore player stats to defaults
        var ps = GameObject.FindObjectOfType<PlayerStats>();
        if (ps != null)
        {
            ps.ResetToDefaults();
            Debug.Log("UpgradeManager: Player stats reset to defaults.");
        }
        else
        {
            Debug.LogWarning("UpgradeManager: PlayerStats not found when resetting upgrades.");
        }
    }
}
