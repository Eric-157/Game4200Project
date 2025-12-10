using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class PlayerStatsData
{
    public int HP;
    public int MaxHP;
    public int Defense;
    public int TempDefense;
    public float MoveSpeed;
    public float AttackSpeed;
    public float Damage;
    public int CritChance;
    public float CritDamage;
    public List<string> PassiveItems = new();
}

public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    public int startHP = 3;
    public int maxHP = 3;
    public int currentHP = 3;
    public int defense = 0;           // permanent
    public int tempDefense = 0;       // temporary (used first)
    public float moveSpeed = 5f;
    public float attackSpeed = 0f;    // modifies cooldown
    public float damage = 1f;
    public int critChance = 100;      // 1/X chance
    public float critDamage = 1f;
    [Header("Damage / Invulnerability")]
    [Tooltip("Seconds of invulnerability after taking damage")]
    public float invulnerabilityDuration = 1.0f;
    private float invulnerableUntil = 0f;
    // frame index when we last applied damage to the player; prevents multiple
    // damage events from different enemies in the exact same frame.
    private int lastDamageFrame = -1;

    [Header("Hit Flash")]
    [Tooltip("Color to flash the player when hit")]
    public Color hitColor = Color.red;
    [Tooltip("Duration of the hit flash in seconds")]
    public float hitFlashDuration = 0.15f;
    private Coroutine hitFlashCoroutine = null;

    [Header("Passive Item System")]
    public List<StatModifier> passiveModifiers = new();

    private string savePath => Path.Combine(Application.persistentDataPath, "player_stats.json");

    void Start()
    {
        LoadStats();
        ApplyModifiers();
    }

    // ========== CORE STATS ==========
    public void TakeDamage(float dmg)
    {
        // Ignore damage when currently invulnerable
        if (Time.time < invulnerableUntil) return;

        // Prevent multiple damage sources from applying in the same frame.
        // This ensures only one enemy hit goes through even if two collisions
        // are processed during the same physics/frame update.
        if (Time.frameCount == lastDamageFrame) return;

        float finalDamage = CalculateDamageTaken(dmg);
        int dmgInt = Mathf.RoundToInt(finalDamage);
        currentHP -= dmgInt;

        // Mark damage applied on this frame so other sources in the same
        // frame won't also apply damage.
        lastDamageFrame = Time.frameCount;

        // Set invulnerability window
        invulnerableUntil = Time.time + invulnerabilityDuration;

        // Trigger hit flash effect
        if (dmgInt > 0)
        {
            StartHitFlash();
        }

        if (currentHP <= 0) PlayerDeath();
    }

    void StartHitFlash()
    {
        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
            RestoreSpriteColors();
        }
        hitFlashCoroutine = StartCoroutine(HitFlashCoroutine());
    }

    private SpriteRenderer[] cachedRenderers = null;
    private Color[] originalColors = null;

    System.Collections.IEnumerator HitFlashCoroutine()
    {
        if (cachedRenderers == null)
            cachedRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (cachedRenderers == null || cachedRenderers.Length == 0)
            yield break;

        // Cache originals
        originalColors = new Color[cachedRenderers.Length];
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            originalColors[i] = cachedRenderers[i].color;
            cachedRenderers[i].color = hitColor;
        }

        yield return new WaitForSeconds(hitFlashDuration);

        RestoreSpriteColors();
        hitFlashCoroutine = null;
    }

    void RestoreSpriteColors()
    {
        if (cachedRenderers == null || originalColors == null) return;
        int len = Mathf.Min(cachedRenderers.Length, originalColors.Length);
        for (int i = 0; i < len; i++)
        {
            if (cachedRenderers[i] != null)
                cachedRenderers[i].color = originalColors[i];
        }
    }

    float CalculateDamageTaken(float dmg)
    {
        int totalDefense = defense + tempDefense;
        float adjusted = dmg;

        if (totalDefense > 0)
        {
            adjusted = dmg - totalDefense;
            if (adjusted < 0) adjusted = 0;
        }
        else if (totalDefense < 0)
        {
            adjusted = dmg + Mathf.Abs(totalDefense);
        }

        if (tempDefense > 0)
        {
            tempDefense -= Mathf.RoundToInt(dmg);
            if (tempDefense < 0) tempDefense = 0;
        }

        return adjusted;
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
    }

    public void AddMaxHP(int amount)
    {
        maxHP += amount;
        currentHP = maxHP;
    }

    public float CalculateAttackDamage()
    {
        float finalDamage = damage;
        int roll = UnityEngine.Random.Range(1, critChance + 1);
        if (roll == 1) finalDamage *= critDamage;
        return finalDamage;
    }

    void PlayerDeath()
    {
        Debug.Log("Player died!");
        currentHP = 0;
        // Pause the game and show death UI if present
        if (PauseManager.Instance != null)
            PauseManager.Instance.Pause();

        var gm = GameObject.FindObjectOfType<GameManager>();
        if (gm != null)
        {
            // notify GameManager (it will destroy room and prepare main menu if needed)
            // Expose a hook; GameManager can implement showing a death UI
            // For now just call ReturnToMainMenu when user chooses from death UI
        }
        // Freeze player input/movement
        var pm = GetComponent<PlayerMovement>();
        if (pm != null) pm.FreezeMovement(99999f);

        // Show death UI if present
        var deathUI = GameObject.FindObjectOfType<DeathMenuUI>();
        if (deathUI != null) deathUI.Show();
    }

    // ========== PASSIVE MODIFIERS ==========
    void ApplyModifiers()
    {
        foreach (var mod in passiveModifiers)
        {
            switch (mod.statType)
            {
                case StatType.HP: maxHP += Mathf.RoundToInt(mod.value); break;
                case StatType.Defense: defense += Mathf.RoundToInt(mod.value); break;
                case StatType.MoveSpeed: moveSpeed += mod.value; break;
                case StatType.AttackSpeed: attackSpeed += mod.value; break;
                case StatType.Damage: damage += mod.value; break;
                case StatType.CritChance: critChance += Mathf.RoundToInt(mod.value); break;
                case StatType.CritDamage: critDamage += mod.value; break;
            }
        }
    }

    // ========== SAVE / LOAD ==========
    public void SaveStats()
    {
        var data = new PlayerStatsData
        {
            HP = currentHP,
            MaxHP = maxHP,
            Defense = defense,
            TempDefense = tempDefense,
            MoveSpeed = moveSpeed,
            AttackSpeed = attackSpeed,
            Damage = damage,
            CritChance = critChance,
            CritDamage = critDamage,
            PassiveItems = new()
        };

        foreach (var mod in passiveModifiers)
            data.PassiveItems.Add(mod.name);

        File.WriteAllText(savePath, JsonUtility.ToJson(data, true));
        Debug.Log($"Player stats saved to {savePath}");
    }

    public void LoadStats()
    {
        if (!File.Exists(savePath))
        {
            currentHP = startHP;
            maxHP = startHP;
            return;
        }

        var json = File.ReadAllText(savePath);
        var data = JsonUtility.FromJson<PlayerStatsData>(json);

        currentHP = data.HP;
        maxHP = data.MaxHP;
        defense = data.Defense;
        tempDefense = data.TempDefense;
        moveSpeed = data.MoveSpeed;
        attackSpeed = data.AttackSpeed;
        damage = data.Damage;
        critChance = data.CritChance;
        critDamage = data.CritDamage;
    }
}

[Serializable]
public enum StatType { HP, Defense, MoveSpeed, AttackSpeed, Damage, CritChance, CritDamage }

[Serializable]
public class StatModifier
{
    public string name;
    public StatType statType;
    public float value;
}
