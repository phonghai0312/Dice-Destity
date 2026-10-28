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

    public DiceInstance(string name, DiceFaceData[] initialFaces)
    {
        diceName = name;
        faces = new DiceFaceData[FACES_COUNT];
        if (initialFaces != null)
        {
            for (int i = 0; i < FACES_COUNT && i < initialFaces.Length; i++)
            {
                faces[i] = initialFaces[i];
            }
        }
    }

    /// <summary>
    /// Tung ngẫu nhiên 1 trong 6 mặt của viên xúc xắc (GDD 3.4: PlayerRoll).
    /// </summary>
    public DiceFaceData Roll()
    {
        currentRolledFaceIndex = UnityEngine.Random.Range(0, FACES_COUNT);
        isSelectedForReroll = false;
        isConsumedThisTurn = false;
        return CurrentRolledFace;
    }

    /// <summary>
    /// Rèn đè 1 mặt mới lên 1 mặt cũ của viên xúc xắc (GDD 3.2: Dice-Crafting).
    /// Mặt cũ bị hủy/phân rã.
    /// </summary>
    public bool ReplaceFace(int slotIndex, DiceFaceData newFace)
    {
        if (newFace == null || slotIndex < 0 || slotIndex >= FACES_COUNT)
        {
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
    public static List<DiceInstance> CreateStarterDiceBag()
    {
        List<DiceInstance> bag = new List<DiceInstance>(3);

        // Viên 1: Xúc xắc Chiến Binh (Thiên về Công, Thủ & Năng lượng)
        bag.Add(new DiceInstance("Xúc Xắc Chiến Binh", new DiceFaceData[]
        {
            DiceFaceData.CreateRuntimeFace("Strike",     DiceFaceType.Attack,  DiceSkillKind.Strike,    4, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Strike",     DiceFaceType.Attack,  DiceSkillKind.Strike,    3, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Block",      DiceFaceType.Defend,  DiceSkillKind.Block,     3, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Mana Spark", DiceFaceType.Energy,  DiceSkillKind.ManaSpark, 2, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Mana Spark", DiceFaceType.Energy,  DiceSkillKind.ManaSpark, 1, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Blank",      DiceFaceType.Special, DiceSkillKind.Blank,     0, DiceFaceRarity.Common)
        }));

        // Viên 2: Xúc xắc Hộ Vệ (Thiên về Thủ, Hồi phục & Năng lượng)
        bag.Add(new DiceInstance("Xúc Xắc Hộ Vệ", new DiceFaceData[]
        {
            DiceFaceData.CreateRuntimeFace("Block",      DiceFaceType.Defend,  DiceSkillKind.Block,      4, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Block",      DiceFaceType.Defend,  DiceSkillKind.Block,      3, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Strike",     DiceFaceType.Attack,  DiceSkillKind.Strike,     3, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Regenerate", DiceFaceType.Heal,    DiceSkillKind.Regenerate, 3, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Mana Spark", DiceFaceType.Energy,  DiceSkillKind.ManaSpark,  2, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Blank",      DiceFaceType.Special, DiceSkillKind.Blank,      0, DiceFaceRarity.Common)
        }));

        // Viên 3: Xúc xắc Định Mệnh (Cân bằng Công, Độc, Thủ & Năng lượng)
        bag.Add(new DiceInstance("Xúc Xắc Định Mệnh", new DiceFaceData[]
        {
            DiceFaceData.CreateRuntimeFace("Strike",       DiceFaceType.Attack,  DiceSkillKind.Strike,      4, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Block",        DiceFaceType.Defend,  DiceSkillKind.Block,       3, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Poison Sting", DiceFaceType.Status,  DiceSkillKind.PoisonSting, 2, DiceFaceRarity.Rare),
            DiceFaceData.CreateRuntimeFace("Regenerate",   DiceFaceType.Heal,    DiceSkillKind.Regenerate,  2, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Mana Spark",   DiceFaceType.Energy,  DiceSkillKind.ManaSpark,   2, DiceFaceRarity.Common),
            DiceFaceData.CreateRuntimeFace("Blank",        DiceFaceType.Special, DiceSkillKind.Blank,       0, DiceFaceRarity.Common)
        }));

        return bag;
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
            DiceFaceData.CreateRuntimeFace("Overcharge",   DiceFaceType.Energy, DiceSkillKind.Overcharge,  4, DiceFaceRarity.Epic)
        };

        return cachedRewardPool;
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
