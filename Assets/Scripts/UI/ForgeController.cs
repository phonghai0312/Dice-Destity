using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ForgeController — Điều phối Xưởng Rèn Xúc Xắc (TheForge) theo GDD mục 2.1 & 3.2:
/// - Rút 3 mặt xúc xắc phần thưởng ngẫu nhiên từ DiceCatalog (10 kỹ năng chuẩn GDD).
/// - Cho phép chuyển đổi giữa 3 viên xúc xắc 6 mặt trong túi (playerDiceBag).
/// - Cho phép chọn 1 trong 3 Mặt Thưởng và chọn 1 trong 6 Mặt Cũ để rèn đè (ReplacePlayerDiceFace).
/// - Hỗ trợ cả Click chọn lẫn Kéo-Thả thẻ Mặt Thưởng thả trực tiếp vào ô Mặt Cũ.
/// </summary>
public class ForgeController : MonoBehaviour
{
    private static ForgeController activeInstance;

    [Header("Auto-discovered (không cần assign)")]
    public Button nextStageButton;
    public Transform rewardsContainer;
    public Transform currentDiceContainer;
    public DiceFaceData[] allPossibleFaces;

    [Header("Dice Face Sprites (Assets/GameAssets)")]
    public Sprite diceFaceAttackSprite;
    public Sprite diceFaceShieldSprite;
    public Sprite diceFacePoisonSprite;
    public Sprite diceFaceFireSprite;

    // Runtime state
    private int selectedDiceIndex = 0;       // Viên xúc xắc đang xem (0..2)
    private int selectedFaceSlotIndex = 5;   // Ô mặt trên viên xúc xắc sẽ bị ghi đè (0..5)
    private int selectedRewardIndex = -1;    // Thẻ phần thưởng đang chọn (0..2)
    private bool isForgingTransition = false;

    private readonly List<GameObject> rewardCards = new List<GameObject>();
    private readonly List<DiceFaceData> rolledRewards = new List<DiceFaceData>();
    private readonly List<GameObject> currentFaceSlots = new List<GameObject>();
    private readonly List<Button> diceTabButtons = new List<Button>();

    private Component forgeTitleLabel;
    private Component forgeSubLabel;
    private Component curDiceLabel;

    private static readonly Color unselectedDim = new Color(0.55f, 0.55f, 0.55f, 0.72f);
    private readonly string[] rewardNames = { "Reward_Cleave", "Reward_Inferno", "Reward_Doom" };

    private System.Type TMP;

    private void Awake()
    {
        // Ngăn trường hợp trong TheForge.unity có nhiều GameObject ForgeController trùng lặp
        if (activeInstance != null && activeInstance != this)
        {
            enabled = false;
            return;
        }
        activeInstance = this;
    }

    private void OnDestroy()
    {
        if (activeInstance == this)
        {
            activeInstance = null;
        }
    }

    private void Start()
    {
        TMP = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        StartCoroutine(InitAfterFrame());
    }

    private IEnumerator InitAfterFrame()
    {
        yield return null;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnsureDiceBagInitialized();
        }

        AutoDiscover();
        CreateBackToMapButtonIfNeeded();
        CreateDiceSelectorTabsIfNeeded();
        SetupCurrentDiceFaceSlots();
        GenerateAndSetupRewards();
        SetupConfirmButton();
        AddFloatingAnimations();

        SelectBestDefaultFaceSlot();
        RefreshCurrentDiceUI();
        RefreshRewardCardsUI();
        RefreshHeaderAndConfirmState();
    }

    private bool HasAvailableForgeUpgrade()
    {
        if (GameManager.Instance == null) return true;
        // Nếu mở TheForge trực tiếp trong Editor (lastClearedStageIndex == 0) hoặc còn lượt rèn sau qua ải
        return GameManager.Instance.pendingForgeUpgrades > 0 || GameManager.Instance.lastClearedStageIndex == 0;
    }

    private void CreateBackToMapButtonIfNeeded()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;
        if (canvas.transform.Find("BackToMapButton") != null) return;

        GameObject btnGo = new GameObject("BackToMapButton");
        btnGo.transform.SetParent(canvas.transform, false);
        RectTransform rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(36f, -30f);
        rt.sizeDelta = new Vector2(240f, 44f);

        Image bg = btnGo.AddComponent<Image>();
        bg.color = new Color(0.14f, 0.11f, 0.20f, 0.95f);

        Outline ol = btnGo.AddComponent<Outline>();
        ol.effectColor = new Color(0.85f, 0.68f, 0.25f, 0.85f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(OnBackToMapClicked);

        GameObject txtGo = new GameObject("Label");
        txtGo.transform.SetParent(btnGo.transform, false);
        RectTransform txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        if (TMP != null)
        {
            Component tmp = txtGo.AddComponent(TMP);
            SetTMPText(tmp, "🗺 ⬅ VỀ BẢN ĐỒ ẢI");
            TMP.GetProperty("fontSize")?.SetValue(tmp, 15f);
            TMP.GetProperty("alignment")?.SetValue(tmp, 514);
            TMP.GetProperty("color")?.SetValue(tmp, new Color(0.96f, 0.88f, 0.68f, 1f));
            TMP.GetProperty("raycastTarget")?.SetValue(tmp, false);
        }
    }

    public void OnBackToMapClicked()
    {
        if (isForgingTransition) return;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadStageMapScene();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("StageMap");
        }
    }

    // =========================================================================
    // 1. TỰ ĐỘNG TÌM KIẾM UI TRONG SCENE THEFORGE
    // =========================================================================

    private void AutoDiscover()
    {
        GameObject confGo = GameObject.Find("ConfirmButton");
        if (confGo != null && nextStageButton == null)
        {
            nextStageButton = confGo.GetComponent<Button>();
        }

        // Tìm container 6 mặt xúc xắc hiện tại (trong Scene tên là CurrentDiceContainer hoặc CurrentDiceBox)
        if (currentDiceContainer == null)
        {
            GameObject cdCont = GameObject.Find("CurrentDiceContainer");
            if (cdCont == null) cdCont = GameObject.Find("CurrentDiceBox");
            if (cdCont != null) currentDiceContainer = cdCont.transform;
        }

        if (rewardsContainer == null)
        {
            GameObject rwCont = GameObject.Find("RewardsContainer");
            if (rwCont != null) rewardsContainer = rwCont.transform;
        }

        GameObject titleGo = GameObject.Find("ForgeTitle");
        if (titleGo != null && TMP != null) forgeTitleLabel = titleGo.GetComponent(TMP);

        GameObject subGo = GameObject.Find("ForgeSub");
        if (subGo != null && TMP != null) forgeSubLabel = subGo.GetComponent(TMP);

        GameObject curLabelGo = GameObject.Find("CurDiceLabel");
        if (curLabelGo != null && TMP != null) curDiceLabel = curLabelGo.GetComponent(TMP);

        if (nextStageButton != null)
        {
            nextStageButton.interactable = false;
        }
    }

    // =========================================================================
    // 2. THANH CHỌN 3 VIÊN XÚC XẮC & 6 Ô MẶT HIỆN TẠI
    // =========================================================================

    private void CreateDiceSelectorTabsIfNeeded()
    {
        diceTabButtons.Clear();
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        Transform existingBar = canvas.transform.Find("DiceBagSelectorBar");
        if (existingBar != null) return;

        GameObject barGo = new GameObject("DiceBagSelectorBar");
        barGo.transform.SetParent(canvas.transform, false);
        RectTransform barRt = barGo.AddComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0.5f, 1f);
        barRt.anchorMax = new Vector2(0.5f, 1f);
        barRt.pivot = new Vector2(0.5f, 1f);
        barRt.anchoredPosition = new Vector2(0f, -138f);
        barRt.sizeDelta = new Vector2(720f, 38f);

        HorizontalLayoutGroup hlg = barGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 14f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        List<DiceInstance> bag = GetActiveDiceBag();
        int count = Mathf.Max(3, bag.Count);

        for (int i = 0; i < count; i++)
        {
            int capturedIdx = i;
            string dieTitle = (i < bag.Count && bag[i] != null) ? bag[i].diceName : $"Xúc Xắc #{i + 1}";

            GameObject tabObj = new GameObject($"DiceTab_{i + 1}");
            tabObj.transform.SetParent(barGo.transform, false);

            Image bg = tabObj.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.12f, 0.18f, 0.92f);

            Outline ol = tabObj.AddComponent<Outline>();
            ol.effectColor = new Color(0.72f, 0.53f, 0.05f, 0.6f);
            ol.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = tabObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => OnSelectDiceTab(capturedIdx));

            GameObject txtGo = new GameObject("Label");
            txtGo.transform.SetParent(tabObj.transform, false);
            RectTransform txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            if (TMP != null)
            {
                Component tmp = txtGo.AddComponent(TMP);
                SetTMPText(tmp, $"VIÊN #{i + 1}: {dieTitle.ToUpper()}");
                TMP.GetProperty("fontSize")?.SetValue(tmp, 14f);
                TMP.GetProperty("alignment")?.SetValue(tmp, 514);
                TMP.GetProperty("color")?.SetValue(tmp, new Color(0.95f, 0.9f, 0.78f, 1f));
                TMP.GetProperty("raycastTarget")?.SetValue(tmp, false);
            }

            diceTabButtons.Add(btn);
        }
    }

    private void SetupCurrentDiceFaceSlots()
    {
        currentFaceSlots.Clear();
        if (currentDiceContainer == null) return;

        for (int i = 0; i < currentDiceContainer.childCount; i++)
        {
            GameObject slotGo = currentDiceContainer.GetChild(i).gameObject;
            currentFaceSlots.Add(slotGo);

            int capturedSlot = i;
            Button btn = slotGo.GetComponent<Button>();
            if (btn == null) btn = slotGo.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnSelectFaceSlot(capturedSlot));

            Outline ol = slotGo.GetComponent<Outline>();
            if (ol == null) ol = slotGo.AddComponent<Outline>();

            // Cho phép thả thẻ phần thưởng trực tiếp vào ô mặt xúc xắc này
            ForgeFaceSlotDropHandler dropHandler = slotGo.GetComponent<ForgeFaceSlotDropHandler>();
            if (dropHandler == null) dropHandler = slotGo.AddComponent<ForgeFaceSlotDropHandler>();
            dropHandler.controller = this;
            dropHandler.faceSlotIndex = capturedSlot;
        }
    }

    public void OnSelectDiceTab(int diceIdx)
    {
        if (isForgingTransition) return;
        List<DiceInstance> bag = GetActiveDiceBag();
        if (diceIdx < 0 || diceIdx >= bag.Count) return;

        selectedDiceIndex = diceIdx;
        SelectBestDefaultFaceSlot();
        RefreshCurrentDiceUI();
        RefreshHeaderAndConfirmState();
    }

    public void OnSelectFaceSlot(int slotIdx)
    {
        if (isForgingTransition) return;
        if (slotIdx < 0 || slotIdx >= DiceInstance.FACES_COUNT) return;

        selectedFaceSlotIndex = slotIdx;
        RefreshCurrentDiceUI();
        RefreshHeaderAndConfirmState();
    }

    /// <summary>
    /// Mặc định ưu tiên chọn sẵn ô mặt Blank (Miss) nếu có để người chơi tiện rèn đè.
    /// </summary>
    private void SelectBestDefaultFaceSlot()
    {
        List<DiceInstance> bag = GetActiveDiceBag();
        if (selectedDiceIndex >= 0 && selectedDiceIndex < bag.Count && bag[selectedDiceIndex] != null)
        {
            DiceFaceData[] faces = bag[selectedDiceIndex].faces;
            if (faces != null)
            {
                for (int i = 0; i < faces.Length; i++)
                {
                    if (faces[i] != null && faces[i].GetResolvedSkillKind() == DiceSkillKind.Blank)
                    {
                        selectedFaceSlotIndex = i;
                        return;
                    }
                }
            }
        }
        selectedFaceSlotIndex = 5;
    }

    // =========================================================================
    // 3. KHỞI TẠO & HIỂN THỊ 3 MẶT XÚC XẮC PHẦN THƯỞNG (REWARDS)
    // =========================================================================

    private void GenerateAndSetupRewards()
    {
        rewardCards.Clear();
        rolledRewards.Clear();

        List<DiceFaceData> randomPool = GameManager.Instance != null
            ? GameManager.Instance.GetOrRollForgeRewards(rewardNames.Length)
            : DiceCatalog.RollRandomRewards(rewardNames.Length);

        for (int i = 0; i < rewardNames.Length; i++)
        {
            GameObject rw = GameObject.Find(rewardNames[i]);
            if (rw == null && rewardsContainer != null && i < rewardsContainer.childCount)
            {
                rw = rewardsContainer.GetChild(i).gameObject;
            }
            if (rw == null) continue;

            rewardCards.Add(rw);

            DiceFaceData rewardFace = i < randomPool.Count
                ? randomPool[i]
                : DiceFaceData.CreateRuntimeFace("Heavy Slash", DiceFaceType.Attack, DiceSkillKind.HeavySlash, 7, DiceFaceRarity.Rare);

            // Nếu trong Scene có gán sẵn icon trong allPossibleFaces thì kế thừa sprite đẹp
            if (rewardFace.icon == null && allPossibleFaces != null && i < allPossibleFaces.Length && allPossibleFaces[i] != null)
            {
                rewardFace.icon = allPossibleFaces[i].icon;
            }

            rolledRewards.Add(rewardFace);

            ForgeAnimator fa = rw.GetComponent<ForgeAnimator>();
            if (fa == null) fa = rw.AddComponent<ForgeAnimator>();
            fa.floatAmplitude = 8f;
            fa.floatSpeed = 0.9f + i * 0.15f;
            fa.floatOffset = i * 1.2f;
            fa.playEntryAnim = true;
            fa.entryDelay = 0.12f + i * 0.1f;

            int capturedIdx = i;
            Button btn = rw.GetComponent<Button>();
            if (btn == null) btn = rw.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnRewardSelected(capturedIdx));

            // Hỗ trợ kéo-thả thẻ phần thưởng lên 1 trong 6 ô mặt cũ
            ForgeRewardDragHandler dragHandler = rw.GetComponent<ForgeRewardDragHandler>();
            if (dragHandler == null) dragHandler = rw.AddComponent<ForgeRewardDragHandler>();
            dragHandler.controller = this;
            dragHandler.rewardIndex = capturedIdx;
        }
    }

    public void OnRewardSelected(int index)
    {
        if (isForgingTransition) return;
        if (index < 0 || index >= rolledRewards.Count) return;

        selectedRewardIndex = index;

        for (int i = 0; i < rewardCards.Count; i++)
        {
            if (rewardCards[i] == null) continue;
            ForgeAnimator fa = rewardCards[i].GetComponent<ForgeAnimator>();
            if (i == index && fa != null)
            {
                fa.Bounce();
            }
        }

        RefreshRewardCardsUI();
        RefreshCurrentDiceUI();
        RefreshHeaderAndConfirmState();
    }

    /// <summary>
    /// Được gọi khi người chơi kéo thẻ phần thưởng thả trực tiếp vào 1 trong 6 ô mặt của viên xúc xắc.
    /// </summary>
    public void OnRewardDroppedOntoFaceSlot(int rewardIdx, int targetFaceSlotIdx)
    {
        if (isForgingTransition) return;
        if (rewardIdx < 0 || rewardIdx >= rolledRewards.Count) return;
        if (targetFaceSlotIdx < 0 || targetFaceSlotIdx >= DiceInstance.FACES_COUNT) return;

        selectedRewardIndex = rewardIdx;
        selectedFaceSlotIndex = targetFaceSlotIdx;

        if (targetFaceSlotIdx < currentFaceSlots.Count && currentFaceSlots[targetFaceSlotIdx] != null)
        {
            StartCoroutine(PulseSlotFeedback(currentFaceSlots[targetFaceSlotIdx].transform));
        }

        RefreshRewardCardsUI();
        RefreshCurrentDiceUI();
        RefreshHeaderAndConfirmState();
    }

    // =========================================================================
    // 4. CẬP NHẬT GIAO DIỆN CHI TIẾT (6 MẶT HIỆN TẠI + 3 MẶT THƯỞNG)
    // =========================================================================

    private void RefreshCurrentDiceUI()
    {
        List<DiceInstance> bag = GetActiveDiceBag();
        if (bag == null || bag.Count == 0) return;

        selectedDiceIndex = Mathf.Clamp(selectedDiceIndex, 0, bag.Count - 1);
        DiceInstance currentDie = bag[selectedDiceIndex];

        // Cập nhật màu thanh Tab chọn viên xúc xắc
        for (int i = 0; i < diceTabButtons.Count; i++)
        {
            if (diceTabButtons[i] == null) continue;
            Image tabImg = diceTabButtons[i].GetComponent<Image>();
            Outline tabOl = diceTabButtons[i].GetComponent<Outline>();
            bool isSelectedTab = (i == selectedDiceIndex);

            if (tabImg != null)
            {
                tabImg.color = isSelectedTab
                    ? new Color(0.55f, 0.36f, 0.06f, 0.98f)
                    : new Color(0.14f, 0.12f, 0.18f, 0.85f);
            }
            if (tabOl != null)
            {
                tabOl.effectColor = isSelectedTab
                    ? new Color(1f, 0.82f, 0.25f, 1f)
                    : new Color(0.35f, 0.3f, 0.4f, 0.5f);
            }
        }

        if (curDiceLabel != null && currentDie != null)
        {
            SetTMPText(
                curDiceLabel,
                $"ĐANG CHỌN: {currentDie.diceName.ToUpper()} (CLICK Ô MẶT CŨ ĐỂ CHỌN VỊ TRÍ GHI ĐÈ)");
        }

        // Cập nhật 6 ô mặt của viên xúc xắc đang chọn
        for (int i = 0; i < currentFaceSlots.Count; i++)
        {
            GameObject slotGo = currentFaceSlots[i];
            if (slotGo == null) continue;

            DiceFaceData face = (currentDie != null && currentDie.faces != null && i < currentDie.faces.Length)
                ? currentDie.faces[i]
                : null;

            bool isSelectedSlot = (i == selectedFaceSlotIndex);
            BindCardVisuals(slotGo, face, isSelectedSlot, isRewardCard: false, slotNumber: i + 1);
        }
    }

    private void RefreshRewardCardsUI()
    {
        for (int i = 0; i < rewardCards.Count && i < rolledRewards.Count; i++)
        {
            GameObject cardGo = rewardCards[i];
            if (cardGo == null) continue;

            DiceFaceData rewardFace = rolledRewards[i];
            bool isSelected = (i == selectedRewardIndex);
            BindCardVisuals(cardGo, rewardFace, isSelected, isRewardCard: true, slotNumber: i + 1);

            if (selectedRewardIndex >= 0 && !isSelected)
            {
                Image bg = cardGo.GetComponent<Image>();
                if (bg != null)
                {
                    bg.color = Color.Lerp(bg.color, unselectedDim, 0.5f);
                }
            }

            // Cập nhật các nhãn Rarity_i và Desc_i có sẵn trong Scene TheForge
            if (rewardFace != null && TMP != null)
            {
                GameObject rarityGo = GameObject.Find($"Rarity_{i}");
                if (rarityGo != null)
                {
                    SetTMPText(rarityGo.GetComponent(TMP), rewardFace.rarity.ToString().ToUpper());
                }

                GameObject descGo = GameObject.Find($"Desc_{i}");
                if (descGo != null)
                {
                    string shortDesc = rewardFace.GetTooltipText();
                    int nlIdx = shortDesc.IndexOf('\n');
                    if (nlIdx >= 0 && nlIdx + 1 < shortDesc.Length)
                    {
                        shortDesc = shortDesc.Substring(nlIdx + 1);
                    }
                    SetTMPText(descGo.GetComponent(TMP), shortDesc);
                }
            }
        }
    }

    private Sprite GetSkillSprite(DiceFaceData face)
    {
        if (face == null) return null;
        if (face.icon != null) return face.icon;

        switch (face.GetResolvedSkillKind())
        {
            case DiceSkillKind.Strike:
            case DiceSkillKind.HeavySlash:
                return diceFaceAttackSprite;
            case DiceSkillKind.Block:
            case DiceSkillKind.IronWall:
                return diceFaceShieldSprite;
            case DiceSkillKind.PoisonSting:
                return diceFacePoisonSprite != null ? diceFacePoisonSprite : diceFaceFireSprite;
            case DiceSkillKind.Vampiric:
            case DiceSkillKind.Regenerate:
            case DiceSkillKind.Purify:
            case DiceSkillKind.ShockStun:
                return diceFaceFireSprite != null ? diceFaceFireSprite : diceFaceAttackSprite;
            default:
                return diceFaceAttackSprite;
        }
    }

    private void BindCardVisuals(GameObject cardGo, DiceFaceData face, bool isSelected, bool isRewardCard, int slotNumber)
    {
        if (cardGo == null || face == null) return;

        Transform nameTr = cardGo.transform.Find("Name");
        if (nameTr == null) nameTr = cardGo.transform.Find("DiceName");
        if (nameTr != null && TMP != null)
        {
            Component nameTmp = nameTr.GetComponent(TMP);
            string displayName = isRewardCard
                ? $"{face.faceName.ToUpper()}\n[{face.rarity}]"
                : $"#{slotNumber} {face.faceName.ToUpper()}";
            SetTMPText(nameTmp, displayName);
        }

        Transform valTr = cardGo.transform.Find("Val");
        if (valTr == null) valTr = cardGo.transform.Find("DiceVal");
        if (valTr != null && TMP != null)
        {
            Component valTmp = valTr.GetComponent(TMP);
            SetTMPText(valTmp, GetFaceSymbolAndValue(face));
        }

        Transform iconTr = cardGo.transform.Find("Icon");
        if (iconTr == null) iconTr = cardGo.transform.Find("DiceIcon");
        Sprite resolvedIcon = GetSkillSprite(face);
        if (iconTr != null && resolvedIcon != null)
        {
            Image iconImg = iconTr.GetComponent<Image>();
            if (iconImg != null) iconImg.sprite = resolvedIcon;
        }

        Image bg = cardGo.GetComponent<Image>();
        if (bg != null)
        {
            Color baseColor = GetFaceTintColor(face.GetResolvedSkillKind());
            bg.color = isSelected
                ? new Color(
                    Mathf.Min(1f, baseColor.r + 0.18f),
                    Mathf.Min(1f, baseColor.g + 0.18f),
                    Mathf.Min(1f, baseColor.b + 0.18f),
                    1f)
                : baseColor;
        }

        Outline ol = cardGo.GetComponent<Outline>();
        if (ol != null)
        {
            if (isSelected)
            {
                ol.effectColor = isRewardCard
                    ? new Color(1f, 0.82f, 0.18f, 1f)
                    : new Color(0.25f, 0.95f, 0.6f, 1f);
                ol.effectDistance = new Vector2(3f, -3f);
            }
            else
            {
                ol.effectColor = new Color(0.18f, 0.15f, 0.22f, 0.6f);
                ol.effectDistance = new Vector2(1.2f, -1.2f);
            }
        }
    }

    private void RefreshHeaderAndConfirmState()
    {
        int nextStage = GameManager.Instance != null ? GameManager.Instance.currentStageIndex : 2;
        bool isVictory = GameManager.Instance != null && GameManager.Instance.isRunVictory;
        bool hasUpgrade = HasAvailableForgeUpgrade();

        if (forgeTitleLabel != null)
        {
            if (isVictory)
            {
                SetTMPText(forgeTitleLabel, "CHIẾN THẮNG HOÀN TOÀN — XƯỞNG RÈN HUYỀN THOẠI");
            }
            else
            {
                SetTMPText(forgeTitleLabel, $"XƯỞNG RÈN XÚC XẮC — CHUẨN BỊ CHO ẢI {nextStage}/3");
            }
        }

        if (!hasUpgrade)
        {
            if (forgeSubLabel != null)
            {
                SetTMPText(
                    forgeSubLabel,
                    "✅ Đã hoàn tất rèn xúc xắc cho lượt này! Bạn có thể kiểm tra 3 viên xúc xắc hoặc quay về Bản Đồ Ải.");
            }

            if (nextStageButton != null)
            {
                nextStageButton.interactable = true;
                UpdateConfirmButtonText("🗺 QUAY VỀ BẢN ĐỒ ẢI");
            }
            return;
        }

        List<DiceInstance> bag = GetActiveDiceBag();
        DiceInstance targetDie = (selectedDiceIndex >= 0 && selectedDiceIndex < bag.Count) ? bag[selectedDiceIndex] : null;
        DiceFaceData oldFace = (targetDie != null && targetDie.faces != null && selectedFaceSlotIndex < targetDie.faces.Length)
            ? targetDie.faces[selectedFaceSlotIndex]
            : null;

        if (selectedRewardIndex >= 0 && selectedRewardIndex < rolledRewards.Count)
        {
            DiceFaceData rewardFace = rolledRewards[selectedRewardIndex];
            string oldName = oldFace != null ? $"{oldFace.faceName} ({oldFace.valueV})" : $"Ô #{selectedFaceSlotIndex + 1}";
            string dieName = targetDie != null ? targetDie.diceName : $"Viên #{selectedDiceIndex + 1}";

            if (forgeSubLabel != null)
            {
                string cleanTooltip = rewardFace.GetTooltipText().Replace("\n", " — ");
                SetTMPText(
                    forgeSubLabel,
                    $"Sẵn sàng rèn: Đè [{rewardFace.faceName} ({rewardFace.valueV})] lên mặt cũ [{oldName}] của {dieName}  |  {cleanTooltip}");
            }

            if (nextStageButton != null)
            {
                nextStageButton.interactable = true;
                UpdateConfirmButtonText("🔥 RÈN MẶT MỚI & VỀ BẢN ĐỒ ẢI");
            }
        }
        else
        {
            if (forgeSubLabel != null)
            {
                SetTMPText(
                    forgeSubLabel,
                    "Bước 1: Chọn 1 trong 3 Mặt Thưởng bên dưới (hoặc kéo thả)  ➔  Bước 2: Chọn 1 trong 6 Ô Mặt Cũ ở trên để ghi đè");
            }

            if (nextStageButton != null)
            {
                nextStageButton.interactable = false;
                UpdateConfirmButtonText("CHỌN MẶT THƯỞNG ĐỂ RÈN");
            }
        }
    }

    private void UpdateConfirmButtonText(string text)
    {
        if (nextStageButton == null || TMP == null) return;
        Component txt = nextStageButton.GetComponentInChildren(TMP);
        SetTMPText(txt, text);
    }

    // =========================================================================
    // 5. XÁC NHẬN RÈN ĐÈ MẶT XÚC XẮC & QUAY VỀ BẢN ĐỒ ẢI (STAGEMAP)
    // =========================================================================

    private void SetupConfirmButton()
    {
        if (nextStageButton == null) return;
        nextStageButton.onClick.RemoveAllListeners();
        nextStageButton.onClick.AddListener(OnNextStageClicked);
    }

    public void OnNextStageClicked()
    {
        if (activeInstance != null && activeInstance != this)
        {
            activeInstance.OnNextStageClicked();
            return;
        }

        if (isForgingTransition) return;

        if (!HasAvailableForgeUpgrade())
        {
            OnBackToMapClicked();
            return;
        }

        if (selectedRewardIndex < 0 || selectedRewardIndex >= rolledRewards.Count)
        {
            Debug.LogWarning("[Forge] Chưa chọn mặt xúc xắc phần thưởng!");
            return;
        }

        StartCoroutine(ExecuteForgeAndAdvanceCoroutine());
    }

    private IEnumerator ExecuteForgeAndAdvanceCoroutine()
    {
        isForgingTransition = true;
        if (nextStageButton != null) nextStageButton.interactable = false;

        DiceFaceData chosenReward = rolledRewards[selectedRewardIndex];

        // Thực hiện ghi đè mặt xúc xắc mới lên ô mặt đã chọn trong GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReplacePlayerDiceFace(selectedDiceIndex, selectedFaceSlotIndex, chosenReward);
            GameManager.Instance.ConsumeForgeUpgrade();
        }

        RefreshCurrentDiceUI();

        if (selectedFaceSlotIndex >= 0 && selectedFaceSlotIndex < currentFaceSlots.Count)
        {
            GameObject targetSlot = currentFaceSlots[selectedFaceSlotIndex];
            if (targetSlot != null)
            {
                yield return StartCoroutine(PulseSlotFeedback(targetSlot.transform));
            }
        }

        if (forgeSubLabel != null)
        {
            SetTMPText(forgeSubLabel, $"🔥 RÈN THÀNH CÔNG: Đã khắc [{chosenReward.faceName}] vào xúc xắc! Đang trở về Bản Đồ Ải...");
        }

        yield return new WaitForSeconds(0.55f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadStageMapScene();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("StageMap");
        }
    }

    private IEnumerator PulseSlotFeedback(Transform targetTransform)
    {
        if (targetTransform == null) yield break;
        Vector3 origScale = Vector3.one;
        float duration = 0.28f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.25f;
            targetTransform.localScale = origScale * s;
            yield return null;
        }

        targetTransform.localScale = origScale;
    }

    // =========================================================================
    // 6. HELPERS & FLOATING ANIMATIONS
    // =========================================================================

    private List<DiceInstance> GetActiveDiceBag()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnsureDiceBagInitialized();
            return GameManager.Instance.playerDiceBag;
        }
        return DiceCatalog.CreateStarterDiceBag();
    }

    private string GetFaceSymbolAndValue(DiceFaceData face)
    {
        if (face == null) return "0";
        switch (face.GetResolvedSkillKind())
        {
            case DiceSkillKind.Strike:
            case DiceSkillKind.HeavySlash:
                return $"⚔{face.valueV}";
            case DiceSkillKind.Vampiric:
                return $"🩸{face.valueV}";
            case DiceSkillKind.Block:
            case DiceSkillKind.IronWall:
                return $"🛡{face.valueV}";
            case DiceSkillKind.Regenerate:
                return $"💚{face.valueV}";
            case DiceSkillKind.Purify:
                return "✨0";
            case DiceSkillKind.PoisonSting:
                return $"☠{face.valueV}";
            case DiceSkillKind.ShockStun:
                return $"⚡{face.valueV}";
            case DiceSkillKind.ManaSpark:
            case DiceSkillKind.ArcaneSurge:
            case DiceSkillKind.Overcharge:
                return $"🔷+{face.valueV}";
            case DiceSkillKind.Blank:
                return "💨0";
            default:
                return face.valueV.ToString();
        }
    }

    private Color GetFaceTintColor(DiceSkillKind kind)
    {
        switch (kind)
        {
            case DiceSkillKind.Strike:
            case DiceSkillKind.HeavySlash:
                return new Color(0.42f, 0.14f, 0.16f, 0.92f);
            case DiceSkillKind.Vampiric:
                return new Color(0.48f, 0.10f, 0.28f, 0.92f);
            case DiceSkillKind.Block:
            case DiceSkillKind.IronWall:
                return new Color(0.13f, 0.26f, 0.44f, 0.92f);
            case DiceSkillKind.Regenerate:
            case DiceSkillKind.Purify:
                return new Color(0.13f, 0.36f, 0.24f, 0.92f);
            case DiceSkillKind.PoisonSting:
                return new Color(0.30f, 0.15f, 0.42f, 0.92f);
            case DiceSkillKind.ShockStun:
                return new Color(0.45f, 0.35f, 0.10f, 0.92f);
            case DiceSkillKind.ManaSpark:
            case DiceSkillKind.ArcaneSurge:
            case DiceSkillKind.Overcharge:
                return new Color(0.12f, 0.34f, 0.56f, 0.94f);
            case DiceSkillKind.Blank:
            default:
                return new Color(0.22f, 0.20f, 0.25f, 0.85f);
        }
    }

    private void SetTMPText(Component tmpComp, string text)
    {
        if (tmpComp == null || TMP == null) return;
        TMP.GetProperty("text")?.SetValue(tmpComp, text);
    }

    private void AddFloatingAnimations()
    {
        AddEntryAnim("CurrentDiceContainer", 0f);
        AddEntryAnim("ForgeTitle", 0f);
        AddEntryAnim("ForgeSub", 0.05f);
        AddEntryAnim("RewardTitle", 0.1f);
        AddEntryAnim("ConfirmButton", 0.35f);
    }

    private void AddEntryAnim(string objName, float delay)
    {
        GameObject go = GameObject.Find(objName);
        if (go == null) return;
        ForgeAnimator fa = go.GetComponent<ForgeAnimator>();
        if (fa == null) fa = go.AddComponent<ForgeAnimator>();
        fa.playEntryAnim = true;
        fa.entryDelay = delay;
        fa.floatAmplitude = 0f;
    }
}

/// <summary>
/// Hỗ trợ kéo-thả thẻ phần thưởng trong Xưởng Rèn lên ô mặt xúc xắc cũ.
/// </summary>
public class ForgeRewardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public ForgeController controller;
    [HideInInspector] public int rewardIndex;

    private RectTransform rt;
    private Canvas canvas;
    private CanvasGroup cg;
    private Vector2 startAnchoredPos;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (controller != null)
        {
            controller.OnRewardSelected(rewardIndex);
        }
        if (rt != null) startAnchoredPos = rt.anchoredPosition;
        if (cg != null)
        {
            cg.alpha = 0.8f;
            cg.blocksRaycasts = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rt == null) return;
        float scale = (canvas != null && canvas.scaleFactor > 0f) ? canvas.scaleFactor : 1f;
        rt.anchoredPosition += eventData.delta / scale;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (rt != null) rt.anchoredPosition = startAnchoredPos;
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
        }
    }
}

/// <summary>
/// Nhận thao tác thả thẻ phần thưởng vào 1 trong 6 ô mặt xúc xắc hiện tại.
/// </summary>
public class ForgeFaceSlotDropHandler : MonoBehaviour, IDropHandler
{
    [HideInInspector] public ForgeController controller;
    [HideInInspector] public int faceSlotIndex;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null || controller == null) return;
        ForgeRewardDragHandler drag = eventData.pointerDrag.GetComponent<ForgeRewardDragHandler>();
        if (drag != null)
        {
            controller.OnRewardDroppedOntoFaceSlot(drag.rewardIndex, faceSlotIndex);
        }
    }
}
