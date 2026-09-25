using UnityEngine;
using System;

public class BattleEntity : MonoBehaviour
{
    // Cấu trúc Data-Driven
    public int maxHP = 20;
    private int currentHP;
    private int currentShield;

    // Observer Pattern: Event thay đổi trạng thái
    public event Action<int, int> OnHPChanged; // <Current, Max>
    public event Action<int> OnShieldChanged;  // <CurrentShield>

    private void Start()
    {
        if (!gameObject.name.Contains("Enemy")) {
            ResetStats();
        }
    }

    public void Initialize(EnemyData data)
    {
        if (data != null) {
            maxHP = data.maxHP;
            currentShield = data.startingShield;
        }
        ResetStats();
    }

    private void ResetStats()
    {
        currentHP = maxHP;
        OnHPChanged?.Invoke(currentHP, maxHP);
        OnShieldChanged?.Invoke(currentShield);
    }

    public void TakeDamage(int damage)
    {
        if (currentShield > 0)
        {
            int absorbed = Mathf.Min(currentShield, damage);
            currentShield -= absorbed;
            damage -= absorbed;
            OnShieldChanged?.Invoke(currentShield);
        }

        if (damage > 0)
        {
            currentHP = Mathf.Max(0, currentHP - damage);
            OnHPChanged?.Invoke(currentHP, maxHP);

            if (currentHP <= 0 && gameObject.name.Contains("Enemy"))
            {
                if (GameManager.Instance != null) {
                    GameManager.Instance.OnEnemyDefeated();
                }
            }
        }
    }

    public void AddShield(int amount)
    {
        currentShield += amount;
        OnShieldChanged?.Invoke(currentShield);
    }

    public bool IsAlive() => currentHP > 0;
    public int GetCurrentHP() => currentHP;
    public int GetMaxHP() => maxHP;
}
