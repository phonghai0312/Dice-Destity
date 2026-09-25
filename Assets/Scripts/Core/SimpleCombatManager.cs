using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// SimpleCombatManager — Gắn vào CombatScene.
/// Tự động khởi tạo, kết nối toàn bộ logic chiến đấu.
/// </summary>
public class SimpleCombatManager : MonoBehaviour
{
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

    [Header("Dice Slots")]
    public List<GameObject> diceSlots = new List<GameObject>();

    [Header("Buttons")]
    public Button endTurnButton;
    public Button rerollButton;

    private int rerollsLeft = 2;
    private int selectedDiceTotal = 0;
    private List<int> selectedDiceIndices = new List<int>();
    private int enemyBaseDamage = 5;

    private System.Type tmpType;

    private void Start()
    {
        tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        LoadStageData();
        RegisterHPEvents();
        SetupDiceButtons();
        SetupActionButtons();
    }

    // ─── Stage Data ────────────────────────────────────────────────────────────

    void LoadStageData()
    {
        if (GameManager.Instance == null)
        {
            // No GameManager (play directly from CombatScene) — use defaults
            if (enemyEntity != null) { enemyEntity.maxHP = 20; }
            if (playerEntity != null) { playerEntity.maxHP = 70; }
            return;
        }

        int stage = GameManager.Instance.currentStageIndex;

        // Player HP persistence
        if (playerEntity != null)
        {
            playerEntity.maxHP = GameManager.Instance.playerMaxHP;
        }

        // Enemy data per stage
        if (GameManager.Instance.stageEnemies != null && stage <= GameManager.Instance.stageEnemies.Length)
        {
            EnemyData data = GameManager.Instance.stageEnemies[stage - 1];
            if (data != null)
            {
                if (enemyEntity != null) enemyEntity.Initialize(data);
                if (backgroundImage != null && data.backgroundSprite != null)
                    backgroundImage.sprite = data.backgroundSprite;
                if (enemyPortraitImage != null && data.enemySprite != null)
                    enemyPortraitImage.sprite = data.enemySprite;
                if (enemyNameLabel != null && tmpType != null)
                    tmpType.GetProperty("text")?.SetValue(enemyNameLabel, data.enemyName.ToUpper());
                enemyBaseDamage = 4 + stage * 2;
                return;
            }
        }

        // Fallback: stage without ScriptableObject data
        int[] hps = { 20, 35, 60 };
        int[] dmgs = { 6, 8, 12 };
        if (enemyEntity != null)
        {
            enemyEntity.maxHP = stage <= hps.Length ? hps[stage-1] : 20;
        }
        enemyBaseDamage = stage <= dmgs.Length ? dmgs[stage-1] : 6;
    }

    // ─── HP Events ─────────────────────────────────────────────────────────────

    void RegisterHPEvents()
    {
        if (playerEntity != null)
        {
            playerEntity.OnHPChanged += (current, max) =>
            {
                UpdateHPBar(playerHPFill, current, max);
                UpdateHPLabel(playerHPLabel, current, max);
                if (current <= 0) StartCoroutine(PlayerDied());
            };
        }

        if (enemyEntity != null)
        {
            enemyEntity.OnHPChanged += (current, max) =>
            {
                UpdateHPBar(enemyHPFill, current, max);
                UpdateHPLabel(enemyHPLabel, current, max);
            };
        }
    }

    void UpdateHPBar(Image fill, int current, int max)
    {
        if (fill == null) return;
        float pct = Mathf.Clamp01((float)current / max);
        RectTransform rt = fill.transform as RectTransform;
        if (rt != null)
        {
            rt.anchorMax = new Vector2(pct, rt.anchorMax.y);
            rt.sizeDelta = new Vector2(0, rt.sizeDelta.y);
        }
    }

    void UpdateHPLabel(Component label, int current, int max)
    {
        if (label == null || tmpType == null) return;
        tmpType.GetProperty("text")?.SetValue(label, $"{current} / {max}");
    }

    // ─── Dice Buttons ──────────────────────────────────────────────────────────

    void SetupDiceButtons()
    {
        int[] diceValues = { 4, 2, 6 };
        for (int i = 0; i < diceSlots.Count && i < diceValues.Length; i++)
        {
            int idx = i;
            int val = diceValues[idx];
            Button btn = diceSlots[i].GetComponent<Button>();
            if (btn == null) continue;

            bool selected = false;
            Image img = diceSlots[i].GetComponent<Image>();
            Color originalColor = img != null ? img.color : Color.white;
            Color selectedColor = new Color(0.72f, 0.53f, 0.05f, 0.9f); // gold

            btn.onClick.AddListener(() =>
            {
                selected = !selected;
                if (img != null) img.color = selected ? selectedColor : originalColor;

                if (selected) { selectedDiceIndices.Add(idx); selectedDiceTotal += val; }
                else { selectedDiceIndices.Remove(idx); selectedDiceTotal -= val; }
            });
        }
    }

    // ─── Action Buttons ────────────────────────────────────────────────────────

    void SetupActionButtons()
    {
        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(OnEndTurn);

        if (rerollButton != null)
            rerollButton.onClick.AddListener(OnReroll);
    }

    public void OnEndTurn()
    {
        if (enemyEntity == null || playerEntity == null) return;

        // 1. Player attacks with selected dice
        int dmg = Mathf.Max(1, selectedDiceTotal);
        if (selectedDiceTotal == 0) dmg = 1; // min 1 damage even with no dice

        enemyEntity.TakeDamage(dmg);

        // 2. Reset dice selection UI
        ResetDiceSelection();

        // 3. If enemy still alive, enemy counter-attacks
        if (enemyEntity.IsAlive())
        {
            StartCoroutine(EnemyAttack());
        }
        // else: enemy death is handled by BattleEntity → GameManager.OnEnemyDefeated
    }

    IEnumerator EnemyAttack()
    {
        yield return new WaitForSeconds(0.6f);
        playerEntity.TakeDamage(enemyBaseDamage);
    }

    void ResetDiceSelection()
    {
        selectedDiceTotal = 0;
        selectedDiceIndices.Clear();

        // Reset dice colors
        foreach (var slot in diceSlots)
        {
            Image img = slot?.GetComponent<Image>();
            // Restore to original color (we stored it; for now just darken back)
        }
    }

    public void OnReroll()
    {
        if (rerollsLeft <= 0) return;
        rerollsLeft--;

        // Randomize dice values visually
        int[] newVals = { Random.Range(1, 7), Random.Range(1, 5), Random.Range(3, 9) };
        System.Type t = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        for (int i = 0; i < diceSlots.Count && i < newVals.Length; i++)
        {
            if (t == null) break;
            var valTxt = diceSlots[i].transform.Find("Val");
            if (valTxt != null)
            {
                Component tmp = valTxt.GetComponent(t);
                if (tmp != null) t.GetProperty("text")?.SetValue(tmp, newVals[i].ToString());
            }
        }

        // Update reroll button text
        if (rerollButton != null && t != null)
        {
            var txt = rerollButton.GetComponentInChildren(t);
            if (txt != null) t.GetProperty("text")?.SetValue(txt, $"RE-ROLL  ({rerollsLeft})");
        }
    }

    IEnumerator PlayerDied()
    {
        yield return new WaitForSeconds(1f);
        if (GameManager.Instance != null)
            GameManager.Instance.LoadMainMenu();
    }
}
