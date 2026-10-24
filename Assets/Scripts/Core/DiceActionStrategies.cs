using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Ngữ cảnh thực thi kỹ năng xúc xắc (cho phép kiểm tra/tiêu thụ thêm xúc xắc phụ như Heavy Slash).
/// </summary>
public class DiceActionContext
{
    public BattleEntity User;
    public BattleEntity Target;
    public DiceFaceData FaceData;
    /// <summary>
    /// Callback tùy chọn để kiểm tra và tiêu thụ 1 mặt Khiên chưa dùng trên bàn (dành cho Heavy Slash).
    /// </summary>
    public System.Func<bool> TryConsumeExtraBlockDie;

    public DiceActionContext(
        BattleEntity user,
        BattleEntity target,
        DiceFaceData faceData,
        System.Func<bool> tryConsumeExtraBlockDie = null)
    {
        User = user;
        Target = target;
        FaceData = faceData;
        TryConsumeExtraBlockDie = tryConsumeExtraBlockDie;
    }
}

/// <summary>
/// Interface Strategy Pattern cho Kỹ năng Xúc xắc (GDD mục 6: IDiceAction).
/// Mỗi mặt xúc xắc triển khai một Strategy độc lập, loại bỏ switch-case cồng kềnh.
/// </summary>
public interface IDiceAction
{
    bool CanExecute(DiceActionContext context, out string failReason);
    void Execute(DiceActionContext context);
}

// ─── 1. STRIKE (Common, 2-4): Gây V sát thương chuẩn lên mục tiêu ────────────
public class StrikeActionStrategy : IDiceAction
{
    public bool CanExecute(DiceActionContext ctx, out string failReason)
    {
        if (ctx.Target == null || !ctx.Target.IsAlive())
        {
            failReason = "Mục tiêu không hợp lệ!";
            return false;
        }
        failReason = null;
        return true;
    }

    public void Execute(DiceActionContext ctx)
    {
        int raw = Mathf.Max(1, ctx.FaceData.valueV);
        int finalDmg = ctx.User != null ? ctx.User.CalculateOutgoingDamage(raw) : raw;
        ctx.Target.TakeDamage(finalDmg);
    }
}

// ─── 2. HEAVY SLASH (Rare, 6-8): Gây V sát thương; yêu cầu tiêu thụ 1 mặt Khiên ──
public class HeavySlashActionStrategy : IDiceAction
{
    public bool CanExecute(DiceActionContext ctx, out string failReason)
    {
        if (ctx.Target == null || !ctx.Target.IsAlive())
        {
            failReason = "Mục tiêu không hợp lệ!";
            return false;
        }
        failReason = null;
        return true;
    }

    public void Execute(DiceActionContext ctx)
    {
        // Ưu tiên tiêu thụ 1 viên xúc xắc mặt Khiên trên khay; nếu không có thì tiêu thụ 2 điểm Shield hiện có
        bool consumedBlockDie = ctx.TryConsumeExtraBlockDie != null && ctx.TryConsumeExtraBlockDie.Invoke();
        if (!consumedBlockDie && ctx.User != null && ctx.User.CurrentShield > 0)
        {
            // Tiêu thụ 1 phần giáp đang có để cường hóa nhát chém
            ctx.User.TakeDamage(Mathf.Min(2, ctx.User.CurrentShield), false);
            consumedBlockDie = true;
        }

        int raw = Mathf.Max(1, ctx.FaceData.valueV);
        // Nếu người chơi không có mặt Khiên nào để tiêu thụ, sát thương giảm một nửa (vẫn dùng được để không kẹt lượt)
        if (!consumedBlockDie)
        {
            raw = Mathf.Max(2, raw / 2);
            Debug.Log("[HeavySlash] Không có mặt Khiên đi kèm -> gây sát thương giảm nửa.");
        }

        int finalDmg = ctx.User != null ? ctx.User.CalculateOutgoingDamage(raw) : raw;
        ctx.Target.TakeDamage(finalDmg);
    }
}

// ─── 3. VAMPIRIC (Epic, 3-4): Gây V sát thương và hồi floor(V/2) HP ──────────
public class VampiricActionStrategy : IDiceAction
{
    public bool CanExecute(DiceActionContext ctx, out string failReason)
    {
        if (ctx.Target == null || !ctx.Target.IsAlive())
        {
            failReason = "Mục tiêu không hợp lệ!";
            return false;
        }
        failReason = null;
        return true;
    }

    public void Execute(DiceActionContext ctx)
    {
        int raw = Mathf.Max(1, ctx.FaceData.valueV);
        int finalDmg = ctx.User != null ? ctx.User.CalculateOutgoingDamage(raw) : raw;
        ctx.Target.TakeDamage(finalDmg);

        if (ctx.User != null)
        {
            int healAmount = Mathf.Max(1, Mathf.FloorToInt(raw * 0.5f));
            ctx.User.Heal(healAmount);
        }
    }
}

// ─── 4. BLOCK (Common, 2-5): Cộng V điểm vào Shield. Tự hủy khi hết lượt ─────
public class BlockActionStrategy : IDiceAction
{
    public bool CanExecute(DiceActionContext ctx, out string failReason)
    {
        failReason = null;
        return ctx.User != null;
    }

    public void Execute(DiceActionContext ctx)
    {
        int shieldVal = Mathf.Max(1, ctx.FaceData.valueV);
        ctx.User.AddShield(shieldVal, retainHalf: false);
    }
}

// ─── 5. IRON WALL (Rare, 4): Cộng V Shield, bảo lưu 50% giáp sang lượt sau ───
public class IronWallActionStrategy : IDiceAction
{
    public bool CanExecute(DiceActionContext ctx, out string failReason)
    {
        failReason = null;
        return ctx.User != null;
    }

    public void Execute(DiceActionContext ctx)
    {
        int shieldVal = Mathf.Max(1, ctx.FaceData.valueV);
        ctx.User.AddShield(shieldVal, retainHalf: true);
    }
}

// ─── 6. REGENERATE (Common, 2-4): Hồi V điểm HP, không vượt HP_max ──────────
public class RegenerateActionStrategy : IDiceAction
{
    public bool CanExecute(DiceActionContext ctx, out string failReason)
    {
        failReason = null;
        return ctx.User != null;
    }

    public void Execute(DiceActionContext ctx)
    {
        int healVal = Mathf.Max(1, ctx.FaceData.valueV);
        ctx.User.Heal(healVal);
    }
}

// ─── 7. PURIFY (Rare, 0): Giải trừ toàn bộ hiệu ứng bất lợi ─────────────────
public class PurifyActionStrategy : IDiceAction
{
    public bool CanExecute(DiceActionContext ctx, out string failReason)
    {
        failReason = null;
        return ctx.User != null;
    }

    public void Execute(DiceActionContext ctx)
    {
        ctx.User.Purify();
        if (ctx.FaceData.valueV > 0)
        {
            ctx.User.Heal(ctx.FaceData.valueV);
        }
    }
}

// ─── 8. POISON STING (Rare, 2-3): Gây 1 dmg và cộng V stack Poison ──────────
public class PoisonStingActionStrategy : IDiceAction
{
    public bool CanExecute(DiceActionContext ctx, out string failReason)
    {
        if (ctx.Target == null || !ctx.Target.IsAlive())
        {
            failReason = "Mục tiêu không hợp lệ!";
            return false;
        }
        failReason = null;
        return true;
    }

    public void Execute(DiceActionContext ctx)
    {
        int stacks = Mathf.Max(1, ctx.FaceData.valueV);
        ctx.Target.TakeDamage(1);
        if (ctx.Target.IsAlive())
        {
            ctx.Target.AddPoison(stacks);
        }
    }
}

// ─── 9. SHOCK / STUN (Epic, 1): Đặt Choáng, vô hiệu hóa Intent địch lượt kế ──
public class ShockStunActionStrategy : IDiceAction
{
    public bool CanExecute(DiceActionContext ctx, out string failReason)
    {
        if (ctx.Target == null || !ctx.Target.IsAlive())
        {
            failReason = "Mục tiêu không hợp lệ!";
            return false;
        }
        failReason = null;
        return true;
    }

    public void Execute(DiceActionContext ctx)
    {
        int stunCount = Mathf.Max(1, ctx.FaceData.valueV);
        ctx.Target.AddStun(stunCount);
    }
}

// ─── 10. BLANK / MISS (Common, 0): Không tạo hiệu ứng ───────────────────────
public class BlankActionStrategy : IDiceAction
{
    public bool CanExecute(DiceActionContext ctx, out string failReason)
    {
        failReason = null;
        return true;
    }

    public void Execute(DiceActionContext ctx)
    {
        Debug.Log("[DiceAction] Mặt Blank (Miss) — Không tạo hiệu ứng.");
    }
}

/// <summary>
/// Registry ánh xạ từng DiceSkillKind sang Strategy tương ứng (không cần switch-case ở Controller).
/// </summary>
public static class DiceActionRegistry
{
    private static readonly Dictionary<DiceSkillKind, IDiceAction> strategies =
        new Dictionary<DiceSkillKind, IDiceAction>
        {
            { DiceSkillKind.Strike,      new StrikeActionStrategy() },
            { DiceSkillKind.HeavySlash,  new HeavySlashActionStrategy() },
            { DiceSkillKind.Vampiric,    new VampiricActionStrategy() },
            { DiceSkillKind.Block,       new BlockActionStrategy() },
            { DiceSkillKind.IronWall,    new IronWallActionStrategy() },
            { DiceSkillKind.Regenerate,  new RegenerateActionStrategy() },
            { DiceSkillKind.Purify,      new PurifyActionStrategy() },
            { DiceSkillKind.PoisonSting, new PoisonStingActionStrategy() },
            { DiceSkillKind.ShockStun,   new ShockStunActionStrategy() },
            { DiceSkillKind.Blank,       new BlankActionStrategy() }
        };

    public static IDiceAction GetStrategy(DiceFaceData faceData)
    {
        if (faceData == null) return strategies[DiceSkillKind.Blank];
        DiceSkillKind kind = faceData.GetResolvedSkillKind();
        if (strategies.TryGetValue(kind, out IDiceAction action))
        {
            return action;
        }
        return strategies[DiceSkillKind.Strike];
    }
}

