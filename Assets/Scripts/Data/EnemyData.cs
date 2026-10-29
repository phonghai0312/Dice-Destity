using UnityEngine;
using System;

/// <summary>
/// Các loại Ý định hành động (Deterministic Intent) của Quái vật theo GDD mục 3.1 & 4.
/// </summary>
public enum EnemyIntentType
{
    Attack,             // Tấn công gây V sát thương
    Defend,             // Phòng thủ cộng V giáp
    AttackAndPoison,    // Tấn công gây V sát thương + cộng secondaryValue tầng Độc (Ải 2 Orc T3)
    ChargeBuffAttack,   // Tích tụ: Buff +V sát thương cho đòn kế tiếp (Ải 3 Minotaur T1)
    AttackAndHeal       // Tấn công gây V sát thương + Hồi secondaryValue HP (Ải 3 Minotaur T3)
}

[Serializable]
public struct EnemyIntentStep
{
    public string intentTitle;
    public EnemyIntentType intentType;
    public int primaryValue;
    public int secondaryValue;

    public EnemyIntentStep(string title, EnemyIntentType type, int primary, int secondary = 0)
    {
        intentTitle = title;
        intentType = type;
        primaryValue = primary;
        secondaryValue = secondary;
    }

    /// <summary>
    /// Chuỗi mô tả Intent hiển thị trên đầu quái để người chơi tính toán chiến thuật (GDD 3.1).
    /// </summary>
    public string GetDisplayDescription(int bonusAtk = 0)
    {
        switch (intentType)
        {
            case EnemyIntentType.Attack:
                int totalAtk = primaryValue + bonusAtk;
                return $"Ý định: {intentTitle} ({totalAtk} ST)";
            case EnemyIntentType.Defend:
                return $"Ý định: {intentTitle} (+{primaryValue} Giáp)";
            case EnemyIntentType.AttackAndPoison:
                int totalPoisonAtk = primaryValue + bonusAtk;
                return $"Ý định: {intentTitle} ({totalPoisonAtk} ST + {secondaryValue} Độc)";
            case EnemyIntentType.ChargeBuffAttack:
                return $"Ý định: {intentTitle} (+{primaryValue} Công lượt sau)";
            case EnemyIntentType.AttackAndHeal:
                int totalSweepAtk = primaryValue + bonusAtk;
                return $"Ý định: {intentTitle} ({totalSweepAtk} ST + Hồi {secondaryValue} HP)";
            default:
                return intentTitle;
        }
    }
}

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Dice & Destiny/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public int maxHP;
    public int startingShield;
    public Sprite enemySprite;
    public Sprite backgroundSprite;

    [Header("Deterministic Intent Cycle (GDD 3.1 & 4)")]
    [Tooltip("Để trống sẽ tự động dùng chu kỳ 3 lượt chuẩn GDD mục 4 theo tên/ải của quái")]
    public EnemyIntentStep[] customIntentCycle;

    /// <summary>
    /// Lấy thông số HP & Giáp khởi đầu chuẩn GDD mục 4 cho từng ải.
    /// </summary>
    public void GetCanonicalStats(int stageIndex, out int resolvedMaxHP, out int resolvedStartingShield)
    {
        resolvedMaxHP = maxHP > 0 ? maxHP : 20;
        resolvedStartingShield = startingShield;

        string lower = (enemyName ?? string.Empty).ToLowerInvariant();
        if (stageIndex == 2 || lower.Contains("orc"))
        {
            if (maxHP <= 0) resolvedMaxHP = 35;
            // GDD mục 4: Ải 2 Armored Orc có 35 HP (+5 Shield khởi đầu)
            if (startingShield <= 0) resolvedStartingShield = 5;
        }
        else if (stageIndex == 3 || lower.Contains("minotaur"))
        {
            if (maxHP <= 0) resolvedMaxHP = 60;
        }
        else if (stageIndex == 1 || lower.Contains("goblin"))
        {
            if (maxHP <= 0) resolvedMaxHP = 20;
        }
    }

    /// <summary>
    /// Lấy hành động dự báo (Intent) của quái ở lượt thứ turnIndex (bắt đầu từ 1) theo đúng bảng GDD mục 4.
    /// </summary>
    public EnemyIntentStep GetIntentForTurn(int turnNumber, int stageIndex)
    {
        int zeroBasedTurn = Mathf.Max(0, turnNumber - 1);

        if (customIntentCycle != null && customIntentCycle.Length > 0)
        {
            return customIntentCycle[zeroBasedTurn % customIntentCycle.Length];
        }

        EnemyIntentStep[] cycle = GetDefaultCycleForStage(stageIndex, enemyName);
        return cycle[zeroBasedTurn % cycle.Length];
    }

    public static EnemyIntentStep[] GetDefaultCycleForStage(int stageIndex, string nameHint = "")
    {
        string lower = (nameHint ?? string.Empty).ToLowerInvariant();

        // Ải 3: Minotaur Boss (60 HP) — T1: Tích tụ (+3 Atk) | T2: Bổ củi (12 dmg) | T3: Lan 6 dmg + Hồi 4 HP
        if (stageIndex == 3 || lower.Contains("minotaur") || lower.Contains("boss"))
        {
            return new EnemyIntentStep[]
            {
                new EnemyIntentStep("Tích Tụ Cuồng Nộ", EnemyIntentType.ChargeBuffAttack, 3, 0),
                new EnemyIntentStep("Bổ Củi Hủy Diệt", EnemyIntentType.Attack, 12, 0),
                new EnemyIntentStep("Quét Rìu Hút Máu", EnemyIntentType.AttackAndHeal, 6, 4)
            };
        }

        // Ải 2: Armored Orc (35 HP, +5 Shield) — T1: 6 dmg | T2: Buff 6 giáp | T3: 4 dmg + Độc 2
        if (stageIndex == 2 || lower.Contains("orc"))
        {
            return new EnemyIntentStep[]
            {
                new EnemyIntentStep("Chém Trọng Kích", EnemyIntentType.Attack, 6, 0),
                new EnemyIntentStep("Giáp Sắt Kiên Cố", EnemyIntentType.Defend, 6, 0),
                new EnemyIntentStep("Lưỡi Đao Tẩm Độc", EnemyIntentType.AttackAndPoison, 4, 2)
            };
        }

        // Ải 1: Goblin Scout (20 HP) — T1: 3 dmg | T2: 4 dmg | T3: Buff 3 giáp
        return new EnemyIntentStep[]
        {
            new EnemyIntentStep("Đâm Lén", EnemyIntentType.Attack, 3, 0),
            new EnemyIntentStep("Chém Nhanh", EnemyIntentType.Attack, 4, 0),
            new EnemyIntentStep("Núp Khiên Gỗ", EnemyIntentType.Defend, 3, 0)
        };
    }
}
