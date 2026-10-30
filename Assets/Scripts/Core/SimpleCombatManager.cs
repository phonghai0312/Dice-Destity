using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// SimpleCombatManager — Điều phối toàn bộ vòng lặp chiến đấu & Giao diện Chiến thuật theo GDD:
/// - Máy trạng thái 5 pha (RoundStart -> PlayerRoll -> PlayerAction -> EnemyExecution -> RoundEnd)
/// - Hệ thống 3 viên xúc xắc 6 mặt từ GameManager.playerDiceBag + Strategy Pattern (IDiceAction)
/// - Hệ thống Hoạt ảnh Hành động Nhân vật riêng biệt cho từng loại Xúc xắc (Chém, Trọng kích, Hút máu,
///   Gồng hồi máu, Thanh tẩy, Dựng khiên, Bất khuất, Đâm độc, Giáng sấm choáng, Gồng cuồng nộ)
/// - Bố cục tổng quát 4 Phân khu (Top Header, Battlefield Arena với HUD dưới chân nhân vật,
///   Tactical Tooltip Bar, và Khay Xúc Xắc 3 cột cân đối).
/// </summary>
public class SimpleCombatManager : MonoBehaviour
{
    public enum CombatPhase
    {
        RoundStart,
        PlayerRoll,
        PlayerAction,
        EnemyExecution,
        RoundEnd,
        CombatOver
    }

    [Header("Entities")]
    public BattleEntity playerEntity;
    public BattleEntity enemyEntity;

    [Header("HP Bar Fill Images")]
    public Image playerHPFill;  // anchorMax.x = currentHP/maxHP
    public Image enemyHPFill;

    [Header("HP Label Text")]
    public Component playerHPLabel;   // TMPro
    public Component enemyHPLabel;

    [Header("Enemy Label")]
    public Component enemyNameLabel;
    public Image backgroundImage;
    public Image enemyPortraitImage;
    public Image playerPortraitImage;

    [Header("Dice Slots")]
    public List<GameObject> diceSlots = new List<GameObject>();

    [Header("Buttons")]
    public Button endTurnButton;
    public Button rerollButton;
    public Button undoButton;

    [Header("Dice Face Sprites (Assets/GameAssets)")]
    public Sprite diceFaceAttackSprite;
    public Sprite diceFaceShieldSprite;
    public Sprite diceFacePoisonSprite;
    public Sprite diceFaceFireSprite;
    public Sprite diceFaceEnergySprite;
    public Sprite diceFaceHealSprite;
    public Sprite diceFaceLightningSprite;
    public Sprite diceFaceBlankSprite;

    [Header("Character Active Skills (Normal & Ultimate)")]
    public int normalSkillBaseCost = 2;
    public int normalSkillDamage = 5;
    public int ultimateSkillBaseCost = 6;
    public int ultimateSkillDamage = 14;
    public int ultimateSkillBurnStacks = 4;

    [Header("Runtime Combat State")]
    public CombatPhase currentPhase = CombatPhase.RoundStart;
    public int currentRound = 0;

    private int rerollsLeft = 1;
    private int selectedDiceTotal = 0;
    private readonly List<int> selectedDiceIndices = new List<int>();
    private readonly List<DiceFaceData> rolledFaces = new List<DiceFaceData>();
    private readonly List<bool> diceUsed = new List<bool>();
    private readonly List<Color> slotBaseColors = new List<Color>();

    private EnemyData currentEnemyData;
    private EnemyIntentStep currentEnemyIntent;
    private int currentStage = 1;
    private CommandManager commandManager;

    private System.Type tmpType;
    private bool isAnimating = false;
    private bool combatEnded = false;

    // Runtime UI Elements
    private Component playerStatusLabel;
    private Component enemyStatusLabel;
    private Component enemyIntentLabel;
    private Image enemyIntentBgImage;
    private Outline enemyIntentOutline;
    private Component tooltipLabel;
    private GameObject tooltipPanel;
    private Component stageProgressLabel;
    private Component trayGuideLabel;
    private Image playerTargetRing;
    private Image enemyTargetRing;

    // Character Skill & Energy Bar UI Elements
    private CharacterSkillBarUI characterSkillBarUI;
    private Component energyStatusLabel;
    private Button normalSkillButton;
    private Button ultimateSkillButton;

    private static Sprite cachedSoftOvalSprite;

    private void EnsureDiceSpritesLoaded()
    {
#if UNITY_EDITOR
        if (diceFaceAttackSprite == null)
            diceFaceAttackSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Dice_Face_Attack.png");
        if (diceFaceShieldSprite == null)
            diceFaceShieldSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Dice_Face_Shield.png");
        if (diceFacePoisonSprite == null)
            diceFacePoisonSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Dice_Face_Poison.png");
        if (diceFaceFireSprite == null)
            diceFaceFireSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Dice_Face_Fire.png");
        if (diceFaceEnergySprite == null)
            diceFaceEnergySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Dice_Face_Energy.png");
        if (diceFaceHealSprite == null)
            diceFaceHealSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Dice_Face_Heal.png");
        if (diceFaceLightningSprite == null)
            diceFaceLightningSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Dice_Face_Lightning.png");
        if (diceFaceBlankSprite == null)
            diceFaceBlankSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Dice_Face_Blank.png");
#endif
    }
    private void Start()
    {
        GameManager.ResolveInstance();
        EnsureDiceSpritesLoaded();
        tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");

        if (Camera.main != null && Camera.main.GetComponent<CameraShake>() == null)
        {
            Camera.main.gameObject.AddComponent<CameraShake>();
        }

        commandManager = FindAnyObjectByType<CommandManager>();
        if (commandManager == null)
        {
            GameObject cmdObj = new GameObject("CommandManager");
            commandManager = cmdObj.AddComponent<CommandManager>();
        }

        if (playerPortraitImage == null && playerEntity != null)
        {
            Transform pImg = playerEntity.transform.Find("PlayerSprite");
            if (pImg != null) playerPortraitImage = pImg.GetComponent<Image>();
        }

        if (enemyPortraitImage == null && enemyEntity != null)
        {
            Transform eImg = enemyEntity.transform.Find("EnemySprite");
            if (eImg != null) enemyPortraitImage = eImg.GetComponent<Image>();
        }

        // Thiết kế lại bố cục tổng quát của CombatScene trước khi gắn Animator để lấy đúng tọa độ chuẩn
        ApplyRedesignedCombatLayout();
        EnsureDropTargetsConfigured();
        EnsureRuntimeCombatOverlays();

        // Gắn và làm mới gốc tọa độ cho CharacterSpriteAnimator
        if (playerPortraitImage != null)
        {
            playerPortraitImage.preserveAspect = true;
            CharacterSpriteAnimator pAnim = playerPortraitImage.GetComponent<CharacterSpriteAnimator>();
            if (pAnim == null) pAnim = playerPortraitImage.gameObject.AddComponent<CharacterSpriteAnimator>();
            pAnim.RefreshOriginTransform();
        }
        if (enemyPortraitImage != null)
        {
            enemyPortraitImage.preserveAspect = true;
            CharacterSpriteAnimator eAnim = enemyPortraitImage.GetComponent<CharacterSpriteAnimator>();
            if (eAnim == null) eAnim = enemyPortraitImage.gameObject.AddComponent<CharacterSpriteAnimator>();
            eAnim.RefreshOriginTransform();
        }

        RegisterEntityEvents();
        LoadStageData();
        SetupDiceSlots();
        SetupActionButtons();

        StartCoroutine(BeginRoundCoroutine());
    }

    private void Update()
    {
        // Phím tắt trong pha PlayerAction: Z = Undo, 1 = Skill Thường, 2 = Skill Tối Thượng
        if (!isAnimating && !combatEnded && currentPhase == CombatPhase.PlayerAction)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                UndoLastDiceAction();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                TryCastNormalSkill();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                TryCastUltimateSkill();
            }
        }
    }

    // =========================================================================
    // 1. THIẾT KẾ LẠI BỐ CỤC TỔNG QUÁT COMBAT SCENE (4 PHÂN KHU RÕ RÀNG)
    // =========================================================================

    private void ApplyRedesignedCombatLayout()
    {
        Canvas rootCanvas = FindAnyObjectByType<Canvas>();
        if (rootCanvas == null) return;
        Transform canvasTr = rootCanvas.transform;

        EnsureSoftOvalSprite();

        // 1. Làm dịu lớp phủ tối (DarkOverlay) để hình nền hầm ngục sắc nét và có chiều sâu hơn
        Transform darkOverlayTr = canvasTr.Find("DarkOverlay");
        if (darkOverlayTr != null)
        {
            Image darkImg = darkOverlayTr.GetComponent<Image>();
            if (darkImg != null)
            {
                darkImg.color = new Color(0.04f, 0.05f, 0.09f, 0.24f);
                darkImg.raycastTarget = false;
            }
        }

        // Ẩn các phần tử trang trí tĩnh cũ gây rối bố cục
        DeactivateIfFound(canvasTr, "CandleL");
        DeactivateIfFound(canvasTr, "CandleR");
        DeactivateIfFound(canvasTr, "VS");
        DeactivateIfFound(canvasTr, "HeroTagGuard");
        DeactivateIfFound(canvasTr, "HeroTagBurn");
        DeactivateIfFound(canvasTr, "EnemyTagEnraged");
        DeactivateIfFound(canvasTr, "Hero_HP_Tag");
        DeactivateIfFound(canvasTr, "Enemy_HP_Tag");

        // 2. PHÂN KHU 1: TOP HEADER BAR (TurnBadge + Tiến trình Ải + Nút về Menu)
        Transform turnBadgeTr = canvasTr.Find("TurnBadge");
        if (turnBadgeTr != null)
        {
            RectTransform tbRt = turnBadgeTr.GetComponent<RectTransform>();
            if (tbRt != null)
            {
                tbRt.anchorMin = new Vector2(0.5f, 1f);
                tbRt.anchorMax = new Vector2(0.5f, 1f);
                tbRt.pivot = new Vector2(0.5f, 1f);
                tbRt.anchoredPosition = new Vector2(0f, -18f);
                tbRt.sizeDelta = new Vector2(540f, 46f);
            }
            Image tbImg = turnBadgeTr.GetComponent<Image>();
            if (tbImg != null)
            {
                tbImg.color = new Color(0.07f, 0.08f, 0.14f, 0.94f);
            }
            EnsureOutline(turnBadgeTr.gameObject, new Color(0.85f, 0.65f, 0.18f, 0.85f), 1.5f);
        }

        // 3. PHÂN KHU 2: SÀN ĐẤU ĐỐI KHÁNG (HIỆP SĨ BÊN TRÁI - QUÁI VẬT BÊN PHẢI)
        // --- 3A. HIỆP SĨ (PlayerArea) ---
        if (playerEntity != null)
        {
            RectTransform pAreaRt = playerEntity.GetComponent<RectTransform>();
            if (pAreaRt != null)
            {
                pAreaRt.anchorMin = new Vector2(0.5f, 0.5f);
                pAreaRt.anchorMax = new Vector2(0.5f, 0.5f);
                pAreaRt.pivot = new Vector2(0.5f, 0.5f);
                pAreaRt.anchoredPosition = new Vector2(-450f, 70f);
                pAreaRt.sizeDelta = new Vector2(290f, 340f);
            }

            CleanCharacterFrameBox(playerEntity.transform);
            CreateCharacterPedestalAndHighlight(
                playerEntity.transform,
                new Color(0.25f, 0.82f, 1f, 0f),
                out playerTargetRing);
        }

        // Cụm HUD Hiệp Sĩ đặt ngay dưới chân Hiệp Sĩ
        Transform heroLabelTr = canvasTr.Find("HeroLabel");
        if (heroLabelTr != null)
        {
            RectTransform hlRt = heroLabelTr.GetComponent<RectTransform>();
            hlRt.anchorMin = new Vector2(0.5f, 0.5f);
            hlRt.anchorMax = new Vector2(0.5f, 0.5f);
            hlRt.pivot = new Vector2(0.5f, 0.5f);
            hlRt.anchoredPosition = new Vector2(-450f, -124f);
            hlRt.sizeDelta = new Vector2(320f, 30f);

            if (tmpType != null)
            {
                Component tmp = heroLabelTr.GetComponent(tmpType);
                SetTMPText(tmp, "HIỆP SĨ BẤT KHUẤT");
                tmpType.GetProperty("fontSize")?.SetValue(tmp, 19f);
                tmpType.GetProperty("color")?.SetValue(tmp, new Color(0.96f, 0.84f, 0.45f, 1f));
                tmpType.GetProperty("alignment")?.SetValue(tmp, 514);
            }
        }

        Transform heroHpBgTr = canvasTr.Find("Hero_HPBg");
        if (heroHpBgTr != null)
        {
            RectTransform hpBgRt = heroHpBgTr.GetComponent<RectTransform>();
            hpBgRt.anchorMin = new Vector2(0.5f, 0.5f);
            hpBgRt.anchorMax = new Vector2(0.5f, 0.5f);
            hpBgRt.pivot = new Vector2(0.5f, 0.5f);
            hpBgRt.anchoredPosition = new Vector2(-450f, -158f);
            hpBgRt.sizeDelta = new Vector2(300f, 26f);

            Image bgImg = heroHpBgTr.GetComponent<Image>();
            if (bgImg != null) bgImg.color = new Color(0.08f, 0.09f, 0.14f, 0.95f);
            EnsureOutline(heroHpBgTr.gameObject, new Color(0.75f, 0.58f, 0.18f, 0.8f), 1.5f);
        }

        if (playerHPFill != null)
        {
            playerHPFill.color = new Color(0.2f, 0.78f, 0.38f, 1f);
        }

        if (playerHPLabel != null)
        {
            RectTransform hpLblRt = playerHPLabel.GetComponent<RectTransform>();
            if (hpLblRt != null)
            {
                hpLblRt.anchorMin = new Vector2(0.5f, 0.5f);
                hpLblRt.anchorMax = new Vector2(0.5f, 0.5f);
                hpLblRt.pivot = new Vector2(0.5f, 0.5f);
                hpLblRt.anchoredPosition = new Vector2(-450f, -158f);
                hpLblRt.sizeDelta = new Vector2(300f, 26f);
                hpLblRt.SetAsLastSibling();
            }
            if (tmpType != null)
            {
                tmpType.GetProperty("fontSize")?.SetValue(playerHPLabel, 16.5f);
                tmpType.GetProperty("color")?.SetValue(playerHPLabel, Color.white);
                tmpType.GetProperty("alignment")?.SetValue(playerHPLabel, 514);
            }
        }

        // --- 3B. QUÁI VẬT (EnemyArea) ---
        if (enemyEntity != null)
        {
            RectTransform eAreaRt = enemyEntity.GetComponent<RectTransform>();
            if (eAreaRt != null)
            {
                eAreaRt.anchorMin = new Vector2(0.5f, 0.5f);
                eAreaRt.anchorMax = new Vector2(0.5f, 0.5f);
                eAreaRt.pivot = new Vector2(0.5f, 0.5f);
                eAreaRt.anchoredPosition = new Vector2(450f, 70f);
                eAreaRt.sizeDelta = new Vector2(300f, 340f);
            }

            CleanCharacterFrameBox(enemyEntity.transform);
            CreateCharacterPedestalAndHighlight(
                enemyEntity.transform,
                new Color(1f, 0.35f, 0.25f, 0f),
                out enemyTargetRing);
        }

        // Đưa Banner Ý Định Quái Vật (IntentBadge) lên phía trên đỉnh đầu Quái Vật
        Transform intentBadgeTr = canvasTr.Find("IntentBadge");
        if (intentBadgeTr != null)
        {
            RectTransform ibRt = intentBadgeTr.GetComponent<RectTransform>();
            ibRt.anchorMin = new Vector2(0.5f, 0.5f);
            ibRt.anchorMax = new Vector2(0.5f, 0.5f);
            ibRt.pivot = new Vector2(0.5f, 0.5f);
            ibRt.anchoredPosition = new Vector2(450f, 262f);
            ibRt.sizeDelta = new Vector2(390f, 44f);

            enemyIntentBgImage = intentBadgeTr.GetComponent<Image>();
            if (enemyIntentBgImage != null)
            {
                enemyIntentBgImage.color = new Color(0.11f, 0.08f, 0.14f, 0.94f);
            }
            enemyIntentOutline = EnsureOutline(intentBadgeTr.gameObject, new Color(0.95f, 0.32f, 0.25f, 0.9f), 1.5f);
        }

        // Cụm HUD Quái Vật đặt ngay dưới chân Quái Vật (đối xứng với Hiệp Sĩ)
        if (enemyNameLabel != null)
        {
            RectTransform elRt = enemyNameLabel.GetComponent<RectTransform>();
            if (elRt != null)
            {
                elRt.anchorMin = new Vector2(0.5f, 0.5f);
                elRt.anchorMax = new Vector2(0.5f, 0.5f);
                elRt.pivot = new Vector2(0.5f, 0.5f);
                elRt.anchoredPosition = new Vector2(450f, -124f);
                elRt.sizeDelta = new Vector2(350f, 30f);
            }
            if (tmpType != null)
            {
                tmpType.GetProperty("fontSize")?.SetValue(enemyNameLabel, 19f);
                tmpType.GetProperty("color")?.SetValue(enemyNameLabel, new Color(1f, 0.75f, 0.45f, 1f));
                tmpType.GetProperty("alignment")?.SetValue(enemyNameLabel, 514);
            }
        }

        Transform enemyHpBgTr = canvasTr.Find("Enemy_HPBg");
        if (enemyHpBgTr != null)
        {
            RectTransform eHpBgRt = enemyHpBgTr.GetComponent<RectTransform>();
            eHpBgRt.anchorMin = new Vector2(0.5f, 0.5f);
            eHpBgRt.anchorMax = new Vector2(0.5f, 0.5f);
            eHpBgRt.pivot = new Vector2(0.5f, 0.5f);
            eHpBgRt.anchoredPosition = new Vector2(450f, -158f);
            eHpBgRt.sizeDelta = new Vector2(300f, 26f);

            Image bgImg = enemyHpBgTr.GetComponent<Image>();
            if (bgImg != null) bgImg.color = new Color(0.08f, 0.09f, 0.14f, 0.95f);
            EnsureOutline(enemyHpBgTr.gameObject, new Color(0.85f, 0.28f, 0.25f, 0.8f), 1.5f);
        }

        if (enemyHPFill != null)
        {
            enemyHPFill.color = new Color(0.86f, 0.22f, 0.22f, 1f);
        }

        if (enemyHPLabel != null)
        {
            RectTransform eHpLblRt = enemyHPLabel.GetComponent<RectTransform>();
            if (eHpLblRt != null)
            {
                eHpLblRt.anchorMin = new Vector2(0.5f, 0.5f);
                eHpLblRt.anchorMax = new Vector2(0.5f, 0.5f);
                eHpLblRt.pivot = new Vector2(0.5f, 0.5f);
                eHpLblRt.anchoredPosition = new Vector2(450f, -158f);
                eHpLblRt.sizeDelta = new Vector2(300f, 26f);
                eHpLblRt.SetAsLastSibling();
            }
            if (tmpType != null)
            {
                tmpType.GetProperty("fontSize")?.SetValue(enemyHPLabel, 16.5f);
                tmpType.GetProperty("color")?.SetValue(enemyHPLabel, Color.white);
                tmpType.GetProperty("alignment")?.SetValue(enemyHPLabel, 514);
            }
        }

        // 4. PHÂN KHU 4: KHAY XÚC XẮC & BẢNG ĐIỀU KHIỂN ĐÁY MÀN HÌNH (DiceTray)
        Transform diceTrayTr = canvasTr.Find("DiceTray");
        if (diceTrayTr != null)
        {
            RectTransform trayRt = diceTrayTr.GetComponent<RectTransform>();
            if (trayRt != null)
            {
                trayRt.anchorMin = new Vector2(0f, 0f);
                trayRt.anchorMax = new Vector2(1f, 0f);
                trayRt.pivot = new Vector2(0.5f, 0f);
                trayRt.anchoredPosition = Vector2.zero;
                trayRt.sizeDelta = new Vector2(0f, 212f);
            }

            Image trayImg = diceTrayTr.GetComponent<Image>();
            if (trayImg != null)
            {
                trayImg.color = new Color(0.06f, 0.06f, 0.1f, 0.95f);
            }

            // Cột Trái: Tiêu đề Khay Xúc Xắc & Hướng dẫn thao tác
            Transform trayLabelTr = diceTrayTr.Find("DiceTrayLabel");
            if (trayLabelTr != null)
            {
                RectTransform tlRt = trayLabelTr.GetComponent<RectTransform>();
                tlRt.anchorMin = new Vector2(0f, 1f);
                tlRt.anchorMax = new Vector2(0f, 1f);
                tlRt.pivot = new Vector2(0f, 1f);
                tlRt.anchoredPosition = new Vector2(42f, -22f);
                tlRt.sizeDelta = new Vector2(380f, 32f);

                if (tmpType != null)
                {
                    Component tmp = trayLabelTr.GetComponent(tmpType);
                    SetTMPText(tmp, "KHAY XÚC XẮC CHIẾN THUẬT");
                    tmpType.GetProperty("fontSize")?.SetValue(tmp, 18f);
                    tmpType.GetProperty("color")?.SetValue(tmp, new Color(0.95f, 0.78f, 0.32f, 1f));
                }
            }

            // Gỡ bỏ hoàn toàn dòng chữ hướng dẫn thao tác khỏi khay xúc xắc
            Transform oldGuide = diceTrayTr.Find("TrayControlGuide");
            if (oldGuide != null) oldGuide.gameObject.SetActive(false);
        }

        // Cột Giữa: Căn giữa 3 Ô Xúc Xắc (Slot 0, 1, 2) tại (-160, 0, +160)
        float[] slotXPositions = { -160f, 0f, 160f };
        for (int i = 0; i < diceSlots.Count; i++)
        {
            if (diceSlots[i] == null) continue;
            RectTransform slotRt = diceSlots[i].GetComponent<RectTransform>();
            if (slotRt != null)
            {
                slotRt.anchorMin = new Vector2(0.5f, 0f);
                slotRt.anchorMax = new Vector2(0.5f, 0f);
                slotRt.pivot = new Vector2(0.5f, 0.5f);
                float posX = i < slotXPositions.Length ? slotXPositions[i] : (-160f + i * 160f);
                slotRt.anchoredPosition = new Vector2(posX, 102f);
                slotRt.sizeDelta = new Vector2(146f, 175f);
            }

            EnsureOutline(diceSlots[i], new Color(0.55f, 0.45f, 0.22f, 0.65f), 1.8f);
            StyleDiceCardChildren(diceSlots[i].transform);
        }

        // Cột Phải: Cụm nút RE-ROLL, UNDO và END TURN gọn gàng trong lòng DiceTray
        if (rerollButton != null)
        {
            RectTransform rrRt = rerollButton.GetComponent<RectTransform>();
            if (rrRt != null)
            {
                rrRt.anchorMin = new Vector2(0.5f, 0f);
                rrRt.anchorMax = new Vector2(0.5f, 0f);
                rrRt.pivot = new Vector2(0.5f, 0.5f);
                rrRt.anchoredPosition = new Vector2(520f, 146f);
                rrRt.sizeDelta = new Vector2(178f, 48f);
            }
            EnsureOutline(rerollButton.gameObject, new Color(0.45f, 0.72f, 1f, 0.75f), 1.2f);
        }

        if (undoButton != null)
        {
            RectTransform unRt = undoButton.GetComponent<RectTransform>();
            if (unRt != null)
            {
                unRt.anchorMin = new Vector2(0.5f, 0f);
                unRt.anchorMax = new Vector2(0.5f, 0f);
                unRt.pivot = new Vector2(0.5f, 0.5f);
                unRt.anchoredPosition = new Vector2(710f, 146f);
                unRt.sizeDelta = new Vector2(178f, 48f);
            }
            EnsureOutline(undoButton.gameObject, new Color(0.85f, 0.65f, 0.25f, 0.75f), 1.2f);
        }

        if (endTurnButton != null)
        {
            RectTransform etRt = endTurnButton.GetComponent<RectTransform>();
            if (etRt != null)
            {
                etRt.anchorMin = new Vector2(0.5f, 0f);
                etRt.anchorMax = new Vector2(0.5f, 0f);
                etRt.pivot = new Vector2(0.5f, 0.5f);
                etRt.anchoredPosition = new Vector2(615f, 70f);
                etRt.sizeDelta = new Vector2(370f, 58f);
            }
            EnsureOutline(endTurnButton.gameObject, new Color(1f, 0.78f, 0.28f, 0.9f), 1.5f);
        }
    }

    private void StyleDiceCardChildren(Transform slotTr)
    {
        if (slotTr == null) return;

        // Ẩn lớp phủ Vignette tối bên trong thẻ xúc xắc nếu có
        Transform vig = slotTr.Find("Vignette");
        if (vig != null) vig.gameObject.SetActive(false);

        Transform nameTr = slotTr.Find("DiceName");
        if (nameTr != null)
        {
            RectTransform rt = nameTr.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -8f);
                rt.sizeDelta = new Vector2(-10f, 38f);
            }
            if (tmpType != null)
            {
                Component tmp = nameTr.GetComponent(tmpType);
                tmpType.GetProperty("fontSize")?.SetValue(tmp, 15.5f);
                tmpType.GetProperty("alignment")?.SetValue(tmp, 514);
                EnsureOutline(nameTr.gameObject, new Color(0.04f, 0.05f, 0.08f, 0.95f), 1.2f);
            }
        }

        Transform iconTr = slotTr.Find("DiceIcon");
        if (iconTr != null)
        {
            RectTransform rt = iconTr.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, 8f);
                rt.sizeDelta = new Vector2(60f, 60f);
            }
            Image iconImg = iconTr.GetComponent<Image>();
            if (iconImg != null) iconImg.preserveAspect = true;
        }

        Transform valTr = slotTr.Find("DiceVal");
        if (valTr != null)
        {
            RectTransform rt = valTr.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 8f);
                rt.sizeDelta = new Vector2(-10f, 44f);
            }
            if (tmpType != null)
            {
                Component tmp = valTr.GetComponent(tmpType);
                tmpType.GetProperty("fontSize")?.SetValue(tmp, 28f);
                tmpType.GetProperty("alignment")?.SetValue(tmp, 514);
                EnsureOutline(valTr.gameObject, new Color(0.04f, 0.05f, 0.08f, 0.95f), 1.5f);
            }
        }
    }

    private void CleanCharacterFrameBox(Transform areaTransform)
    {
        if (areaTransform == null) return;
        // Tắt hộp đen Vignette và các vạch khung vuông cứng quanh nhân vật để nhân vật đứng tự nhiên trên sàn đấu
        string[] boxParts = { "Vignette", "TL_H", "TL_V", "TR_H", "TR_V", "BL_H", "BL_V", "BR_H", "BR_V" };
        foreach (string part in boxParts)
        {
            Transform child = areaTransform.Find(part);
            if (child != null) child.gameObject.SetActive(false);
        }
    }

    private void CreateCharacterPedestalAndHighlight(
        Transform areaTransform,
        Color initialHighlightColor,
        out Image highlightRingImage)
    {
        highlightRingImage = null;
        if (areaTransform == null) return;

        // 1. Bóng đổ dưới chân nhân vật (Shadow Pedestal)
        Transform existingShadow = areaTransform.Find("CharacterShadowPedestal");
        if (existingShadow == null)
        {
            GameObject shadowGo = new GameObject("CharacterShadowPedestal");
            shadowGo.transform.SetParent(areaTransform, false);
            shadowGo.transform.SetAsFirstSibling();

            RectTransform sRt = shadowGo.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.5f, 0f);
            sRt.anchorMax = new Vector2(0.5f, 0f);
            sRt.pivot = new Vector2(0.5f, 0.5f);
            sRt.anchoredPosition = new Vector2(0f, 14f);
            sRt.sizeDelta = new Vector2(230f, 44f);

            Image sImg = shadowGo.AddComponent<Image>();
            sImg.sprite = cachedSoftOvalSprite;
            sImg.color = new Color(0f, 0f, 0f, 0.62f);
            sImg.raycastTarget = false;
        }

        // 2. Vòng sáng chỉ định mục tiêu khi kéo xúc xắc (Target Highlight Ring)
        Transform existingRing = areaTransform.Find("TargetHighlightRing");
        if (existingRing == null)
        {
            GameObject ringGo = new GameObject("TargetHighlightRing");
            ringGo.transform.SetParent(areaTransform, false);
            ringGo.transform.SetSiblingIndex(1);

            RectTransform rRt = ringGo.AddComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0.5f, 0f);
            rRt.anchorMax = new Vector2(0.5f, 0f);
            rRt.pivot = new Vector2(0.5f, 0.5f);
            rRt.anchoredPosition = new Vector2(0f, 16f);
            rRt.sizeDelta = new Vector2(260f, 56f);

            highlightRingImage = ringGo.AddComponent<Image>();
            highlightRingImage.sprite = cachedSoftOvalSprite;
            highlightRingImage.color = initialHighlightColor;
            highlightRingImage.raycastTarget = false;
        }
        else
        {
            highlightRingImage = existingRing.GetComponent<Image>();
        }
    }

    private static void EnsureSoftOvalSprite()
    {
        if (cachedSoftOvalSprite != null) return;
        int w = 64, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (x / (float)(w - 1)) * 2f - 1f;
                float ny = (y / (float)(h - 1)) * 2f - 1f;
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        }
        tex.Apply();
        cachedSoftOvalSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    private void DeactivateIfFound(Transform parent, string childName)
    {
        Transform t = parent.Find(childName);
        if (t != null) t.gameObject.SetActive(false);
    }

    private Outline EnsureOutline(GameObject go, Color color, float dist)
    {
        if (go == null) return null;
        Outline outline = go.GetComponent<Outline>();
        if (outline == null) outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(dist, -dist);
        return outline;
    }

    // =========================================================================
    // 2. KHỞI TẠO DỮ LIỆU MÀN CHƠI & UI ĐỘNG
    // =========================================================================

    private void LoadStageData()
    {
        GameManager gm = GameManager.ResolveInstance();
        if (gm != null)
        {
            gm.EnsureDiceBagInitialized();
            currentStage = Mathf.Max(1, gm.currentStageIndex);

            normalSkillBaseCost = GameManager.Instance.normalSkillBaseCost;
            normalSkillDamage = GameManager.Instance.GetNormalSkillDamage();
            ultimateSkillBaseCost = GameManager.Instance.ultimateSkillBaseCost;
            ultimateSkillDamage = GameManager.Instance.GetUltimateSkillDamage();
            ultimateSkillBurnStacks = GameManager.Instance.GetUltimateSkillBurnStacks();

            if (GameManager.Instance.stageEnemies != null &&
                currentStage - 1 < GameManager.Instance.stageEnemies.Length)
            {
                currentEnemyData = GameManager.Instance.stageEnemies[currentStage - 1];
            }
        }
        else
        {
            currentStage = 1;
            currentEnemyData = null;
        }

        string displayEnemyName = currentStage == 3 ? "Minotaur Boss" : (currentStage == 2 ? "Armored Orc" : "Goblin Scout");
        int canonicalEnemyHP = currentStage == 3 ? 60 : (currentStage == 2 ? 35 : 20);
        int startingShield = currentStage == 2 ? 5 : 0;

        if (currentEnemyData != null)
        {
            if (!string.IsNullOrWhiteSpace(currentEnemyData.enemyName))
            {
                displayEnemyName = currentEnemyData.enemyName;
            }
            currentEnemyData.GetCanonicalStats(currentStage, out canonicalEnemyHP, out startingShield);

            if (backgroundImage != null && currentEnemyData.backgroundSprite != null)
                backgroundImage.sprite = currentEnemyData.backgroundSprite;
            if (enemyPortraitImage != null && currentEnemyData.enemySprite != null)
                enemyPortraitImage.sprite = currentEnemyData.enemySprite;
        }

        if (enemyEntity != null)
        {
            enemyEntity.maxHP = canonicalEnemyHP;
            enemyEntity.Initialize(currentEnemyData);
            enemyEntity.SetHP(canonicalEnemyHP, canonicalEnemyHP);
            if (startingShield > enemyEntity.CurrentShield)
            {
                enemyEntity.AddShield(startingShield - enemyEntity.CurrentShield);
            }
        }

        if (enemyNameLabel != null)
        {
            SetTMPText(enemyNameLabel, $"{displayEnemyName.ToUpper()} (ẢI {currentStage}/3)");
        }

        // Khởi tạo chỉ số Hiệp Sĩ (giữ lượng HP hiện tại xuyên suốt hành trình)
        if (playerEntity != null)
        {
            int maxHP = GameManager.Instance != null ? GameManager.Instance.playerMaxHP : 70;
            int curHP = GameManager.Instance != null && GameManager.Instance.playerCurrentHP > 0
                ? GameManager.Instance.playerCurrentHP
                : maxHP;

            playerEntity.maxHP = maxHP;
            playerEntity.Initialize(null);
            playerEntity.SetHP(Mathf.Clamp(curHP, 1, maxHP), maxHP);
        }

        UpdateStageProgressUI();
    }

    private void EnsureDropTargetsConfigured()
    {
        if (enemyEntity != null)
        {
            EnsureDropTargetOnObject(enemyEntity.gameObject, DropTarget.TargetType.Enemy);
        }
        if (enemyPortraitImage != null)
        {
            EnsureDropTargetOnObject(enemyPortraitImage.gameObject, DropTarget.TargetType.Enemy);
        }

        if (playerEntity != null)
        {
            EnsureDropTargetOnObject(playerEntity.gameObject, DropTarget.TargetType.Player);
        }
        if (playerPortraitImage != null)
        {
            EnsureDropTargetOnObject(playerPortraitImage.gameObject, DropTarget.TargetType.Player);
        }
    }

    private void EnsureDropTargetOnObject(GameObject targetObj, DropTarget.TargetType type)
    {
        if (targetObj == null) return;
        DropTarget dt = targetObj.GetComponent<DropTarget>();
        if (dt == null) dt = targetObj.AddComponent<DropTarget>();
        dt.targetType = type;
        dt.combatManager = this;
    }

    private void EnsureRuntimeCombatOverlays()
    {
        Canvas rootCanvas = FindAnyObjectByType<Canvas>();
        if (rootCanvas == null) return;

        // 1. Nhãn trạng thái & Giáp của Player (đặt ngay dưới Thanh Máu Hiệp Sĩ, không đè lên tên)
        if (playerEntity != null && playerStatusLabel == null)
        {
            GameObject pBadgeBg = CreateStatusPillPanel(
                rootCanvas.transform,
                "PlayerStatusPanel",
                new Vector2(-450f, -195f),
                new Vector2(340f, 34f),
                new Color(0.08f, 0.09f, 0.14f, 0.95f));
            EnsureOutline(pBadgeBg, new Color(0.75f, 0.58f, 0.22f, 0.85f), 1.2f);

            playerStatusLabel = CreateOverlayLabel(
                pBadgeBg.transform,
                "PlayerStatusBadge",
                Vector2.zero,
                new Vector2(330f, 32f),
                16.5f,
                new Color(0.68f, 0.92f, 1f, 1f));
        }

        // 2. Nhãn trạng thái & Giáp của Enemy (đặt ngay dưới Thanh Máu Quái Vật)
        if (enemyEntity != null && enemyStatusLabel == null)
        {
            GameObject eBadgeBg = CreateStatusPillPanel(
                rootCanvas.transform,
                "EnemyStatusPanel",
                new Vector2(450f, -195f),
                new Vector2(340f, 34f),
                new Color(0.08f, 0.09f, 0.14f, 0.95f));
            EnsureOutline(eBadgeBg, new Color(0.75f, 0.58f, 0.22f, 0.85f), 1.2f);

            enemyStatusLabel = CreateOverlayLabel(
                eBadgeBg.transform,
                "EnemyStatusBadge",
                Vector2.zero,
                new Vector2(330f, 32f),
                16.5f,
                new Color(1f, 0.88f, 0.55f, 1f));
        }

        // 3. Banner Ý Định Quái Vật (Enemy Intent)
        GameObject sceneIntentTextGo = GameObject.Find("IntentText");
        if (sceneIntentTextGo != null && tmpType != null)
        {
            enemyIntentLabel = sceneIntentTextGo.GetComponent(tmpType);
            tmpType.GetProperty("fontSize")?.SetValue(enemyIntentLabel, 17.5f);
            tmpType.GetProperty("color")?.SetValue(enemyIntentLabel, new Color(1f, 0.94f, 0.78f, 1f));
        }
        else if (enemyEntity != null && enemyIntentLabel == null)
        {
            GameObject intentBg = new GameObject("EnemyIntentBanner");
            intentBg.transform.SetParent(rootCanvas.transform, false);
            RectTransform bgRt = intentBg.AddComponent<RectTransform>();
            bgRt.anchoredPosition = new Vector2(450f, 262f);
            bgRt.sizeDelta = new Vector2(390f, 44f);

            enemyIntentBgImage = intentBg.AddComponent<Image>();
            enemyIntentBgImage.color = new Color(0.09f, 0.09f, 0.14f, 0.92f);
            enemyIntentBgImage.raycastTarget = false;

            enemyIntentOutline = EnsureOutline(intentBg, new Color(0.95f, 0.35f, 0.25f, 0.9f), 1.5f);

            enemyIntentLabel = CreateOverlayLabel(
                intentBg.transform,
                "IntentText",
                Vector2.zero,
                new Vector2(376f, 40f),
                17.5f,
                new Color(1f, 0.94f, 0.78f, 1f));
        }

        // 4. Huy hiệu Tiến Trình Hành Trình 3 Ải ở Góc Trái Đỉnh
        if (stageProgressLabel == null)
        {
            GameObject progBg = CreateStatusPillPanel(
                rootCanvas.transform,
                "StageProgressBanner",
                new Vector2(-650f, 498f),
                new Vector2(460f, 38f),
                new Color(0.72f, 0.55f, 0.18f, 0.65f));

            stageProgressLabel = CreateOverlayLabel(
                progBg.transform,
                "StageProgressText",
                Vector2.zero,
                new Vector2(445f, 34f),
                15.5f,
                new Color(0.95f, 0.9f, 0.75f, 1f));

            progBg.SetActive(false);
        }

        // 5. Panel Tooltip Chiến Thuật nằm ngay trên Khay Xúc Xắc (DiceTray)
        if (tooltipPanel == null)
        {
            tooltipPanel = new GameObject("DiceTooltipBanner");
            tooltipPanel.transform.SetParent(rootCanvas.transform, false);
            RectTransform rt = tooltipPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 218f);
            rt.sizeDelta = new Vector2(1020f, 42f);

            Image bg = tooltipPanel.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.14f, 0.93f);
            bg.raycastTarget = false;
            EnsureOutline(tooltipPanel, new Color(0.75f, 0.6f, 0.22f, 0.65f), 1.2f);

            tooltipLabel = CreateOverlayLabel(
                tooltipPanel.transform,
                "TooltipText",
                Vector2.zero,
                new Vector2(996f, 38f),
                16.5f,
                new Color(0.92f, 0.96f, 1f, 1f));

            SetDefaultHintText();
        }

        // 6. Tạo nút UNDO đặt ngang hàng với nút RE-ROLL trong Khay Xúc Xắc
        if (undoButton == null && rerollButton != null)
        {
            GameObject undoObj = Instantiate(rerollButton.gameObject, rerollButton.transform.parent);
            undoObj.name = "UndoButton";
            RectTransform undoRt = undoObj.GetComponent<RectTransform>();
            if (undoRt != null)
            {
                undoRt.anchorMin = new Vector2(0.5f, 0f);
                undoRt.anchorMax = new Vector2(0.5f, 0f);
                undoRt.pivot = new Vector2(0.5f, 0.5f);
                undoRt.anchoredPosition = new Vector2(710f, 146f);
                undoRt.sizeDelta = new Vector2(178f, 48f);
            }
            EnsureOutline(undoObj, new Color(0.85f, 0.65f, 0.25f, 0.75f), 1.2f);

            undoButton = undoObj.GetComponent<Button>();
            undoButton.onClick.RemoveAllListeners();
            UpdateButtonCaption(undoButton, "UNDO (0) [Z]");
        }

        // 7. Cụm Thanh Năng Lượng (10 vạch Energy Pips) & 2 Kỹ Năng Nhân Vật (Skill Thường & Skill Tối Thượng)
        if (characterSkillBarUI == null)
        {
            characterSkillBarUI = rootCanvas.GetComponentInChildren<CharacterSkillBarUI>(true);
            if (characterSkillBarUI == null)
            {
                characterSkillBarUI = gameObject.AddComponent<CharacterSkillBarUI>();
            }
            characterSkillBarUI.Initialize(this, playerEntity, rootCanvas.transform);
        }

        // 8. Nút Truy Cập Nhanh Bách Khoa Xúc Xắc (Codex) & Menu Tạm Dừng [ESC]
        PauseMenuUI.AttachToCanvas(rootCanvas.transform);
    }

    private Button CreateSkillButton(
        Transform parent,
        string name,
        Vector2 anchoredPos,
        Vector2 size,
        Color bgColor,
        Color outlineColor,
        UnityEngine.Events.UnityAction onClickAction)
    {
        GameObject btnGo = new GameObject(name);
        btnGo.transform.SetParent(parent, false);
        RectTransform rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = btnGo.AddComponent<Image>();
        img.color = bgColor;
        EnsureOutline(btnGo, outlineColor, 1.4f);

        Button btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClickAction);

        CreateOverlayLabel(
            btnGo.transform,
            "SkillCaption",
            Vector2.zero,
            size - new Vector2(10f, 4f),
            13.5f,
            Color.white);

        return btn;
    }

    private GameObject CreateStatusPillPanel(
        Transform parent,
        string name,
        Vector2 anchoredPos,
        Vector2 size,
        Color outlineColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.07f, 0.12f, 0.88f);
        bg.raycastTarget = false;

        EnsureOutline(go, outlineColor, 1.2f);
        return go;
    }

    private Component CreateOverlayLabel(
        Transform parent,
        string name,
        Vector2 anchoredPos,
        Vector2 size,
        float fontSize,
        Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        if (tmpType != null)
        {
            Component tmp = go.AddComponent(tmpType);
            tmpType.GetProperty("fontSize")?.SetValue(tmp, fontSize);
            tmpType.GetProperty("color")?.SetValue(tmp, color);
            tmpType.GetProperty("alignment")?.SetValue(tmp, 514); // Center
            tmpType.GetProperty("raycastTarget")?.SetValue(tmp, false);
            return tmp;
        }

        return null;
    }

    private void UpdateStageProgressUI()
    {
        if (stageProgressLabel == null) return;
        string s1 = currentStage == 1 ? "<color=#ffd166><b>[Ải 1: Goblin]</b></color>" : (currentStage > 1 ? "<color=#52b788>[X] Ải 1</color>" : "Ải 1");
        string s2 = currentStage == 2 ? "<color=#ffd166><b>[Ải 2: Orc]</b></color>" : (currentStage > 2 ? "<color=#52b788>[X] Ải 2</color>" : "Ải 2");
        string s3 = currentStage == 3 ? "<color=#ef476f><b>[Ải 3: Minotaur]</b></color>" : "Ải 3";
        SetTMPText(stageProgressLabel, $"HÀNH TRÌNH:  {s1}  >>  {s2}  >>  {s3}");
    }

    // =========================================================================
    // 3. ĐĂNG KÝ OBSERVER EVENTS CHO MÁU, GIÁP, HIỆU ỨNG & NHẬN SÁT THƯƠNG
    // =========================================================================

    private void RegisterEntityEvents()
    {
        if (playerEntity != null)
        {
            playerEntity.OnHPChanged += (current, max) =>
            {
                UpdateHPBar(playerHPFill, current, max);
                UpdateHPLabel(playerHPLabel, current, max);
                RefreshEntityStatusDisplay(playerEntity, playerStatusLabel);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.playerCurrentHP = current;
                }
                if (current <= 0 && !combatEnded)
                {
                    StartCoroutine(HandlePlayerDefeated());
                }
            };

            playerEntity.OnDamageTaken += (dmg) =>
            {
                SpawnFloatingText(playerEntity.transform, dmg, false);
                if (playerPortraitImage != null)
                {
                    CharacterSpriteAnimator pAnim = playerPortraitImage.GetComponent<CharacterSpriteAnimator>();
                    if (pAnim != null) pAnim.PlayTakeDamage();
                }
                if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.22f, 9f);
            };

            playerEntity.OnHealed += (healAmount) =>
            {
                SpawnCustomFloatingText(playerEntity.transform, $"+{healAmount} HP", new Color(0.25f, 0.98f, 0.5f));
            };

            playerEntity.OnShieldChanged += (shield) =>
            {
                RefreshEntityStatusDisplay(playerEntity, playerStatusLabel);
            };

            playerEntity.OnEnergyChanged += (curEnergy, maxEnergy) =>
            {
                UpdateCharacterSkillsUI();
                RefreshEntityStatusDisplay(playerEntity, playerStatusLabel);
            };

            playerEntity.OnStatusChanged += () =>
            {
                RefreshEntityStatusDisplay(playerEntity, playerStatusLabel);
                UpdateCharacterSkillsUI();
            };
        }

        if (enemyEntity != null)
        {
            enemyEntity.OnHPChanged += (current, max) =>
            {
                UpdateHPBar(enemyHPFill, current, max);
                UpdateHPLabel(enemyHPLabel, current, max);
                RefreshEntityStatusDisplay(enemyEntity, enemyStatusLabel);
                if (current <= 0 && !combatEnded)
                {
                    StartCoroutine(HandleEnemyDefeated());
                }
            };

            enemyEntity.OnDamageTaken += (dmg) =>
            {
                SpawnFloatingText(enemyEntity.transform, dmg, false);
                if (enemyPortraitImage != null)
                {
                    CharacterSpriteAnimator eAnim = enemyPortraitImage.GetComponent<CharacterSpriteAnimator>();
                    if (eAnim != null) eAnim.PlayTakeDamage();
                }
                if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.22f, 9f);
            };

            enemyEntity.OnHealed += (healAmount) =>
            {
                SpawnCustomFloatingText(enemyEntity.transform, $"+{healAmount} HP", new Color(0.25f, 0.98f, 0.5f));
            };

            enemyEntity.OnShieldChanged += (shield) =>
            {
                RefreshEntityStatusDisplay(enemyEntity, enemyStatusLabel);
            };

            enemyEntity.OnStatusChanged += () =>
            {
                RefreshEntityStatusDisplay(enemyEntity, enemyStatusLabel);
                UpdateEnemyIntentUI();
            };
        }
    }

    private void RefreshEntityStatusDisplay(BattleEntity entity, Component statusLabel)
    {
        if (entity == null || statusLabel == null) return;

        List<string> badges = new List<string>();
        if (entity.CurrentShield > 0) badges.Add($"<color=#38bdf8>Giáp: {entity.CurrentShield}</color>");
        if (entity.RetainHalfShieldNextTurn) badges.Add("<color=#fde047>Giữ 50% Giáp</color>");
        if (entity.PoisonStacks > 0) badges.Add($"<color=#c084fc>Độc: {entity.PoisonStacks}</color>");
        if (entity.BurnStacks > 0) badges.Add($"<color=#fb923c>Thiêu đốt: {entity.BurnStacks}</color>");
        if (entity.StunStacks > 0) badges.Add($"<color=#facc15>Choáng: {entity.StunStacks}</color>");
        if (entity.WeakStacks > 0) badges.Add($"<color=#94a3b8>Yếu: {entity.WeakStacks}</color>");
        if (entity.BonusAttack > 0) badges.Add($"<color=#fb923c>+Công: {entity.BonusAttack}</color>");

        string text;
        if (entity == playerEntity)
        {
            string energyStr = $"<color=#7dd3fc>Năng lượng: {entity.CurrentEnergy} / {entity.MaxEnergy}</color>";
            if (badges.Count > 0)
            {
                text = $"{string.Join("   ", badges)}   |   {energyStr}";
            }
            else
            {
                text = energyStr;
            }
        }
        else
        {
            text = badges.Count > 0 ? string.Join("   ", badges) : "<color=#94a3b8>Giáp: 0 - Bình thường</color>";
        }
        SetTMPText(statusLabel, text);
    }

    // =========================================================================
    // 4. CẤU HÌNH XÚC XẮC & NÚT ĐIỀU KHIỂN
    // =========================================================================

    private void SetupDiceSlots()
    {
        rolledFaces.Clear();
        diceUsed.Clear();
        slotBaseColors.Clear();

        for (int i = 0; i < diceSlots.Count; i++)
        {
            rolledFaces.Add(null);
            diceUsed.Add(false);

            GameObject slotObj = diceSlots[i];
            if (slotObj == null)
            {
                slotBaseColors.Add(Color.white);
                continue;
            }

            Image img = slotObj.GetComponent<Image>();
            slotBaseColors.Add(img != null ? img.color : Color.white);

            DraggableDice drag = slotObj.GetComponent<DraggableDice>();
            if (drag == null) drag = slotObj.AddComponent<DraggableDice>();
            drag.combatManager = this;
            drag.slotIndex = i;

            int capturedIndex = i;
            Button btn = slotObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnDiceSlotLeftClicked(capturedIndex));
            }
        }
    }

    private void SetupActionButtons()
    {
        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveAllListeners();
            endTurnButton.onClick.AddListener(() =>
            {
                if (!isAnimating && !combatEnded && currentPhase == CombatPhase.PlayerAction)
                {
                    StartCoroutine(ExecuteEndTurnSequence());
                }
            });
        }

        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(() =>
            {
                if (!isAnimating && !combatEnded && currentPhase == CombatPhase.PlayerAction)
                {
                    StartCoroutine(AnimateReroll());
                }
            });
        }

        if (undoButton != null)
        {
            undoButton.onClick.RemoveAllListeners();
            undoButton.onClick.AddListener(() =>
            {
                if (!isAnimating && !combatEnded && currentPhase == CombatPhase.PlayerAction)
                {
                    UndoLastDiceAction();
                }
            });
        }
    }

    // =========================================================================
    // 5. MÁY TRẠNG THÁI 5 PHA (5-PHASE TURN STATE MACHINE)
    // =========================================================================

    private IEnumerator BeginRoundCoroutine()
    {
        if (combatEnded) yield break;

        isAnimating = true;
        currentPhase = CombatPhase.RoundStart;
        currentRound++;

        if (playerEntity != null) playerEntity.ResetShieldForNewTurn();
        if (enemyEntity != null) enemyEntity.ResetShieldForNewTurn();

        if (commandManager != null) commandManager.ClearHistory();
        rerollsLeft = GameManager.Instance != null ? Mathf.Max(1, GameManager.Instance.maxRerollsPerTurn) : 1;

        if (currentEnemyData != null)
        {
            currentEnemyIntent = currentEnemyData.GetIntentForTurn(currentRound, currentStage);
        }
        else
        {
            EnemyIntentStep[] cycle = EnemyData.GetDefaultCycleForStage(currentStage);
            currentEnemyIntent = cycle[(currentRound - 1) % cycle.Length];
        }

        UpdateEnemyIntentUI();
        UpdateControlButtonsUI();

        yield return new WaitForSeconds(0.2f);

        // Kiểm tra nếu Người chơi bị Choáng (Stun)
        if (playerEntity != null && playerEntity.CheckAndConsumeStun())
        {
            SpawnCustomFloatingText(playerEntity.transform, "BỊ CHOÁNG!", new Color(1f, 0.85f, 0.2f));
            yield return new WaitForSeconds(0.8f);
            yield return StartCoroutine(ExecuteEnemyAndRoundEndCoroutine());
            yield break;
        }

        // 2. Pha Gieo Xúc Xắc (PlayerRoll)
        currentPhase = CombatPhase.PlayerRoll;
        UpdateEnemyIntentUI();
        yield return StartCoroutine(RollAllDiceCoroutine(false));

        // 3. Mở Pha Hành Động (PlayerAction)
        currentPhase = CombatPhase.PlayerAction;
        isAnimating = false;
        SetDefaultHintText();
        UpdateEnemyIntentUI();
        UpdateControlButtonsUI();
    }

    private IEnumerator RollAllDiceCoroutine(bool onlySelectedOrUnused)
    {
        List<int> indicesToRoll = new List<int>();

        if (!onlySelectedOrUnused)
        {
            selectedDiceIndices.Clear();
            selectedDiceTotal = 0;
            for (int i = 0; i < diceSlots.Count; i++)
            {
                diceUsed[i] = false;
                indicesToRoll.Add(i);
            }
        }
        else
        {
            if (selectedDiceIndices.Count > 0)
            {
                foreach (int idx in selectedDiceIndices)
                {
                    if (idx >= 0 && idx < diceSlots.Count && !diceUsed[idx])
                    {
                        indicesToRoll.Add(idx);
                    }
                }
            }
            else
            {
                for (int i = 0; i < diceSlots.Count; i++)
                {
                    if (!diceUsed[i]) indicesToRoll.Add(i);
                }
            }

            selectedDiceIndices.Clear();
            selectedDiceTotal = 0;
        }

        // Hiệu ứng lắc và nhảy số ngẫu nhiên (0.35s)
        float duration = 0.35f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            foreach (int i in indicesToRoll)
            {
                SetDiceSlotText(i, "ĐANG GIEO...", Random.Range(1, 9).ToString());
                if (i >= 0 && i < diceSlots.Count && diceSlots[i] != null)
                {
                    float tilt = Mathf.Sin((elapsed * 45f) + i) * 9f;
                    diceSlots[i].transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
                }
            }
            elapsed += 0.05f;
            yield return new WaitForSeconds(0.05f);
        }

        foreach (int i in indicesToRoll)
        {
            if (i >= 0 && i < diceSlots.Count && diceSlots[i] != null)
            {
                diceSlots[i].transform.localRotation = Quaternion.identity;
            }
        }

        GameManager gm = GameManager.ResolveInstance();
        List<DiceInstance> bag = gm != null && gm.playerDiceBag != null && gm.playerDiceBag.Count > 0
            ? gm.playerDiceBag
            : DiceCatalog.CreateStarterDiceBag();

        foreach (int i in indicesToRoll)
        {
            DiceFaceData rolled = null;
            if (bag != null && i < bag.Count && bag[i] != null)
            {
                rolled = bag[i].Roll();
            }
            else
            {
                rolled = DiceFaceData.CreateRuntimeFace("Strike", DiceFaceType.Attack, DiceSkillKind.Strike, 4, DiceFaceRarity.Common);
            }

            rolledFaces[i] = rolled;
        }

        RefreshDiceSlotVisuals();
    }

    // =========================================================================
    // 6. TƯƠNG TÁC XÚC XẮC & KÍCH HOẠT HÀNH ĐỘNG NHÂN VẬT THEO TỪNG KỸ NĂNG
    // =========================================================================

    public bool CanInteractWithDiceSlot(int slotIndex)
    {
        if (isAnimating || combatEnded || currentPhase != CombatPhase.PlayerAction) return false;
        if (slotIndex < 0 || slotIndex >= diceSlots.Count) return false;
        if (slotIndex < diceUsed.Count && diceUsed[slotIndex]) return false;
        if (slotIndex >= rolledFaces.Count || rolledFaces[slotIndex] == null) return false;
        return true;
    }

    /// <summary>
    /// Bật/tắt vòng sáng chỉ định mục tiêu dưới chân Hiệp Sĩ hoặc Quái Vật khi người chơi đang kéo xúc xắc.
    /// </summary>
    public void OnDiceDragStateChanged(int slotIndex, bool isDragging)
    {
        if (playerTargetRing != null) playerTargetRing.color = new Color(0.25f, 0.85f, 1f, 0f);
        if (enemyTargetRing != null) enemyTargetRing.color = new Color(1f, 0.35f, 0.25f, 0f);

        if (!isDragging || slotIndex < 0 || slotIndex >= rolledFaces.Count || rolledFaces[slotIndex] == null)
        {
            return;
        }

        DiceFaceData face = rolledFaces[slotIndex];
        if (face.TargetsEnemy())
        {
            if (enemyTargetRing != null)
            {
                enemyTargetRing.color = new Color(1f, 0.32f, 0.22f, 0.78f);
            }
            if (tooltipLabel != null)
            {
                SetTMPText(tooltipLabel, $"Thả [{face.faceName}] vào <b>QUÁI VẬT</b> ở bên phải để tung đòn!");
            }
        }
        else
        {
            if (playerTargetRing != null)
            {
                playerTargetRing.color = new Color(0.25f, 0.92f, 0.65f, 0.78f);
            }
            if (tooltipLabel != null)
            {
                SetTMPText(tooltipLabel, $"Thả [{face.faceName}] vào <b>HIỆP SĨ</b> ở bên trái để kích hoạt hỗ trợ!");
            }
        }
    }

    private void OnDiceSlotLeftClicked(int slotIndex)
    {
        if (!CanInteractWithDiceSlot(slotIndex)) return;

        if (selectedDiceIndices.Contains(slotIndex))
        {
            selectedDiceIndices.Remove(slotIndex);
        }
        else
        {
            selectedDiceIndices.Add(slotIndex);
        }

        RecalculateSelectedTotal();
        RefreshDiceSlotVisuals();
        ShowDiceTooltip(slotIndex);
        UpdateControlButtonsUI();
    }

    public void UseDiceSlotImmediately(int slotIndex)
    {
        if (!CanInteractWithDiceSlot(slotIndex)) return;

        DiceFaceData face = rolledFaces[slotIndex];
        DropTarget.TargetType naturalTarget = face.TargetsEnemy()
            ? DropTarget.TargetType.Enemy
            : DropTarget.TargetType.Player;

        TryUseDiceFromDrag(slotIndex, naturalTarget);
    }

    public bool TryUseDiceFromDrag(int slotIndex, DropTarget.TargetType dropTargetType)
    {
        if (!CanInteractWithDiceSlot(slotIndex)) return false;
        if (playerEntity == null || enemyEntity == null) return false;

        DiceFaceData face = rolledFaces[slotIndex];
        if (face == null) return false;

        BattleEntity resolvedTarget = face.TargetsEnemy() ? enemyEntity : playerEntity;

        // Chụp Snapshot trước khi thực thi kỹ năng để phục vụ Undo
        EntityStateSnapshot playerSnapBefore = playerEntity.CaptureSnapshot();
        EntityStateSnapshot enemySnapBefore = enemyEntity.CaptureSnapshot();
        int extraConsumedSlot = -1;

        IDiceAction strategy = DiceActionRegistry.GetStrategy(face);
        DiceActionContext ctx = new DiceActionContext(
            playerEntity,
            resolvedTarget,
            face,
            () => TryConsumeUnusedBlockSlot(slotIndex, out extraConsumedSlot));

        if (!strategy.CanExecute(ctx, out string failReason))
        {
            if (tooltipLabel != null && !string.IsNullOrEmpty(failReason))
            {
                SetTMPText(tooltipLabel, failReason);
            }
            return false;
        }

        // Thực thi hiệu ứng logic của kỹ năng xúc xắc
        strategy.Execute(ctx);

        // Ghi nhận vào CommandManager để hỗ trợ Undo
        if (commandManager != null)
        {
            commandManager.RecordDiceAction(new CombatCommandRecord
            {
                description = $"{face.faceName} ({face.valueV})",
                primaryDiceSlotIndex = slotIndex,
                extraConsumedSlotIndex = extraConsumedSlot,
                playerBefore = playerSnapBefore,
                enemyBefore = enemySnapBefore
            });
        }

        // Kích hoạt Hoạt ảnh Hành động Nhân vật tương ứng với từng mặt Xúc xắc!
        TriggerPlayerCharacterActionAnimation(face);
        SpawnSkillFeedbackFloatingText(face, resolvedTarget);

        // Đánh dấu viên xúc xắc đã dùng
        diceUsed[slotIndex] = true;
        selectedDiceIndices.Remove(slotIndex);
        if (extraConsumedSlot >= 0)
        {
            selectedDiceIndices.Remove(extraConsumedSlot);
        }

        RecalculateSelectedTotal();
        RefreshDiceSlotVisuals();
        UpdateControlButtonsUI();

        return true;
    }

    /// <summary>
    /// Điều phối hoạt ảnh hành động của Hiệp Sĩ theo đúng bản chất kỹ năng xúc xắc vừa sử dụng:
    /// - Strike / HeavySlash: Lao tới chém kiếm (HeavySlash có vệt chém chữ X khổng lồ)
    /// - Vampiric: Chém huyết kiếm & gồng hấp thụ máu
    /// - Regenerate / Purify: Gồng tụ khí hồi máu / thanh tẩy kèm vòng hào quang dâng lên
    /// - Block / IronWall: Dậm chân thế thủ & dựng lá chắn quang học trước ngực
    /// - PoisonSting: Lao đâm mũi độc & nổ độc tím trên mục tiêu
    /// - ShockStun: Giương vũ khí phóng tia sét vàng gây choáng
    /// - Blank: Nghiêng người trượt đà
    /// </summary>
    private void TriggerPlayerCharacterActionAnimation(DiceFaceData face)
    {
        if (face == null || playerPortraitImage == null) return;

        CharacterSpriteAnimator pAnim = playerPortraitImage.GetComponent<CharacterSpriteAnimator>();
        if (pAnim == null) return;

        Transform enemyVisualTr = enemyPortraitImage != null
            ? enemyPortraitImage.transform
            : (enemyEntity != null ? enemyEntity.transform : null);

        DiceSkillKind kind = face.GetResolvedSkillKind();
        switch (kind)
        {
            case DiceSkillKind.Strike:
                pAnim.PlaySlashAttack(enemyVisualTr, isHeavySlash: false);
                break;

            case DiceSkillKind.HeavySlash:
                pAnim.PlaySlashAttack(enemyVisualTr, isHeavySlash: true);
                if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.28f, 14f);
                break;

            case DiceSkillKind.Vampiric:
                pAnim.PlayVampiricAttack(enemyVisualTr);
                break;

            case DiceSkillKind.Block:
                pAnim.PlayGuardBrace(isIronWall: false);
                break;

            case DiceSkillKind.IronWall:
                pAnim.PlayGuardBrace(isIronWall: true);
                break;

            case DiceSkillKind.Regenerate:
                pAnim.PlayHealPowerUp(isPurify: false);
                break;

            case DiceSkillKind.Purify:
                pAnim.PlayHealPowerUp(isPurify: true);
                break;

            case DiceSkillKind.PoisonSting:
                pAnim.PlayPoisonStrike(enemyVisualTr);
                break;

            case DiceSkillKind.ShockStun:
                pAnim.PlayShockStunStrike(enemyVisualTr);
                if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.24f, 11f);
                break;

            case DiceSkillKind.Ignite:
                pAnim.PlaySlashAttack(enemyVisualTr, isHeavySlash: true);
                break;

            case DiceSkillKind.ManaSpark:
            case DiceSkillKind.ArcaneSurge:
            case DiceSkillKind.Overcharge:
                pAnim.PlayHealPowerUp(isPurify: true);
                break;

            case DiceSkillKind.Blank:
                pAnim.PlayMissFumble();
                break;
        }
    }

    // ─── HỆ THỐNG 2 KỸ NĂNG NHÂN VẬT (SKILL THƯỜNG & SKILL TỐI THƯỢNG) ─────────

    /// <summary>
    /// Kỹ năng Thường: Tiêu hao Năng lượng thấp (mặc định 2 Energy), gây 1 lượng đơn vị sát thương lên đối thủ.
    /// </summary>
    public void TryCastNormalSkill()
    {
        if (isAnimating || combatEnded || currentPhase != CombatPhase.PlayerAction) return;
        if (playerEntity == null || enemyEntity == null || !enemyEntity.IsAlive()) return;

        int cost = playerEntity.GetEffectiveSkillCost(normalSkillBaseCost);
        if (playerEntity.CurrentEnergy < cost)
        {
            if (tooltipLabel != null)
            {
                SetTMPText(tooltipLabel, $"Không đủ Năng lượng để dùng Kỹ năng Thường! (Cần {cost} NL, hiện có {playerEntity.CurrentEnergy} NL)");
            }
            return;
        }

        EntityStateSnapshot playerSnapBefore = playerEntity.CaptureSnapshot();
        EntityStateSnapshot enemySnapBefore = enemyEntity.CaptureSnapshot();

        if (!playerEntity.TrySpendEnergyForSkill(normalSkillBaseCost)) return;

        int finalDmg = playerEntity.CalculateOutgoingDamage(normalSkillDamage);
        enemyEntity.TakeDamage(finalDmg);

        if (commandManager != null)
        {
            commandManager.RecordDiceAction(new CombatCommandRecord
            {
                description = $"Skill Thường (-{cost} NL, {finalDmg} Sát thương)",
                primaryDiceSlotIndex = -1,
                extraConsumedSlotIndex = -1,
                playerBefore = playerSnapBefore,
                enemyBefore = enemySnapBefore
            });
        }

        if (playerPortraitImage != null)
        {
            CharacterSpriteAnimator pAnim = playerPortraitImage.GetComponent<CharacterSpriteAnimator>();
            Transform enemyVisualTr = enemyPortraitImage != null ? enemyPortraitImage.transform : enemyEntity.transform;
            if (pAnim != null) pAnim.PlaySlashAttack(enemyVisualTr, isHeavySlash: false);
        }

        SpawnCustomFloatingText(playerEntity.transform, $"SKILL THƯỜNG! (-{cost} NL)", new Color(0.45f, 0.88f, 1f));
        UpdateControlButtonsUI();
    }

    /// <summary>
    /// Kỹ năng Tối thượng (Ultimate): Tiêu hao Năng lượng lớn (mặc định 6 Energy),
    /// gây 1 lượng sát thương lớn và áp hiệu ứng Thiêu đốt (Burn) lên đối thủ.
    /// </summary>
    public void TryCastUltimateSkill()
    {
        if (isAnimating || combatEnded || currentPhase != CombatPhase.PlayerAction) return;
        if (playerEntity == null || enemyEntity == null || !enemyEntity.IsAlive()) return;

        int cost = playerEntity.GetEffectiveSkillCost(ultimateSkillBaseCost);
        if (playerEntity.CurrentEnergy < cost)
        {
            if (tooltipLabel != null)
            {
                SetTMPText(tooltipLabel, $"Không đủ Năng lượng để tung Kỹ năng Tối thượng! (Cần {cost} NL, hiện có {playerEntity.CurrentEnergy} NL)");
            }
            return;
        }

        EntityStateSnapshot playerSnapBefore = playerEntity.CaptureSnapshot();
        EntityStateSnapshot enemySnapBefore = enemyEntity.CaptureSnapshot();

        if (!playerEntity.TrySpendEnergyForSkill(ultimateSkillBaseCost)) return;

        int finalDmg = playerEntity.CalculateOutgoingDamage(ultimateSkillDamage);
        enemyEntity.TakeDamage(finalDmg);
        if (enemyEntity.IsAlive())
        {
            enemyEntity.AddBurn(ultimateSkillBurnStacks);
        }

        if (commandManager != null)
        {
            commandManager.RecordDiceAction(new CombatCommandRecord
            {
                description = $"Skill Tối Thượng (-{cost} NL, {finalDmg} Sát thương + {ultimateSkillBurnStacks} Thiêu đốt)",
                primaryDiceSlotIndex = -1,
                extraConsumedSlotIndex = -1,
                playerBefore = playerSnapBefore,
                enemyBefore = enemySnapBefore
            });
        }

        if (playerPortraitImage != null)
        {
            CharacterSpriteAnimator pAnim = playerPortraitImage.GetComponent<CharacterSpriteAnimator>();
            Transform enemyVisualTr = enemyPortraitImage != null ? enemyPortraitImage.transform : enemyEntity.transform;
            if (pAnim != null) pAnim.PlaySlashAttack(enemyVisualTr, isHeavySlash: true);
        }

        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.32f, 16f);
        SpawnCustomFloatingText(playerEntity.transform, $"TỐI THƯỢNG! (-{cost} NL)", new Color(1f, 0.55f, 0.15f));
        SpawnCustomFloatingText(enemyEntity.transform, $"+{ultimateSkillBurnStacks} THIÊU ĐỐT!", new Color(1f, 0.42f, 0.12f));
        UpdateControlButtonsUI();
    }

    private bool TryConsumeUnusedBlockSlot(int excludeSlotIndex, out int consumedSlotIndex)
    {
        consumedSlotIndex = -1;
        for (int i = 0; i < rolledFaces.Count && i < diceUsed.Count; i++)
        {
            if (i == excludeSlotIndex || diceUsed[i] || rolledFaces[i] == null) continue;
            DiceSkillKind kind = rolledFaces[i].GetResolvedSkillKind();
            if (kind == DiceSkillKind.Block || kind == DiceSkillKind.IronWall)
            {
                diceUsed[i] = true;
                consumedSlotIndex = i;
                return true;
            }
        }
        return false;
    }

    private void SpawnSkillFeedbackFloatingText(DiceFaceData face, BattleEntity target)
    {
        if (face == null || target == null) return;
        DiceSkillKind kind = face.GetResolvedSkillKind();

        switch (kind)
        {
            case DiceSkillKind.Strike:
                SpawnCustomFloatingText(playerEntity.transform, "CHÉM!", new Color(1f, 0.88f, 0.45f));
                break;
            case DiceSkillKind.HeavySlash:
                SpawnCustomFloatingText(playerEntity.transform, "TRỌNG KÍCH!", new Color(1f, 0.45f, 0.2f));
                break;
            case DiceSkillKind.Vampiric:
                SpawnCustomFloatingText(playerEntity.transform, "HÚT HUYẾT!", new Color(1f, 0.3f, 0.55f));
                break;
            case DiceSkillKind.Block:
                SpawnCustomFloatingText(playerEntity.transform, $"DỰNG KHIÊN +{face.valueV}", new Color(0.35f, 0.82f, 1f));
                break;
            case DiceSkillKind.IronWall:
                SpawnCustomFloatingText(playerEntity.transform, $"BẤT KHUẤT +{face.valueV}", new Color(1f, 0.85f, 0.35f));
                break;
            case DiceSkillKind.Regenerate:
                SpawnCustomFloatingText(playerEntity.transform, "HỒI PHỤC!", new Color(0.35f, 0.98f, 0.55f));
                break;
            case DiceSkillKind.Purify:
                SpawnCustomFloatingText(playerEntity.transform, "THANH TẨY!", new Color(0.55f, 0.98f, 1f));
                break;
            case DiceSkillKind.PoisonSting:
                SpawnCustomFloatingText(enemyEntity.transform, $"+{face.valueV} TRÚNG ĐỘC", new Color(0.75f, 0.38f, 1f));
                break;
            case DiceSkillKind.ShockStun:
                SpawnCustomFloatingText(enemyEntity.transform, "CHOÁNG!", new Color(1f, 0.92f, 0.25f));
                break;
            case DiceSkillKind.Ignite:
                SpawnCustomFloatingText(enemyEntity.transform, $"+{face.valueV} THIÊU ĐỐT!", new Color(1f, 0.55f, 0.15f));
                break;
            case DiceSkillKind.ManaSpark:
                SpawnCustomFloatingText(playerEntity.transform, $"+{face.valueV} NĂNG LƯỢNG!", new Color(0.38f, 0.88f, 1f));
                break;
            case DiceSkillKind.ArcaneSurge:
                SpawnCustomFloatingText(playerEntity.transform, $"+{face.valueV} NĂNG LƯỢNG & +2 GIÁP!", new Color(0.45f, 0.92f, 1f));
                break;
            case DiceSkillKind.Overcharge:
                SpawnCustomFloatingText(playerEntity.transform, $"+{face.valueV} QUÁ TẢI (-1 NL SKILL)!", new Color(0.95f, 0.78f, 0.25f));
                break;
            case DiceSkillKind.Blank:
                SpawnCustomFloatingText(playerEntity.transform, "TRƯỢT ĐÀ!", new Color(0.75f, 0.75f, 0.75f));
                break;
        }
    }

    public void UndoLastDiceAction()
    {
        if (isAnimating || combatEnded || currentPhase != CombatPhase.PlayerAction) return;
        if (commandManager == null) return;

        if (commandManager.TryPopLastDiceAction(out CombatCommandRecord record))
        {
            if (playerEntity != null) playerEntity.RestoreSnapshot(record.playerBefore);
            if (enemyEntity != null) enemyEntity.RestoreSnapshot(record.enemyBefore);

            if (record.primaryDiceSlotIndex >= 0 && record.primaryDiceSlotIndex < diceUsed.Count)
            {
                diceUsed[record.primaryDiceSlotIndex] = false;
            }
            if (record.extraConsumedSlotIndex >= 0 && record.extraConsumedSlotIndex < diceUsed.Count)
            {
                diceUsed[record.extraConsumedSlotIndex] = false;
            }

            RecalculateSelectedTotal();
            RefreshDiceSlotVisuals();
            UpdateControlButtonsUI();

            if (tooltipLabel != null)
            {
                SetTMPText(tooltipLabel, $"Đã hoàn tác (Undo): <b>[{record.description}]</b>");
            }
        }
    }

    private IEnumerator AnimateReroll()
    {
        if (rerollsLeft <= 0) yield break;

        bool hasUnused = false;
        for (int i = 0; i < diceUsed.Count; i++)
        {
            if (!diceUsed[i]) { hasUnused = true; break; }
        }
        if (!hasUnused) yield break;

        rerollsLeft--;
        isAnimating = true;
        UpdateControlButtonsUI();

        yield return StartCoroutine(RollAllDiceCoroutine(true));

        isAnimating = false;
        UpdateControlButtonsUI();
    }

    private IEnumerator ExecuteEndTurnSequence()
    {
        if (enemyEntity == null || playerEntity == null || combatEnded) yield break;
        isAnimating = true;
        UpdateControlButtonsUI();

        // Nếu người chơi đã Click chọn các viên xúc xắc mà chưa Kéo-Thả, tự động kích hoạt tuần tự kèm hoạt ảnh!
        if (selectedDiceIndices.Count > 0)
        {
            List<int> pendingSlots = new List<int>(selectedDiceIndices);
            foreach (int slotIdx in pendingSlots)
            {
                if (combatEnded || !enemyEntity.IsAlive()) break;
                if (slotIdx >= 0 && slotIdx < diceUsed.Count && !diceUsed[slotIdx])
                {
                    DiceFaceData face = rolledFaces[slotIdx];
                    if (face != null)
                    {
                        DropTarget.TargetType naturalTarget = face.TargetsEnemy()
                            ? DropTarget.TargetType.Enemy
                            : DropTarget.TargetType.Player;

                        isAnimating = false;
                        TryUseDiceFromDrag(slotIdx, naturalTarget);
                        isAnimating = true;
                        yield return new WaitForSeconds(0.45f);
                    }
                }
            }
        }

        if (combatEnded || !enemyEntity.IsAlive()) yield break;

        yield return StartCoroutine(ExecuteEnemyAndRoundEndCoroutine());
    }

    private IEnumerator ExecuteEnemyAndRoundEndCoroutine()
    {
        // 4. Pha Quái Hành Động (EnemyExecution)
        currentPhase = CombatPhase.EnemyExecution;
        if (commandManager != null) commandManager.EndTurn();
        UpdateEnemyIntentUI();
        UpdateControlButtonsUI();

        yield return new WaitForSeconds(0.45f);

        if (enemyEntity != null && enemyEntity.IsAlive() && !combatEnded)
        {
            if (enemyEntity.CheckAndConsumeStun())
            {
                SpawnCustomFloatingText(enemyEntity.transform, "BỊ CHOÁNG (MẤT LƯỢT)!", new Color(1f, 0.9f, 0.25f));
                yield return new WaitForSeconds(0.75f);
            }
            else
            {
                // Kích hoạt hoạt ảnh hành động của Quái Vật theo đúng loại Intent của nó!
                yield return StartCoroutine(PlayEnemyIntentAnimationAndExecute());
            }
        }

        if (combatEnded || playerEntity == null || !playerEntity.IsAlive() || enemyEntity == null || !enemyEntity.IsAlive())
        {
            yield break;
        }

        // 5. Pha Kết Thúc Hiệp (RoundEnd): Kích hoạt Độc (Poison) & Thiêu đốt (Burn) xuyên giáp, giảm Suy Yếu
        currentPhase = CombatPhase.RoundEnd;
        UpdateEnemyIntentUI();

        if (enemyEntity.PoisonStacks > 0)
        {
            SpawnCustomFloatingText(enemyEntity.transform, $"ĐỘC -{enemyEntity.PoisonStacks}", new Color(0.68f, 0.32f, 0.95f));
        }
        if (enemyEntity.BurnStacks > 0)
        {
            SpawnCustomFloatingText(enemyEntity.transform, $"THIÊU ĐỐT -{enemyEntity.BurnStacks}", new Color(1f, 0.45f, 0.15f));
        }
        enemyEntity.ProcessRoundEndStatuses(false);

        if (combatEnded || !enemyEntity.IsAlive()) yield break;

        if (playerEntity.PoisonStacks > 0)
        {
            SpawnCustomFloatingText(playerEntity.transform, $"ĐỘC -{playerEntity.PoisonStacks}", new Color(0.68f, 0.32f, 0.95f));
        }
        if (playerEntity.BurnStacks > 0)
        {
            SpawnCustomFloatingText(playerEntity.transform, $"THIÊU ĐỐT -{playerEntity.BurnStacks}", new Color(1f, 0.45f, 0.15f));
        }
        playerEntity.ProcessRoundEndStatuses(false);

        if (combatEnded || !playerEntity.IsAlive()) yield break;

        yield return new WaitForSeconds(0.38f);

        // Chuyển sang hiệp kế tiếp
        StartCoroutine(BeginRoundCoroutine());
    }

    private IEnumerator PlayEnemyIntentAnimationAndExecute()
    {
        CharacterSpriteAnimator eAnim = enemyPortraitImage != null
            ? enemyPortraitImage.GetComponent<CharacterSpriteAnimator>()
            : null;

        Transform playerVisualTr = playerPortraitImage != null
            ? playerPortraitImage.transform
            : (playerEntity != null ? playerEntity.transform : null);

        if (eAnim != null)
        {
            switch (currentEnemyIntent.intentType)
            {
                case EnemyIntentType.Attack:
                    eAnim.PlaySlashAttack(playerVisualTr, isHeavySlash: currentStage >= 3);
                    yield return new WaitForSeconds(0.22f);
                    break;

                case EnemyIntentType.Defend:
                    eAnim.PlayGuardBrace(isIronWall: false);
                    yield return new WaitForSeconds(0.18f);
                    break;

                case EnemyIntentType.AttackAndPoison:
                    eAnim.PlayPoisonStrike(playerVisualTr);
                    yield return new WaitForSeconds(0.18f);
                    break;

                case EnemyIntentType.ChargeBuffAttack:
                    eAnim.PlayEnrageCharge();
                    yield return new WaitForSeconds(0.24f);
                    break;

                case EnemyIntentType.AttackAndHeal:
                    eAnim.PlayVampiricAttack(playerVisualTr);
                    yield return new WaitForSeconds(0.22f);
                    break;
            }
        }

        ExecuteCurrentEnemyIntent();
        yield return new WaitForSeconds(0.62f);
    }

    private void ExecuteCurrentEnemyIntent()
    {
        if (enemyEntity == null || playerEntity == null) return;

        switch (currentEnemyIntent.intentType)
        {
            case EnemyIntentType.Attack:
            {
                int dmg = enemyEntity.CalculateOutgoingDamage(currentEnemyIntent.primaryValue);
                playerEntity.TakeDamage(dmg);
                break;
            }
            case EnemyIntentType.Defend:
            {
                enemyEntity.AddShield(currentEnemyIntent.primaryValue);
                SpawnCustomFloatingText(enemyEntity.transform, $"+{currentEnemyIntent.primaryValue} DỰNG GIÁP", new Color(0.4f, 0.82f, 1f));
                break;
            }
            case EnemyIntentType.AttackAndPoison:
            {
                int dmg = enemyEntity.CalculateOutgoingDamage(currentEnemyIntent.primaryValue);
                playerEntity.TakeDamage(dmg);
                playerEntity.AddPoison(currentEnemyIntent.secondaryValue);
                SpawnCustomFloatingText(playerEntity.transform, $"+{currentEnemyIntent.secondaryValue} TRÚNG ĐỘC", new Color(0.75f, 0.35f, 0.98f));
                break;
            }
            case EnemyIntentType.ChargeBuffAttack:
            {
                enemyEntity.AddBonusAttack(currentEnemyIntent.primaryValue);
                SpawnCustomFloatingText(enemyEntity.transform, $"CUỒNG NỘ +{currentEnemyIntent.primaryValue} CÔNG!", new Color(1f, 0.5f, 0.2f));
                break;
            }
            case EnemyIntentType.AttackAndHeal:
            {
                int dmg = enemyEntity.CalculateOutgoingDamage(currentEnemyIntent.primaryValue);
                playerEntity.TakeDamage(dmg);
                enemyEntity.Heal(currentEnemyIntent.secondaryValue);
                break;
            }
        }
    }

    // =========================================================================
    // 7. HIỂN THỊ UI XÚC XẮC, TOOLTIP, INTENT & GAME JUICE
    // =========================================================================

    public void RefreshDiceSlotVisuals()
    {
        for (int i = 0; i < diceSlots.Count; i++)
        {
            GameObject slotObj = diceSlots[i];
            if (slotObj == null) continue;

            DiceFaceData face = i < rolledFaces.Count ? rolledFaces[i] : null;
            bool used = i < diceUsed.Count && diceUsed[i];
            bool selected = selectedDiceIndices.Contains(i);

            CanvasGroup cg = slotObj.GetComponent<CanvasGroup>();
            if (cg == null) cg = slotObj.AddComponent<CanvasGroup>();
            cg.alpha = used ? 0.32f : 1f;
            cg.blocksRaycasts = true;

            slotObj.transform.localScale = selected && !used
                ? new Vector3(1.07f, 1.07f, 1f)
                : Vector3.one;

            if (face != null)
            {
                string valDisplay = GetFaceValueSymbol(face);
                string targetTag = face.TargetsEnemy() ? "<size=11><color=#fca5a5>Địch</color></size>" : "<size=11><color=#86efac>Hiệp Sĩ</color></size>";
                string titleDisplay = used
                    ? $"{face.faceName}\n<size=11><color=#94a3b8>(ĐÃ DÙNG)</color></size>"
                    : $"{face.faceName}\n{targetTag}";
                SetDiceSlotText(i, titleDisplay, valDisplay);
                SetDiceSlotIcon(i, GetSkillSprite(face));
            }

            Image img = slotObj.GetComponent<Image>();
            if (img != null)
            {
                if (used)
                {
                    img.color = new Color(0.18f, 0.18f, 0.22f, 0.65f);
                }
                else if (selected)
                {
                    img.color = face != null ? Color.Lerp(GetSkillTintColor(face.GetResolvedSkillKind(), slotBaseColors[i]), Color.white, 0.45f) : new Color(0.86f, 0.64f, 0.14f, 0.98f);
                }
                else if (face != null)
                {
                    img.color = GetSkillTintColor(face.GetResolvedSkillKind(), slotBaseColors[i]);
                }
                else
                {
                    img.color = slotBaseColors[i];
                }
            }

            Outline outline = slotObj.GetComponent<Outline>();
            if (outline != null)
            {
                if (used)
                {
                    outline.effectColor = new Color(0.28f, 0.28f, 0.32f, 0.4f);
                }
                else if (selected)
                {
                    outline.effectColor = new Color(1f, 0.88f, 0.35f, 1f);
                }
                else if (face != null)
                {
                    outline.effectColor = GetSkillOutlineColor(face.GetResolvedSkillKind());
                }
                else
                {
                    outline.effectColor = new Color(0.55f, 0.45f, 0.22f, 0.65f);
                }
            }
        }
    }

    private string GetFaceValueSymbol(DiceFaceData face)
    {
        if (face == null) return "0";
        switch (face.GetResolvedSkillKind())
        {
            case DiceSkillKind.Strike:
            case DiceSkillKind.HeavySlash:
                return face.valueV.ToString();
            case DiceSkillKind.Vampiric:
                return face.valueV.ToString();
            case DiceSkillKind.Block:
            case DiceSkillKind.IronWall:
                return face.valueV.ToString();
            case DiceSkillKind.Regenerate:
                return face.valueV.ToString();
            case DiceSkillKind.Purify:
                return "0";
            case DiceSkillKind.PoisonSting:
                return face.valueV.ToString();
            case DiceSkillKind.ShockStun:
                return face.valueV.ToString();
            case DiceSkillKind.Ignite:
                return face.valueV.ToString();
            case DiceSkillKind.ManaSpark:
            case DiceSkillKind.ArcaneSurge:
            case DiceSkillKind.Overcharge:
                return $"+{face.valueV}";
            case DiceSkillKind.Blank:
                return "0";
            default:
                return face.valueV.ToString();
        }
    }

    /// <summary>
    /// Màu nền các ô xúc xắc khớp logic với màu file sprite xúc xắc theo yêu cầu:
    /// - Xanh lá - Hồi máu
    /// - Trắng - Giáp
    /// - Vàng - Choáng
    /// - Đỏ - Tấn công
    /// - Tím - Độc
    /// - Cam - Thiêu đốt
    /// - Xanh dương - Năng lượng
    /// </summary>
    private Color GetSkillOutlineColor(DiceSkillKind kind)
    {
        switch (kind)
        {
            case DiceSkillKind.Strike:
            case DiceSkillKind.HeavySlash:
            case DiceSkillKind.Vampiric:
                return new Color(0.95f, 0.28f, 0.32f, 0.95f);
            case DiceSkillKind.Block:
            case DiceSkillKind.IronWall:
                return new Color(0.68f, 0.75f, 0.88f, 0.95f);
            case DiceSkillKind.Regenerate:
            case DiceSkillKind.Purify:
                return new Color(0.28f, 0.95f, 0.45f, 0.95f);
            case DiceSkillKind.PoisonSting:
                return new Color(0.78f, 0.35f, 0.98f, 0.95f);
            case DiceSkillKind.ShockStun:
                return new Color(0.98f, 0.85f, 0.22f, 0.95f);
            case DiceSkillKind.Ignite:
                return new Color(1f, 0.55f, 0.15f, 0.95f);
            case DiceSkillKind.ManaSpark:
            case DiceSkillKind.ArcaneSurge:
            case DiceSkillKind.Overcharge:
                return new Color(0.35f, 0.78f, 1f, 0.95f);
            default:
                return new Color(0.55f, 0.45f, 0.22f, 0.65f);
        }
    }

    private Color GetSkillTintColor(DiceSkillKind kind, Color fallback)
    {
        switch (kind)
        {
            case DiceSkillKind.Strike:
            case DiceSkillKind.HeavySlash:
            case DiceSkillKind.Vampiric:
                return new Color(0.70f, 0.14f, 0.16f, 0.96f);   // Đỏ - Tấn công

            case DiceSkillKind.Block:
            case DiceSkillKind.IronWall:
                return new Color(0.90f, 0.92f, 0.95f, 0.98f);   // Trắng - Giáp

            case DiceSkillKind.Regenerate:
            case DiceSkillKind.Purify:
                return new Color(0.12f, 0.58f, 0.26f, 0.96f);   // Xanh lá - Hồi máu

            case DiceSkillKind.PoisonSting:
                return new Color(0.54f, 0.18f, 0.74f, 0.96f);   // Tím - Độc

            case DiceSkillKind.ShockStun:
                return new Color(0.86f, 0.70f, 0.10f, 0.96f);   // Vàng - Choáng

            case DiceSkillKind.Ignite:
                return new Color(0.92f, 0.46f, 0.08f, 0.96f);   // Cam - Thiêu đốt

            case DiceSkillKind.ManaSpark:
            case DiceSkillKind.ArcaneSurge:
            case DiceSkillKind.Overcharge:
                return new Color(0.12f, 0.46f, 0.88f, 0.96f);   // Xanh dương - Năng lượng

            case DiceSkillKind.Blank:
                return new Color(0.24f, 0.25f, 0.30f, 0.88f);   // Xám - Blank

            default:
                return fallback;
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
            case DiceSkillKind.Vampiric:
                return diceFaceAttackSprite;
            case DiceSkillKind.Block:
            case DiceSkillKind.IronWall:
                return diceFaceShieldSprite;
            case DiceSkillKind.PoisonSting:
                return diceFacePoisonSprite != null ? diceFacePoisonSprite : diceFaceAttackSprite;
            case DiceSkillKind.Regenerate:
            case DiceSkillKind.Purify:
                return diceFaceHealSprite != null ? diceFaceHealSprite : diceFaceShieldSprite;
            case DiceSkillKind.ShockStun:
                return diceFaceLightningSprite != null ? diceFaceLightningSprite : diceFaceAttackSprite;
            case DiceSkillKind.Ignite:
                return diceFaceFireSprite != null ? diceFaceFireSprite : diceFaceAttackSprite;
            case DiceSkillKind.ManaSpark:
            case DiceSkillKind.ArcaneSurge:
            case DiceSkillKind.Overcharge:
                return diceFaceEnergySprite != null ? diceFaceEnergySprite : diceFaceShieldSprite;
            case DiceSkillKind.Blank:
                return diceFaceBlankSprite != null ? diceFaceBlankSprite : diceFaceShieldSprite;
            default:
                return diceFaceAttackSprite;
        }
    }
    private void SetDiceSlotIcon(int slotIndex, Sprite sprite)
    {
        if (sprite == null || slotIndex < 0 || slotIndex >= diceSlots.Count || diceSlots[slotIndex] == null) return;
        Transform slotTransform = diceSlots[slotIndex].transform;
        Transform iconTr = slotTransform.Find("DiceIcon");
        if (iconTr == null) iconTr = slotTransform.Find("Icon");
        if (iconTr != null)
        {
            Image iconImg = iconTr.GetComponent<Image>();
            if (iconImg != null) iconImg.sprite = sprite;
        }
    }

    private void SetDiceSlotText(int slotIndex, string nameText, string valueText)
    {
        if (slotIndex < 0 || slotIndex >= diceSlots.Count || diceSlots[slotIndex] == null) return;
        Transform slotTransform = diceSlots[slotIndex].transform;

        Transform nameTr = slotTransform.Find("DiceName");
        if (nameTr == null) nameTr = slotTransform.Find("Name");
        if (nameTr != null && tmpType != null)
        {
            Component nameTmp = nameTr.GetComponent(tmpType);
            SetTMPText(nameTmp, nameText);
        }

        Transform valTr = slotTransform.Find("DiceVal");
        if (valTr == null) valTr = slotTransform.Find("Val");
        if (valTr != null && tmpType != null)
        {
            Component valTmp = valTr.GetComponent(tmpType);
            SetTMPText(valTmp, valueText);
        }
    }

    public void ShowDiceTooltip(int slotIndex)
    {
        if (tooltipLabel == null) return;
        if (slotIndex < 0 || slotIndex >= rolledFaces.Count || rolledFaces[slotIndex] == null)
        {
            SetDefaultHintText();
            return;
        }

        DiceFaceData face = rolledFaces[slotIndex];
        string actionVerb = GetActionAnimationDescription(face.GetResolvedSkillKind());
        string flatTooltip = face.GetTooltipText().Replace("\n", " — ");
        SetTMPText(tooltipLabel, $"{flatTooltip}   |   <i>{actionVerb}</i>");
    }

    private string GetActionAnimationDescription(DiceSkillKind kind)
    {
        switch (kind)
        {
            case DiceSkillKind.Strike: return "Hành động: Lao tới chém kiếm";
            case DiceSkillKind.HeavySlash: return "Hành động: Trọng kích chém chữ X";
            case DiceSkillKind.Vampiric: return "Hành động: Chém huyết kiếm & Hấp thụ";
            case DiceSkillKind.Block: return "Hành động: Dậm chân dựng khiên chắn";
            case DiceSkillKind.IronWall: return "Hành động: Thế thủ Kim Cang Bất Khuất";
            case DiceSkillKind.Regenerate: return "Hành động: Gồng tụ khí hồi máu";
            case DiceSkillKind.Purify: return "Hành động: Gồng thanh tẩy độc tố";
            case DiceSkillKind.PoisonSting: return "Hành động: Đâm mũi kiếm kịch độc";
            case DiceSkillKind.ShockStun: return "Hành động: Giáng sấm sét gây choáng";
            case DiceSkillKind.Ignite: return "Hành động: Chém bốc lửa & Thiêu đốt";
            case DiceSkillKind.ManaSpark:
            case DiceSkillKind.ArcaneSurge:
            case DiceSkillKind.Overcharge: return "Hành động: Tụ khí nạp Năng lượng kỹ năng";
            default: return "Hành động: Trượt đà";
        }
    }

    public void HideTooltip()
    {
        SetDefaultHintText();
    }

    public void ShowCustomBannerTooltip(string message)
    {
        if (tooltipLabel == null) return;
        SetTMPText(tooltipLabel, message);
    }

    public bool CanPlayerCastCharacterSkills()
    {
        return !isAnimating && !combatEnded && currentPhase == CombatPhase.PlayerAction;
    }

    private void SetDefaultHintText()
    {
        if (tooltipLabel == null) return;
        SetTMPText(tooltipLabel, string.Empty);
    }

    private void LateUpdate()
    {
        // Panel tooltip chỉ hiển thị khi có nội dung (hover xúc xắc, hover skill hoặc thông báo lỗi)
        if (tooltipPanel != null && tooltipLabel != null && tmpType != null)
        {
            string txt = tmpType.GetProperty("text")?.GetValue(tooltipLabel) as string;
            bool shouldShow = !string.IsNullOrWhiteSpace(txt);
            if (tooltipPanel.activeSelf != shouldShow)
            {
                tooltipPanel.SetActive(shouldShow);
            }
        }
    }
    private void UpdateEnemyIntentUI()
    {
        int bonusAtk = enemyEntity != null ? enemyEntity.BonusAttack : 0;
        string desc = currentEnemyIntent.GetDisplayDescription(bonusAtk);
        if (enemyIntentLabel != null)
        {
            if (enemyEntity != null && enemyEntity.StunStacks > 0)
            {
                SetTMPText(enemyIntentLabel, "ĐANG BỊ CHOÁNG — Mất lượt hành động kế tiếp!");
            }
            else
            {
                SetTMPText(enemyIntentLabel, $"Ý ĐỊNH ĐỊCH:  {desc}");
            }
        }

        // Đổi màu viền khung Intent theo loại hành động sắp tới của Quái Vật
        if (enemyIntentOutline != null)
        {
            switch (currentEnemyIntent.intentType)
            {
                case EnemyIntentType.Defend:
                    enemyIntentOutline.effectColor = new Color(0.3f, 0.8f, 1f, 0.9f);
                    break;
                case EnemyIntentType.AttackAndPoison:
                    enemyIntentOutline.effectColor = new Color(0.75f, 0.35f, 1f, 0.9f);
                    break;
                case EnemyIntentType.ChargeBuffAttack:
                    enemyIntentOutline.effectColor = new Color(1f, 0.6f, 0.15f, 0.95f);
                    break;
                default:
                    enemyIntentOutline.effectColor = new Color(0.95f, 0.3f, 0.25f, 0.9f);
                    break;
            }
        }

        GameObject turnBadgeGo = GameObject.Find("TurnBadge");
        if (turnBadgeGo != null && tmpType != null)
        {
            Component turnTmp = turnBadgeGo.GetComponentInChildren(tmpType);
            string phaseText = GetPhaseDisplayName(currentPhase);
            SetTMPText(turnTmp, $"ẢI {currentStage}/3   -   HIỆP {currentRound}   -   {phaseText}");
        }
    }

    private string GetPhaseDisplayName(CombatPhase phase)
    {
        switch (phase)
        {
            case CombatPhase.PlayerRoll: return "<color=#93c5fd>ĐANG GIEO XÚC XẮC</color>";
            case CombatPhase.PlayerAction: return "<color=#fde047>LƯỢT HIỆP SĨ</color>";
            case CombatPhase.EnemyExecution: return "<color=#fca5a5>QUÁI VẬT HÀNH ĐỘNG</color>";
            case CombatPhase.RoundEnd: return "<color=#d8b4fe>KẾT THÚC HIỆP</color>";
            case CombatPhase.CombatOver: return "<color=#86efac>KẾT THÚC TRẬN</color>";
            default: return "KHỞI ĐẦU HIỆP";
        }
    }

    private void UpdateControlButtonsUI()
    {
        if (rerollButton != null)
        {
            rerollButton.interactable = !isAnimating && currentPhase == CombatPhase.PlayerAction && rerollsLeft > 0;
            UpdateButtonCaption(rerollButton, $"GIEO LẠI ({rerollsLeft})");
        }

        if (endTurnButton != null)
        {
            endTurnButton.interactable = !isAnimating && currentPhase == CombatPhase.PlayerAction;
            string endCaption = selectedDiceIndices.Count > 0
                ? $"DÙNG ({selectedDiceIndices.Count}) & KẾT THÚC LƯỢT"
                : "KẾT THÚC LƯỢT (END TURN)";
            UpdateButtonCaption(endTurnButton, endCaption);
        }

        if (undoButton != null)
        {
            int undoCount = commandManager != null ? commandManager.UndoCount : 0;
            bool canUndo = !isAnimating &&
                           currentPhase == CombatPhase.PlayerAction &&
                           undoCount > 0;
            undoButton.interactable = canUndo;
            UpdateButtonCaption(undoButton, $"UNDO ({undoCount}) [Z]");
        }

        UpdateCharacterSkillsUI();
    }

    private void UpdateCharacterSkillsUI()
    {
        if (characterSkillBarUI != null)
        {
            characterSkillBarUI.RefreshSkillBarState();
        }
    }

    private void UpdateButtonCaption(Button btn, string text)
    {
        if (btn == null || tmpType == null) return;
        Component txt = btn.GetComponentInChildren(tmpType);
        SetTMPText(txt, text);
    }

    private void RecalculateSelectedTotal()
    {
        selectedDiceTotal = 0;
        foreach (int idx in selectedDiceIndices)
        {
            if (idx >= 0 && idx < rolledFaces.Count && rolledFaces[idx] != null)
            {
                selectedDiceTotal += rolledFaces[idx].valueV;
            }
        }
    }

    // =========================================================================
    // 8. HIỆU ỨNG THANH MÁU, CHỮ NỔI (FLOATING TEXT) & XỬ LÝ THẮNG / THUA
    // =========================================================================

    private void UpdateHPBar(Image fill, int current, int max)
    {
        if (fill == null || max <= 0) return;
        float targetPct = Mathf.Clamp01((float)current / max);
        StartCoroutine(SmoothLerpHP(fill, targetPct));
    }

    private IEnumerator SmoothLerpHP(Image fill, float targetPct)
    {
        RectTransform rt = fill.transform as RectTransform;
        if (rt == null) yield break;

        float startPct = rt.anchorMax.x;
        float elapsed = 0f;
        float duration = 0.32f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);
            rt.anchorMax = new Vector2(Mathf.Lerp(startPct, targetPct, t), rt.anchorMax.y);
            rt.sizeDelta = new Vector2(0f, rt.sizeDelta.y);
            yield return null;
        }
        rt.anchorMax = new Vector2(targetPct, rt.anchorMax.y);
    }

    private void SpawnFloatingText(Transform parent, int damage, bool isPierce)
    {
        if (parent == null) return;
        GameObject go = new GameObject("FloatingText");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(Random.Range(-25f, 25f), 65f);

        if (tmpType != null)
        {
            Component tmp = go.AddComponent(tmpType);
            SetTMPText(tmp, "-" + damage.ToString());
            tmpType.GetProperty("fontSize")?.SetValue(tmp, 44f);
            Color red = new Color(0.98f, 0.25f, 0.25f);
            tmpType.GetProperty("color")?.SetValue(tmp, red);
            tmpType.GetProperty("alignment")?.SetValue(tmp, 514);
        }

        FloatingText ft = go.AddComponent<FloatingText>();
        ft.floatSpeed = 85f;
        ft.fadeDuration = 0.85f;
        ft.Setup(damage, isPierce);
    }

    private void SpawnCustomFloatingText(Transform parent, string message, Color color)
    {
        if (parent == null) return;
        GameObject go = new GameObject("CustomFloatingText");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(Random.Range(-20f, 20f), 95f);
        rt.sizeDelta = new Vector2(360f, 52f);

        if (tmpType != null)
        {
            Component tmp = go.AddComponent(tmpType);
            SetTMPText(tmp, message);
            tmpType.GetProperty("fontSize")?.SetValue(tmp, 28f);
            tmpType.GetProperty("color")?.SetValue(tmp, color);
            tmpType.GetProperty("alignment")?.SetValue(tmp, 514);
        }

        FloatingText ft = go.AddComponent<FloatingText>();
        ft.floatSpeed = 68f;
        ft.fadeDuration = 0.95f;
        ft.SetupCustom(message, color);
    }

    private void UpdateHPLabel(Component label, int current, int max)
    {
        if (label == null) return;
        SetTMPText(label, $"{current} / {max} HP");
    }

    private void SetTMPText(Component tmpComp, string value)
    {
        if (tmpComp == null || tmpType == null) return;
        tmpType.GetProperty("text")?.SetValue(tmpComp, value);
    }

    private IEnumerator HandleEnemyDefeated()
    {
        if (combatEnded) yield break;
        combatEnded = true;
        currentPhase = CombatPhase.CombatOver;
        isAnimating = true;
        UpdateControlButtonsUI();

        UpdateEnemyIntentUI();
        if (enemyIntentLabel != null)
        {
            SetTMPText(enemyIntentLabel, "CHIẾN THẮNG! QUÁI VẬT ĐÃ BỊ TIÊU DIỆT!");
        }

        // Hiệp sĩ gồng ăn mừng chiến thắng
        if (playerPortraitImage != null)
        {
            CharacterSpriteAnimator pAnim = playerPortraitImage.GetComponent<CharacterSpriteAnimator>();
            if (pAnim != null) pAnim.PlayHealPowerUp(isPurify: true);
        }

        yield return new WaitForSeconds(0.75f);

        int clearedStage = currentStage;
        int remHP = playerEntity != null ? Mathf.Max(1, playerEntity.CurrentHP) : 70;
        int maxHP = playerEntity != null ? playerEntity.MaxHP : 70;
        string eName = currentEnemyData != null && !string.IsNullOrWhiteSpace(currentEnemyData.enemyName)
            ? currentEnemyData.enemyName
            : (clearedStage == 3 ? "Minotaur Boss" : (clearedStage == 2 ? "Armored Orc" : "Goblin Scout"));

        GameManager gm = GameManager.ResolveInstance();
        if (gm != null)
        {
            gm.playerCurrentHP = remHP;
            int totalStages = gm.GetTotalStages();
            gm.lastClearedStageIndex = Mathf.Max(gm.lastClearedStageIndex, clearedStage);
            gm.heroMapStageIndex = clearedStage;
            gm.pendingForgeUpgrades = Mathf.Max(1, gm.pendingForgeUpgrades + 1);

            if (clearedStage < totalStages)
            {
                gm.currentStageIndex = clearedStage + 1;
                gm.highestUnlockedStageIndex = Mathf.Max(gm.highestUnlockedStageIndex, gm.currentStageIndex);
                gm.isRunVictory = false;
            }
            else
            {
                gm.highestUnlockedStageIndex = totalStages;
                gm.isRunVictory = true;
            }
        }

        Canvas rootCanvas = FindAnyObjectByType<Canvas>();
        if (rootCanvas != null)
        {
            CombatResultModalUI modal = CombatResultModalUI.ShowOrCreate(rootCanvas.transform);
            if (modal != null)
            {
                modal.ShowVictory(clearedStage, currentRound, remHP, maxHP, eName);
                yield break;
            }
        }

        if (gm != null) gm.LoadStageMapScene();
        else UnityEngine.SceneManagement.SceneManager.LoadScene("StageMap");
    }

    private IEnumerator HandlePlayerDefeated()
    {
        if (combatEnded) yield break;
        combatEnded = true;
        currentPhase = CombatPhase.CombatOver;
        isAnimating = true;
        UpdateControlButtonsUI();

        UpdateEnemyIntentUI();
        if (enemyIntentLabel != null)
        {
            SetTMPText(enemyIntentLabel, "HIỆP SĨ ĐÃ NGÃ XUỐNG — HÀNH TRÌNH KHÉP LẠI!");
        }

        yield return new WaitForSeconds(0.95f);

        string eName = currentEnemyData != null && !string.IsNullOrWhiteSpace(currentEnemyData.enemyName)
            ? currentEnemyData.enemyName
            : (currentStage == 3 ? "Minotaur Boss" : (currentStage == 2 ? "Armored Orc" : "Goblin Scout"));

        Canvas rootCanvas = FindAnyObjectByType<Canvas>();
        if (rootCanvas != null)
        {
            CombatResultModalUI modal = CombatResultModalUI.ShowOrCreate(rootCanvas.transform);
            if (modal != null)
            {
                modal.ShowDefeat(currentStage, currentRound, eName);
                yield break;
            }
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadMainMenu();
        }
    }
}
