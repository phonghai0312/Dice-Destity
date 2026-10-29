using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// CombatResultModalUI — Hiển thị bảng tổng kết trận đấu (Chiến thắng Ải / Chinh phục Trùm cuối / Thất bại Permadeath)
/// - Thưởng 1 mặt xúc xắc mới (chọn 1 trong 3) khi vượt ải
/// - Tự động mở khóa 1 ô mặt trống mới trên xúc xắc
/// - Cho phép chuyển đến Lò Rèn để rèn đè hoặc quay về Bản đồ
/// </summary>
public class CombatResultModalUI : MonoBehaviour
{
    private GameObject modalRoot;
    private RectTransform dialogRt;
    private TextMeshProUGUI badgeTitleText;
    private TextMeshProUGUI mainHeaderText;
    private TextMeshProUGUI subDescText;
    private TextMeshProUGUI unlockBannerText;
    private GameObject unlockBannerGo;
    private GameObject rewardCardsContainer;
    private TextMeshProUGUI statsBoxText;
    private GameObject statsBoxGo;
    private Button primaryActionBtn;
    private TextMeshProUGUI primaryActionText;
    private Button secondaryActionBtn;
    private TextMeshProUGUI secondaryActionText;
    private Outline dialogOutline;

    private readonly List<GameObject> spawnedRewardCardGos = new List<GameObject>();
    private bool hasPickedReward = false;
    private int selectedRewardIdx = -1;

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
        backdrop.color = new Color(0.02f, 0.02f, 0.05f, 0.88f);

        GameObject dialog = new GameObject("ResultDialogBox", typeof(RectTransform), typeof(Image), typeof(Outline));
        dialog.transform.SetParent(modalRoot.transform, false);
        dialogRt = dialog.GetComponent<RectTransform>();
        dialogRt.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRt.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRt.sizeDelta = new Vector2(860f, 620f);
        dialogRt.anchoredPosition = Vector2.zero;

        Image dImg = dialog.GetComponent<Image>();
        dImg.color = new Color(0.08f, 0.07f, 0.13f, 0.98f);

        dialogOutline = dialog.GetComponent<Outline>();
        dialogOutline.effectColor = new Color(0.95f, 0.78f, 0.25f, 0.95f);
        dialogOutline.effectDistance = new Vector2(2.5f, -2.5f);

        // Badge trên đỉnh
        GameObject badgeGo = new GameObject("TopBadge", typeof(RectTransform), typeof(Image), typeof(Outline));
        badgeGo.transform.SetParent(dialog.transform, false);
        RectTransform bRt = badgeGo.GetComponent<RectTransform>();
        bRt.anchoredPosition = new Vector2(0f, 275f);
        bRt.sizeDelta = new Vector2(460f, 34f);
        badgeGo.GetComponent<Image>().color = new Color(0.14f, 0.12f, 0.22f, 0.96f);
        Outline bOl = badgeGo.GetComponent<Outline>();
        bOl.effectColor = new Color(0.85f, 0.68f, 0.25f, 0.8f);
        bOl.effectDistance = new Vector2(1.2f, -1.2f);

        badgeTitleText = CreateTMP(badgeGo.transform, "BadgeLabel", Vector2.zero, new Vector2(440f, 30f), 14f, new Color(1f, 0.88f, 0.45f, 1f));

        // Tiêu đề lớn
        mainHeaderText = CreateTMP(dialog.transform, "MainHeader", new Vector2(0f, 226f), new Vector2(760f, 44f), 30f, new Color(1f, 0.86f, 0.28f, 1f));
        mainHeaderText.fontStyle = FontStyles.Bold;

        // Mô tả kẻ địch bị hạ
        subDescText = CreateTMP(dialog.transform, "SubDesc", new Vector2(0f, 180f), new Vector2(740f, 40f), 15.5f, new Color(0.88f, 0.92f, 0.96f, 1f));

        // Banner thông báo mở khóa ô mặt mới
        unlockBannerGo = new GameObject("UnlockBanner", typeof(RectTransform), typeof(Image), typeof(Outline));
        unlockBannerGo.transform.SetParent(dialog.transform, false);
        RectTransform ubRt = unlockBannerGo.GetComponent<RectTransform>();
        ubRt.anchoredPosition = new Vector2(0f, 134f);
        ubRt.sizeDelta = new Vector2(760f, 36f);
        unlockBannerGo.GetComponent<Image>().color = new Color(0.12f, 0.26f, 0.18f, 0.95f);
        Outline ubOl = unlockBannerGo.GetComponent<Outline>();
        ubOl.effectColor = new Color(0.35f, 0.95f, 0.55f, 0.85f);
        ubOl.effectDistance = new Vector2(1.2f, -1.2f);
        unlockBannerText = CreateTMP(unlockBannerGo.transform, "UnlockLabel", Vector2.zero, new Vector2(740f, 32f), 14.5f, new Color(0.45f, 1f, 0.65f, 1f));
        unlockBannerText.fontStyle = FontStyles.Bold;

        // Container 3 thẻ phần thưởng (Chọn 1 trong 3)
        rewardCardsContainer = new GameObject("RewardCardsContainer", typeof(RectTransform));
        rewardCardsContainer.transform.SetParent(dialog.transform, false);
        RectTransform rcRt = rewardCardsContainer.GetComponent<RectTransform>();
        rcRt.anchoredPosition = new Vector2(0f, 2f);
        rcRt.sizeDelta = new Vector2(780f, 200f);

        // Hộp thống kê chiến trận (dùng khi Đánh bại hoặc Thất bại)
        statsBoxGo = new GameObject("StatsSummaryBox", typeof(RectTransform), typeof(Image), typeof(Outline));
        statsBoxGo.transform.SetParent(dialog.transform, false);
        RectTransform sbRt = statsBoxGo.GetComponent<RectTransform>();
        sbRt.anchoredPosition = new Vector2(0f, -145f);
        sbRt.sizeDelta = new Vector2(760f, 58f);
        statsBoxGo.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.09f, 0.95f);
        Outline sbOl = statsBoxGo.GetComponent<Outline>();
        sbOl.effectColor = new Color(0.45f, 0.38f, 0.62f, 0.7f);
        sbOl.effectDistance = new Vector2(1.2f, -1.2f);
        statsBoxText = CreateTMP(statsBoxGo.transform, "StatsText", Vector2.zero, new Vector2(740f, 50f), 14f, new Color(0.92f, 0.95f, 1f, 1f));

        // Nút hành động chính (Bên trái)
        primaryActionBtn = CreateActionButton(
            dialog.transform,
            "PrimaryButton",
            new Vector2(-190f, -240f),
            new Vector2(340f, 52f),
            new Color(0.14f, 0.38f, 0.24f, 0.98f),
            new Color(0.35f, 0.92f, 0.58f, 0.95f),
            out primaryActionText);

        // Nút hành động phụ (Bên phải)
        secondaryActionBtn = CreateActionButton(
            dialog.transform,
            "SecondaryButton",
            new Vector2(190f, -240f),
            new Vector2(340f, 52f),
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

        hasPickedReward = false;
        selectedRewardIdx = -1;

        if (isFinalBossVictory)
        {
            if (dialogRt != null) dialogRt.sizeDelta = new Vector2(760f, 480f);
            if (unlockBannerGo != null) unlockBannerGo.SetActive(false);
            if (rewardCardsContainer != null) rewardCardsContainer.SetActive(false);
            if (statsBoxGo != null)
            {
                statsBoxGo.SetActive(true);
                RectTransform sbRt = statsBoxGo.GetComponent<RectTransform>();
                if (sbRt != null) sbRt.anchoredPosition = new Vector2(0f, -20f);
            }

            badgeTitleText.text = "CHINH PHỤC HẦM NGỤC • HOÀN TẤT HÀNH TRÌNH";
            mainHeaderText.text = "CHIẾN THẮNG HUYỀN THOẠI!";
            mainHeaderText.color = new Color(1f, 0.88f, 0.28f, 1f);
            subDescText.text = $"Hiệp Sĩ đã tiêu diệt trùm cuối <b>{enemyName}</b> và phá giải lời nguyền của toàn bộ {totalStages} Ải!";

            int normLvl = gm != null ? gm.normalSkillLevel : 1;
            int ultLvl = gm != null ? gm.ultimateSkillLevel : 1;
            statsBoxText.text =
                $"Máu Hiệp Sĩ: <b><color=#4ade80>{remainingHP} / {maxHP} HP</color></b>   •   Số hiệp chiến đấu: <b>{totalRounds} Hiệp</b>\n" +
                $"Kỹ năng Thường: <b>Lv.{normLvl}</b>   •   Kỹ năng Tối thượng: <b>Lv.{ultLvl}</b>";

            primaryActionBtn.interactable = true;
            primaryActionBtn.onClick.RemoveAllListeners();
            primaryActionText.text = "CHƠI LẠI TỪ ĐẦU (NEW RUN)";
            primaryActionBtn.onClick.AddListener(() =>
            {
                if (gm != null) gm.StartNewRun();
                else SceneManager.LoadScene("CombatScene");
            });

            secondaryActionBtn.interactable = true;
            secondaryActionBtn.onClick.RemoveAllListeners();
            secondaryActionText.text = "VỀ MENU CHÍNH";
            secondaryActionBtn.onClick.AddListener(() =>
            {
                if (gm != null) gm.LoadMainMenu();
                else SceneManager.LoadScene("MainMenu");
            });
        }
        else
        {
            // Vượt ải thường (Ải 1 hoặc Ải 2)
            if (dialogRt != null) dialogRt.sizeDelta = new Vector2(860f, 620f);

            // 1. Tự động mở khóa 1 ô mặt xúc xắc mới
            int unlockedDie = -1;
            int unlockedSlot = -1;
            if (gm != null)
            {
                gm.UnlockNextLockedSlot(out unlockedDie, out unlockedSlot);
            }

            if (unlockBannerGo != null)
            {
                unlockBannerGo.SetActive(true);
                if (unlockedDie >= 0 && gm != null && unlockedDie < gm.playerDiceBag.Count)
                {
                    string dieName = gm.playerDiceBag[unlockedDie].diceName;
                    unlockBannerText.text = $"[MỞ KHÓA] <b>ĐÃ MỞ KHÓA Ô MẶT MỚI:</b> {dieName} • Ô #{unlockedSlot + 1} đã sẵn sàng để dán mặt!";
                }
                else
                {
                    unlockBannerText.text = "[HOÀN TẤT] Tất cả các ô xúc xắc đều đã mở khóa hoàn toàn!";
                }
            }

            badgeTitleText.text = $"VƯỢT QUA ẢI {clearedStage} / {totalStages} • PHẦN THƯỞNG CHIẾN THẮNG";
            mainHeaderText.text = "🎉 CHIẾN THẮNG TRẬN ĐẤU!";
            mainHeaderText.color = new Color(0.35f, 0.96f, 0.62f, 1f);
            subDescText.text = $"Kẻ địch <b>{enemyName}</b> đã bị hạ! <b>Hãy chọn 1 trong 3 Mặt Xúc Xắc</b> dưới đây để thêm vào Túi dự trữ:";

            if (statsBoxGo != null)
            {
                statsBoxGo.SetActive(true);
                RectTransform sbRt = statsBoxGo.GetComponent<RectTransform>();
                if (sbRt != null) sbRt.anchoredPosition = new Vector2(0f, -145f);
                statsBoxText.text = $"Máu: <b><color=#4ade80>{remainingHP}/{maxHP}</color></b> • Hiệp: <b>{totalRounds}</b> • Mẹo: Vào <b>Lò Rèn</b> để rèn đè hoặc ghép đôi cường hóa mặt!";
            }

            // 2. Sinh 3 phần thưởng ngẫu nhiên theo bậc ải
            if (rewardCardsContainer != null)
            {
                rewardCardsContainer.SetActive(true);
                BuildRewardCardsUI(clearedStage, gm);
            }

            // 3. Nút điều hướng (khóa cho tới khi chọn phần thưởng)
            primaryActionBtn.interactable = false;
            primaryActionBtn.onClick.RemoveAllListeners();
            primaryActionText.text = "VỀ BẢN ĐỒ HÀNH TRÌNH";
            primaryActionBtn.onClick.AddListener(() =>
            {
                if (gm != null) gm.LoadStageMapScene();
                else SceneManager.LoadScene("StageMap");
            });

            secondaryActionBtn.interactable = false;
            secondaryActionBtn.onClick.RemoveAllListeners();
            secondaryActionText.text = "VÀO LÒ RÈN ĐỂ DÁN MẶT";
            secondaryActionBtn.onClick.AddListener(() =>
            {
                if (gm != null) gm.LoadForgeScene();
                else SceneManager.LoadScene("TheForge");
            });

            SetActionButtonsHighlighted(false);
        }
    }

    private void BuildRewardCardsUI(int clearedStage, GameManager gm)
    {
        // Xóa các card cũ
        foreach (GameObject c in spawnedRewardCardGos)
        {
            if (c != null) Destroy(c);
        }
        spawnedRewardCardGos.Clear();

        List<DiceFaceData> options = DiceCatalog.RollStageRewardOptions(clearedStage, 3);
        if (options == null || options.Count == 0) return;

        float[] xPositions = new float[] { -255f, 0f, 255f };

        for (int i = 0; i < options.Count && i < 3; i++)
        {
            int cardIndex = i;
            DiceFaceData face = options[i];

            GameObject cardGo = new GameObject($"RewardCard_{i}", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
            cardGo.transform.SetParent(rewardCardsContainer.transform, false);
            spawnedRewardCardGos.Add(cardGo);

            RectTransform cRt = cardGo.GetComponent<RectTransform>();
            cRt.anchoredPosition = new Vector2(xPositions[i], 0f);
            cRt.sizeDelta = new Vector2(235f, 185f);

            Color rarityBorderColor = GetRarityColor(face.rarity);
            Image cImg = cardGo.GetComponent<Image>();
            cImg.color = new Color(0.11f, 0.10f, 0.18f, 0.98f);

            Outline cOl = cardGo.GetComponent<Outline>();
            cOl.effectColor = rarityBorderColor;
            cOl.effectDistance = new Vector2(2f, -2f);

            // Level & Rarity Badge
            GameObject topBadge = new GameObject("TopBadge", typeof(RectTransform), typeof(Image));
            topBadge.transform.SetParent(cardGo.transform, false);
            RectTransform tbRt = topBadge.GetComponent<RectTransform>();
            tbRt.anchoredPosition = new Vector2(0f, 72f);
            tbRt.sizeDelta = new Vector2(215f, 24f);
            topBadge.GetComponent<Image>().color = new Color(0.18f, 0.16f, 0.28f, 0.9f);
            string rarityName = face.rarity == DiceFaceRarity.Epic ? "SỬ THI" : (face.rarity == DiceFaceRarity.Rare ? "HIẾM" : "THƯỜNG");
            TextMeshProUGUI tbText = CreateTMP(topBadge.transform, "BadgeText", Vector2.zero, new Vector2(205f, 22f), 12f, rarityBorderColor);
            tbText.text = $"{rarityName} • CẤP {face.level}";
            tbText.fontStyle = FontStyles.Bold;

            // Face Name
            TextMeshProUGUI nameTmp = CreateTMP(cardGo.transform, "Name", new Vector2(0f, 40f), new Vector2(215f, 28f), 16f, Color.white);
            nameTmp.text = face.faceName;
            nameTmp.fontStyle = FontStyles.Bold;

            // Stat Display (e.g., ⚔ +5 ST)
            string statString = GetStatDisplayString(face);
            Color statColor = GetFaceTypeColor(face.type);
            TextMeshProUGUI statTmp = CreateTMP(cardGo.transform, "Stat", new Vector2(0f, 10f), new Vector2(215f, 26f), 17f, statColor);
            statTmp.text = statString;
            statTmp.fontStyle = FontStyles.Bold;

            // Description / Tooltip
            TextMeshProUGUI descTmp = CreateTMP(cardGo.transform, "Desc", new Vector2(0f, -26f), new Vector2(215f, 36f), 12f, new Color(0.82f, 0.85f, 0.90f, 1f));
            descTmp.text = GetSkillDescription(face.skillKind, face.valueV);

            // Button label at bottom of card
            GameObject btnLabelGo = new GameObject("BtnLabel", typeof(RectTransform), typeof(Image));
            btnLabelGo.transform.SetParent(cardGo.transform, false);
            RectTransform blRt = btnLabelGo.GetComponent<RectTransform>();
            blRt.anchoredPosition = new Vector2(0f, -66f);
            blRt.sizeDelta = new Vector2(200f, 28f);
            Image blImg = btnLabelGo.GetComponent<Image>();
            blImg.color = new Color(0.18f, 0.35f, 0.22f, 0.95f);

            TextMeshProUGUI blText = CreateTMP(btnLabelGo.transform, "Text", Vector2.zero, new Vector2(190f, 24f), 13f, Color.white);
            blText.text = "CHỌN MẶT NÀY";
            blText.fontStyle = FontStyles.Bold;

            // Click Handler
            Button cardBtn = cardGo.GetComponent<Button>();
            cardBtn.targetGraphic = cImg;
            cardBtn.onClick.AddListener(() =>
            {
                OnRewardCardSelected(cardIndex, face, gm);
            });
        }
    }

    private void OnRewardCardSelected(int cardIndex, DiceFaceData face, GameManager gm)
    {
        if (hasPickedReward) return;
        hasPickedReward = true;
        selectedRewardIdx = cardIndex;

        // Thêm mặt vào Pool dự trữ
        if (gm != null)
        {
            gm.AddFaceToPool(face.Clone());
        }

        // Cập nhật giao diện 3 thẻ
        for (int i = 0; i < spawnedRewardCardGos.Count; i++)
        {
            GameObject cGo = spawnedRewardCardGos[i];
            if (cGo == null) continue;

            Outline cOl = cGo.GetComponent<Outline>();
            Button cBtn = cGo.GetComponent<Button>();
            Image cImg = cGo.GetComponent<Image>();
            Transform btnLabel = cGo.transform.Find("BtnLabel");

            if (i == cardIndex)
            {
                // Thẻ đã chọn: Viền vàng sáng, nền đậm, badge check
                if (cOl != null)
                {
                    cOl.effectColor = new Color(1f, 0.88f, 0.25f, 1f);
                    cOl.effectDistance = new Vector2(3f, -3f);
                }
                if (cImg != null) cImg.color = new Color(0.18f, 0.16f, 0.08f, 1f);
                if (btnLabel != null)
                {
                    btnLabel.GetComponent<Image>().color = new Color(0.16f, 0.58f, 0.28f, 1f);
                    TextMeshProUGUI t = btnLabel.GetComponentInChildren<TextMeshProUGUI>();
                    if (t != null) t.text = "[ ĐÃ CHỌN THÀNH CÔNG ]";
                }
            }
            else
            {
                // Thẻ còn lại: Làm mờ
                if (cImg != null) cImg.color = new Color(0.06f, 0.06f, 0.09f, 0.65f);
                if (cOl != null) cOl.effectColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);
                if (btnLabel != null)
                {
                    btnLabel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.6f);
                    TextMeshProUGUI t = btnLabel.GetComponentInChildren<TextMeshProUGUI>();
                    if (t != null) { t.text = "BỎ QUA"; t.color = new Color(0.5f, 0.5f, 0.5f, 0.6f); }
                }
            }

            if (cBtn != null) cBtn.interactable = false;
        }

        // Kích hoạt 2 nút hành động
        primaryActionBtn.interactable = true;
        secondaryActionBtn.interactable = true;
        SetActionButtonsHighlighted(true);
    }

    private void SetActionButtonsHighlighted(bool active)
    {
        if (primaryActionBtn != null)
        {
            Image img = primaryActionBtn.GetComponent<Image>();
            if (img != null) img.color = active ? new Color(0.14f, 0.38f, 0.24f, 1f) : new Color(0.14f, 0.14f, 0.14f, 0.5f);
        }

        if (secondaryActionBtn != null)
        {
            Image img = secondaryActionBtn.GetComponent<Image>();
            if (img != null) img.color = active ? new Color(0.52f, 0.32f, 0.08f, 1f) : new Color(0.2f, 0.2f, 0.2f, 0.5f);
        }
    }

    public void ShowDefeat(int failedStage, int totalRounds, string enemyName)
    {
        transform.SetAsLastSibling();
        modalRoot.SetActive(true);

        if (dialogRt != null) dialogRt.sizeDelta = new Vector2(760f, 480f);
        if (unlockBannerGo != null) unlockBannerGo.SetActive(false);
        if (rewardCardsContainer != null) rewardCardsContainer.SetActive(false);
        if (statsBoxGo != null)
        {
            statsBoxGo.SetActive(true);
            RectTransform sbRt = statsBoxGo.GetComponent<RectTransform>();
            if (sbRt != null) sbRt.anchoredPosition = new Vector2(0f, -20f);
        }

        if (dialogOutline != null)
        {
            dialogOutline.effectColor = new Color(0.95f, 0.25f, 0.25f, 1f);
        }

        badgeTitleText.text = $"THẤT BẠI TẠI ẢI {failedStage} • QUY TẮC PERMADEATH";
        mainHeaderText.text = "HIỆP SĨ ĐÃ NGÃ XUỐNG";
        mainHeaderText.color = new Color(0.98f, 0.35f, 0.35f, 1f);
        subDescText.text = $"Hiệp Sĩ đã gục ngã trước <b>{enemyName}</b>. Theo luật <b>Permadeath</b> của dòng game Roguelike, hành trình này đã khép lại.";

        statsBoxText.text =
            $"📍 Dừng chân tại: <b>Ải {failedStage}/3 ({enemyName})</b>   •   ⏱ Số hiệp cầm cự: <b>{totalRounds} Hiệp</b>\n" +
            "<i>Mẹo: Tích lũy các mặt Năng lượng (NL) và Giáp ảo để tung Kỹ năng Tối thượng gây Thiêu đốt xuyên giáp!</i>";

        primaryActionBtn.interactable = true;
        primaryActionBtn.onClick.RemoveAllListeners();
        primaryActionText.text = "THỬ LẠI TỪ ẢI 1 (NEW RUN)";
        primaryActionBtn.onClick.AddListener(() =>
        {
            GameManager gm = GameManager.ResolveInstance();
            if (gm != null) gm.StartNewRun();
            else SceneManager.LoadScene("CombatScene");
        });

        secondaryActionBtn.interactable = true;
        secondaryActionBtn.onClick.RemoveAllListeners();
        secondaryActionText.text = "VỀ MENU CHÍNH";
        secondaryActionBtn.onClick.AddListener(() =>
        {
            GameManager gm = GameManager.ResolveInstance();
            if (gm != null) gm.LoadMainMenu();
            else SceneManager.LoadScene("MainMenu");
        });

        SetActionButtonsHighlighted(true);
    }

    private Color GetRarityColor(DiceFaceRarity rarity)
    {
        switch (rarity)
        {
            case DiceFaceRarity.Epic:      return new Color(0.85f, 0.45f, 1f, 1f);
            
            case DiceFaceRarity.Rare:      return new Color(0.35f, 0.78f, 1f, 1f);
            default:                       return new Color(0.75f, 0.80f, 0.85f, 0.9f);
        }
    }

    private Color GetFaceTypeColor(DiceFaceType type)
    {
        switch (type)
        {
            case DiceFaceType.Attack: return new Color(0.96f, 0.32f, 0.32f, 1f);
            case DiceFaceType.Defend: return new Color(0.35f, 0.65f, 1f, 1f);
            case DiceFaceType.Heal:   return new Color(0.35f, 0.95f, 0.55f, 1f);
            case DiceFaceType.Energy: return new Color(1f, 0.84f, 0.25f, 1f);
            case DiceFaceType.Status: return new Color(0.85f, 0.45f, 1f, 1f);
            default:                  return Color.white;
        }
    }

    private string GetStatDisplayString(DiceFaceData face)
    {
        if (face == null) return "";
        switch (face.skillKind)
        {
            case DiceSkillKind.Strike:
            case DiceSkillKind.HeavySlash:
            case DiceSkillKind.Vampiric:
                return $"+{face.valueV} Sát Thương";
            case DiceSkillKind.Block:
            case DiceSkillKind.IronWall:
                return $"+{face.valueV} Giáp Ảo";
            case DiceSkillKind.Regenerate:
                return $"+{face.valueV} Hồi Máu";
            case DiceSkillKind.ManaSpark:
            case DiceSkillKind.ArcaneSurge:
                return $"+{face.valueV} Năng Lượng";
            case DiceSkillKind.PoisonSting:
                return $"+{face.valueV} Độc Đố";
            case DiceSkillKind.Ignite:
                return $"+{face.valueV} Thiêu Đốt";
            case DiceSkillKind.ShockStun:
                return $"+{face.valueV} Choáng";
            default:
                return face.valueV > 0 ? $"+{face.valueV}" : "Mặt Trống";
        }
    }

    private string GetSkillDescription(DiceSkillKind kind, int val)
    {
        switch (kind)
        {
            case DiceSkillKind.Strike:      return $"Gây {val} ST vật lý trực tiếp lên kẻ địch.";
            case DiceSkillKind.Block:       return $"Tạo {val} điểm Giáp ảo chặn sát thương.";
            case DiceSkillKind.Regenerate:  return $"Hồi phục {val} HP lập tức cho Hiệp Sĩ.";
            case DiceSkillKind.ManaSpark:   return $"Tích lũy {val} Năng Lượng để dùng Kỹ Năng.";
            case DiceSkillKind.HeavySlash:  return $"Chém mạnh gây {val} ST, tăng nếu có Giáp.";
            case DiceSkillKind.IronWall:    return $"Phòng thủ kiên cố: +{val} Giáp và miễn nhiễm.";
            case DiceSkillKind.Vampiric:    return $"Gây {val} ST và hồi máu bằng 50% ST.";
            case DiceSkillKind.PoisonSting: return $"Gây {val} ST và cộng dồn {val} tầng Độc.";
            case DiceSkillKind.Ignite:      return $"Gây {val} ST và cộng dồn {val} tầng Thiêu Đốt.";
            case DiceSkillKind.ShockStun:   return $"Gây {val} ST và làm choáng kẻ địch.";
            case DiceSkillKind.ArcaneSurge: return $"Bộc phá ma pháp: +{val} NL tức thì.";
            default:                        return "Mặt xúc xắc chiến thuật cơ bản.";
        }
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
