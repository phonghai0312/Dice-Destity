using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// SimpleCombatManager — Điều phối toàn bộ vòng lặp chiến đấu theo GDD:
/// - Máy trạng thái 5 pha (RoundStart -> PlayerRoll -> PlayerAction -> EnemyExecution -> RoundEnd)
/// - Hệ thống 3 viên xúc xắc 6 mặt từ GameManager.playerDiceBag + Strategy Pattern (IDiceAction)
/// - Hỗ trợ song song Kéo-Thả (Drag & Drop), Click chọn + End Turn, Click phải/Double-click dùng ngay
/// - Re-roll có giới hạn lượt, Undo bước đi (CommandManager Snapshot), Tooltip, Enemy Intent & Status UI.
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

    // Runtime UI Elements (tự động gắn nếu Scene chưa có sẵn)
    private Component playerStatusLabel;
    private Component enemyStatusLabel;
    private Component enemyIntentLabel;
    private Component tooltipLabel;
    private GameObject tooltipPanel;

    private void Start()
    {
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

        if (playerPortraitImage != null && playerPortraitImage.GetComponent<CharacterSpriteAnimator>() == null)
        {
            playerPortraitImage.gameObject.AddComponent<CharacterSpriteAnimator>();
        }
        if (enemyPortraitImage != null && enemyPortraitImage.GetComponent<CharacterSpriteAnimator>() == null)
        {
            enemyPortraitImage.gameObject.AddComponent<CharacterSpriteAnimator>();
        }

        EnsureDropTargetsConfigured();
        EnsureRuntimeCombatOverlays();
        RegisterEntityEvents();
        LoadStageData();
        SetupDiceSlots();
        SetupActionButtons();

        StartCoroutine(BeginRoundCoroutine());
    }

    private void Update()
    {
        // Phím tắt Z để Undo nhanh trong pha PlayerAction
        if (!isAnimating && !combatEnded && currentPhase == CombatPhase.PlayerAction)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                UndoLastDiceAction();
            }
        }
    }

    // =========================================================================
    // 1. KHỞI TẠO DỮ LIỆU MÀN CHƠI & UI TỰ ĐỘNG
    // =========================================================================

    private void LoadStageData()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnsureDiceBagInitialized();
            currentStage = Mathf.Max(1, GameManager.Instance.currentStageIndex);

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
            SetTMPText(enemyNameLabel, $"{displayEnemyName.ToUpper()} (STAGE {currentStage}/3)");
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

        // 1. Nhãn trạng thái & Giáp của Player
        if (playerEntity != null && playerStatusLabel == null)
        {
            playerStatusLabel = CreateOverlayLabel(
                playerEntity.transform,
                "PlayerStatusBadge",
                new Vector2(0f, -150f),
                new Vector2(340f, 44f),
                20f,
                new Color(0.55f, 0.88f, 1f, 1f));
        }

        // 2. Nhãn trạng thái & Giáp của Enemy
        if (enemyEntity != null && enemyStatusLabel == null)
        {
            enemyStatusLabel = CreateOverlayLabel(
                enemyEntity.transform,
                "EnemyStatusBadge",
                new Vector2(0f, -150f),
                new Vector2(360f, 44f),
                20f,
                new Color(1f, 0.85f, 0.45f, 1f));
        }

        // 3. Banner Ý Định Quái Vật (Enemy Intent) — ưu tiên dùng sẵn IntentBadge/IntentText trong Scene nếu có
        GameObject sceneIntentTextGo = GameObject.Find("IntentText");
        if (sceneIntentTextGo != null && tmpType != null)
        {
            enemyIntentLabel = sceneIntentTextGo.GetComponent(tmpType);
        }
        else if (enemyEntity != null && enemyIntentLabel == null)
        {
            GameObject intentBg = new GameObject("EnemyIntentBanner");
            intentBg.transform.SetParent(enemyEntity.transform, false);
            RectTransform bgRt = intentBg.AddComponent<RectTransform>();
            bgRt.anchoredPosition = new Vector2(0f, 165f);
            bgRt.sizeDelta = new Vector2(380f, 46f);

            Image bgImg = intentBg.AddComponent<Image>();
            bgImg.color = new Color(0.09f, 0.09f, 0.14f, 0.88f);
            bgImg.raycastTarget = false;

            Outline outline = intentBg.AddComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.35f, 0.25f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            enemyIntentLabel = CreateOverlayLabel(
                intentBg.transform,
                "IntentText",
                Vector2.zero,
                new Vector2(368f, 42f),
                19f,
                new Color(1f, 0.92f, 0.72f, 1f));
        }

        // 4. Panel Tooltip hiển thị thông tin mặt xúc xắc & hướng dẫn thao tác
        if (tooltipPanel == null)
        {
            tooltipPanel = new GameObject("DiceTooltipBanner");
            tooltipPanel.transform.SetParent(rootCanvas.transform, false);
            RectTransform rt = tooltipPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 195f);
            rt.sizeDelta = new Vector2(760f, 48f);

            Image bg = tooltipPanel.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.13f, 0.9f);
            bg.raycastTarget = false;

            tooltipLabel = CreateOverlayLabel(
                tooltipPanel.transform,
                "TooltipText",
                Vector2.zero,
                new Vector2(740f, 44f),
                17f,
                new Color(0.9f, 0.95f, 1f, 1f));

            SetDefaultHintText();
        }

        // 5. Tạo nút UNDO cạnh nút Reroll nếu chưa có
        if (undoButton == null && rerollButton != null)
        {
            GameObject undoObj = Instantiate(rerollButton.gameObject, rerollButton.transform.parent);
            undoObj.name = "UndoButton";
            RectTransform rerollRt = rerollButton.GetComponent<RectTransform>();
            RectTransform undoRt = undoObj.GetComponent<RectTransform>();
            if (rerollRt != null && undoRt != null)
            {
                undoRt.anchoredPosition = rerollRt.anchoredPosition + new Vector2(0f, 56f);
                undoRt.sizeDelta = new Vector2(rerollRt.sizeDelta.x, Mathf.Min(46f, rerollRt.sizeDelta.y));
            }

            undoButton = undoObj.GetComponent<Button>();
            undoButton.onClick.RemoveAllListeners();
            UpdateButtonCaption(undoButton, "UNDO (0) [Z]");
        }
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

    // =========================================================================
    // 2. ĐĂNG KÝ OBSERVER EVENTS CHO MÁU, GIÁP, HIỆU ỨNG TRẠNG THÁI
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
                if (playerPortraitImage != null) StartCoroutine(HitFlash(playerPortraitImage));
                if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.2f, 8f);
            };

            playerEntity.OnHealed += (healAmount) =>
            {
                SpawnCustomFloatingText(playerEntity.transform, $"+{healAmount} HP", new Color(0.25f, 0.95f, 0.45f));
            };

            playerEntity.OnShieldChanged += (shield) =>
            {
                RefreshEntityStatusDisplay(playerEntity, playerStatusLabel);
            };

            playerEntity.OnStatusChanged += () =>
            {
                RefreshEntityStatusDisplay(playerEntity, playerStatusLabel);
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
                if (enemyPortraitImage != null) StartCoroutine(HitFlash(enemyPortraitImage));
                if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.2f, 8f);
            };

            enemyEntity.OnHealed += (healAmount) =>
            {
                SpawnCustomFloatingText(enemyEntity.transform, $"+{healAmount} HP", new Color(0.25f, 0.95f, 0.45f));
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
        if (entity.CurrentShield > 0) badges.Add($"🛡 Giáp:{entity.CurrentShield}");
        if (entity.RetainHalfShieldNextTurn) badges.Add("🏰 BấtKhuất");
        if (entity.PoisonStacks > 0) badges.Add($"☠ Độc:{entity.PoisonStacks}");
        if (entity.StunStacks > 0) badges.Add($"⚡ Choáng:{entity.StunStacks}");
        if (entity.WeakStacks > 0) badges.Add($"⬇ Yếu:{entity.WeakStacks}");
        if (entity.BonusAttack > 0) badges.Add($"🔥 +Công:{entity.BonusAttack}");

        string text = badges.Count > 0 ? string.Join("  ", badges) : "Trạng thái: Bình thường";
        SetTMPText(statusLabel, text);
    }

    // =========================================================================
    // 3. CẤU HÌNH XÚC XẮC & NÚT ĐIỀU KHIỂN
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

            // Gắn DraggableDice để hỗ trợ Kéo-Thả, Hover Tooltip & Click Phải/Double-Click
            DraggableDice drag = slotObj.GetComponent<DraggableDice>();
            if (drag == null) drag = slotObj.AddComponent<DraggableDice>();
            drag.combatManager = this;
            drag.slotIndex = i;

            // Gắn sự kiện Click Trái để chọn/bỏ chọn xúc xắc (dùng cho Re-roll hoặc End Turn)
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
    // 4. MÁY TRẠNG THÁI 5 PHA (5-PHASE TURN STATE MACHINE)
    // =========================================================================

    private IEnumerator BeginRoundCoroutine()
    {
        if (combatEnded) yield break;

        isAnimating = true;
        currentPhase = CombatPhase.RoundStart;
        currentRound++;

        // 1. Pha Khởi Đầu (RoundStart): Reset giáp đầu hiệp (có tính Iron Wall giữ 50% giáp)
        if (playerEntity != null) playerEntity.ResetShieldForNewTurn();
        if (enemyEntity != null) enemyEntity.ResetShieldForNewTurn();

        // Xóa lịch sử Undo của hiệp cũ & cập nhật lượt Re-roll
        if (commandManager != null) commandManager.ClearHistory();
        rerollsLeft = GameManager.Instance != null ? Mathf.Max(1, GameManager.Instance.maxRerollsPerTurn) : 1;

        // Cập nhật Ý Định Quái Vật (Enemy Intent) theo kịch bản GDD mục 4
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
            SpawnCustomFloatingText(playerEntity.transform, "⚡ BỊ CHOÁNG!", new Color(1f, 0.85f, 0.2f));
            yield return new WaitForSeconds(0.8f);
            yield return StartCoroutine(ExecuteEnemyAndRoundEndCoroutine());
            yield break;
        }

        // 2. Pha Gieo Xúc Xắc (PlayerRoll)
        currentPhase = CombatPhase.PlayerRoll;
        yield return StartCoroutine(RollAllDiceCoroutine(false));

        // 3. Mở Pha Hành Động (PlayerAction)
        currentPhase = CombatPhase.PlayerAction;
        isAnimating = false;
        SetDefaultHintText();
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
            // Nếu người chơi có click chọn các viên muốn gieo lại thì chỉ gieo lại các viên đó;
            // nếu không chọn viên nào thì gieo lại toàn bộ các viên chưa dùng.
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

        // Hiệu ứng nhảy số ngẫu nhiên (0.35s)
        float duration = 0.35f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            foreach (int i in indicesToRoll)
            {
                SetDiceSlotText(i, "ĐANG GIEO...", Random.Range(1, 9).ToString());
            }
            elapsed += 0.05f;
            yield return new WaitForSeconds(0.05f);
        }

        // Lấy kết quả thực tế từ túi xúc xắc 6 mặt trong GameManager
        List<DiceInstance> bag = GameManager.Instance != null
            ? GameManager.Instance.playerDiceBag
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
    // 5. TƯƠNG TÁC XÚC XẮC (KÉO-THẢ, CLICK, DOUBLE-CLICK, RE-ROLL, UNDO)
    // =========================================================================

    public bool CanInteractWithDiceSlot(int slotIndex)
    {
        if (isAnimating || combatEnded || currentPhase != CombatPhase.PlayerAction) return false;
        if (slotIndex < 0 || slotIndex >= diceSlots.Count) return false;
        if (slotIndex < diceUsed.Count && diceUsed[slotIndex]) return false;
        if (slotIndex >= rolledFaces.Count || rolledFaces[slotIndex] == null) return false;
        return true;
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

    /// <summary>
    /// Kích hoạt ngay lập tức viên xúc xắc lên mục tiêu tự nhiên của nó (dùng khi Click phải hoặc Double-Click).
    /// </summary>
    public void UseDiceSlotImmediately(int slotIndex)
    {
        if (!CanInteractWithDiceSlot(slotIndex)) return;

        DiceFaceData face = rolledFaces[slotIndex];
        DropTarget.TargetType naturalTarget = face.TargetsEnemy()
            ? DropTarget.TargetType.Enemy
            : DropTarget.TargetType.Player;

        TryUseDiceFromDrag(slotIndex, naturalTarget);
    }

    /// <summary>
    /// Kích hoạt viên xúc xắc khi Kéo-Thả vào mục tiêu hoặc gọi trực tiếp.
    /// </summary>
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

        // Hiển thị chữ nổi báo hiệu kỹ năng vừa dùng & hoạt ảnh vung kiếm nếu tấn công quái
        if (resolvedTarget == enemyEntity && playerPortraitImage != null)
        {
            CharacterSpriteAnimator pAnim = playerPortraitImage.GetComponent<CharacterSpriteAnimator>();
            if (pAnim != null)
            {
                pAnim.PlaySlashAttack(enemyPortraitImage != null ? enemyPortraitImage.transform : enemyEntity.transform);
            }
        }
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
            case DiceSkillKind.Block:
                SpawnCustomFloatingText(playerEntity.transform, $"+{face.valueV} 🛡 GIÁP", new Color(0.35f, 0.78f, 1f));
                break;
            case DiceSkillKind.IronWall:
                SpawnCustomFloatingText(playerEntity.transform, $"+{face.valueV} 🛡 BẤT KHUẤT", new Color(0.45f, 0.85f, 1f));
                break;
            case DiceSkillKind.Purify:
                SpawnCustomFloatingText(playerEntity.transform, "✨ THANH TẨY", new Color(0.5f, 1f, 0.8f));
                break;
            case DiceSkillKind.PoisonSting:
                SpawnCustomFloatingText(enemyEntity.transform, $"+{face.valueV} ☠ ĐỘC", new Color(0.65f, 0.35f, 0.95f));
                break;
            case DiceSkillKind.ShockStun:
                SpawnCustomFloatingText(enemyEntity.transform, "⚡ CHOÁNG!", new Color(1f, 0.9f, 0.25f));
                break;
            case DiceSkillKind.Blank:
                SpawnCustomFloatingText(playerEntity.transform, "💨 TRƯỢT!", new Color(0.75f, 0.75f, 0.75f));
                break;
        }
    }

    /// <summary>
    /// Hoàn tác (Undo) viên xúc xắc vừa sử dụng trong lượt hiện tại.
    /// </summary>
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
                SetTMPText(tooltipLabel, $"Đã hoàn tác (Undo): [{record.description}]");
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

        // Nếu người chơi đã Click chọn các viên xúc xắc mà chưa Kéo-Thả, tự động kích hoạt chúng theo thứ tự!
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

                        // Tạm mở cờ isAnimating để thực thi lệnh
                        isAnimating = false;
                        TryUseDiceFromDrag(slotIdx, naturalTarget);
                        isAnimating = true;
                        yield return new WaitForSeconds(0.32f);
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
        UpdateControlButtonsUI();

        yield return new WaitForSeconds(0.45f);

        if (enemyEntity != null && enemyEntity.IsAlive() && !combatEnded)
        {
            if (enemyEntity.CheckAndConsumeStun())
            {
                SpawnCustomFloatingText(enemyEntity.transform, "⚡ BỊ CHOÁNG (MẤT LƯỢT)!", new Color(1f, 0.9f, 0.25f));
                yield return new WaitForSeconds(0.75f);
            }
            else
            {
                if (enemyPortraitImage != null)
                {
                    CharacterSpriteAnimator eAnim = enemyPortraitImage.GetComponent<CharacterSpriteAnimator>();
                    if (eAnim != null)
                    {
                        eAnim.PlaySlashAttack(playerPortraitImage != null ? playerPortraitImage.transform : playerEntity.transform);
                    }
                    yield return new WaitForSeconds(0.18f);
                }

                ExecuteCurrentEnemyIntent();
                yield return new WaitForSeconds(0.65f);
            }
        }

        if (combatEnded || playerEntity == null || !playerEntity.IsAlive() || enemyEntity == null || !enemyEntity.IsAlive())
        {
            yield break;
        }

        // 5. Pha Kết Thúc Hiệp (RoundEnd): Kích hoạt Độc (Poison) xuyên giáp & giảm Suy Yếu
        currentPhase = CombatPhase.RoundEnd;

        if (enemyEntity.PoisonStacks > 0)
        {
            SpawnCustomFloatingText(enemyEntity.transform, $"☠ ĐỘC -{enemyEntity.PoisonStacks}", new Color(0.68f, 0.32f, 0.95f));
        }
        enemyEntity.ProcessRoundEndStatuses(false);

        if (combatEnded || !enemyEntity.IsAlive()) yield break;

        if (playerEntity.PoisonStacks > 0)
        {
            SpawnCustomFloatingText(playerEntity.transform, $"☠ ĐỘC -{playerEntity.PoisonStacks}", new Color(0.68f, 0.32f, 0.95f));
        }
        playerEntity.ProcessRoundEndStatuses(false);

        if (combatEnded || !playerEntity.IsAlive()) yield break;

        yield return new WaitForSeconds(0.35f);

        // Chuyển sang hiệp kế tiếp
        StartCoroutine(BeginRoundCoroutine());
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
                SpawnCustomFloatingText(enemyEntity.transform, $"+{currentEnemyIntent.primaryValue} 🛡 GIÁP", new Color(0.4f, 0.8f, 1f));
                break;
            }
            case EnemyIntentType.AttackAndPoison:
            {
                int dmg = enemyEntity.CalculateOutgoingDamage(currentEnemyIntent.primaryValue);
                playerEntity.TakeDamage(dmg);
                playerEntity.AddPoison(currentEnemyIntent.secondaryValue);
                SpawnCustomFloatingText(playerEntity.transform, $"+{currentEnemyIntent.secondaryValue} ☠ ĐỘC", new Color(0.7f, 0.35f, 0.95f));
                break;
            }
            case EnemyIntentType.ChargeBuffAttack:
            {
                enemyEntity.AddBonusAttack(currentEnemyIntent.primaryValue);
                SpawnCustomFloatingText(enemyEntity.transform, $"🔥 TÍCH TỤ +{currentEnemyIntent.primaryValue} CÔNG!", new Color(1f, 0.5f, 0.2f));
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
    // 6. HIỂN THỊ UI XÚC XẮC, TOOLTIP, INTENT & GAME JUICE
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

            if (face != null)
            {
                string valDisplay = GetFaceValueSymbol(face);
                string titleDisplay = used ? $"{face.faceName}\n(ĐÃ DÙNG)" : face.faceName;
                SetDiceSlotText(i, titleDisplay, valDisplay);
                SetDiceSlotIcon(i, GetSkillSprite(face));
            }

            Image img = slotObj.GetComponent<Image>();
            if (img != null)
            {
                if (used)
                {
                    img.color = new Color(0.22f, 0.22f, 0.26f, 0.65f);
                }
                else if (selected)
                {
                    img.color = new Color(0.85f, 0.65f, 0.12f, 0.96f);
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
        }
    }

    private string GetFaceValueSymbol(DiceFaceData face)
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
                return "✨";
            case DiceSkillKind.PoisonSting:
                return $"☠{face.valueV}";
            case DiceSkillKind.ShockStun:
                return $"⚡{face.valueV}";
            case DiceSkillKind.Blank:
                return "💨0";
            default:
                return face.valueV.ToString();
        }
    }

    private Color GetSkillTintColor(DiceSkillKind kind, Color fallback)
    {
        switch (kind)
        {
            case DiceSkillKind.Strike:
            case DiceSkillKind.HeavySlash:
                return new Color(0.42f, 0.16f, 0.18f, 0.92f);
            case DiceSkillKind.Vampiric:
                return new Color(0.48f, 0.12f, 0.28f, 0.92f);
            case DiceSkillKind.Block:
            case DiceSkillKind.IronWall:
                return new Color(0.14f, 0.28f, 0.46f, 0.92f);
            case DiceSkillKind.Regenerate:
            case DiceSkillKind.Purify:
                return new Color(0.14f, 0.38f, 0.26f, 0.92f);
            case DiceSkillKind.PoisonSting:
                return new Color(0.32f, 0.16f, 0.44f, 0.92f);
            case DiceSkillKind.ShockStun:
                return new Color(0.46f, 0.36f, 0.12f, 0.92f);
            case DiceSkillKind.Blank:
                return new Color(0.25f, 0.25f, 0.28f, 0.85f);
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
        string targetHint = face.TargetsEnemy() ? "Mục tiêu: Quái vật" : "Mục tiêu: Hiệp Sĩ";
        string flatTooltip = face.GetTooltipText().Replace("\n", " — ");
        SetTMPText(tooltipLabel, $"{flatTooltip}  |  ({targetHint})");
    }

    public void HideTooltip()
    {
        SetDefaultHintText();
    }

    private void SetDefaultHintText()
    {
        if (tooltipLabel == null) return;
        SetTMPText(
            tooltipLabel,
            "Mẹo: Kéo-thả xúc xắc vào mục tiêu (hoặc Click phải/Double-click dùng ngay) | Click trái chọn Re-roll / End Turn | Phím Z: Undo");
    }

    private void UpdateEnemyIntentUI()
    {
        int bonusAtk = enemyEntity != null ? enemyEntity.BonusAttack : 0;
        string desc = currentEnemyIntent.GetDisplayDescription(bonusAtk);
        if (enemyIntentLabel != null)
        {
            SetTMPText(enemyIntentLabel, $"HIỆP {currentRound} — {desc}");
        }

        GameObject turnBadgeGo = GameObject.Find("TurnBadge");
        if (turnBadgeGo != null && tmpType != null)
        {
            Component turnTmp = turnBadgeGo.GetComponentInChildren(tmpType);
            SetTMPText(turnTmp, $"ROUND {currentRound} — STAGE {currentStage}/3");
        }
    }

    private void UpdateControlButtonsUI()
    {
        if (rerollButton != null)
        {
            rerollButton.interactable = !isAnimating && currentPhase == CombatPhase.PlayerAction && rerollsLeft > 0;
            UpdateButtonCaption(rerollButton, $"RE-ROLL ({rerollsLeft})");
        }

        if (endTurnButton != null)
        {
            endTurnButton.interactable = !isAnimating && currentPhase == CombatPhase.PlayerAction;
            string endCaption = selectedDiceIndices.Count > 0
                ? $"DÙNG ({selectedDiceIndices.Count}) & END TURN"
                : "END TURN";
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
    // 7. HIỆU ỨNG GAME JUICE & XỬ LÝ THẮNG / THUA CHUYỂN MÀN
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
        float duration = 0.35f;

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

    private IEnumerator HitFlash(Image img)
    {
        if (img == null) yield break;
        Color original = Color.white;
        img.color = Color.red;

        Vector3 originalPos = img.transform.localPosition;
        img.transform.localPosition = originalPos + new Vector3(10f, 0f, 0f);

        yield return new WaitForSeconds(0.1f);

        if (img != null)
        {
            img.color = original;
            img.transform.localPosition = originalPos;
        }
    }

    private void SpawnFloatingText(Transform parent, int damage, bool isPierce)
    {
        if (parent == null) return;
        GameObject go = new GameObject("FloatingText");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(Random.Range(-25f, 25f), 55f);

        if (tmpType != null)
        {
            Component tmp = go.AddComponent(tmpType);
            SetTMPText(tmp, "-" + damage.ToString());
            tmpType.GetProperty("fontSize")?.SetValue(tmp, 44f);
            Color red = new Color(0.94f, 0.27f, 0.27f);
            tmpType.GetProperty("color")?.SetValue(tmp, red);
            tmpType.GetProperty("alignment")?.SetValue(tmp, 514);
        }

        FloatingText ft = go.AddComponent<FloatingText>();
        ft.floatSpeed = 80f;
        ft.fadeDuration = 0.8f;
        ft.Setup(damage, isPierce);
    }

    private void SpawnCustomFloatingText(Transform parent, string message, Color color)
    {
        if (parent == null) return;
        GameObject go = new GameObject("CustomFloatingText");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(Random.Range(-20f, 20f), 85f);
        rt.sizeDelta = new Vector2(320f, 50f);

        if (tmpType != null)
        {
            Component tmp = go.AddComponent(tmpType);
            SetTMPText(tmp, message);
            tmpType.GetProperty("fontSize")?.SetValue(tmp, 30f);
            tmpType.GetProperty("color")?.SetValue(tmp, color);
            tmpType.GetProperty("alignment")?.SetValue(tmp, 514);
        }

        FloatingText ft = go.AddComponent<FloatingText>();
        ft.floatSpeed = 65f;
        ft.fadeDuration = 0.95f;
        ft.SetupCustom(message, color);
    }

    private void UpdateHPLabel(Component label, int current, int max)
    {
        if (label == null) return;
        SetTMPText(label, $"{current} / {max}");
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

        if (enemyIntentLabel != null)
        {
            SetTMPText(enemyIntentLabel, "🎉 CHIẾN THẮNG! QUÁI VẬT ĐÃ BỊ HẠ GỤC!");
        }

        yield return new WaitForSeconds(0.8f);

        if (GameManager.Instance != null)
        {
            if (playerEntity != null)
            {
                GameManager.Instance.playerCurrentHP = Mathf.Max(1, playerEntity.CurrentHP);
            }
            GameManager.Instance.OnEnemyDefeated();
        }
    }

    private IEnumerator HandlePlayerDefeated()
    {
        if (combatEnded) yield break;
        combatEnded = true;
        currentPhase = CombatPhase.CombatOver;
        isAnimating = true;

        if (enemyIntentLabel != null)
        {
            SetTMPText(enemyIntentLabel, "💀 HIỆP SĨ ĐÃ NGÃ XUỐNG — HÀNH TRÌNH KHÉP LẠI!");
        }

        yield return new WaitForSeconds(1.5f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadMainMenu();
        }
    }
}
