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
        float finalDamage = CalculateDamageTaken(dmg);
        currentHP -= Mathf.RoundToInt(finalDamage);
        if (currentHP <= 0) PlayerDeath();
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
