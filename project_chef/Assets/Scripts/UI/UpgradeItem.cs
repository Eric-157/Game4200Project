using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to an Upgrade button object in the Main Menu. Configure the fields in inspector.
/// Behavior:
/// - `id` is used to persist the purchase in PlayerPrefs (key: "upgrade_<id>")
/// - `cost` is deducted from GameManager.ingredients when purchased
/// - `value` is applied to the player's stat (float, works for int stats by rounding when needed)
/// - Purchase is one-time; button is disabled & visually grayed when bought
/// - If the upgrade was previously purchased (PlayerPrefs), the effect is applied automatically on Awake
/// </summary>
public class UpgradeItem : MonoBehaviour
{
    public string id = "upgrade_id";
    public enum UpgradeType { Damage, MaxHP, AttackSpeed }
    public UpgradeType upgradeType = UpgradeType.Damage;
    [Tooltip("Numeric value applied to the player's stat. Use positive values to increase, negative to decrease.")]
    public float value = 1f;
    [Tooltip("Ingredient cost to purchase this upgrade")]
    public int cost = 5;

    [Header("UI")]
    public Button buyButton;
    public Image buttonImage; // optional: will be used to gray out when purchased
    public Color purchasedColor = Color.gray;

    const string PrefKeyPrefix = "upgrade_";

    void Awake()
    {
        if (buyButton == null)
            buyButton = GetComponent<Button>();

        if (buttonImage == null && buyButton != null)
            buttonImage = buyButton.image;

        // cache original button color so we can restore it on reset
        if (buttonImage != null)
            originalColor = buttonImage.color;

        // Register with UpgradeManager so it can reset this upgrade later
        var mgr = GameObject.FindObjectOfType<UpgradeManager>();
        if (mgr == null)
        {
            // Create a manager automatically if none exists in the scene
            var go = new GameObject("_UpgradeManager");
            mgr = go.AddComponent<UpgradeManager>();
        }
        mgr.Register(this);

        bool purchased = PlayerPrefs.GetInt(PrefKeyPrefix + id, 0) == 1;
        if (purchased)
        {
            // Mark UI as purchased now, but defer applying the effect until UpgradeManager.Start()
            // so PlayerStats can capture its defaults in Awake before any stat changes are applied.
            SetPurchasedVisuals();
        }
        else
        {
            if (buyButton != null)
            {
                buyButton.onClick.AddListener(OnBuyClicked);
            }
        }
    }

    Color originalColor = Color.white;

    /// <summary>
    /// Revert visuals to unpurchased state and re-enable the buy button.
    /// Does not attempt to undo stat changes (UpgradeManager will restore player defaults).
    /// </summary>
    public void RevertPurchaseVisuals()
    {
        string key = PrefKeyPrefix + id;
        PlayerPrefs.DeleteKey(key);
        if (buyButton != null) buyButton.interactable = true;
        if (buttonImage != null) buttonImage.color = originalColor;
        if (buyButton != null) buyButton.onClick.RemoveListener(OnBuyClicked);
        if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);
    }

    /// <summary>
    /// Called by UpgradeManager at Start to apply any purchased upgrades after PlayerStats
    /// has captured its defaults. Safe to call multiple times.
    /// </summary>
    public void ApplyIfPurchased()
    {
        if (PlayerPrefs.GetInt(PrefKeyPrefix + id, 0) == 1)
        {
            ApplyEffectToPlayer();
        }
    }

    void OnBuyClicked()
    {
        if (PlayerPrefs.GetInt(PrefKeyPrefix + id, 0) == 1) return;

        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("UpgradeItem: GameManager not found.");
            return;
        }

        if (gm.ingredients < cost)
        {
            // Not enough ingredients
            Debug.Log("Not enough ingredients to purchase " + id);
            return;
        }

        // Deduct cost and persist purchase
        gm.ingredients -= cost;
        PlayerPrefs.SetInt(PrefKeyPrefix + id, 1);
        PlayerPrefs.Save();

        ApplyEffectToPlayer();
        SetPurchasedVisuals();

        if (buyButton != null)
            buyButton.onClick.RemoveListener(OnBuyClicked);
    }

    void ApplyEffectToPlayer()
    {
        var ps = GameObject.FindObjectOfType<PlayerStats>();
        if (ps == null)
        {
            Debug.LogWarning("UpgradeItem: PlayerStats not found to apply upgrade.");
            return;
        }

        switch (upgradeType)
        {
            case UpgradeType.Damage:
                ps.damage += value;
                break;
            case UpgradeType.MaxHP:
                // value may be fractional; round to int for HP
                int addHp = Mathf.RoundToInt(value);
                ps.maxHP += addHp;
                ps.currentHP += addHp; // give the player the extra HP immediately
                break;
            case UpgradeType.AttackSpeed:
                // Attack speed is a float; add the value (negative value can reduce cooldown if your system uses that)
                ps.attackSpeed += value;
                break;
        }

        // Persist the player's stat change to disk so it survives app restarts
        ps.SaveStats();
    }

    void SetPurchasedVisuals()
    {
        if (buyButton != null)
            buyButton.interactable = false;

        if (buttonImage != null)
            buttonImage.color = purchasedColor;
    }
}
