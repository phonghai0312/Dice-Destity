using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// DiceInstance — Mô hình một viên xúc xắc lục diện (đúng 6 mặt) theo GDD mục 3.1 & 3.2.
/// </summary>
[Serializable]
public class DiceInstance
{
    public const int FACES_COUNT = 6;

    public string diceName = "Xúc Xắc Vận Mệnh";
    public DiceFaceData[] faces = new DiceFaceData[FACES_COUNT];
    public bool[] slotLocked = new bool[FACES_COUNT];

    [NonSerialized] public int currentRolledFaceIndex = 0;
    [NonSerialized] public bool isSelectedForReroll = false;
    [NonSerialized] public bool isConsumedThisTurn = false;

    public DiceFaceData CurrentRolledFace
    {
        get
        {
            if (faces == null || faces.Length == 0) return null;
            int idx = Mathf.Clamp(currentRolledFaceIndex, 0, faces.Length - 1);
            return faces[idx];
        }
    }

    public DiceInstance(string name, DiceFaceData[] initialFaces, bool[] lockedSlots = null)
    {
        diceName = name;
        faces = new DiceFaceData[FACES_COUNT];
        slotLocked = new bool[FACES_COUNT];

        if (initialFaces != null)
        {
            for (int i = 0; i < FACES_COUNT && i < initialFaces.Length; i++)
            {
                faces[i] = initialFaces[i];
            }
        }

        if (lockedSlots != null)
        {
            for (int i = 0; i < FACES_COUNT && i < lockedSlots.Length; i++)
            {
                slotLocked[i] = lockedSlots[i];
            }
        }
        else
        {
            // Mặc định: Ban đầu 4 mặt khả dụng (0..3), 2 mặt còn lại (4..5) bị khoá
            slotLocked[4] = true;
            slotLocked[5] = true;
        }
    }

    public bool IsSlotLocked(int slotIndex)
    {
        if (slotLocked == null || slotLocked.Length < FACES_COUNT)
        {
            bool[] newLocked = new bool[FACES_COUNT];
            if (slotLocked != null)
            {
                for (int i = 0; i < slotLocked.Length && i < FACES_COUNT; i++)
                    newLocked[i] = slotLocked[i];
            }
            slotLocked = newLocked;
        }
        if (slotIndex < 0 || slotIndex >= FACES_COUNT) return false;
        return slotLocked[slotIndex];
    }

    public void UnlockSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < FACES_COUNT)
        {
            if (slotLocked == null || slotLocked.Length < FACES_COUNT)
                IsSlotLocked(slotIndex);

            slotLocked[slotIndex] = false;
            if (faces[slotIndex] == null)
            {
                faces[slotIndex] = DiceFaceData.CreateRuntimeFace("Trống", DiceFaceType.Special, DiceSkillKind.Blank, 0, DiceFaceRarity.Common);
            }
        }
    }

    public int GetUnlockedSlotCount()
    {
        int count = 0;
        for (int i = 0; i < FACES_COUNT; i++)
        {
            if (!IsSlotLocked(i)) count++;
        }
        return count;
    }

    public int CountFaceKind(DiceSkillKind kind)
    {
        int count = 0;
        for (int i = 0; i < FACES_COUNT; i++)
        {
            if (!IsSlotLocked(i) && faces[i] != null && faces[i].GetResolvedSkillKind() == kind)
                count++;
        }
        return count;
    }

    public bool CanEquipFace(int targetSlot, DiceSkillKind newKind, out string reason)
    {
        if (targetSlot < 0 || targetSlot >= FACES_COUNT)
        {
            reason = "Vị trí ô không hợp lệ!";
            return false;
        }
        if (IsSlotLocked(targetSlot))
        {
            reason = "Ô này đang bị khóa! Vượt ải để mở khóa.";
            return false;
        }
        if (newKind == DiceSkillKind.Blank)
        {
            reason = "";
            return true;
        }

        DiceFaceData currentFace = faces[targetSlot];
        DiceSkillKind currentKind = currentFace != null ? currentFace.GetResolvedSkillKind() : DiceSkillKind.Blank;

        // Nếu thay thế vào ô đang chứa chính loại mặt này (ví dụ đè cấp cao hơn lên cùng loại), số lượng không tăng
        if (currentKind == newKind)
        {
            reason = "";
            return true;
        }

        int currentCountOfNewKind = CountFaceKind(newKind);
        if (currentCountOfNewKind >= 2)
        {
            reason = $"Mỗi viên xúc xắc chỉ được gắn tối đa 2 mặt cùng loại ({newKind})!";
            return false;
        }

        reason = "";
        return true;
    }

    /// <summary>
    /// Tung ngẫu nhiên trong các mặt KHẢ DỤNG (chưa bị khoá) của viên xúc xắc.
    /// </summary>
    public DiceFaceData Roll()
    {
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < FACES_COUNT; i++)
        {
            if (!IsSlotLocked(i) && faces[i] != null)
            {
                availableIndices.Add(i);
            }
        }

        if (availableIndices.Count > 0)
        {
            currentRolledFaceIndex = availableIndices[UnityEngine.Random.Range(0, availableIndices.Count)];
        }
        else
        {
            currentRolledFaceIndex = 0;
        }

        isSelectedForReroll = false;
        isConsumedThisTurn = false;
        return CurrentRolledFace;
    }

    /// <summary>
    /// Rèn đè 1 mặt mới lên 1 mặt cũ của viên xúc xắc.
    /// Tuân thủ quy tắc: Không rèn vào ô bị khoá & Tối đa 2 mặt cùng loại/viên.
    /// </summary>
    public bool ReplaceFace(int slotIndex, DiceFaceData newFace)
    {
        if (newFace == null || slotIndex < 0 || slotIndex >= FACES_COUNT)
        {
            return false;
        }
        string reason;
        if (!CanEquipFace(slotIndex, newFace.GetResolvedSkillKind(), out reason))
        {
            Debug.LogWarning($"[DiceInstance] Không thể rèn đè vào ô #{slotIndex}: {reason}");
            return false;
        }

        faces[slotIndex] = newFace;
        return true;
    }
}

/// <summary>
/// DiceCatalog — Khởi tạo bộ túi xúc xắc cơ bản (Starter Dice Bag) và danh mục 13 kỹ năng chuẩn GDD.
/// </summary>
public static class DiceCatalog
{
    private static List<DiceFaceData> cachedRewardPool;

    /// <summary>
    /// Tạo bộ 3 viên xúc xắc khởi đầu (mỗi viên 6 mặt) theo GDD:
    /// Có đầy đủ Tấn công (Strike), Phòng thủ (Block), Hồi máu (Regenerate), Năng lượng (Mana Spark) và 1 mặt Blank (Miss) để rèn đè.
    /// </summary>
    /// <summary>
    /// Tạo bộ 3 viên xúc xắc khởi đầu chuẩn New Game:
    /// - Chỉ bao gồm 4 hệ: Tấn công (Strike), Giáp (Block), Hồi máu (Regenerate), Năng lượng (Mana Spark).
    /// - Mỗi viên ban đầu chỉ mở 4 mặt (ô 0..3), 2 mặt còn lại (ô 4..5) bị khoá.
    /// - Mỗi mặt xuất hiện tối đa 2 lần trên cùng 1 viên.
    /// </summary>
    public static List<DiceInstance> CreateStarterDiceBag()
    {
        List<DiceInstance> bag = new List<DiceInstance>(3);
        bool[] locked4and5 = new bool[] { false, false, false, false, true, true };

        // Viên 1: Xúc Xắc Chiến Binh (2 Công, 1 Thủ, 1 Năng lượng, 2 Ô Khoá)
        bag.Add(new DiceInstance("Xúc Xắc Chiến Binh", new DiceFaceData[]
        {
            DiceFaceData.CreateRuntimeFace("Strike",     DiceFaceType.Attack,  DiceSkillKind.Strike,    3, DiceFaceRarity.Common, null, null, 1),
            DiceFaceData.CreateRuntimeFace("Strike",     DiceFaceType.Attack,  DiceSkillKind.Strike,    3, DiceFaceRarity.Common, null, null, 1),
            DiceFaceData.CreateRuntimeFace("Block",      DiceFaceType.Defend,  DiceSkillKind.Block,     3, DiceFaceRarity.Common, null, null, 1),
            DiceFaceData.CreateRuntimeFace("Mana Spark", DiceFaceType.Energy,  DiceSkillKind.ManaSpark, 1, DiceFaceRarity.Common, null, null, 1),
            DiceFaceData.CreateRuntimeFace("Trống",      DiceFaceType.Special, DiceSkillKind.Blank,     0, DiceFaceRarity.Common, null, null, 1),
            DiceFaceData.CreateRuntimeFace("Trống",      DiceFaceType.Special, DiceSkillKind.Blank,     0, DiceFaceRarity.Common, null, null, 1)
        }, locked4and5));

        // Viên 2: Xúc Xắc Hộ Vệ (2 Thủ, 1 Công, 1 Hồi máu, 2 Ô Khoá)
        bag.Add(new DiceInstance("Xúc Xắc Hộ Vệ", new DiceFaceData[]
        {
            DiceFaceData.CreateRuntimeFace("Block",      DiceFaceType.Defend,  DiceSkillKind.Block,      3, DiceFaceRarity.Common, null, null, 1),
            DiceFaceData.CreateRuntimeFace("Block",      DiceFaceType.Defend,  DiceSkillKind.Block,      3, DiceFaceRarity.Common, null, null, 1),
            DiceFaceData.CreateRuntimeFace("Strike",     DiceFaceType.Attack,  DiceSkillKind.Strike,     3, DiceFaceRarity.Common, null, null, 1),
            DiceFaceData.CreateRuntimeFace("Regenerate", DiceFaceType.Heal,    DiceSkillKind.Regenerate, 2, DiceFaceRarity.Common, null, null, 1),
            DiceFaceData.CreateRuntimeFace("Trống",      DiceFaceType.Special, DiceSkillKind.Blank,      0, DiceFaceRarity.Common, null, null, 1),
            DiceFaceData.CreateRuntimeFace("Trống",      DiceFaceType.Special, DiceSkillKind.Blank,      0, DiceFaceRarity.Common, null, null, 1)
        }, locked4and5));

        // Viên 3: Xúc Xắc Cân Bằng (1 Công, 1 Thủ, 1 Hồi máu, 1 Năng lượng, 2 Ô Khoá)
        bag.Add(new DiceInstance("Xúc Xắc Cân Bằng", new DiceFaceData[]
        {
            DiceFaceData.CreateRuntimeFace("Strike",     DiceFaceType.Attack,  DiceSkillKind.Strike,     3, DiceFaceRarity.Common, null, null, 1),
            DiceFaceData.CreateRuntimeFace("Block",      DiceFaceType.Defend,  DiceSkillKind.Block,      3, DiceFaceRarity.Common, null, null, 1),
            DiceFaceData.CreateRuntimeFace("Regenerate", DiceFaceType.Heal,    DiceSkillKind.Regenerate, 2, DiceFaceRarity.Common, null, null, 1),
            DiceFaceData.CreateRuntimeFace("Mana Spark", DiceFaceType.Energy,  DiceSkillKind.ManaSpark,  1, DiceFaceRarity.Common, null, null, 1),
            DiceFaceData.CreateRuntimeFace("Trống",      DiceFaceType.Special, DiceSkillKind.Blank,      0, DiceFaceRarity.Common, null, null, 1),
            DiceFaceData.CreateRuntimeFace("Trống",      DiceFaceType.Special, DiceSkillKind.Blank,      0, DiceFaceRarity.Common, null, null, 1)
        }, locked4and5));

        return bag;
    }

    /// <summary>
    /// Kho mặt dự trữ khởi đầu (Starter Reserve Pool) khi bắt đầu New Game.
    /// Chỉ gồm 4 hệ cơ bản: Tấn công, Giáp, Hồi máu, Năng lượng.
    /// Cho sẵn 2 mặt Strike để người chơi có thể thử nghiệm tính năng CƯỜNG HOÁ ngay từ đầu.
    /// </summary>
    public static List<DiceFaceData> CreateStarterReservePool()
    {
        return new List<DiceFaceData>
        {
            DiceFaceData.CreateRuntimeFace("Strike",     DiceFaceType.Attack, DiceSkillKind.Strike,     3, DiceFaceRarity.Common, null, null, 1),
            DiceFaceData.CreateRuntimeFace("Strike",     DiceFaceType.Attack, DiceSkillKind.Strike,     3, DiceFaceRarity.Common, null, null, 1),
            DiceFaceData.CreateRuntimeFace("Block",      DiceFaceType.Defend, DiceSkillKind.Block,      3, DiceFaceRarity.Common, null, null, 1),
            DiceFaceData.CreateRuntimeFace("Mana Spark", DiceFaceType.Energy, DiceSkillKind.ManaSpark,  1, DiceFaceRarity.Common, null, null, 1)
        };
    }

    /// <summary>
    /// Sinh ngẫu nhiên 3 phần thưởng mặt xúc xắc sau khi vượt ải (Chọn 1 trong 3).
    /// - Ải 1 xong: Cung cấp mặt nâng cấp Lv.1-Lv.2 của Công/Thủ/Hồi/Năng lượng và bắt đầu xuất hiện Độc/Lửa.
    /// - Ải 2 xong: Cung cấp mặt Lv.2-Lv.3 và các kỹ năng Hiếm/Sử thi (Heavy Slash, Iron Wall, Vampiric, Shock).
    /// </summary>
    public static List<DiceFaceData> RollStageRewardOptions(int clearedStage, int count = 3)
    {
        List<DiceFaceData> rewardPool = new List<DiceFaceData>();

        if (clearedStage <= 1)
        {
            rewardPool.Add(DiceFaceData.CreateRuntimeFace("Strike Lv.2",     DiceFaceType.Attack,  DiceSkillKind.Strike,     5, DiceFaceRarity.Rare,   null, null, 2));
            rewardPool.Add(DiceFaceData.CreateRuntimeFace("Block Lv.2",      DiceFaceType.Defend,  DiceSkillKind.Block,      5, DiceFaceRarity.Rare,   null, null, 2));
            rewardPool.Add(DiceFaceData.CreateRuntimeFace("Regenerate Lv.2", DiceFaceType.Heal,    DiceSkillKind.Regenerate, 4, DiceFaceRarity.Rare,   null, null, 2));
            rewardPool.Add(DiceFaceData.CreateRuntimeFace("Mana Spark Lv.2", DiceFaceType.Energy,  DiceSkillKind.ManaSpark,  2, DiceFaceRarity.Rare,   null, null, 2));
            rewardPool.Add(DiceFaceData.CreateRuntimeFace("Poison Sting",    DiceFaceType.Status,  DiceSkillKind.PoisonSting,2, DiceFaceRarity.Rare,   null, null, 1));
            rewardPool.Add(DiceFaceData.CreateRuntimeFace("Ignite",          DiceFaceType.Status,  DiceSkillKind.Ignite,     2, DiceFaceRarity.Rare,   null, null, 1));
        }
        else
        {
            rewardPool.Add(DiceFaceData.CreateRuntimeFace("Heavy Slash",     DiceFaceType.Attack,  DiceSkillKind.HeavySlash, 7, DiceFaceRarity.Rare,   null, null, 1));
            rewardPool.Add(DiceFaceData.CreateRuntimeFace("Iron Wall",       DiceFaceType.Defend,  DiceSkillKind.IronWall,   4, DiceFaceRarity.Rare,   null, null, 1));
            rewardPool.Add(DiceFaceData.CreateRuntimeFace("Vampiric",        DiceFaceType.Attack,  DiceSkillKind.Vampiric,   4, DiceFaceRarity.Epic,   null, null, 1));
            rewardPool.Add(DiceFaceData.CreateRuntimeFace("Shock / Stun",    DiceFaceType.Status,  DiceSkillKind.ShockStun,  1, DiceFaceRarity.Epic,   null, null, 1));
            rewardPool.Add(DiceFaceData.CreateRuntimeFace("Arcane Surge",    DiceFaceType.Energy,  DiceSkillKind.ArcaneSurge,3, DiceFaceRarity.Rare,   null, null, 1));
            rewardPool.Add(DiceFaceData.CreateRuntimeFace("Strike Lv.3",     DiceFaceType.Attack,  DiceSkillKind.Strike,     8, DiceFaceRarity.Epic,   null, null, 3));
            rewardPool.Add(DiceFaceData.CreateRuntimeFace("Block Lv.3",      DiceFaceType.Defend,  DiceSkillKind.Block,      8, DiceFaceRarity.Epic,   null, null, 3));
        }

        List<DiceFaceData> picked = new List<DiceFaceData>();
        for (int i = 0; i < count && rewardPool.Count > 0; i++)
        {
            int rIdx = UnityEngine.Random.Range(0, rewardPool.Count);
            picked.Add(rewardPool[rIdx]);
            rewardPool.RemoveAt(rIdx);
        }
        return picked;
    }

    /// <summary>
    /// Danh sách đầy đủ các mặt xúc xắc nâng cấp dùng làm phần thưởng trong Xưởng Rèn (đủ 13 loại mặt trong GDD).
    /// </summary>
    public static List<DiceFaceData> GetForgeRewardPool()
    {
        if (cachedRewardPool != null && cachedRewardPool.Count > 0)
        {
            return cachedRewardPool;
        }

        cachedRewardPool = new List<DiceFaceData>
        {
            DiceFaceData.CreateRuntimeFace("Heavy Slash",  DiceFaceType.Attack, DiceSkillKind.HeavySlash,  7, DiceFaceRarity.Rare),
            DiceFaceData.CreateRuntimeFace("Heavy Slash",  DiceFaceType.Attack, DiceSkillKind.HeavySlash,  8, DiceFaceRarity.Rare),
            DiceFaceData.CreateRuntimeFace("Vampiric",     DiceFaceType.Attack, DiceSkillKind.Vampiric,    4, DiceFaceRarity.Epic),
            DiceFaceData.CreateRuntimeFace("Iron Wall",    DiceFaceType.Defend, DiceSkillKind.IronWall,    4, DiceFaceRarity.Rare),
            DiceFaceData.CreateRuntimeFace("Block",        DiceFaceType.Defend, DiceSkillKind.Block,       5, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Regenerate",   DiceFaceType.Heal,   DiceSkillKind.Regenerate,  4, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Purify",       DiceFaceType.Heal,   DiceSkillKind.Purify,      0, DiceFaceRarity.Rare),
            DiceFaceData.CreateRuntimeFace("Poison Sting", DiceFaceType.Status, DiceSkillKind.PoisonSting, 3, DiceFaceRarity.Rare),
            DiceFaceData.CreateRuntimeFace("Shock / Stun", DiceFaceType.Status, DiceSkillKind.ShockStun,   1, DiceFaceRarity.Epic),
            DiceFaceData.CreateRuntimeFace("Strike",       DiceFaceType.Attack, DiceSkillKind.Strike,      4, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Mana Spark",   DiceFaceType.Energy, DiceSkillKind.ManaSpark,   2, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Arcane Surge", DiceFaceType.Energy, DiceSkillKind.ArcaneSurge, 3, DiceFaceRarity.Rare),
            DiceFaceData.CreateRuntimeFace("Overcharge",   DiceFaceType.Energy, DiceSkillKind.Overcharge,  4, DiceFaceRarity.Epic),
            DiceFaceData.CreateRuntimeFace("Ignite",       DiceFaceType.Status, DiceSkillKind.Ignite,      3, DiceFaceRarity.Rare)
        };

        return cachedRewardPool;
    }

    /// <summary>
    /// Danh sách chuẩn 13 mặt xúc xắc (không trùng lặp) dùng cho Bách Khoa Xúc Xắc (Codex).
    /// </summary>
    public static List<DiceFaceData> GetAllCanonicalFaces()
    {
        return new List<DiceFaceData>
        {
            DiceFaceData.CreateRuntimeFace("Strike",       DiceFaceType.Attack,  DiceSkillKind.Strike,      4, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Heavy Slash",  DiceFaceType.Attack,  DiceSkillKind.HeavySlash,  7, DiceFaceRarity.Rare),
            DiceFaceData.CreateRuntimeFace("Vampiric",     DiceFaceType.Attack,  DiceSkillKind.Vampiric,    4, DiceFaceRarity.Epic),
            DiceFaceData.CreateRuntimeFace("Block",        DiceFaceType.Defend,  DiceSkillKind.Block,       4, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Iron Wall",    DiceFaceType.Defend,  DiceSkillKind.IronWall,    4, DiceFaceRarity.Rare),
            DiceFaceData.CreateRuntimeFace("Regenerate",   DiceFaceType.Heal,    DiceSkillKind.Regenerate,  3, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Purify",       DiceFaceType.Heal,    DiceSkillKind.Purify,      0, DiceFaceRarity.Rare),
            DiceFaceData.CreateRuntimeFace("Poison Sting", DiceFaceType.Status,  DiceSkillKind.PoisonSting, 2, DiceFaceRarity.Rare),
            DiceFaceData.CreateRuntimeFace("Shock / Stun", DiceFaceType.Status,  DiceSkillKind.ShockStun,   1, DiceFaceRarity.Epic),
            DiceFaceData.CreateRuntimeFace("Blank",        DiceFaceType.Special, DiceSkillKind.Blank,       0, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Mana Spark",   DiceFaceType.Energy,  DiceSkillKind.ManaSpark,   2, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Arcane Surge", DiceFaceType.Energy,  DiceSkillKind.ArcaneSurge, 3, DiceFaceRarity.Rare),
            DiceFaceData.CreateRuntimeFace("Overcharge",   DiceFaceType.Energy,  DiceSkillKind.Overcharge,  4, DiceFaceRarity.Epic),
            DiceFaceData.CreateRuntimeFace("Ignite",       DiceFaceType.Status,  DiceSkillKind.Ignite,      3, DiceFaceRarity.Rare)
        };
    }

    /// <summary>
    /// Rút ngẫu nhiên N mặt xúc xắc khác nhau để hiển thị tại Xưởng Rèn (TheForge).
    /// </summary>
    public static List<DiceFaceData> RollRandomRewards(int count = 3)
    {
        List<DiceFaceData> pool = new List<DiceFaceData>(GetForgeRewardPool());
        List<DiceFaceData> result = new List<DiceFaceData>(count);

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int idx = UnityEngine.Random.Range(0, pool.Count);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        return result;
    }
}
