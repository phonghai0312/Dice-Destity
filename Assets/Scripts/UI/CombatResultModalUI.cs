using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// CombatResultModalUI — Hiển thị bảng tổng kết trận đấu (Chiến thắng Ải / Chinh phục Trùm cuối / Thất bại Permadeath)
/// theo đúng quy chuẩn Roguelike trong tài liệu thiết kế.
/// </summary>
public class CombatResultModalUI : MonoBehaviour
{
    private GameObject modalRoot;
    private TextMeshProUGUI badgeTitleText;
    private TextMeshProUGUI mainHeaderText;
    private TextMeshProUGUI subDescText;
    private TextMeshProUGUI statsBoxText;
    private Button primaryActionBtn;
    private TextMeshProUGUI primaryActionText;
    private Button secondaryActionBtn;
    private TextMeshProUGUI secondaryActionText;
    private Outline dialogOutline;

    public static CombatResultModalUI ShowOrCreate(Transform parentCanvas)
    {
        if (parentCanvas == null) return null;

        CombatResultModalUI existing = parentCanvas.GetComponentInChildren<CombatResultModalUI>(true);
        if (existing != null)
        {
            return existing;
        }

        GameObject go = new GameObject("CombatResultModalUI", typeof(RectTransform));
        go.transform.SetParent(parentCanvas, false);
        go.transform.SetAsLastSibling();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        CombatResultModalUI ui = go.AddComponent<CombatResultModalUI>();
        ui.BuildModalHierarchy();
        return ui;
    }

    private void BuildModalHierarchy()
    {
        modalRoot = gameObject;

        Image backdrop = modalRoot.AddComponent<Image>();
        backdrop.color = new Color(0.02f, 0.02f, 0.05f, 0.84f);

        GameObject dialog = new GameObject("ResultDialogBox", typeof(RectTransform), typeof(Image), typeof(Outline));
        dialog.transform.SetParent(modalRoot.transform, false);
        RectTransform dRt = dialog.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0.5f, 0.5f);
        dRt.anchorMax = new Vector2(0.5f, 0.5f);
        dRt.sizeDelta = new Vector2(720f, 430f);
        dRt.anchoredPosition = Vector2.zero;

        Image dImg = dialog.GetComponent<Image>();
        dImg.color = new Color(0.08f, 0.07f, 0.13f, 0.98f);

        dialogOutline = dialog.GetComponent<Outline>();
        dialogOutline.effectColor = new Color(0.95f, 0.78f, 0.25f, 0.95f);
        dialogOutline.effectDistance = new Vector2(2.5f, -2.5f);

        // Badge trên đỉnh
        GameObject badgeGo = new GameObject("TopBadge", typeof(RectTransform), typeof(Image), typeof(Outline));
        badgeGo.transform.SetParent(dialog.transform, false);
        RectTransform bRt = badgeGo.GetComponent<RectTransform>();
        bRt.anchoredPosition = new Vector2(0f, 168f);
        bRt.sizeDelta = new Vector2(360f, 34f);
        badgeGo.GetComponent<Image>().color = new Color(0.14f, 0.12f, 0.22f, 0.96f);
        Outline bOl = badgeGo.GetComponent<Outline>();
        bOl.effectColor = new Color(0.85f, 0.68f, 0.25f, 0.8f);
        bOl.effectDistance = new Vector2(1.2f, -1.2f);

        badgeTitleText = CreateTMP(badgeGo.transform, "BadgeLabel", Vector2.zero, new Vector2(340f, 30f), 14f, new Color(1f, 0.88f, 0.45f, 1f));

        // Tiêu đề lớn
        mainHeaderText = CreateTMP(dialog.transform, "MainHeader", new Vector2(0f, 114f), new Vector2(660f, 48f), 32f, new Color(1f, 0.86f, 0.28f, 1f));
        mainHeaderText.fontStyle = FontStyles.Bold;

        // Mô tả
        subDescText = CreateTMP(dialog.transform, "SubDesc", new Vector2(0f, 52f), new Vector2(640f, 58f), 16.5f, new Color(0.88f, 0.92f, 0.96f, 1f));

        // Hộp thống kê chiến trận
        GameObject statsBox = new GameObject("StatsSummaryBox", typeof(RectTransform), typeof(Image), typeof(Outline));
        statsBox.transform.SetParent(dialog.transform, false);
        RectTransform sbRt = statsBox.GetComponent<RectTransform>();
        sbRt.anchoredPosition = new Vector2(0f, -34f);
        sbRt.sizeDelta = new Vector2(620f, 96f);
        statsBox.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.09f, 0.95f);
        Outline sbOl = statsBox.GetComponent<Outline>();
        sbOl.effectColor = new Color(0.45f, 0.38f, 0.62f, 0.7f);
        sbOl.effectDistance = new Vector2(1.2f, -1.2f);

        statsBoxText = CreateTMP(statsBox.transform, "StatsText", Vector2.zero, new Vector2(590f, 84f), 15.5f, new Color(0.92f, 0.95f, 1f, 1f));

        // Nút hành động chính (Bên trái / Giữa)
        primaryActionBtn = CreateActionButton(
            dialog.transform,
            "PrimaryButton",
            new Vector2(-160f, -142f),
            new Vector2(290f, 54f),
            new Color(0.14f, 0.38f, 0.24f, 0.98f),
            new Color(0.35f, 0.92f, 0.58f, 0.95f),
            out primaryActionText);

        // Nút hành động phụ (Bên phải)
        secondaryActionBtn = CreateActionButton(
            dialog.transform,
            "SecondaryButton",
            new Vector2(160f, -142f),
            new Vector2(290f, 54f),
            new Color(0.48f, 0.28f, 0.06f, 0.98f),
            new Color(1f, 0.82f, 0.25f, 0.95f),
            out secondaryActionText);

        modalRoot.SetActive(false);
    }

    public void ShowVictory(int clearedStage, int totalRounds, int remainingHP, int maxHP, string enemyName)
    {
        transform.SetAsLastSibling();
        modalRoot.SetActive(true);

        GameManager gm = GameManager.ResolveInstance();
        int totalStages = gm != null ? gm.GetTotalStages() : 3;
        bool isFinalBossVictory = clearedStage >= totalStages;

        if (dialogOutline != null)
        {
            dialogOutline.effectColor = new Color(1f, 0.84f, 0.25f, 1f);
        }

        if (isFinalBossVictory)
        {
            badgeTitleText.text = "👑 CHINH PHỤC HẦM NGỤC • HOÀN TẤT HÀNH TRÌNH";
            mainHeaderText.text = "🏆 CHIẾN THẮNG HUYỀN THOẠI!";
            mainHeaderText.color = new Color(1f, 0.88f, 0.28f, 1f);
            subDescText.text = $"Hiệp Sĩ đã tiêu diệt trùm cuối <b>{enemyName}</b> và phá giải lời nguyền của toàn bộ {totalStages} Ải!";
        }
        else
        {
            badgeTitleText.text = $"⚔ VƯỢT QUA ẢI {clearedStage} / {totalStages} • NHẬN +1 LƯỢT RÈN";
            mainHeaderText.text = "🎉 CHIẾN THẮNG TRẬN ĐẤU!";
            mainHeaderText.color = new Color(0.35f, 0.96f, 0.62f, 1f);
            subDescText.text = $"Kẻ địch <b>{enemyName}</b> đã bị hạ gục! Bạn nhận được <b>+1 Lượt Rèn</b> để khắc mặt xúc xắc mới hoặc nâng cấp Kỹ năng.";
        }

        int normLvl = gm != null ? gm.normalSkillLevel : 1;
        int ultLvl = gm != null ? gm.ultimateSkillLevel : 1;

        statsBoxText.text =
            $"♥ Máu Hiệp Sĩ bảo toàn: <b><color=#4ade80>{remainingHP} / {maxHP} HP</color></b>   •   ⏱ Số hiệp chiến đấu: <b>{totalRounds} Hiệp</b>\n" +
            $"⚔ Kỹ năng Thường: <b>Lv.{normLvl}</b>   •   🔥 Kỹ năng Tối thượng: <b>Lv.{ultLvl}</b>   •   🔨 Phần thưởng: <b>+1 Lượt Rèn Xúc Xắc / Kỹ Năng</b>";

        primaryActionBtn.onClick.RemoveAllListeners();
        primaryActionText.text = "🗺 VỀ BẢN ĐỒ HÀNH TRÌNH";
        primaryActionBtn.onClick.AddListener(() =>
        {
            if (gm != null) gm.LoadStageMapScene();
            else SceneManager.LoadScene("StageMap");
        });

        secondaryActionBtn.onClick.RemoveAllListeners();
        secondaryActionText.text = "🔨 VÀO XƯỞNG RÈN NGAY";
        secondaryActionBtn.onClick.AddListener(() =>
        {
            if (gm != null) gm.LoadForgeScene();
            else SceneManager.LoadScene("TheForge");
        });
    }

    public void ShowDefeat(int failedStage, int totalRounds, string enemyName)
    {
        transform.SetAsLastSibling();
        modalRoot.SetActive(true);

        if (dialogOutline != null)
        {
            dialogOutline.effectColor = new Color(0.95f, 0.25f, 0.25f, 1f);
        }

        badgeTitleText.text = $"💀 THẤT BẠI TẠI ẢI {failedStage} • QUY TẮC PERMADEATH";
        mainHeaderText.text = "⚔ HIỆP SĨ ĐÃ NGÃ XUỐNG";
        mainHeaderText.color = new Color(0.98f, 0.35f, 0.35f, 1f);
        subDescText.text = $"Hiệp Sĩ đã gục ngã trước <b>{enemyName}</b>. Theo luật <b>Permadeath</b> của dòng game Roguelike, hành trình này đã khép lại.";

        statsBoxText.text =
            $"📍 Dừng chân tại: <b>Ải {failedStage}/3 ({enemyName})</b>   •   ⏱ Số hiệp cầm cự: <b>{totalRounds} Hiệp</b>\n" +
            "💡 <i>Mẹo: Tích lũy các mặt Năng lượng (🔷) và Giáp ảo (🛡) để tung Kỹ năng Tối thượng gây Thiêu đốt xuyên giáp!</i>";

        primaryActionBtn.onClick.RemoveAllListeners();
        primaryActionText.text = "🔄 THỬ LẠI TỪ ẢI 1 (NEW RUN)";
        primaryActionBtn.onClick.AddListener(() =>
        {
            GameManager gm = GameManager.ResolveInstance();
            if (gm != null) gm.StartNewRun();
            else SceneManager.LoadScene("CombatScene");
        });

        secondaryActionBtn.onClick.RemoveAllListeners();
        secondaryActionText.text = "🏠 VỀ MENU CHÍNH";
        secondaryActionBtn.onClick.AddListener(() =>
        {
            GameManager gm = GameManager.ResolveInstance();
            if (gm != null) gm.LoadMainMenu();
            else SceneManager.LoadScene("MainMenu");
        });
    }

    private TextMeshProUGUI CreateTMP(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    private Button CreateActionButton(
        Transform parent,
        string name,
        Vector2 pos,
        Vector2 size,
        Color bgColor,
        Color outlineColor,
        out TextMeshProUGUI labelTmp)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        img.color = bgColor;

        Outline ol = go.GetComponent<Outline>();
        ol.effectColor = outlineColor;
        ol.effectDistance = new Vector2(1.8f, -1.8f);

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        labelTmp = CreateTMP(go.transform, "Label", Vector2.zero, size - new Vector2(12f, 6f), 15.5f, Color.white);
        labelTmp.fontStyle = FontStyles.Bold;
        return btn;
    }
}

