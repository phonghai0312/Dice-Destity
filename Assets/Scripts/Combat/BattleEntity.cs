using UnityEngine;
using System;

/// <summary>
/// Snapshot trạng thái của thực thể để phục vụ cơ chế Hoàn tác (Undo - Command Stack) trong GDD mục 2.2 & 3.4.
/// </summary>
[Serializable]
public struct EntityStateSnapshot
{
    public int currentHP;
    public int maxHP;
    public int currentShield;
    public bool retainHalfShieldNextTurn;
    public int poisonStacks;
    public int burnStacks;
    public int stunStacks;
    public int weakStacks;
    public int bonusAttack;
    public int currentEnergy;
    public int maxEnergy;
    public int nextSkillCostReduction;
}

/// <summary>
/// BattleEntity — Thực thể chiến đấu (Player & Enemy) theo GDD mục 3.1 & 3.3.
/// Quản lý: HP, Shield (reset đầu lượt hoặc giữ 50% với Iron Wall), Energy (Năng lượng kỹ năng),
/// Túi hiệu ứng trạng thái (Poison, Burn/Thiêu đốt, Stun, Weak, BonusAttack) và Observer Events.
/// </summary>
public class BattleEntity : MonoBehaviour
{
    [Header("Core Stats (GDD 3.1)")]
    public int maxHP = 20;
    [SerializeField] private int currentHP;
    [SerializeField] private int currentShield;
    [SerializeField] private bool retainHalfShieldNextTurn;

    [Header("Player Energy / Skill Resource")]
    public int maxEnergy = 10;
    [SerializeField] private int currentEnergy;
    [SerializeField] private int nextSkillCostReduction;

    [Header("Status Effects (GDD 3.1 & 3.3)")]
    [SerializeField] private int poisonStacks;
    [SerializeField] private int burnStacks;
    [SerializeField] private int stunStacks;
    [SerializeField] private int weakStacks;
    [SerializeField] private int bonusAttack;

    // Observer Pattern Events (GDD mục 6)
    public event Action<int, int> OnHPChanged;     // <CurrentHP, MaxHP>
    public event Action<int> OnShieldChanged;      // <CurrentShield>
    public event Action<int> OnDamageTaken;        // <DamageAmount>
    public event Action<int> OnHealed;             // <HealAmount>
    public event Action<int, int> OnEnergyChanged; // <CurrentEnergy, MaxEnergy>
    public event Action OnStatusChanged;           // Kích hoạt khi Poison/Burn/Stun/Weak/BonusAtk/Energy thay đổi

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public int CurrentShield => currentShield;
    public int CurrentEnergy => currentEnergy;
    public int MaxEnergy => maxEnergy;
    public int NextSkillCostReduction => nextSkillCostReduction;
    public int PoisonStacks => poisonStacks;
    public int BurnStacks => burnStacks;
    public int StunStacks => stunStacks;
    public int WeakStacks => weakStacks;
    public int BonusAttack => bonusAttack;
    public bool RetainHalfShieldNextTurn => retainHalfShieldNextTurn;

    private void Start()
    {
        if (!IsEnemyEntity() && currentHP <= 0)
        {
            ResetStats();
        }
    }

    public bool IsEnemyEntity()
    {
        return gameObject.name.IndexOf("Enemy", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Khởi tạo thông số cho Enemy từ EnemyData hoặc khởi tạo Player đầu trận.
    /// </summary>
    public void Initialize(EnemyData data)
    {
        ClearAllStatuses();
        retainHalfShieldNextTurn = false;
        currentEnergy = 0;
        nextSkillCostReduction = 0;

        if (data != null)
        {
            maxHP = data.maxHP;
            currentShield = data.startingShield;
            currentHP = maxHP;
        }
        else
        {
            currentShield = 0;
            if (GameManager.Instance != null && !IsEnemyEntity())
            {
                maxHP = GameManager.Instance.playerMaxHP;
                currentHP = Mathf.Clamp(GameManager.Instance.playerCurrentHP, 1, maxHP);
            }
            else
            {
                currentHP = maxHP;
            }
        }

        NotifyAllStats();
    }

    /// <summary>
    /// Thiết lập HP trực tiếp cho Player khi chuyển ải (giữ máu Roguelike).
    /// </summary>
    public void SetHP(int hp, int max)
    {
        maxHP = Mathf.Max(1, max);
        currentHP = Mathf.Clamp(hp, 0, maxHP);
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    private void ResetStats()
    {
        currentHP = maxHP;
        NotifyAllStats();
    }

    public void NotifyAllStats()
    {
        OnHPChanged?.Invoke(currentHP, maxHP);
        OnShieldChanged?.Invoke(currentShield);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        OnStatusChanged?.Invoke();
    }

    // ─── Quản lý Thanh Năng Lượng & Chi Phí Kỹ Năng Nhân Vật ───────────────────

    /// <summary>
    /// Tích lũy Năng lượng từ các mặt xúc xắc Năng lượng (Mana Spark, Arcane Surge, Overcharge).
    /// </summary>
    public void AddEnergy(int amount, int skillDiscount = 0)
    {
        if (amount > 0)
        {
            currentEnergy = Mathf.Clamp(currentEnergy + amount, 0, maxEnergy);
        }
        if (skillDiscount > 0)
        {
            nextSkillCostReduction += skillDiscount;
        }
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        OnStatusChanged?.Invoke();
    }

    /// <summary>
    /// Tính chi phí Năng lượng thực tế của kỹ năng sau khi trừ giảm giá từ mặt Overcharge (nếu có).
    /// </summary>
    public int GetEffectiveSkillCost(int baseCost)
    {
        return Mathf.Max(0, baseCost - nextSkillCostReduction);
    }

    /// <summary>
    /// Kiểm tra và tiêu hao Năng lượng để thi triển Kỹ năng Thường hoặc Kỹ năng Tối thượng.
    /// </summary>
    public bool TrySpendEnergyForSkill(int baseCost)
    {
        int effectiveCost = GetEffectiveSkillCost(baseCost);
        if (currentEnergy < effectiveCost)
        {
            return false;
        }

        currentEnergy -= effectiveCost;
        nextSkillCostReduction = 0; // Tiêu thụ hiệu ứng giảm chi phí cho kỹ năng tiếp theo
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        OnStatusChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Xóa hiệu ứng giảm chi phí kỹ năng trong lượt khi kết thúc hiệp (Năng lượng vẫn được bảo lưu giữa các hiệp).
    /// </summary>
    public void ResetTurnSkillDiscount()
    {
        if (nextSkillCostReduction != 0)
        {
            nextSkillCostReduction = 0;
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
            OnStatusChanged?.Invoke();
        }
    }

    // ─── Quy tắc Toán học Chiến đấu (GDD 3.3) ──────────────────────────────────

    /// <summary>
    /// Tính sát thương đầu ra thực tế từ sát thương gốc (áp dụng BonusAttack và giảm 25% nếu bị Weak).
    /// Công thức GDD 3.3: nếu bị dính Weak -> D_actual = max(1, floor(D_raw * 0.75)).
    /// </summary>
    public int CalculateOutgoingDamage(int rawDamage, bool consumeBonusAttack = true)
    {
        if (rawDamage <= 0) return 0;

        int totalRaw = rawDamage + bonusAttack;
        if (consumeBonusAttack && bonusAttack != 0)
        {
            bonusAttack = 0;
            OnStatusChanged?.Invoke();
        }

        if (weakStacks > 0)
        {
            return Mathf.Max(1, Mathf.FloorToInt(totalRaw * 0.75f));
        }

        return totalRaw;
    }

    /// <summary>
    /// Nhận sát thương chuẩn (trừ vào Shield trước, phần dư mới trừ vào HP).
    /// </summary>
    public void TakeDamage(int damage, bool triggerDefeatCallback = true)
    {
        if (damage <= 0 || currentHP <= 0) return;

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
            OnDamageTaken?.Invoke(damage);
            OnHPChanged?.Invoke(currentHP, maxHP);

            SyncPlayerHPToGameManager();

            if (currentHP <= 0 && IsEnemyEntity() && triggerDefeatCallback)
            {
                if (GameManager.Instance != null && FindAnyObjectByType<SimpleCombatManager>() == null)
                {
                    GameManager.Instance.OnEnemyDefeated();
                }
            }
        }
    }

    /// <summary>
    /// Nhận sát thương xuyên giáp (dùng cho Độc - Poison và Thiêu đốt - Burn ở cuối mỗi Round).
    /// </summary>
    public void TakeDirectDamage(int directDamage, bool triggerDefeatCallback = true)
    {
        if (directDamage <= 0 || currentHP <= 0) return;

        currentHP = Mathf.Max(0, currentHP - directDamage);
        OnDamageTaken?.Invoke(directDamage);
        OnHPChanged?.Invoke(currentHP, maxHP);

        SyncPlayerHPToGameManager();

        if (currentHP <= 0 && IsEnemyEntity() && triggerDefeatCallback)
        {
            if (GameManager.Instance != null && FindAnyObjectByType<SimpleCombatManager>() == null)
            {
                GameManager.Instance.OnEnemyDefeated();
            }
        }
    }

    /// <summary>
    /// Hồi máu theo công thức GDD 3.3: HP_new = min(HP_max, HP_current + amount).
    /// Khắc chế bởi Thiêu Đốt (Burn): Khi đang bị thiêu đốt, vết thương bị tổn thương sâu làm giảm 50% lượng hồi máu.
    /// </summary>
    public int Heal(int amount)
    {
        if (amount <= 0 || currentHP <= 0) return 0;

        // Khắc chế từ Thiêu đốt (Burn): Lửa thiêu đốt làm giảm 50% khả năng hồi máu
        if (burnStacks > 0)
        {
            amount = Mathf.Max(1, Mathf.FloorToInt(amount * 0.5f));
        }

        int previousHP = currentHP;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        int actualHealed = currentHP - previousHP;

        if (actualHealed > 0)
        {
            OnHealed?.Invoke(actualHealed);
        }

        OnHPChanged?.Invoke(currentHP, maxHP);
        SyncPlayerHPToGameManager();
        return actualHealed;
    }

    /// <summary>
    /// Cộng Giáp ảo (Block / Iron Wall).
    /// Nếu retainHalf = true (Iron Wall), bảo lưu 50% giáp sang lượt kế tiếp.
    /// </summary>
    public void AddShield(int amount, bool retainHalf = false)
    {
        if (amount <= 0) return;

        currentShield += amount;
        if (retainHalf)
        {
            retainHalfShieldNextTurn = true;
        }
        OnShieldChanged?.Invoke(currentShield);
        OnStatusChanged?.Invoke();
    }

    /// <summary>
    /// Làm mới Shield ở đầu lượt (RoundStart - GDD 3.4).
    /// Nếu có hiệu ứng Iron Wall thì giữ lại 50% giáp, ngược lại đặt về 0.
    /// </summary>
    public void ResetShieldForNewTurn()
    {
        if (retainHalfShieldNextTurn)
        {
            currentShield = Mathf.FloorToInt(currentShield * 0.5f);
            retainHalfShieldNextTurn = false;
        }
        else
        {
            currentShield = 0;
        }

        OnShieldChanged?.Invoke(currentShield);
        OnStatusChanged?.Invoke();
    }

    // ─── Quản lý Hiệu ứng Trạng thái (Status Effects - GDD 3.1, 3.2, 3.4) ──────

    public void AddPoison(int stacks)
    {
        if (stacks <= 0) return;
        poisonStacks += stacks;
        OnStatusChanged?.Invoke();
    }

    public void AddBurn(int stacks)
    {
        if (stacks <= 0) return;
        burnStacks += stacks;
        OnStatusChanged?.Invoke();
    }

    public void AddStun(int stacks = 1)
    {
        if (stacks <= 0) return;
        stunStacks += stacks;
        OnStatusChanged?.Invoke();
    }

    public void AddWeak(int stacks)
    {
        if (stacks <= 0) return;
        weakStacks += stacks;
        OnStatusChanged?.Invoke();
    }

    public void AddBonusAttack(int amount)
    {
        if (amount == 0) return;
        bonusAttack += amount;
        OnStatusChanged?.Invoke();
    }

    /// <summary>
    /// Kỹ năng Purify (GDD 3.2): Giải trừ toàn bộ hiệu ứng bất lợi (Poison, Burn, Weak, Stun).
    /// </summary>
    public void Purify()
    {
        poisonStacks = 0;
        burnStacks = 0;
        weakStacks = 0;
        stunStacks = 0;
        OnStatusChanged?.Invoke();
    }

    /// <summary>
    /// Xóa toàn bộ số tầng Độc (dùng khi chiêu Kích Nổ Độc - Venom Burst phát tác tiêu thụ độc).
    /// </summary>
    public int ClearPoison()
    {
        int prev = poisonStacks;
        poisonStacks = 0;
        OnStatusChanged?.Invoke();
        return prev;
    }

    public void ClearAllStatuses()
    {
        poisonStacks = 0;
        burnStacks = 0;
        weakStacks = 0;
        stunStacks = 0;
        bonusAttack = 0;
        OnStatusChanged?.Invoke();
    }

    /// <summary>
    /// Kiểm tra và tiêu thụ 1 tầng Choáng (Stun) ở pha EnemyExecution (GDD 3.4).
    /// Trả về true nếu thực thể đang bị Choáng và phải bỏ qua lượt.
    /// </summary>
    public bool CheckAndConsumeStun()
    {
        if (stunStacks > 0)
        {
            stunStacks = Mathf.Max(0, stunStacks - 1);
            OnStatusChanged?.Invoke();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Xử lý cuối mỗi Round (RoundEnd - GDD 3.3 & 3.4):
    /// - Kích hoạt Độc (Poison: trừ máu trực tiếp, giảm 1 stack).
    /// - Kích hoạt Thiêu đốt (Burn: trừ máu trực tiếp, giảm 1 stack).
    /// - Giảm 1 stack Suy yếu (Weak) nếu có.
    /// Trả về tổng lượng sát thương theo thời gian (Độc + Thiêu đốt) đã gây ra.
    /// </summary>
    public int ProcessRoundEndStatuses(bool triggerDefeatCallback = true)
    {
        int totalDotDamage = 0;

        if (poisonStacks > 0 && currentHP > 0)
        {
            int poisonDmg = poisonStacks;
            poisonStacks = Mathf.Max(0, poisonStacks - 1);
            totalDotDamage += poisonDmg;
            TakeDirectDamage(poisonDmg, triggerDefeatCallback);
        }

        if (burnStacks > 0 && currentHP > 0)
        {
            int burnDmg = burnStacks;
            burnStacks = Mathf.Max(0, burnStacks - 1);
            totalDotDamage += burnDmg;
            TakeDirectDamage(burnDmg, triggerDefeatCallback);
        }

        if (weakStacks > 0)
        {
            weakStacks = Mathf.Max(0, weakStacks - 1);
        }

        ResetTurnSkillDiscount();
        OnStatusChanged?.Invoke();
        return totalDotDamage;
    }

    // ─── Hỗ trợ Hoàn tác (Undo Snapshot) ──────────────────────────────────────

    public EntityStateSnapshot CaptureSnapshot()
    {
        return new EntityStateSnapshot
        {
            currentHP = this.currentHP,
            maxHP = this.maxHP,
            currentShield = this.currentShield,
            retainHalfShieldNextTurn = this.retainHalfShieldNextTurn,
            poisonStacks = this.poisonStacks,
            burnStacks = this.burnStacks,
            stunStacks = this.stunStacks,
            weakStacks = this.weakStacks,
            bonusAttack = this.bonusAttack,
            currentEnergy = this.currentEnergy,
            maxEnergy = this.maxEnergy,
            nextSkillCostReduction = this.nextSkillCostReduction
        };
    }

    public void RestoreSnapshot(EntityStateSnapshot snapshot)
    {
        this.maxHP = snapshot.maxHP;
        this.currentHP = snapshot.currentHP;
        this.currentShield = snapshot.currentShield;
        this.retainHalfShieldNextTurn = snapshot.retainHalfShieldNextTurn;
        this.poisonStacks = snapshot.poisonStacks;
        this.burnStacks = snapshot.burnStacks;
        this.stunStacks = snapshot.stunStacks;
        this.weakStacks = snapshot.weakStacks;
        this.bonusAttack = snapshot.bonusAttack;
        this.currentEnergy = snapshot.currentEnergy;
        this.maxEnergy = snapshot.maxEnergy > 0 ? snapshot.maxEnergy : 10;
        this.nextSkillCostReduction = snapshot.nextSkillCostReduction;

        SyncPlayerHPToGameManager();
        NotifyAllStats();
    }

    private void SyncPlayerHPToGameManager()
    {
        if (!IsEnemyEntity() && GameManager.Instance != null)
        {
            GameManager.Instance.playerCurrentHP = currentHP;
        }
    }

    // Các hàm getter giữ nguyên chữ ký cũ để tương thích 100%
    public bool IsAlive() => currentHP > 0;
    public int GetCurrentHP() => currentHP;
    public int GetMaxHP() => maxHP;
    public int GetCurrentShield() => currentShield;
}
