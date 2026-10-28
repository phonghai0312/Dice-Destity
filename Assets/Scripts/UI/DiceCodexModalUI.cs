using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// DiceCodexModalUI — Cửa sổ Từ Điển 13 Mặt Xúc Xắc & Kiểm Tra Túi Xúc Xắc + Kỹ Năng Nhân Vật.
/// Có thể mở từ MainMenu, StageMap, TheForge hoặc CombatScene để người chơi tra cứu công thức
/// sát thương, độ hiếm và trạng thái 3 viên xúc xắc 6 mặt đang trang bị.
/// </summary>
public class DiceCodexModalUI : MonoBehaviour
{
    private GameObject modalRoot;
    private GameObject catalogTabView;
    private GameObject bagAndSkillsTabView;
    private Button tabCatalogBtn;
    private Button tabBagBtn;
    private TextMeshProUGUI bagSummaryText;
    private TextMeshProUGUI skillsSummaryText;

    public static DiceCodexModalUI ToggleOrCreate(Transform parentCanvas)
    {
        if (parentCanvas == null)
        {
            Canvas found = FindAnyObjectByType<Canvas>();
            if (found != null) parentCanvas = found.transform;
        }
        if (parentCanvas == null) return null;

        DiceCodexModalUI existing = parentCanvas.GetComponentInChildren<DiceCodexModalUI>(true);
        if (existing != null)
        {
            bool nextState = !existing.gameObject.activeSelf;
            existing.gameObject.SetActive(nextState);
            if (nextState)
            {
                existing.transform.SetAsLastSibling();
                existing.RefreshBagAndSkillsContent();
            }
            return existing;
        }

        GameObject go = new GameObject("DiceCodexModalUI", typeof(RectTransform));
        go.transform.SetParent(parentCanvas, false);
        go.transform.SetAsLastSibling();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        DiceCodexModalUI codex = go.AddComponent<DiceCodexModalUI>();
        codex.BuildCodexHierarchy();
        codex.RefreshBagAndSkillsContent();
        return codex;
    }

    private void BuildCodexHierarchy()
    {
        modalRoot = gameObject;

        Image backdrop = modalRoot.AddComponent<Image>();
        backdrop.color = new Color(0.02f, 0.02f, 0.05f, 0.88f);

        GameObject panel = new GameObject("CodexPanel", typeof(RectTransform), typeof(Image), typeof(Outline));
        panel.transform.SetParent(modalRoot.transform, false);
        RectTransform pRt = panel.GetComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0.5f, 0.5f);
        pRt.anchorMax = new Vector2(0.5f, 0.5f);
        pRt.sizeDelta = new Vector2(1420f, 840f);
        pRt.anchoredPosition = Vector2.zero;

        panel.GetComponent<Image>().color = new Color(0.07f, 0.06f, 0.12f, 0.98f);
        Outline pOl = panel.GetComponent<Outline>();
        pOl.effectColor = new Color(0.92f, 0.75f, 0.25f, 0.95f);
        pOl.effectDistance = new Vector2(2.5f, -2.5f);

        // Header
        TextMeshProUGUI titleTmp = CreateTMP(
            panel.transform,
            "CodexTitle",
            "📖 BÁCH KHOA XÚC XẮC & TÚI ĐỒ CHIẾN THUẬT (CODEX)",
            new Vector2(-240f, 376f),
            new Vector2(880f, 44f),
            25f,
            new Color(1f, 0.86f, 0.34f, 1f),
            TextAlignmentOptions.Left);
        titleTmp.fontStyle = FontStyles.Bold;

        // 2 Tab chuyển đổi: [13 Mặt Xúc Xắc] và [Túi Xúc Xắc & Kỹ Năng Hiệp Sĩ]
        tabCatalogBtn = CreateStyledButton(
            panel.transform,
            "TabCatalogButton",
            "🎲 13 MẶT XÚC XẮC CHUẨN GDD",
            new Vector2(210f, 376f),
            new Vector2(260f, 42f),
            new Color(0.48f, 0.32f, 0.08f, 0.98f),
            new Color(1f, 0.84f, 0.28f, 1f),
            14.5f);
        tabCatalogBtn.onClick.AddListener(() => SwitchTab(showCatalog: true));

        tabBagBtn = CreateStyledButton(
            panel.transform,
            "TabBagButton",
            "🎒 TÚI XÚC XẮC & KỸ NĂNG HIỆN TẠI",
            new Vector2(485f, 376f),
            new Vector2(270f, 42f),
            new Color(0.14f, 0.16f, 0.26f, 0.96f),
            new Color(0.45f, 0.72f, 1f, 0.85f),
            14.5f);
        tabBagBtn.onClick.AddListener(() => SwitchTab(showCatalog: false));

        // Nút Đóng
        Button closeBtn = CreateStyledButton(
            panel.transform,
            "CloseCodexButton",
            "✖ ĐÓNG",
            new Vector2(655f, 376f),
            new Vector2(76f, 42f),
            new Color(0.56f, 0.14f, 0.16f, 0.98f),
            new Color(1f, 0.45f, 0.45f, 0.9f),
            14f);
        closeBtn.onClick.AddListener(() => gameObject.SetActive(false));

        // --- TAB 1: DANH MỤC 13 MẶT XÚC XẮC ---
        catalogTabView = new GameObject("CatalogTabView", typeof(RectTransform), typeof(GridLayoutGroup));
        catalogTabView.transform.SetParent(panel.transform, false);
        RectTransform catRt = catalogTabView.GetComponent<RectTransform>();
        catRt.anchoredPosition = new Vector2(0f, -28f);
        catRt.sizeDelta = new Vector2(1360f, 710f);

        GridLayoutGroup grid = catalogTabView.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(326f, 162f);
        grid.spacing = new Vector2(16f, 16f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.UpperCenter;

        PopulateAll13FacesGrid(catalogTabView.transform);

        // --- TAB 2: TÚI XÚC XẮC (3 VIÊN) & 2 KỸ NĂNG NHÂN VẬT ---
        bagAndSkillsTabView = new GameObject("BagAndSkillsTabView", typeof(RectTransform));
        bagAndSkillsTabView.transform.SetParent(panel.transform, false);
        RectTransform bagRt = bagAndSkillsTabView.GetComponent<RectTransform>();
        bagRt.anchoredPosition = new Vector2(0f, -28f);
        bagRt.sizeDelta = new Vector2(1360f, 710f);

        // Khung trái: 3 Viên Xúc Xắc
        GameObject bagBox = CreateCardFrame(
            bagAndSkillsTabView.transform,
            "PlayerBagBox",
            new Vector2(-335f, 0f),
            new Vector2(660f, 690f),
            new Color(0.09f, 0.08f, 0.15f, 0.96f),
            new Color(0.85f, 0.68f, 0.25f, 0.85f));

        bagSummaryText = CreateTMP(
            bagBox.transform,
            "BagSummaryText",
            "",
            Vector2.zero,
            new Vector2(616f, 646f),
            15.5f,
            new Color(0.92f, 0.95f, 1f, 1f),
            TextAlignmentOptions.TopLeft);

        // Khung phải: 2 Kỹ Năng Chủ Động & Cơ chế Năng Lượng / Thiêu Đốt
        GameObject skillBox = CreateCardFrame(
            bagAndSkillsTabView.transform,
            "PlayerSkillsBox",
            new Vector2(345f, 0f),
            new Vector2(660f, 690f),
            new Color(0.09f, 0.08f, 0.15f, 0.96f),
            new Color(0.35f, 0.78f, 1f, 0.85f));

        skillsSummaryText = CreateTMP(
            skillBox.transform,
            "SkillsSummaryText",
            "",
            Vector2.zero,
            new Vector2(616f, 646f),
            15.5f,
            new Color(0.92f, 0.95f, 1f, 1f),
            TextAlignmentOptions.TopLeft);

        SwitchTab(showCatalog: true);
    }

    private void SwitchTab(bool showCatalog)
    {
        if (catalogTabView != null) catalogTabView.SetActive(showCatalog);
        if (bagAndSkillsTabView != null) bagAndSkillsTabView.SetActive(!showCatalog);

        if (tabCatalogBtn != null)
        {
            Image img = tabCatalogBtn.GetComponent<Image>();
            if (img != null)
            {
                img.color = showCatalog
                    ? new Color(0.55f, 0.36f, 0.08f, 0.98f)
                    : new Color(0.14f, 0.14f, 0.22f, 0.92f);
            }
        }

        if (tabBagBtn != null)
        {
            Image img = tabBagBtn.GetComponent<Image>();
            if (img != null)
            {
                img.color = !showCatalog
                    ? new Color(0.18f, 0.36f, 0.58f, 0.98f)
                    : new Color(0.14f, 0.14f, 0.22f, 0.92f);
            }
        }

        if (!showCatalog)
        {
            RefreshBagAndSkillsContent();
        }
    }

    private void PopulateAll13FacesGrid(Transform gridParent)
    {
        List<DiceFaceData> allFaces = DiceCatalog.GetAllCanonicalFaces();

        for (int i = 0; i < allFaces.Count; i++)
        {
            DiceFaceData face = allFaces[i];
            if (face == null) continue;

            Color cardBg = GetCategoryColor(face.type);
            Color borderCol = GetRarityBorderColor(face.rarity);

            GameObject card = CreateCardFrame(
                gridParent,
                $"CodexFace_{i + 1}_{face.faceName}",
                Vector2.zero,
                new Vector2(326f, 162f),
                cardBg,
                borderCol);

            string symbol = GetSkillEmoji(face.GetResolvedSkillKind());
            string rarityTag = GetRarityColoredTag(face.rarity);
            string typeLabel = GetFaceTypeVietnamese(face.type);

            TextMeshProUGUI header = CreateTMP(
                card.transform,
                "CardHeader",
                $"<b>#{i + 1}. {symbol} {face.faceName.ToUpper()}</b>   {rarityTag}\n<size=12><color=#cbd5e1>Nhóm: {typeLabel}  •  Chỉ số V = {face.valueV}</color></size>",
                new Vector2(0f, 44f),
                new Vector2(302f, 52f),
                14.5f,
                new Color(1f, 0.94f, 0.78f, 1f),
                TextAlignmentOptions.TopLeft);

            string cleanRule = face.GetTooltipText();
            int nl = cleanRule.IndexOf('\n');
            if (nl >= 0 && nl + 1 < cleanRule.Length) cleanRule = cleanRule.Substring(nl + 1);

            CreateTMP(
                card.transform,
                "CardRule",
                cleanRule,
                new Vector2(0f, -26f),
                new Vector2(302f, 82f),
                13.5f,
                new Color(0.90f, 0.94f, 0.98f, 1f),
                TextAlignmentOptions.TopLeft);
        }
    }

    public void RefreshBagAndSkillsContent()
    {
        GameManager gm = GameManager.ResolveInstance();
        List<DiceInstance> bag = (gm != null && gm.playerDiceBag != null && gm.playerDiceBag.Count > 0)
            ? gm.playerDiceBag
            : DiceCatalog.CreateStarterDiceBag();

        if (bagSummaryText != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("<size=20><color=#fde047><b>🎒 TÚI XÚC XẮC LỤC DIỆN ĐANG TRANG BỊ (3 VIÊN × 6 MẶT)</b></color></size>\n");

            for (int d = 0; d < bag.Count; d++)
            {
                DiceInstance die = bag[d];
                if (die == null) continue;
                sb.AppendLine($"<color=#38bdf8><b>■ VIÊN #{d + 1}: {die.diceName.ToUpper()}</b></color>");
                if (die.faces != null)
                {
                    for (int f = 0; f < die.faces.Length; f++)
                    {
                        DiceFaceData face = die.faces[f];
                        if (face == null) continue;
                        string emoji = GetSkillEmoji(face.GetResolvedSkillKind());
                        sb.AppendLine($"   • Ô #{f + 1}: <b>{emoji} {face.faceName}</b> (V={face.valueV}, {face.rarity})");
                    }
                }
                sb.AppendLine();
            }
            bagSummaryText.text = sb.ToString();
        }

        if (skillsSummaryText != null)
        {
            int normLvl = gm != null ? gm.normalSkillLevel : 1;
            int normCost = gm != null ? gm.normalSkillBaseCost : 2;
            int normDmg = gm != null ? gm.GetNormalSkillDamage() : 5;

            int ultLvl = gm != null ? gm.ultimateSkillLevel : 1;
            int ultCost = gm != null ? gm.ultimateSkillBaseCost : 6;
            int ultDmg = gm != null ? gm.GetUltimateSkillDamage() : 14;
            int ultBurn = gm != null ? gm.GetUltimateSkillBurnStacks() : 4;

            int curHP = gm != null ? gm.playerCurrentHP : 70;
            int maxHP = gm != null ? gm.playerMaxHP : 70;

            skillsSummaryText.text =
                "<size=20><color=#fde047><b>⚡ HỆ THỐNG KỸ NĂNG NHÂN VẬT & NĂNG LƯỢNG</b></color></size>\n\n" +
                $"• <b>Máu Hiệp Sĩ hiện tại:</b> <color=#4ade80>{curHP} / {maxHP} HP</color>\n" +
                "• <b>Thanh Năng Lượng (Energy):</b> Khởi đầu mỗi trận ở mức <b>0 / 10 🔷</b>.\n" +
                "  Sử dụng các mặt xúc xắc <i>Mana Spark (+2🔷)</i>, <i>Arcane Surge (+3🔷, +2🛡)</i> hoặc <i>Overcharge (+4🔷, giảm 1🔷 chi phí)</i> để nạp đầy thanh kỹ năng.\n\n" +
                $"<color=#38bdf8><b>1. ⚔ KỸ NĂNG THƯỜNG — CẤP {normLvl} (Phím tắt [1])</b></color>\n" +
                $"   • Chi phí: <b>{normCost} Năng lượng (🔷)</b>\n" +
                $"   • Hiệu ứng: Gây ngay <b>{normDmg} sát thương đơn vị</b> lên mục tiêu.\n" +
                "   • Nâng cấp tại Xưởng Rèn: <b>+3 Sát thương</b> mỗi cấp.\n\n" +
                $"<color=#fb923c><b>2. 🔥 KỸ NĂNG TỐI THƯỢNG (ULTIMATE) — CẤP {ultLvl} (Phím tắt [2])</b></color>\n" +
                $"   • Chi phí: <b>{ultCost} Năng lượng (🔷)</b>\n" +
                $"   • Hiệu ứng: Giáng đòn hủy diệt gây <b>{ultDmg} sát thương lớn</b> và áp <b>+{ultBurn} tầng Thiêu đốt (🔥 Burn)</b> lên đối thủ!\n" +
                "   • Nâng cấp tại Xưởng Rèn: <b>+4 Sát thương & +2 tầng Thiêu đốt</b> mỗi cấp.\n\n" +
                "<color=#facc15><b>🔥 CƠ CHẾ HIỆU ỨNG THIÊU ĐỐT (BURN):</b></color>\n" +
                "   • Vào cuối mỗi hiệp (RoundEnd), mục tiêu dính Thiêu đốt chịu <b>sát thương chuẩn bỏ qua Giáp</b> bằng đúng số tầng Thiêu đốt hiện có, sau đó giảm đi 1 tầng.";
        }
    }

    private string GetSkillEmoji(DiceSkillKind kind)
    {
        switch (kind)
        {
            case DiceSkillKind.Strike: return "⚔";
            case DiceSkillKind.HeavySlash: return "💥";
            case DiceSkillKind.Vampiric: return "🩸";
            case DiceSkillKind.Block: return "🛡";
            case DiceSkillKind.IronWall: return "🏰";
            case DiceSkillKind.Regenerate: return "💚";
            case DiceSkillKind.Purify: return "✨";
            case DiceSkillKind.PoisonSting: return "☠";
            case DiceSkillKind.ShockStun: return "⚡";
            case DiceSkillKind.ManaSpark:
            case DiceSkillKind.ArcaneSurge:
            case DiceSkillKind.Overcharge: return "🔷";
            default: return "💨";
        }
    }

    private string GetFaceTypeVietnamese(DiceFaceType t)
    {
        switch (t)
        {
            case DiceFaceType.Attack: return "Tấn Công";
            case DiceFaceType.Defend: return "Phòng Thủ";
            case DiceFaceType.Heal: return "Hồi Phục";
            case DiceFaceType.Status: return "Hiệu Ứng";
            case DiceFaceType.Energy: return "Năng Lượng";
            default: return "Đặc Biệt";
        }
    }

    private Color GetCategoryColor(DiceFaceType t)
    {
        switch (t)
        {
            case DiceFaceType.Attack: return new Color(0.32f, 0.11f, 0.14f, 0.96f);
            case DiceFaceType.Defend: return new Color(0.11f, 0.22f, 0.38f, 0.96f);
            case DiceFaceType.Heal: return new Color(0.11f, 0.30f, 0.20f, 0.96f);
            case DiceFaceType.Status: return new Color(0.26f, 0.13f, 0.36f, 0.96f);
            case DiceFaceType.Energy: return new Color(0.10f, 0.28f, 0.46f, 0.96f);
            default: return new Color(0.18f, 0.18f, 0.22f, 0.94f);
        }
    }

    private Color GetRarityBorderColor(DiceFaceRarity rarity)
    {
        switch (rarity)
        {
            case DiceFaceRarity.Epic: return new Color(0.98f, 0.58f, 0.16f, 0.95f);
            case DiceFaceRarity.Rare: return new Color(0.35f, 0.78f, 1f, 0.95f);
            default: return new Color(0.65f, 0.68f, 0.75f, 0.75f);
        }
    }

    private string GetRarityColoredTag(DiceFaceRarity rarity)
    {
        switch (rarity)
        {
            case DiceFaceRarity.Epic: return "<color=#fb923c>[EPIC]</color>";
            case DiceFaceRarity.Rare: return "<color=#38bdf8>[RARE]</color>";
            default: return "<color=#cbd5e1>[COMMON]</color>";
        }
    }

    private GameObject CreateCardFrame(Transform parent, string name, Vector2 pos, Vector2 size, Color bgCol, Color borderCol)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        go.GetComponent<Image>().color = bgCol;
        Outline ol = go.GetComponent<Outline>();
        ol.effectColor = borderCol;
        ol.effectDistance = new Vector2(1.6f, -1.6f);
        return go;
    }

    private Button CreateStyledButton(Transform parent, string name, string labelText, Vector2 pos, Vector2 size, Color bgCol, Color borderCol, float fontSize)
    {
        GameObject go = CreateCardFrame(parent, name, pos, size, bgCol, borderCol);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();

        TextMeshProUGUI tmp = CreateTMP(go.transform, "Label", labelText, Vector2.zero, size - new Vector2(10f, 4f), fontSize, Color.white, TextAlignmentOptions.Center);
        tmp.fontStyle = FontStyles.Bold;
        return btn;
    }

    private TextMeshProUGUI CreateTMP(Transform parent, string name, string content, Vector2 pos, Vector2 size, float fontSize, Color col, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = col;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }
}
