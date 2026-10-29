using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// ForgeController — Điều phối Giao diện Lò Rèn Huyền Thuật mới:
/// - Bấm 1 mặt trong Pool để đánh dấu ĐÃ CHỌN, rồi bấm vào Ô Xúc Xắc để gán / thay thế / ghi đè.
/// - Mặt trong Pool KHÔNG BIẾN MẤT (đóng vai trò hiển thị thư viện để gán).
/// - Quy tắc kiểm tra logic rõ ràng:
///   + Mỗi loại mặt tối đa 2 lần trên cùng 1 viên xúc xắc.
///   + Mỗi loại mặt tối đa 4 lần trên toàn bộ 3 viên xúc xắc.
///   + Báo lỗi cụ thể lý do nếu vi phạm một trong các điều kiện trên.
/// - Bấm 2 mặt cùng loại trong Pool sẽ hiện NÚT KẾT HỢP.
/// - Nhấn KẾT HỢP sẽ hỏi: "Bạn có muốn cường hoá mặt xúc xắc này không?".
///   Nếu CÓ: Gộp 2 mặt trong Pool thành mặt Cấp cao hơn, VÀ TỰ ĐỘNG THAY THẾ tất cả các mặt cũ tương ứng
///   trên cả 3 viên xúc xắc thành mặt mới được cường hóa!
/// </summary>
public class ForgeController : MonoBehaviour
{
    public static ForgeController Instance { get; private set; }

    [Header("Legacy Compatibility Fields (Editor scripts)")]
    public Transform rewardsContainer;
    public Transform currentDiceContainer;
    public Button nextStageButton;
    public DiceFaceData[] allPossibleFaces;

    public void OnNextStageClicked()
    {
        GameManager gm = GameManager.ResolveInstance();
        if (gm != null) gm.LoadStageMapScene();
        else SceneManager.LoadScene("StageMap");
    }

    [Header("Sprite Icons (Assets/GameAssets)")]
    public Sprite diceFaceAttackSprite;
    public Sprite diceFaceShieldSprite;
    public Sprite diceFacePoisonSprite;
    public Sprite diceFaceFireSprite;
    public Sprite diceFaceEnergySprite;
    public Sprite diceFaceHealSprite;
    public Sprite diceFaceLightningSprite;
    public Sprite diceFaceBlankSprite;

    private int selectedDiceIndex = 0;       // 0=Viên 1, 1=Viên 2, 2=Viên 3
    private int selectedPoolIndexA = -1;     // Mặt thứ nhất được chọn trong Pool
    private int selectedPoolIndexB = -1;     // Mặt thứ hai được chọn trong Pool (để kết hợp)

    // UI Root references
    private GameObject forgeRootGo;
    private Canvas rootCanvas;
    private TextMeshProUGUI statusToastText;
    private Coroutine statusToastCoroutine;

    // Left Panel (Pool)
    private TextMeshProUGUI poolCountText;
    private Transform poolGridContent;
    private readonly List<GameObject> spawnedPoolCardGos = new List<GameObject>();

    // Action Bar (Kết Hợp Cường Hóa)
    private GameObject combineActionBarGo;
    private TextMeshProUGUI combineGuideText;
    private Button combineExecuteBtn;
    private TextMeshProUGUI combineExecuteBtnText;

    // Right Panel (Dice)
    private readonly List<Button> diceTabButtons = new List<Button>();
    private readonly List<TextMeshProUGUI> diceTabLabels = new List<TextMeshProUGUI>();
    private TextMeshProUGUI dieSummaryTitleText;
    private TextMeshProUGUI dieSummaryRuleText;
    private Transform dieSlotsContainer;
    private readonly List<GameObject> spawnedDieSlotGos = new List<GameObject>();

    // Skill Bar
    private GameObject skillBarGo;
    private Button upgradeNormalSkillBtn;
    private TextMeshProUGUI upgradeNormalSkillText;
    private Button upgradeUltimateSkillBtn;
    private TextMeshProUGUI upgradeUltimateSkillText;
    private TextMeshProUGUI pendingUpgradesBadgeText;

    // Confirmation Modal (Hộp thoại xác nhận Cường Hóa)
    private GameObject confirmModalGo;
    private TextMeshProUGUI confirmModalBodyText;
    private Button confirmModalYesBtn;
    private Button confirmModalNoBtn;

    public Canvas RootCanvas => rootCanvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        GameManager gm = GameManager.ResolveInstance();
        if (gm != null)
        {
            gm.EnsureDiceBagInitialized();
        }

        StartCoroutine(InitLayoutRoutine());
    }

    private IEnumerator InitLayoutRoutine()
    {
        yield return null;

        HideLegacyUIElements();
        BuildNewForgeLayout();
        RefreshAllUI();
        ShowToast("Nhấp 1 mặt trong Túi rồi nhấp Ô Xúc Xắc để Gán • Nhấp 2 mặt giống nhau để hiện nút Kết Hợp Cường Hóa.");
    }

    private void HideLegacyUIElements()
    {
        string[] legacyNames = new string[]
        {
            "RewardsContainer", "CurrentDiceContainer", "CurrentDiceBox",
            "ConfirmButton", "ConfirmHint", "GoldDivider", "RewardLabel",
            "PickLabel", "Rarity_0", "Rarity_1", "Rarity_2", "Desc_0",
            "Desc_1", "Desc_2", "ForgeHeader", "ForgeSub", "CurDiceLabel",
            "DiceCount", "BackToMapButton", "SkillForgeUpgradeBar", "DiceSelectorTabs"
        };

        foreach (string n in legacyNames)
        {
            GameObject obj = GameObject.Find(n);
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }

    private void BuildNewForgeLayout()
    {
        rootCanvas = FindAnyObjectByType<Canvas>();
        if (rootCanvas == null)
        {
            Debug.LogError("[ForgeController] Không tìm thấy Canvas trong Scene TheForge!");
            return;
        }

        Transform existingRoot = rootCanvas.transform.Find("NewForgeRoot");
        if (existingRoot != null)
        {
            Destroy(existingRoot.gameObject);
        }

        forgeRootGo = new GameObject("NewForgeRoot", typeof(RectTransform));
        forgeRootGo.transform.SetParent(rootCanvas.transform, false);
        forgeRootGo.transform.SetAsLastSibling();

        RectTransform rRt = forgeRootGo.GetComponent<RectTransform>();
        rRt.anchorMin = Vector2.zero;
        rRt.anchorMax = Vector2.one;
        rRt.offsetMin = Vector2.zero;
        rRt.offsetMax = Vector2.zero;

        BuildHeader(forgeRootGo.transform);
        BuildLeftPoolPanel(forgeRootGo.transform);
        BuildRightDicePanel(forgeRootGo.transform);
        BuildBottomSkillBar(forgeRootGo.transform);
        BuildConfirmationModal(forgeRootGo.transform);
    }

    // =========================================================================
    // 1. HEADER (Title, Back Button, Toast Status)
    // =========================================================================
    private void BuildHeader(Transform parent)
    {
        // Nút Về Bản Đồ (Góc trên bên trái)
        GameObject backBtnGo = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
        backBtnGo.transform.SetParent(parent, false);
        RectTransform bRt = backBtnGo.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0f, 1f);
        bRt.anchorMax = new Vector2(0f, 1f);
        bRt.pivot = new Vector2(0f, 1f);
        bRt.anchoredPosition = new Vector2(35f, -22f);
        bRt.sizeDelta = new Vector2(230f, 46f);

        Image bImg = backBtnGo.GetComponent<Image>();
        bImg.color = new Color(0.14f, 0.12f, 0.22f, 0.98f);
        Outline bOl = backBtnGo.GetComponent<Outline>();
        bOl.effectColor = new Color(0.85f, 0.65f, 0.25f, 0.9f);
        bOl.effectDistance = new Vector2(1.5f, -1.5f);

        Button bBtn = backBtnGo.GetComponent<Button>();
        bBtn.targetGraphic = bImg;
        bBtn.onClick.AddListener(() =>
        {
            GameManager gm = GameManager.ResolveInstance();
            if (gm != null) gm.LoadStageMapScene();
            else SceneManager.LoadScene("StageMap");
        });

        TextMeshProUGUI bLabel = CreateTMP(backBtnGo.transform, "Label", Vector2.zero, new Vector2(210f, 40f), 15f, new Color(1f, 0.88f, 0.45f, 1f));
        bLabel.text = "<- VỀ BẢN ĐỒ ẢI";
        bLabel.fontStyle = FontStyles.Bold;

        // Tiêu đề Lò Rèn
        TextMeshProUGUI titleTmp = CreateTMP(parent, "ForgeTitle", new Vector2(0f, 480f), new Vector2(700f, 44f), 28f, new Color(1f, 0.86f, 0.28f, 1f));
        titleTmp.text = "LÒ RÈN HUYỀN THUẬT (THE MYSTIC FORGE)";
        titleTmp.fontStyle = FontStyles.Bold;

        // Hộp thông báo trạng thái Toast
        GameObject toastGo = new GameObject("StatusToastBox", typeof(RectTransform), typeof(Image), typeof(Outline));
        toastGo.transform.SetParent(parent, false);
        RectTransform tRt = toastGo.GetComponent<RectTransform>();
        tRt.anchoredPosition = new Vector2(0f, 435f);
        tRt.sizeDelta = new Vector2(980f, 36f);

        Image tImg = toastGo.GetComponent<Image>();
        tImg.color = new Color(0.08f, 0.08f, 0.14f, 0.95f);
        Outline tOl = toastGo.GetComponent<Outline>();
        tOl.effectColor = new Color(0.45f, 0.55f, 0.85f, 0.8f);
        tOl.effectDistance = new Vector2(1.2f, -1.2f);

        statusToastText = CreateTMP(toastGo.transform, "ToastLabel", Vector2.zero, new Vector2(960f, 32f), 14.5f, new Color(0.92f, 0.96f, 1f, 1f));
        statusToastText.fontStyle = FontStyles.Bold;
    }

    // =========================================================================
    // 2. LEFT PANEL: TÚI MẶT XÚC XẮC DỰ TRỮ (POOL GRID & COMBINE BAR)
    // =========================================================================
    private void BuildLeftPoolPanel(Transform parent)
    {
        GameObject panel = new GameObject("LeftPanel_FacePool", typeof(RectTransform), typeof(Image), typeof(Outline));
        panel.transform.SetParent(parent, false);
        RectTransform pRt = panel.GetComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0.5f, 0.5f);
        pRt.anchorMax = new Vector2(0.5f, 0.5f);
        pRt.anchoredPosition = new Vector2(-460f, -25f);
        pRt.sizeDelta = new Vector2(860f, 680f);

        Image pImg = panel.GetComponent<Image>();
        pImg.color = new Color(0.07f, 0.06f, 0.12f, 0.96f);
        Outline pOl = panel.GetComponent<Outline>();
        pOl.effectColor = new Color(0.35f, 0.45f, 0.75f, 0.8f);
        pOl.effectDistance = new Vector2(2f, -2f);

        // Header Panel Pool
        GameObject head = new GameObject("PoolHeader", typeof(RectTransform), typeof(Image));
        head.transform.SetParent(panel.transform, false);
        RectTransform hRt = head.GetComponent<RectTransform>();
        hRt.anchoredPosition = new Vector2(0f, 310f);
        hRt.sizeDelta = new Vector2(840f, 44f);
        head.GetComponent<Image>().color = new Color(0.12f, 0.10f, 0.20f, 0.98f);

        TextMeshProUGUI hTitle = CreateTMP(head.transform, "Title", new Vector2(-140f, 0f), new Vector2(500f, 36f), 17f, new Color(0.92f, 0.85f, 0.35f, 1f));
        hTitle.text = "TÚI MẶT XÚC XẮC DỰ TRỮ (POOL)";
        hTitle.fontStyle = FontStyles.Bold;
        hTitle.alignment = TextAlignmentOptions.Left;

        poolCountText = CreateTMP(head.transform, "Count", new Vector2(260f, 0f), new Vector2(260f, 36f), 14.5f, new Color(0.6f, 0.85f, 1f, 1f));
        poolCountText.text = "Số lượng: 0 mặt";
        poolCountText.alignment = TextAlignmentOptions.Right;

        // Thanh thao tác Cường Hóa / Hướng dẫn (Combine Action Bar)
        combineActionBarGo = new GameObject("CombineActionBar", typeof(RectTransform), typeof(Image), typeof(Outline));
        combineActionBarGo.transform.SetParent(panel.transform, false);
        RectTransform cbRt = combineActionBarGo.GetComponent<RectTransform>();
        cbRt.anchoredPosition = new Vector2(0f, 268f);
        cbRt.sizeDelta = new Vector2(830f, 40f);

        Image cbImg = combineActionBarGo.GetComponent<Image>();
        cbImg.color = new Color(0.10f, 0.08f, 0.18f, 0.95f);
        Outline cbOl = combineActionBarGo.GetComponent<Outline>();
        cbOl.effectColor = new Color(0.4f, 0.35f, 0.65f, 0.6f);
        cbOl.effectDistance = new Vector2(1.2f, -1.2f);

        combineGuideText = CreateTMP(combineActionBarGo.transform, "GuideText", Vector2.zero, new Vector2(810f, 32f), 13f, new Color(0.85f, 0.88f, 0.96f, 0.9f));
        combineGuideText.text = "<i>Nhấp 1 mặt để gán vào xúc xắc • Nhấp 2 mặt giống nhau để KẾT HỢP CƯỜNG HÓA</i>";

        // Nút Kết Hợp Cường Hóa (ẩn khi chưa chọn 2 mặt giống nhau)
        combineExecuteBtn = CreateSimpleButton(combineActionBarGo.transform, "CombineExecuteBtn", Vector2.zero, new Vector2(620f, 36f),
            new Color(0.48f, 0.28f, 0.06f, 1f), new Color(1f, 0.85f, 0.25f, 1f), out combineExecuteBtnText);
        combineExecuteBtn.onClick.AddListener(OnCombineExecuteButtonClicked);
        combineExecuteBtn.gameObject.SetActive(false);

        // Scroll Area for Pool Cards
        GameObject scrollGo = new GameObject("PoolScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
        scrollGo.transform.SetParent(panel.transform, false);
        RectTransform sRt = scrollGo.GetComponent<RectTransform>();
        sRt.anchoredPosition = new Vector2(0f, -32f);
        sRt.sizeDelta = new Vector2(830f, 540f);

        Image sImg = scrollGo.GetComponent<Image>();
        sImg.color = new Color(0.04f, 0.04f, 0.07f, 0.85f);
        Mask mask = scrollGo.GetComponent<Mask>();
        mask.showMaskGraphic = true;

        ScrollRect sr = scrollGo.GetComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;

        GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        contentGo.transform.SetParent(scrollGo.transform, false);
        poolGridContent = contentGo.transform;

        RectTransform cRt = contentGo.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0.5f, 1f);
        cRt.anchoredPosition = Vector2.zero;
        cRt.sizeDelta = new Vector2(0f, 600f);

        GridLayoutGroup grid = contentGo.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(150f, 185f);
        grid.spacing = new Vector2(12f, 14f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;

        ContentSizeFitter csf = contentGo.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.content = cRt;
    }

    // =========================================================================
    // 3. RIGHT PANEL: 3 VIÊN XÚC XẮC & 6 Ô MẶT
    // =========================================================================
    private void BuildRightDicePanel(Transform parent)
    {
        GameObject panel = new GameObject("RightPanel_DiceBag", typeof(RectTransform), typeof(Image), typeof(Outline));
        panel.transform.SetParent(parent, false);
        RectTransform pRt = panel.GetComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0.5f, 0.5f);
        pRt.anchorMax = new Vector2(0.5f, 0.5f);
        pRt.anchoredPosition = new Vector2(460f, -25f);
        pRt.sizeDelta = new Vector2(860f, 680f);

        Image pImg = panel.GetComponent<Image>();
        pImg.color = new Color(0.07f, 0.06f, 0.12f, 0.96f);
        Outline pOl = panel.GetComponent<Outline>();
        pOl.effectColor = new Color(0.85f, 0.65f, 0.25f, 0.8f);
        pOl.effectDistance = new Vector2(2f, -2f);

        // 3 Tabs chọn Xúc Xắc trên đỉnh
        GameObject tabsGo = new GameObject("DiceTabs", typeof(RectTransform));
        tabsGo.transform.SetParent(panel.transform, false);
        RectTransform tbRt = tabsGo.GetComponent<RectTransform>();
        tbRt.anchoredPosition = new Vector2(0f, 310f);
        tbRt.sizeDelta = new Vector2(840f, 44f);

        diceTabButtons.Clear();
        diceTabLabels.Clear();

        string[] tabNames = new string[] { "VIÊN 1: CHIẾN BINH", "VIÊN 2: HỘ VỆ", "VIÊN 3: CÂN BẰNG" };
        float[] tabX = new float[] { -280f, 0f, 280f };

        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            GameObject tabBtnGo = new GameObject($"Tab_{i}", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
            tabBtnGo.transform.SetParent(tabsGo.transform, false);
            RectTransform tRt = tabBtnGo.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(tabX[i], 0f);
            tRt.sizeDelta = new Vector2(265f, 40f);

            Image tImg = tabBtnGo.GetComponent<Image>();
            tImg.color = new Color(0.14f, 0.12f, 0.20f, 0.95f);
            Outline tOl = tabBtnGo.GetComponent<Outline>();
            tOl.effectColor = new Color(0.6f, 0.5f, 0.25f, 0.7f);
            tOl.effectDistance = new Vector2(1.2f, -1.2f);

            Button btn = tabBtnGo.GetComponent<Button>();
            btn.targetGraphic = tImg;
            btn.onClick.AddListener(() =>
            {
                selectedDiceIndex = idx;
                RefreshRightDiceUI();
                ShowToast($"Đã chuyển sang {tabNames[idx]}.");
            });

            TextMeshProUGUI tLabel = CreateTMP(tabBtnGo.transform, "Label", Vector2.zero, new Vector2(250f, 34f), 13.5f, Color.white);
            tLabel.text = tabNames[i];
            tLabel.fontStyle = FontStyles.Bold;

            diceTabButtons.Add(btn);
            diceTabLabels.Add(tLabel);
        }

        // Thông tin tóm tắt về Viên Xúc Xắc đang chọn
        dieSummaryTitleText = CreateTMP(panel.transform, "DieTitle", new Vector2(0f, 268f), new Vector2(820f, 28f), 16f, new Color(1f, 0.88f, 0.35f, 1f));
        dieSummaryTitleText.fontStyle = FontStyles.Bold;

        dieSummaryRuleText = CreateTMP(panel.transform, "DieRule", new Vector2(0f, 240f), new Vector2(820f, 24f), 12.5f, new Color(0.85f, 0.88f, 0.95f, 0.9f));

        // Khung hiển thị 6 Ô Mặt Xúc Xắc (2 hàng x 3 cột)
        GameObject slotsGo = new GameObject("DieSlotsGrid", typeof(RectTransform), typeof(GridLayoutGroup));
        slotsGo.transform.SetParent(panel.transform, false);
        dieSlotsContainer = slotsGo.transform;

        RectTransform slRt = slotsGo.GetComponent<RectTransform>();
        slRt.anchoredPosition = new Vector2(0f, -40f);
        slRt.sizeDelta = new Vector2(820f, 480f);

        GridLayoutGroup slGrid = slotsGo.GetComponent<GridLayoutGroup>();
        slGrid.cellSize = new Vector2(250f, 220f);
        slGrid.spacing = new Vector2(24f, 20f);
        slGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        slGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
        slGrid.childAlignment = TextAnchor.MiddleCenter;
        slGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        slGrid.constraintCount = 3;
    }

    // =========================================================================
    // 4. BOTTOM BAR: NÂNG CẤP KỸ NĂNG NHÂN VẬT (GDD 3.2)
    // =========================================================================
    private void BuildBottomSkillBar(Transform parent)
    {
        skillBarGo = new GameObject("SkillForgeBar", typeof(RectTransform), typeof(Image), typeof(Outline));
        skillBarGo.transform.SetParent(parent, false);
        RectTransform bRt = skillBarGo.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0.5f, 0f);
        bRt.anchorMax = new Vector2(0.5f, 0f);
        bRt.anchoredPosition = new Vector2(0f, 32f);
        bRt.sizeDelta = new Vector2(1100f, 48f);

        Image img = skillBarGo.GetComponent<Image>();
        img.color = new Color(0.08f, 0.07f, 0.14f, 0.95f);
        Outline ol = skillBarGo.GetComponent<Outline>();
        ol.effectColor = new Color(0.5f, 0.45f, 0.7f, 0.7f);
        ol.effectDistance = new Vector2(1.2f, -1.2f);

        pendingUpgradesBadgeText = CreateTMP(skillBarGo.transform, "Badge", new Vector2(-380f, 0f), new Vector2(300f, 40f), 13.5f, new Color(1f, 0.85f, 0.35f, 1f));
        pendingUpgradesBadgeText.alignment = TextAlignmentOptions.Left;
        pendingUpgradesBadgeText.fontStyle = FontStyles.Bold;

        // Nút nâng cấp Kỹ Năng Thường
        upgradeNormalSkillBtn = CreateSimpleButton(skillBarGo.transform, "NormalSkillBtn", new Vector2(20f, 0f), new Vector2(290f, 38f),
            new Color(0.12f, 0.32f, 0.20f, 0.95f), new Color(0.35f, 0.85f, 0.5f, 0.9f), out upgradeNormalSkillText);
        upgradeNormalSkillBtn.onClick.AddListener(() =>
        {
            GameManager gm = GameManager.ResolveInstance();
            if (gm != null && gm.pendingForgeUpgrades > 0)
            {
                gm.UpgradeNormalSkill();
                gm.ConsumeForgeUpgrade();
                RefreshAllUI();
                ShowToast($"[CẢI TIẾN] Đã nâng cấp Kỹ năng Thường lên Cấp {gm.normalSkillLevel} (+{gm.GetNormalSkillDamage()} ST)!");
            }
            else
            {
                ShowToast("[CHÚ Ý] Bạn không còn Lượt Rèn khả dụng để nâng cấp Kỹ Năng.");
            }
        });

        // Nút nâng cấp Kỹ Năng Tối Thượng
        upgradeUltimateSkillBtn = CreateSimpleButton(skillBarGo.transform, "UltSkillBtn", new Vector2(340f, 0f), new Vector2(320f, 38f),
            new Color(0.42f, 0.22f, 0.08f, 0.95f), new Color(1f, 0.75f, 0.25f, 0.9f), out upgradeUltimateSkillText);
        upgradeUltimateSkillBtn.onClick.AddListener(() =>
        {
            GameManager gm = GameManager.ResolveInstance();
            if (gm != null && gm.pendingForgeUpgrades > 0)
            {
                gm.UpgradeUltimateSkill();
                gm.ConsumeForgeUpgrade();
                RefreshAllUI();
                ShowToast($"[CẢI TIẾN] Đã nâng cấp Kỹ năng Tối thượng lên Cấp {gm.ultimateSkillLevel} ({gm.GetUltimateSkillDamage()} ST + {gm.GetUltimateSkillBurnStacks()} Cháy)!");
            }
            else
            {
                ShowToast("[CHÚ Ý] Bạn không còn Lượt Rèn khả dụng để nâng cấp Kỹ Năng.");
            }
        });
    }

    // =========================================================================
    // 5. CONFIRMATION MODAL (XÁC NHẬN CƯỜNG HÓA MẶT XÚC XẮC)
    // =========================================================================
    private void BuildConfirmationModal(Transform parent)
    {
        confirmModalGo = new GameObject("ConfirmationModalRoot", typeof(RectTransform), typeof(Image));
        confirmModalGo.transform.SetParent(parent, false);
        confirmModalGo.transform.SetAsLastSibling();

        RectTransform mRt = confirmModalGo.GetComponent<RectTransform>();
        mRt.anchorMin = Vector2.zero;
        mRt.anchorMax = Vector2.one;
        mRt.offsetMin = Vector2.zero;
        mRt.offsetMax = Vector2.zero;

        Image backdrop = confirmModalGo.GetComponent<Image>();
        backdrop.color = new Color(0.02f, 0.02f, 0.04f, 0.88f);

        // Dialog Box
        GameObject dialog = new GameObject("DialogBox", typeof(RectTransform), typeof(Image), typeof(Outline));
        dialog.transform.SetParent(confirmModalGo.transform, false);
        RectTransform dRt = dialog.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0.5f, 0.5f);
        dRt.anchorMax = new Vector2(0.5f, 0.5f);
        dRt.sizeDelta = new Vector2(660f, 380f);
        dRt.anchoredPosition = Vector2.zero;

        Image dImg = dialog.GetComponent<Image>();
        dImg.color = new Color(0.10f, 0.09f, 0.17f, 0.98f);
        Outline dOl = dialog.GetComponent<Outline>();
        dOl.effectColor = new Color(1f, 0.85f, 0.25f, 0.95f);
        dOl.effectDistance = new Vector2(2.5f, -2.5f);

        // Header Title
        TextMeshProUGUI mTitle = CreateTMP(dialog.transform, "Title", new Vector2(0f, 145f), new Vector2(620f, 40f), 22f, new Color(1f, 0.88f, 0.35f, 1f));
        mTitle.text = "XÁC NHẬN CƯỜNG HÓA MẶT XÚC XẮC";
        mTitle.fontStyle = FontStyles.Bold;

        // Body Message
        confirmModalBodyText = CreateTMP(dialog.transform, "Body", new Vector2(0f, 35f), new Vector2(600f, 170f), 15.5f, Color.white);

        // Nút CÓ, CƯỜNG HÓA NGAY
        TextMeshProUGUI yesText;
        confirmModalYesBtn = CreateSimpleButton(dialog.transform, "YesButton", new Vector2(-140f, -135f), new Vector2(250f, 48f),
            new Color(0.14f, 0.42f, 0.22f, 0.98f), new Color(0.35f, 0.95f, 0.55f, 0.95f), out yesText);
        yesText.text = "CÓ, CƯỜNG HÓA NGAY";
        confirmModalYesBtn.onClick.AddListener(OnConfirmMergeYesClicked);

        // Nút HỦY BỎ
        TextMeshProUGUI noText;
        confirmModalNoBtn = CreateSimpleButton(dialog.transform, "NoButton", new Vector2(140f, -135f), new Vector2(250f, 48f),
            new Color(0.42f, 0.16f, 0.16f, 0.98f), new Color(0.95f, 0.45f, 0.45f, 0.95f), out noText);
        noText.text = "HỦY BỎ";
        confirmModalNoBtn.onClick.AddListener(() =>
        {
            confirmModalGo.SetActive(false);
            ShowToast("Đã hủy thao tác kết hợp.");
        });

        confirmModalGo.SetActive(false);
    }

    // =========================================================================
    // 6. REFRESH & POPULATE UI
    // =========================================================================
    public void RefreshAllUI()
    {
        RefreshLeftPoolUI();
        RefreshRightDiceUI();
        RefreshSkillBarUI();
        RefreshCombineActionBar();
    }

    public void RefreshLeftPoolUI()
    {
        foreach (GameObject c in spawnedPoolCardGos)
        {
            if (c != null) Destroy(c);
        }
        spawnedPoolCardGos.Clear();

        GameManager gm = GameManager.ResolveInstance();
        List<DiceFaceData> pool = gm != null ? gm.playerFacePool : null;
        int count = pool != null ? pool.Count : 0;

        if (poolCountText != null)
        {
            poolCountText.text = $"Số lượng: <b><color=#fbbf24>{count} mặt</color></b>";
        }

        if (pool == null || poolGridContent == null) return;

        for (int i = 0; i < pool.Count; i++)
        {
            int poolIdx = i;
            DiceFaceData face = pool[i];
            if (face == null) continue;

            bool isSelectedA = (selectedPoolIndexA == poolIdx);
            bool isSelectedB = (selectedPoolIndexB == poolIdx);

            GameObject cardGo = CreatePoolCardItem(poolGridContent, poolIdx, face, isSelectedA, isSelectedB);
            spawnedPoolCardGos.Add(cardGo);
        }
    }

    private GameObject CreatePoolCardItem(Transform parent, int poolIdx, DiceFaceData face, bool isSelectedA, bool isSelectedB)
    {
        GameObject cardGo = new GameObject($"PoolCard_{poolIdx}", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button), typeof(ForgeDraggableCard));
        cardGo.transform.SetParent(parent, false);

        RectTransform rt = cardGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(150f, 185f);

        Image img = cardGo.GetComponent<Image>();
        img.color = (isSelectedA || isSelectedB) ? new Color(0.18f, 0.14f, 0.28f, 1f) : new Color(0.11f, 0.10f, 0.17f, 0.98f);
        img.raycastTarget = true;

        Color rarityColor = GetRarityColor(face.rarity);
        Outline ol = cardGo.GetComponent<Outline>();

        if (isSelectedA && isSelectedB)
        {
            ol.effectColor = new Color(0.85f, 0.45f, 1f, 1f);
            ol.effectDistance = new Vector2(3f, -3f);
        }
        else if (isSelectedA || isSelectedB)
        {
            ol.effectColor = new Color(1f, 0.88f, 0.25f, 1f);
            ol.effectDistance = new Vector2(3f, -3f);
        }
        else
        {
            ol.effectColor = rarityColor;
            ol.effectDistance = new Vector2(1.5f, -1.5f);
        }

        // Level Badge trên đầu
        GameObject badgeGo = new GameObject("Badge", typeof(RectTransform), typeof(Image));
        badgeGo.transform.SetParent(cardGo.transform, false);
        RectTransform bRt = badgeGo.GetComponent<RectTransform>();
        bRt.anchoredPosition = new Vector2(0f, 74f);
        bRt.sizeDelta = new Vector2(140f, 22f);
        Image bImg = badgeGo.GetComponent<Image>();
        bImg.color = (isSelectedA || isSelectedB) ? new Color(0.35f, 0.25f, 0.08f, 0.95f) : new Color(0.16f, 0.14f, 0.25f, 0.95f);
        bImg.raycastTarget = false;

        TextMeshProUGUI bTmp = CreateTMP(badgeGo.transform, "Text", Vector2.zero, new Vector2(136f, 20f), 11.5f,
            (isSelectedA || isSelectedB) ? new Color(1f, 0.88f, 0.25f, 1f) : rarityColor);

        if (selectedPoolIndexA >= 0 && selectedPoolIndexB >= 0 && (isSelectedA || isSelectedB))
        {
            bTmp.text = "[ GHÉP ĐÔI ]";
        }
        else if (isSelectedA)
        {
            bTmp.text = "[ ĐÃ CHỌN ]";
        }
        else
        {
            bTmp.text = $"{GetRarityLabel(face.rarity)} • CẤP {face.level}";
        }
        bTmp.fontStyle = FontStyles.Bold;

        // Face Name
        TextMeshProUGUI nameTmp = CreateTMP(cardGo.transform, "Name", new Vector2(0f, 44f), new Vector2(140f, 24f), 13.5f, Color.white);
        nameTmp.text = face.faceName;
        nameTmp.fontStyle = FontStyles.Bold;

        // Stat Badge (e.g. +3 Sát Thương, +3 Giáp Ảo)
        Color typeColor = GetFaceTypeColor(face.type);
        TextMeshProUGUI statTmp = CreateTMP(cardGo.transform, "Stat", new Vector2(0f, 16f), new Vector2(140f, 22f), 15f, typeColor);
        statTmp.text = GetStatDisplayString(face);
        statTmp.fontStyle = FontStyles.Bold;

        // Icon minh họa mặt
        Sprite s = GetSpriteForSkill(face.skillKind);
        if (s != null)
        {
            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(cardGo.transform, false);
            RectTransform iRt = iconGo.GetComponent<RectTransform>();
            iRt.anchoredPosition = new Vector2(0f, -16f);
            iRt.sizeDelta = new Vector2(36f, 36f);
            Image iImg = iconGo.GetComponent<Image>();
            iImg.sprite = s;
            iImg.color = typeColor;
            iImg.raycastTarget = false;
        }

        // Mô tả ngắn gọn
        TextMeshProUGUI descTmp = CreateTMP(cardGo.transform, "Desc", new Vector2(0f, -58f), new Vector2(140f, 40f), 10.5f, new Color(0.82f, 0.86f, 0.92f, 0.9f));
        descTmp.text = GetSkillDescription(face.skillKind, face.valueV);

        // Click Event: Bấm vào thẻ trong Pool
        Button btn = cardGo.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() =>
        {
            OnPoolCardClicked(poolIdx);
        });

        // Draggable setup (vẫn hỗ trợ kéo thả nếu muốn)
        ForgeDraggableCard dragCard = cardGo.GetComponent<ForgeDraggableCard>();
        dragCard.location = ForgeDraggableCard.CardLocation.Pool;
        dragCard.poolIndex = poolIdx;
        dragCard.faceData = face;

        return cardGo;
    }

    private void RefreshCombineActionBar()
    {
        if (combineActionBarGo == null) return;

        GameManager gm = GameManager.ResolveInstance();
        List<DiceFaceData> pool = gm != null ? gm.playerFacePool : null;

        bool hasMatchingPair = false;
        DiceFaceData faceA = null;
        DiceFaceData faceB = null;

        if (pool != null && selectedPoolIndexA >= 0 && selectedPoolIndexA < pool.Count &&
            selectedPoolIndexB >= 0 && selectedPoolIndexB < pool.Count && selectedPoolIndexA != selectedPoolIndexB)
        {
            faceA = pool[selectedPoolIndexA];
            faceB = pool[selectedPoolIndexB];
            if (faceA != null && faceB != null && faceA.CanMergeWith(faceB))
            {
                hasMatchingPair = true;
            }
        }

        if (hasMatchingPair && combineExecuteBtn != null && combineExecuteBtnText != null)
        {
            combineExecuteBtn.gameObject.SetActive(true);
            if (combineGuideText != null) combineGuideText.gameObject.SetActive(false);
            combineExecuteBtnText.text = $"KẾT HỢP CƯỜNG HÓA ({faceA.faceName} Cấp {faceA.level} + Cấp {faceB.level} -> Cấp {faceA.level + 1})";
        }
        else
        {
            if (combineExecuteBtn != null) combineExecuteBtn.gameObject.SetActive(false);
            if (combineGuideText != null)
            {
                combineGuideText.gameObject.SetActive(true);
                if (selectedPoolIndexA >= 0 && pool != null && selectedPoolIndexA < pool.Count)
                {
                    DiceFaceData picked = pool[selectedPoolIndexA];
                    combineGuideText.text = $"Đang chọn: <b><color=#fbbf24>{picked.faceName} Cấp {picked.level}</color></b> • Nhấp Ô Xúc Xắc bên phải để gán, hoặc nhấp mặt giống nhau để Kết Hợp";
                }
                else
                {
                    combineGuideText.text = "<i>Nhấp 1 mặt để gán vào xúc xắc • Nhấp 2 mặt giống nhau để KẾT HỢP CƯỜNG HÓA</i>";
                }
            }
        }
    }

    public void RefreshRightDiceUI()
    {
        // 1. Cập nhật Tab highlight
        for (int i = 0; i < diceTabButtons.Count; i++)
        {
            bool isCurrent = (i == selectedDiceIndex);
            Image img = diceTabButtons[i].GetComponent<Image>();
            Outline ol = diceTabButtons[i].GetComponent<Outline>();
            if (img != null) img.color = isCurrent ? new Color(0.38f, 0.28f, 0.10f, 1f) : new Color(0.12f, 0.10f, 0.18f, 0.92f);
            if (ol != null) ol.effectColor = isCurrent ? new Color(1f, 0.88f, 0.25f, 1f) : new Color(0.45f, 0.40f, 0.30f, 0.6f);
            if (i < diceTabLabels.Count && diceTabLabels[i] != null)
            {
                diceTabLabels[i].color = isCurrent ? new Color(1f, 0.92f, 0.45f, 1f) : new Color(0.75f, 0.75f, 0.85f, 0.9f);
            }
        }

        // 2. Lấy DiceInstance tương ứng
        GameManager gm = GameManager.ResolveInstance();
        DiceInstance curDie = (gm != null && selectedDiceIndex >= 0 && selectedDiceIndex < gm.playerDiceBag.Count)
            ? gm.playerDiceBag[selectedDiceIndex]
            : null;

        // Xóa các ô cũ
        foreach (GameObject s in spawnedDieSlotGos)
        {
            if (s != null) Destroy(s);
        }
        spawnedDieSlotGos.Clear();

        if (curDie == null || dieSlotsContainer == null) return;

        int unlockedCount = curDie.GetUnlockedSlotCount();
        if (dieSummaryTitleText != null)
        {
            dieSummaryTitleText.text = $"<b>{curDie.diceName}</b> • <color=#4ade80>{unlockedCount}/6 Mặt khả dụng</color> ({(6 - unlockedCount)} Ô khóa)";
        }

        if (dieSummaryRuleText != null)
        {
            int cStrike = curDie.CountFaceKind(DiceSkillKind.Strike);
            int cBlock = curDie.CountFaceKind(DiceSkillKind.Block);
            int cHeal = curDie.CountFaceKind(DiceSkillKind.Regenerate);
            int cEnergy = curDie.CountFaceKind(DiceSkillKind.ManaSpark);

            int tStrike = gm != null ? gm.CountTotalFaceKindAcrossAllDice(DiceSkillKind.Strike) : 0;
            int tBlock = gm != null ? gm.CountTotalFaceKindAcrossAllDice(DiceSkillKind.Block) : 0;
            int tHeal = gm != null ? gm.CountTotalFaceKindAcrossAllDice(DiceSkillKind.Regenerate) : 0;
            int tEnergy = gm != null ? gm.CountTotalFaceKindAcrossAllDice(DiceSkillKind.ManaSpark) : 0;

            dieSummaryRuleText.text = $"Viên này (tối đa 2): Công {cStrike}/2 • Thủ {cBlock}/2 • Hồi {cHeal}/2 • NL {cEnergy}/2 | Toàn bộ (tối đa 4): Công {tStrike}/4 • Thủ {tBlock}/4 • Hồi {tHeal}/4 • NL {tEnergy}/4";
        }

        // 3. Tạo 6 Ô Mặt Xúc Xắc
        for (int s = 0; s < DiceInstance.FACES_COUNT; s++)
        {
            int slotIdx = s;
            bool isLocked = curDie.IsSlotLocked(s);
            DiceFaceData face = curDie.faces[s];

            GameObject slotGo = CreateDieSlotItem(dieSlotsContainer, selectedDiceIndex, slotIdx, isLocked, face);
            spawnedDieSlotGos.Add(slotGo);
        }
    }

    private GameObject CreateDieSlotItem(Transform parent, int dieIdx, int slotIdx, bool isLocked, DiceFaceData face)
    {
        GameObject slotGo = new GameObject($"DieSlot_{slotIdx}", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button), typeof(ForgeDraggableCard));
        slotGo.transform.SetParent(parent, false);

        RectTransform rt = slotGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(250f, 220f);

        Image img = slotGo.GetComponent<Image>();
        img.raycastTarget = true;
        Outline ol = slotGo.GetComponent<Outline>();

        ForgeDraggableCard dragCard = slotGo.GetComponent<ForgeDraggableCard>();
        dragCard.location = ForgeDraggableCard.CardLocation.DieSlot;
        dragCard.dieIndex = dieIdx;
        dragCard.slotIndex = slotIdx;
        dragCard.faceData = face;
        dragCard.isLockedSlot = isLocked;

        // Button click event: Bấm vào ô xúc xắc để gán
        Button btn = slotGo.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() =>
        {
            OnDieSlotClicked(dieIdx, slotIdx);
        });

        // Header nhỏ hiển thị Ô #
        GameObject slotHeader = new GameObject("SlotHeader", typeof(RectTransform), typeof(Image));
        slotHeader.transform.SetParent(slotGo.transform, false);
        RectTransform shRt = slotHeader.GetComponent<RectTransform>();
        shRt.anchoredPosition = new Vector2(0f, 92f);
        shRt.sizeDelta = new Vector2(236f, 24f);
        Image shImg = slotHeader.GetComponent<Image>();
        shImg.raycastTarget = false;

        if (isLocked)
        {
            // CASE A: Ô ĐANG KHÓA
            img.color = new Color(0.05f, 0.05f, 0.07f, 0.95f);
            ol.effectColor = new Color(0.3f, 0.25f, 0.25f, 0.7f);
            ol.effectDistance = new Vector2(1.5f, -1.5f);

            shImg.color = new Color(0.12f, 0.08f, 0.08f, 0.9f);
            TextMeshProUGUI shText = CreateTMP(slotHeader.transform, "Text", Vector2.zero, new Vector2(230f, 22f), 12f, new Color(0.9f, 0.4f, 0.4f, 1f));
            shText.text = $"Ô #{slotIdx + 1} • [ ĐANG KHÓA ]";
            shText.fontStyle = FontStyles.Bold;

            TextMeshProUGUI lockTitle = CreateTMP(slotGo.transform, "LockTitle", new Vector2(0f, 25f), new Vector2(230f, 36f), 20f, new Color(0.7f, 0.35f, 0.35f, 1f));
            lockTitle.text = "[ KHÓA ]";
            lockTitle.fontStyle = FontStyles.Bold;

            TextMeshProUGUI lockDesc = CreateTMP(slotGo.transform, "LockDesc", new Vector2(0f, -25f), new Vector2(220f, 50f), 12.5f, new Color(0.65f, 0.65f, 0.65f, 0.9f));
            lockDesc.text = "Đánh thắng thêm Ải trên Bản Đồ để mở khóa ô này!";
        }
        else if (face == null || face.skillKind == DiceSkillKind.Blank)
        {
            // CASE B: Ô MẶT TRỐNG ĐÃ MỞ KHÓA
            img.color = new Color(0.08f, 0.10f, 0.16f, 0.95f);
            ol.effectColor = new Color(0.35f, 0.85f, 0.6f, 0.85f);
            ol.effectDistance = new Vector2(2f, -2f);

            shImg.color = new Color(0.10f, 0.22f, 0.16f, 0.9f);
            TextMeshProUGUI shText = CreateTMP(slotHeader.transform, "Text", Vector2.zero, new Vector2(230f, 22f), 12f, new Color(0.45f, 1f, 0.65f, 1f));
            shText.text = $"Ô #{slotIdx + 1} • SẴN SÀNG";
            shText.fontStyle = FontStyles.Bold;

            TextMeshProUGUI blankTitle = CreateTMP(slotGo.transform, "BlankTitle", new Vector2(0f, 30f), new Vector2(230f, 32f), 17f, new Color(0.4f, 1f, 0.6f, 1f));
            blankTitle.text = "[ MẶT TRỐNG ]";
            blankTitle.fontStyle = FontStyles.Bold;

            TextMeshProUGUI blankDesc = CreateTMP(slotGo.transform, "BlankDesc", new Vector2(0f, -20f), new Vector2(220f, 50f), 12.5f, new Color(0.85f, 0.92f, 1f, 0.9f));
            blankDesc.text = "Nhấp chọn mặt trong Túi rồi nhấp vào đây để ghi đè!";
        }
        else
        {
            // CASE C: Ô ĐANG CÓ MẶT HOẠT ĐỘNG
            img.color = new Color(0.11f, 0.10f, 0.18f, 0.98f);
            Color rarityColor = GetRarityColor(face.rarity);
            ol.effectColor = rarityColor;
            ol.effectDistance = new Vector2(2f, -2f);

            shImg.color = new Color(0.16f, 0.14f, 0.25f, 0.95f);
            TextMeshProUGUI shText = CreateTMP(slotHeader.transform, "Text", Vector2.zero, new Vector2(230f, 22f), 12f, rarityColor);
            shText.text = $"Ô #{slotIdx + 1} • {GetRarityLabel(face.rarity)} • CẤP {face.level}";
            shText.fontStyle = FontStyles.Bold;

            // Tên mặt
            TextMeshProUGUI nameTmp = CreateTMP(slotGo.transform, "Name", new Vector2(0f, 55f), new Vector2(230f, 28f), 16f, Color.white);
            nameTmp.text = face.faceName;
            nameTmp.fontStyle = FontStyles.Bold;

            // Stat
            Color typeColor = GetFaceTypeColor(face.type);
            TextMeshProUGUI statTmp = CreateTMP(slotGo.transform, "Stat", new Vector2(0f, 24f), new Vector2(230f, 26f), 17f, typeColor);
            statTmp.text = GetStatDisplayString(face);
            statTmp.fontStyle = FontStyles.Bold;

            // Icon
            Sprite s = GetSpriteForSkill(face.skillKind);
            if (s != null)
            {
                GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(slotGo.transform, false);
                RectTransform iRt = iconGo.GetComponent<RectTransform>();
                iRt.anchoredPosition = new Vector2(0f, -14f);
                iRt.sizeDelta = new Vector2(40f, 40f);
                Image iImg = iconGo.GetComponent<Image>();
                iImg.sprite = s;
                iImg.color = typeColor;
                iImg.raycastTarget = false;
            }

            // Mô tả
            TextMeshProUGUI descTmp = CreateTMP(slotGo.transform, "Desc", new Vector2(0f, -62f), new Vector2(230f, 44f), 11.5f, new Color(0.82f, 0.86f, 0.92f, 0.9f));
            descTmp.text = GetSkillDescription(face.skillKind, face.valueV);
        }

        return slotGo;
    }

    private void RefreshSkillBarUI()
    {
        GameManager gm = GameManager.ResolveInstance();
        int pending = gm != null ? gm.pendingForgeUpgrades : 0;
        int normLvl = gm != null ? gm.normalSkillLevel : 1;
        int ultLvl = gm != null ? gm.ultimateSkillLevel : 1;
        int normDmg = gm != null ? gm.GetNormalSkillDamage() : 5;
        int ultDmg = gm != null ? gm.GetUltimateSkillDamage() : 14;
        int ultBurn = gm != null ? gm.GetUltimateSkillBurnStacks() : 4;

        if (pendingUpgradesBadgeText != null)
        {
            pendingUpgradesBadgeText.text = $"Lượt Rèn Kỹ Năng còn lại: <b><color=#fbbf24>{pending}</color></b>";
        }

        if (upgradeNormalSkillText != null)
        {
            upgradeNormalSkillText.text = $"Nâng Skill Thường (Lv.{normLvl} -> Lv.{normLvl + 1}: {normDmg + 3} ST)";
        }

        if (upgradeUltimateSkillText != null)
        {
            upgradeUltimateSkillText.text = $"Nâng Skill Tối Thượng (Lv.{ultLvl} -> Lv.{ultLvl + 1}: {ultDmg + 4} ST + {ultBurn + 2} Cháy)";
        }

        bool canUpgrade = pending > 0;
        if (upgradeNormalSkillBtn != null) upgradeNormalSkillBtn.interactable = canUpgrade;
        if (upgradeUltimateSkillBtn != null) upgradeUltimateSkillBtn.interactable = canUpgrade;
    }

    // =========================================================================
    // 7. XỬ LÝ CLICK: CHỌN MẶT TRONG POOL & GÁN LÊN XÚC XẮC
    // =========================================================================
    public void OnPoolCardClicked(int clickedIdx)
    {
        GameManager gm = GameManager.ResolveInstance();
        List<DiceFaceData> pool = gm != null ? gm.playerFacePool : null;
        if (pool == null || clickedIdx < 0 || clickedIdx >= pool.Count) return;

        DiceFaceData clickedFace = pool[clickedIdx];
        if (clickedFace == null) return;

        // CASE 1: Chưa chọn mặt nào -> Chọn mặt thứ nhất
        if (selectedPoolIndexA < 0)
        {
            selectedPoolIndexA = clickedIdx;
            selectedPoolIndexB = -1;
            RefreshLeftPoolUI();
            RefreshCombineActionBar();
            ShowToast($"Đã chọn <b>{clickedFace.faceName} Cấp {clickedFace.level}</b>. Hãy nhấp Ô Xúc Xắc bên phải để gán, hoặc nhấp mặt giống nhau để Kết Hợp.");
            return;
        }

        // CASE 2: Bấm lại chính mặt thứ nhất -> Bỏ chọn
        if (selectedPoolIndexA == clickedIdx)
        {
            selectedPoolIndexA = -1;
            selectedPoolIndexB = -1;
            RefreshLeftPoolUI();
            RefreshCombineActionBar();
            ShowToast("Đã hủy chọn mặt xúc xắc.");
            return;
        }

        // CASE 3: Bấm lại chính mặt thứ hai -> Bỏ chọn mặt thứ hai
        if (selectedPoolIndexB == clickedIdx)
        {
            selectedPoolIndexB = -1;
            RefreshLeftPoolUI();
            RefreshCombineActionBar();
            ShowToast($"Đã hủy chọn mặt thứ hai. Vẫn đang chọn <b>{pool[selectedPoolIndexA].faceName} Cấp {pool[selectedPoolIndexA].level}</b>.");
            return;
        }

        // CASE 4: Đã có mặt thứ nhất, bấm mặt khác -> Kiểm tra có trùng loại và cấp để kết hợp không
        DiceFaceData faceA = pool[selectedPoolIndexA];
        if (faceA != null && faceA.CanMergeWith(clickedFace))
        {
            // Trùng loại & cùng cấp -> Chọn thành cặp kết hợp!
            selectedPoolIndexB = clickedIdx;
            RefreshLeftPoolUI();
            RefreshCombineActionBar();
            ShowToast($"Đã chọn 2 mặt <b>{faceA.faceName} Cấp {faceA.level}</b>. Hãy nhấn nút <b>[ KẾT HỢP ]</b> bên dưới để Cường Hóa!");
        }
        else
        {
            // Không cùng loại / khác cấp -> Chuyển sang chọn mặt mới này làm mặt đơn để gán
            selectedPoolIndexA = clickedIdx;
            selectedPoolIndexB = -1;
            RefreshLeftPoolUI();
            RefreshCombineActionBar();
            ShowToast($"Đã chuyển sang chọn <b>{clickedFace.faceName} Cấp {clickedFace.level}</b>. Nhấp vào Ô Xúc Xắc bên phải để gán.");
        }
    }

    public void OnDieSlotClicked(int dieIdx, int slotIdx)
    {
        GameManager gm = GameManager.ResolveInstance();
        if (gm == null) return;

        // Nếu đã có mặt được chọn trong Pool -> Thực hiện gán!
        if (selectedPoolIndexA >= 0 && selectedPoolIndexA < gm.playerFacePool.Count)
        {
            DiceFaceData faceToEquip = gm.playerFacePool[selectedPoolIndexA];
            bool success = gm.EquipFaceFromPoolToDie(selectedPoolIndexA, dieIdx, slotIdx, out string errorMessage);

            if (success)
            {
                ShowToast($"[GÁN THÀNH CÔNG] Đã gán <b>{faceToEquip.faceName} Cấp {faceToEquip.level}</b> vào Ô #{slotIdx + 1}! Mặt trong Pool vẫn được giữ nguyên.");
                RefreshRightDiceUI();
            }
            else
            {
                ShowToast($"[KHÔNG THỂ GÁN] {errorMessage}");
            }
            return;
        }

        // Nếu chưa chọn mặt nào trong Pool -> Hiển thị thông tin ô
        if (dieIdx >= 0 && dieIdx < gm.playerDiceBag.Count)
        {
            DiceInstance die = gm.playerDiceBag[dieIdx];
            if (die.IsSlotLocked(slotIdx))
            {
                ShowToast("[KHÓA] Ô này đang bị khóa! Vượt thêm Ải trên Bản Đồ để mở khóa.");
            }
            else if (die.faces[slotIdx] != null && die.faces[slotIdx].skillKind != DiceSkillKind.Blank)
            {
                ShowToast($"Ô #{slotIdx + 1}: <b>{die.faces[slotIdx].faceName} Cấp {die.faces[slotIdx].level}</b>. Hãy chọn 1 mặt trong Túi bên trái rồi nhấp vào đây để thay thế.");
            }
            else
            {
                ShowToast($"Ô #{slotIdx + 1} đang trống. Hãy chọn 1 mặt trong Túi bên trái rồi nhấp vào đây để gán.");
            }
        }
    }

    // =========================================================================
    // 8. XỬ LÝ NÚT KẾT HỢP & HỘP THOẠI XÁC NHẬN CƯỜNG HÓA
    // =========================================================================
    private void OnCombineExecuteButtonClicked()
    {
        GameManager gm = GameManager.ResolveInstance();
        List<DiceFaceData> pool = gm != null ? gm.playerFacePool : null;
        if (pool == null || selectedPoolIndexA < 0 || selectedPoolIndexB < 0) return;

        DiceFaceData faceA = pool[selectedPoolIndexA];
        DiceFaceData faceB = pool[selectedPoolIndexB];
        if (faceA == null || faceB == null || !faceA.CanMergeWith(faceB))
        {
            ShowToast("[CHÚ Ý] Chỉ có thể kết hợp 2 mặt CÙNG LOẠI và CÙNG CẤP!");
            return;
        }

        DiceFaceData previewUpgraded = DiceFaceData.CreateUpgradedFace(faceA, faceB);

        // Hiển thị hộp thoại hỏi người chơi
        if (confirmModalGo != null && confirmModalBodyText != null)
        {
            confirmModalBodyText.text =
                $"<b>Bạn có muốn cường hoá mặt xúc xắc này không?</b>\n\n" +
                $"Gộp 2 mặt: <b>{faceA.faceName} Cấp {faceA.level}</b>\n" +
                $"---> Tạo thành: <b><color=#fbbf24>{previewUpgraded.faceName} Cấp {previewUpgraded.level}</color></b> (+{previewUpgraded.valueV})\n\n" +
                $"<i>(Lưu ý: Tất cả các mặt '{faceA.faceName} Cấp {faceA.level}' đang gắn trên cả 3 viên xúc xắc sẽ <b>tự động thay thế thành mặt Cấp {previewUpgraded.level}</b> mới này!)</i>";

            confirmModalGo.SetActive(true);
        }
    }

    private void OnConfirmMergeYesClicked()
    {
        GameManager gm = GameManager.ResolveInstance();
        if (gm == null) return;

        bool merged = gm.MergeFacesInPoolAndUpgradeDice(selectedPoolIndexA, selectedPoolIndexB, out DiceFaceData upgradedFace, out int autoUpgradedCount);

        if (confirmModalGo != null)
        {
            confirmModalGo.SetActive(false);
        }

        if (merged && upgradedFace != null)
        {
            selectedPoolIndexA = -1;
            selectedPoolIndexB = -1;
            RefreshAllUI();
            ShowToast($"[CƯỜNG HÓA THÀNH CÔNG] Đã tạo <b>{upgradedFace.faceName} Cấp {upgradedFace.level}</b> (+{upgradedFace.valueV})! Tự động nâng cấp {autoUpgradedCount} mặt trên các viên xúc xắc.");
        }
        else
        {
            ShowToast("[CHÚ Ý] Không thể cường hóa mặt xúc xắc!");
        }
    }

    // =========================================================================
    // 9. HỖ TRỢ KÉO THẢ (DRAG & DROP) TƯƠNG THÍCH ĐỒNG BỘ
    // =========================================================================
    public void HandleCardDrop(ForgeDraggableCard source, ForgeDraggableCard target)
    {
        if (source == null || target == null) return;
        GameManager gm = GameManager.ResolveInstance();
        if (gm == null) return;

        // Kéo thẻ trong Pool thả vào Ô Xúc Xắc
        if (source.location == ForgeDraggableCard.CardLocation.Pool && target.location == ForgeDraggableCard.CardLocation.DieSlot)
        {
            bool success = gm.EquipFaceFromPoolToDie(source.poolIndex, target.dieIndex, target.slotIndex, out string errorMsg);
            if (success)
            {
                source.dropSuccessful = true;
                ShowToast($"[GÁN THÀNH CÔNG] Đã gán mặt vào Ô #{target.slotIndex + 1}! Mặt trong Pool vẫn được giữ nguyên.");
                RefreshRightDiceUI();
            }
            else
            {
                ShowToast($"[KHÔNG THỂ GÁN] {errorMsg}");
            }
            return;
        }

        // Kéo thẻ trong Pool thả vào thẻ khác trong Pool để Cường Hóa
        if (source.location == ForgeDraggableCard.CardLocation.Pool && target.location == ForgeDraggableCard.CardLocation.Pool)
        {
            if (source.poolIndex != target.poolIndex)
            {
                selectedPoolIndexA = source.poolIndex;
                selectedPoolIndexB = target.poolIndex;
                RefreshLeftPoolUI();
                RefreshCombineActionBar();
                OnCombineExecuteButtonClicked();
            }
        }
    }

    public void ShowToast(string message)
    {
        if (statusToastText == null) return;
        statusToastText.text = message;

        if (statusToastCoroutine != null)
        {
            StopCoroutine(statusToastCoroutine);
        }
        statusToastCoroutine = StartCoroutine(ToastFadeRoutine());
    }

    private IEnumerator ToastFadeRoutine()
    {
        yield return new WaitForSeconds(6f);
        if (statusToastText != null)
        {
            statusToastText.text = "Nhấp 1 mặt trong Túi rồi nhấp Ô Xúc Xắc để Gán • Nhấp 2 mặt giống nhau để Kết Hợp Cường Hóa.";
        }
    }

    // =========================================================================
    // 10. HELPER UI METHODS
    // =========================================================================
    private Color GetRarityColor(DiceFaceRarity rarity)
    {
        switch (rarity)
        {
            case DiceFaceRarity.Epic: return new Color(0.85f, 0.45f, 1f, 1f);
            case DiceFaceRarity.Rare: return new Color(0.35f, 0.78f, 1f, 1f);
            default:                  return new Color(0.75f, 0.80f, 0.85f, 0.9f);
        }
    }

    private string GetRarityLabel(DiceFaceRarity rarity)
    {
        switch (rarity)
        {
            case DiceFaceRarity.Epic: return "SỬ THI";
            case DiceFaceRarity.Rare: return "HIẾM";
            default:                  return "THƯỜNG";
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

    private Sprite GetSpriteForSkill(DiceSkillKind kind)
    {
        switch (kind)
        {
            case DiceSkillKind.Strike:
            case DiceSkillKind.HeavySlash:
            case DiceSkillKind.Vampiric:
                return diceFaceAttackSprite;
            case DiceSkillKind.Block:
            case DiceSkillKind.IronWall:
                return diceFaceShieldSprite;
            case DiceSkillKind.Regenerate:
                return diceFaceHealSprite;
            case DiceSkillKind.ManaSpark:
            case DiceSkillKind.ArcaneSurge:
                return diceFaceEnergySprite;
            case DiceSkillKind.PoisonSting:
                return diceFacePoisonSprite;
            case DiceSkillKind.Ignite:
                return diceFaceFireSprite;
            case DiceSkillKind.ShockStun:
                return diceFaceLightningSprite;
            default:
                return diceFaceBlankSprite;
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
                return $"+{face.valueV} Độc Tố";
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
            case DiceSkillKind.Strike:      return $"Gây {val} ST vật lý trực tiếp.";
            case DiceSkillKind.Block:       return $"Tạo {val} điểm Giáp ảo.";
            case DiceSkillKind.Regenerate:  return $"Hồi phục {val} HP lập tức.";
            case DiceSkillKind.ManaSpark:   return $"Tích lũy {val} Năng Lượng.";
            case DiceSkillKind.HeavySlash:  return $"Chém mạnh gây {val} ST.";
            case DiceSkillKind.IronWall:    return $"Phòng thủ kiên cố: +{val} Giáp.";
            case DiceSkillKind.Vampiric:    return $"Gây {val} ST và hồi 50% HP.";
            case DiceSkillKind.PoisonSting: return $"Gây {val} ST và {val} tầng Độc.";
            case DiceSkillKind.Ignite:      return $"Gây {val} ST và {val} tầng Thiêu Đốt.";
            case DiceSkillKind.ShockStun:   return $"Gây {val} ST và làm choáng.";
            case DiceSkillKind.ArcaneSurge: return $"Bộc phá ma pháp: +{val} NL.";
            default:                        return "Mặt xúc xắc chiến thuật.";
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

    private Button CreateSimpleButton(
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
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        labelTmp = CreateTMP(go.transform, "Label", Vector2.zero, size - new Vector2(10f, 6f), 13.5f, Color.white);
        labelTmp.fontStyle = FontStyles.Bold;
        return btn;
    }
}

/// <summary>
/// ForgeDraggableCard — Thành phần hỗ trợ Kéo-Thả (Drag & Drop) và Click chọn:
/// </summary>
public class ForgeDraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public enum CardLocation
    {
        Pool,
        DieSlot
    }

    public CardLocation location;
    public int poolIndex = -1;
    public int dieIndex = -1;
    public int slotIndex = -1;
    public DiceFaceData faceData;
    public bool isLockedSlot = false;

    [System.NonSerialized] public bool dropSuccessful = false;

    private Transform originalParent;
    private int originalSiblingIndex;
    private GameObject placeholder;
    private CanvasGroup canvasGroup;
    private Outline cardOutline;
    private Color defaultOutlineColor = Color.white;
    private RectTransform rectTransform;

    private static ForgeDraggableCard currentlyDraggedCard;
    public static ForgeDraggableCard CurrentlyDragged => currentlyDraggedCard;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        cardOutline = GetComponent<Outline>();
        if (cardOutline != null)
        {
            defaultOutlineColor = cardOutline.effectColor;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (location == CardLocation.DieSlot) return; // Chỉ kéo từ Pool
        if (faceData == null || faceData.skillKind == DiceSkillKind.Blank) return;

        currentlyDraggedCard = this;
        dropSuccessful = false;

        ForgeController controller = ForgeController.Instance;
        Canvas canvas = controller != null ? controller.RootCanvas : GetComponentInParent<Canvas>();
        if (canvas == null) return;

        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        // Tạo 1 ô placeholder mờ giữ chỗ trong Grid Pool
        placeholder = new GameObject("DragPlaceholder", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
        placeholder.transform.SetParent(originalParent, false);
        placeholder.transform.SetSiblingIndex(originalSiblingIndex);

        RectTransform pRt = placeholder.GetComponent<RectTransform>();
        pRt.sizeDelta = rectTransform.sizeDelta;

        LayoutElement le = placeholder.GetComponent<LayoutElement>();
        le.preferredWidth = rectTransform.sizeDelta.x;
        le.preferredHeight = rectTransform.sizeDelta.y;

        Image pImg = placeholder.GetComponent<Image>();
        pImg.color = new Color(0.12f, 0.10f, 0.20f, 0.45f);
        pImg.raycastTarget = false;

        Outline pOl = placeholder.AddComponent<Outline>();
        pOl.effectColor = new Color(0.45f, 0.45f, 0.70f, 0.5f);
        pOl.effectDistance = new Vector2(1.5f, -1.5f);

        // Đưa thẻ lên Canvas root để di chuyển tự do trên mọi layer
        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.94f;
        transform.localScale = Vector3.one * 1.06f;

        if (cardOutline != null)
        {
            cardOutline.effectColor = new Color(1f, 0.88f, 0.25f, 1f);
            cardOutline.effectDistance = new Vector2(3f, -3f);
        }

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentlyDraggedCard != this) return;

        ForgeController controller = ForgeController.Instance;
        Canvas canvas = controller != null ? controller.RootCanvas : GetComponentInParent<Canvas>();
        if (canvas == null) return;

        RectTransform canvasRt = canvas.transform as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, eventData.position, canvas.worldCamera, out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (currentlyDraggedCard != this) return;

        // Cho dù thả thành công hay không, thẻ trong Pool luôn quay về vị trí ban đầu (vì thẻ trong pool không biến mất)
        if (placeholder != null)
        {
            Destroy(placeholder);
            placeholder = null;
        }

        transform.SetParent(originalParent, false);
        transform.SetSiblingIndex(originalSiblingIndex);
        transform.localScale = Vector3.one;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        if (cardOutline != null)
        {
            cardOutline.effectColor = defaultOutlineColor;
            cardOutline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        currentlyDraggedCard = null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (currentlyDraggedCard == null) return;
        ForgeController controller = ForgeController.Instance;
        if (controller == null) return;

        controller.HandleCardDrop(currentlyDraggedCard, this);
    }
}
