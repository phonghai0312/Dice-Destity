using UnityEngine;

public enum DiceFaceType { Attack, Defend, Heal, Special, Status, Energy }
public enum DiceFaceRarity { Common, Rare, Epic }

/// <summary>
/// Danh sách 13 kỹ năng mặt xúc xắc chuẩn theo tài liệu thiết kế (10 mặt gốc + 3 mặt Năng lượng).
/// </summary>
public enum DiceSkillKind
{
    AutoDetect = 0,
    Strike,         // Common (2-4): Gây V sát thương chuẩn
    HeavySlash,     // Rare (6-8): Gây V sát thương; yêu cầu tiêu thụ thêm 1 mặt Khiên hoặc có sẵn Khiên
    Vampiric,       // Epic (3-4): Gây V sát thương, đồng thời hồi floor(V/2) HP
    Block,          // Common (2-5): Cộng trực tiếp V điểm vào Shield. Tự hủy khi hết lượt
    IronWall,       // Rare (4): Cộng V Shield, bảo lưu 50% giáp sang lượt sau
    Regenerate,     // Common (2-4): Hồi V điểm HP. Không vượt ngưỡng HP_max
    Purify,         // Rare (0): Giải trừ toàn bộ hiệu ứng bất lợi
    PoisonSting,    // Rare (2-3): Gây 1 dmg và cộng V stack Poison vào mục tiêu
    ShockStun,      // Epic (1): Đặt Choáng, vô hiệu hóa Intent địch ở lượt kế
    Blank,          // Common (0): Không tạo hiệu ứng (mặt rủi ro cần tìm cách rèn đè)
    ManaSpark,      // Common (1-2): Tích lũy ngay lập tức +V điểm Năng lượng vào thanh kỹ năng
    ArcaneSurge,    // Rare (2-3): Tích lũy +V Năng lượng và cộng thêm 2 Shield cho bản thân
    Overcharge      // Epic (4): Tích lũy +4 Năng lượng; giảm 1 chi phí Năng lượng cho kỹ năng tiếp theo trong lượt
}

[CreateAssetMenu(fileName = "NewDiceFace", menuName = "Dice & Destiny/Dice Face")]
public class DiceFaceData : ScriptableObject
{
    public string faceName;
    public Sprite icon;
    public DiceFaceType type;
    public int valueV;
    public DiceFaceRarity rarity;

    [Header("Skill Behavior (GDD 3.2)")]
    public DiceSkillKind skillKind = DiceSkillKind.AutoDetect;
    [TextArea(2, 4)]
    public string description;

    /// <summary>
    /// Xác định kỹ năng thực tế của mặt xúc xắc (tự động suy luận từ tên/loại nếu để AutoDetect).
    /// </summary>
    public DiceSkillKind GetResolvedSkillKind()
    {
        if (skillKind != DiceSkillKind.AutoDetect)
        {
            return skillKind;
        }

        string lower = (faceName ?? string.Empty).ToLowerInvariant();
        if (lower.Contains("heavy") || lower.Contains("cleave")) return DiceSkillKind.HeavySlash;
        if (lower.Contains("vampir") || lower.Contains("drain") || lower.Contains("hút")) return DiceSkillKind.Vampiric;
        if (lower.Contains("iron") || lower.Contains("wall")) return DiceSkillKind.IronWall;
        if (lower.Contains("purify") || lower.Contains("cleanse") || lower.Contains("thanh tẩy")) return DiceSkillKind.Purify;
        if (lower.Contains("poison") || lower.Contains("venom") || lower.Contains("độc")) return DiceSkillKind.PoisonSting;
        if (lower.Contains("shock") || lower.Contains("stun") || lower.Contains("choáng") || lower.Contains("doom")) return DiceSkillKind.ShockStun;
        if (lower.Contains("overcharge") || lower.Contains("quá tải")) return DiceSkillKind.Overcharge;
        if (lower.Contains("arcane") || lower.Contains("surge")) return DiceSkillKind.ArcaneSurge;
        if (lower.Contains("mana") || lower.Contains("spark") || lower.Contains("năng lượng")) return DiceSkillKind.ManaSpark;
        if (lower.Contains("blank") || lower.Contains("miss") || lower.Contains("trượt")) return DiceSkillKind.Blank;
        if (lower.Contains("regen") || lower.Contains("heal") || lower.Contains("hồi")) return DiceSkillKind.Regenerate;
        if (lower.Contains("block") || lower.Contains("shield") || lower.Contains("khiên") || lower.Contains("thủ")) return DiceSkillKind.Block;

        switch (type)
        {
            case DiceFaceType.Attack: return DiceSkillKind.Strike;
            case DiceFaceType.Defend: return DiceSkillKind.Block;
            case DiceFaceType.Heal:   return DiceSkillKind.Regenerate;
            case DiceFaceType.Status: return DiceSkillKind.PoisonSting;
            case DiceFaceType.Energy: return DiceSkillKind.ManaSpark;
            default:                  return valueV > 0 ? DiceSkillKind.Strike : DiceSkillKind.Blank;
        }
    }

    /// <summary>
    /// Kiểm tra mặt xúc xắc này nhắm vào Kẻ địch (true) hay Bản thân người chơi (false).
    /// </summary>
    public bool TargetsEnemy()
    {
        DiceSkillKind resolved = GetResolvedSkillKind();
        switch (resolved)
        {
            case DiceSkillKind.Strike:
            case DiceSkillKind.HeavySlash:
            case DiceSkillKind.Vampiric:
            case DiceSkillKind.PoisonSting:
            case DiceSkillKind.ShockStun:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Nội dung Tooltip chi tiết khi người chơi hover chuột vào mặt xúc xắc (GDD mục 2.2).
    /// </summary>
    public string GetTooltipText()
    {
        if (!string.IsNullOrWhiteSpace(description))
        {
            return $"<b>{faceName}</b> ({rarity})\n{description}";
        }

        DiceSkillKind resolved = GetResolvedSkillKind();
        string ruleText;
        switch (resolved)
        {
            case DiceSkillKind.Strike:
                ruleText = $"Gây {valueV} sát thương chuẩn lên mục tiêu.";
                break;
            case DiceSkillKind.HeavySlash:
                ruleText = $"Gây {valueV} sát thương lớn (Yêu cầu tiêu thụ 1 mặt Khiên hoặc đang có Giáp).";
                break;
            case DiceSkillKind.Vampiric:
                ruleText = $"Gây {valueV} sát thương và hồi {Mathf.Max(1, valueV / 2)} HP cho bản thân.";
                break;
            case DiceSkillKind.Block:
                ruleText = $"Cộng +{valueV} Giáp ảo (Tự hủy khi hết lượt).";
                break;
            case DiceSkillKind.IronWall:
                ruleText = $"Cộng +{valueV} Giáp ảo và bảo lưu 50% Giáp sang lượt sau.";
                break;
            case DiceSkillKind.Regenerate:
                ruleText = $"Hồi phục +{valueV} HP cho bản thân.";
                break;
            case DiceSkillKind.Purify:
                ruleText = "Giải trừ toàn bộ hiệu ứng bất lợi (Độc, Thiêu đốt, Choáng, Suy yếu).";
                break;
            case DiceSkillKind.PoisonSting:
                ruleText = $"Gây 1 sát thương và cộng +{valueV} tầng Độc vào mục tiêu.";
                break;
            case DiceSkillKind.ShockStun:
                ruleText = "Đặt Choáng (Stun), vô hiệu hóa hành động của địch ở lượt kế.";
                break;
            case DiceSkillKind.ManaSpark:
                ruleText = $"Tích lũy ngay lập tức +{valueV} điểm Năng lượng vào thanh kỹ năng.";
                break;
            case DiceSkillKind.ArcaneSurge:
                ruleText = $"Tích lũy +{valueV} Năng lượng và cộng thêm +2 Giáp ảo cho bản thân.";
                break;
            case DiceSkillKind.Overcharge:
                ruleText = $"Tích lũy +{valueV} Năng lượng và giảm 1 chi phí Năng lượng cho kỹ năng tiếp theo trong lượt.";
                break;
            case DiceSkillKind.Blank:
            default:
                ruleText = "Trượt (Miss) — Không tạo hiệu ứng. Nên rèn đè ở Xưởng Rèn!";
                break;
        }

        return $"<b>{faceName}</b> [{rarity}]\n{ruleText}";
    }

    /// <summary>
    /// Khởi tạo nhanh một DiceFaceData lúc chạy (Runtime) theo thông số bảng GDD 3.2.
    /// </summary>
    public static DiceFaceData CreateRuntimeFace(
        string name,
        DiceFaceType faceType,
        DiceSkillKind kind,
        int value,
        DiceFaceRarity faceRarity,
        Sprite faceIcon = null,
        string customDesc = null)
    {
        DiceFaceData data = CreateInstance<DiceFaceData>();
        data.name = $"Face_{name}_{value}";
        data.faceName = name;
        data.type = faceType;
        data.skillKind = kind;
        data.valueV = value;
        data.rarity = faceRarity;
        data.icon = faceIcon;
        data.description = customDesc;
        return data;
    }
}
