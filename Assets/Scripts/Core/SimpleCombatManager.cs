using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// SimpleCombatManager — Gắn vào CombatScene.
/// Cập nhật: Tích hợp đầy đủ Game Juice (Smooth HP, Camera Shake, Hit Flash, Floating Text).
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
    public Image playerPortraitImage; // Thêm reference để nháy đỏ player

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
    private bool isAnimating = false;

    private void Start()
    {
        tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        
        // Cài đặt Camera Shake nếu chưa có
        if (Camera.main != null && Camera.main.GetComponent<CameraShake>() == null)
            Camera.main.gameObject.AddComponent<CameraShake>();

        // Lấy Player Portrait nếu chưa có
        if (playerPortraitImage == null && playerEntity != null)
        {
            var pImg = playerEntity.transform.Find("PlayerSprite");
            if (pImg != null) playerPortraitImage = pImg.GetComponent<Image>();
        }

        LoadStageData();
        RegisterHPEvents();
        SetupDiceButtons();
        SetupActionButtons();
    }

    void LoadStageData()
    {
        if (GameManager.Instance == null)
        {
            if (enemyEntity != null) { enemyEntity.maxHP = 20; }
            if (playerEntity != null) { playerEntity.maxHP = 70; }
            return;
        }

        int stage = GameManager.Instance.currentStageIndex;
        if (playerEntity != null) playerEntity.maxHP = GameManager.Instance.playerMaxHP;

        if (GameManager.Instance.stageEnemies != null && stage <= GameManager.Instance.stageEnemies.Length)
        {
            EnemyData data = GameManager.Instance.stageEnemies[stage - 1];
            if (data != null)
            {
                if (enemyEntity != null) enemyEntity.Initialize(data);
                if (backgroundImage != null && data.backgroundSprite != null) backgroundImage.sprite = data.backgroundSprite;
                if (enemyPortraitImage != null && data.enemySprite != null) enemyPortraitImage.sprite = data.enemySprite;
                if (enemyNameLabel != null && tmpType != null)
                    tmpType.GetProperty("text")?.SetValue(enemyNameLabel, data.enemyName.ToUpper());
                enemyBaseDamage = 4 + stage * 2;
                return;
            }
        }

        int[] hps = { 20, 35, 60 };
        int[] dmgs = { 6, 8, 12 };
        if (enemyEntity != null) enemyEntity.maxHP = stage <= hps.Length ? hps[stage-1] : 20;
        enemyBaseDamage = stage <= dmgs.Length ? dmgs[stage-1] : 6;
    }

    void RegisterHPEvents()
    {
        if (playerEntity != null)
        {
            playerEntity.OnHPChanged += (current, max) => {
                UpdateHPBar(playerHPFill, current, max);
                UpdateHPLabel(playerHPLabel, current, max);
                if (current <= 0) StartCoroutine(PlayerDied());
            };
            playerEntity.OnDamageTaken += (dmg) => {
                SpawnFloatingText(playerEntity.transform, dmg);
                if (playerPortraitImage != null) StartCoroutine(HitFlash(playerPortraitImage));
                if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.2f, 8f);
            };
        }

        if (enemyEntity != null)
        {
            enemyEntity.OnHPChanged += (current, max) => {
                UpdateHPBar(enemyHPFill, current, max);
                UpdateHPLabel(enemyHPLabel, current, max);
            };
            enemyEntity.OnDamageTaken += (dmg) => {
                SpawnFloatingText(enemyEntity.transform, dmg);
                if (enemyPortraitImage != null) StartCoroutine(HitFlash(enemyPortraitImage));
                if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.2f, 8f);
            };
        }
    }

    // --- GAME JUICE: Cập nhật HP bar mượt mà ---
    void UpdateHPBar(Image fill, int current, int max)
    {
        if (fill == null) return;
        float targetPct = Mathf.Clamp01((float)current / max);
        StartCoroutine(SmoothLerpHP(fill, targetPct));
    }

    IEnumerator SmoothLerpHP(Image fill, float targetPct)
    {
        RectTransform rt = fill.transform as RectTransform;
        if (rt == null) yield break;
        
        float startPct = rt.anchorMax.x;
        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); // Smoothstep
            rt.anchorMax = new Vector2(Mathf.Lerp(startPct, targetPct, t), rt.anchorMax.y);
            rt.sizeDelta = new Vector2(0, rt.sizeDelta.y);
            yield return null;
        }
        rt.anchorMax = new Vector2(targetPct, rt.anchorMax.y);
    }

    // --- GAME JUICE: Chớp đỏ khi bị đánh ---
    IEnumerator HitFlash(Image img)
    {
        Color original = Color.white;
        img.color = Color.red;
        
        // Thêm hiệu ứng lùi lại (Knockback)
        Vector3 originalPos = img.transform.localPosition;
        img.transform.localPosition = originalPos + new Vector3(10f, 0, 0);

        yield return new WaitForSeconds(0.1f);
        
        img.color = original;
        img.transform.localPosition = originalPos;
    }

    // --- GAME JUICE: Sát thương bay lên ---
    void SpawnFloatingText(Transform parent, int damage)
    {
        GameObject go = new GameObject("FloatingText");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, 50); // Hiện phía trên nhân vật
        
        if (tmpType != null)
        {
            Component tmp = go.AddComponent(tmpType);
            tmpType.GetProperty("text")?.SetValue(tmp, "-" + damage.ToString());
            tmpType.GetProperty("fontSize")?.SetValue(tmp, 48f);
            Color red; ColorUtility.TryParseHtmlString("#ef4444", out red);
            tmpType.GetProperty("color")?.SetValue(tmp, red);
            tmpType.GetProperty("alignment")?.SetValue(tmp, 514); // center
        }

        FloatingText ft = go.AddComponent<FloatingText>();
        ft.floatSpeed = 80f;
        ft.fadeDuration = 0.8f;
        ft.Setup(damage, false);
    }

    void UpdateHPLabel(Component label, int current, int max)
    {
        if (label == null || tmpType == null) return;
        tmpType.GetProperty("text")?.SetValue(label, $"{current} / {max}");
    }

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
            Color selectedColor = new Color(0.72f, 0.53f, 0.05f, 0.9f);

            btn.onClick.AddListener(() =>
            {
                if (isAnimating) return; // Không cho click khi đang có animation
                
                selected = !selected;
                if (img != null) img.color = selected ? selectedColor : originalColor;

                if (selected) { selectedDiceIndices.Add(idx); selectedDiceTotal += val; }
                else { selectedDiceIndices.Remove(idx); selectedDiceTotal -= val; }
            });
        }
    }

    void SetupActionButtons()
    {
        if (endTurnButton != null) endTurnButton.onClick.AddListener(() => {
            if (!isAnimating) StartCoroutine(ExecuteCombatSequence());
        });
        if (rerollButton != null) rerollButton.onClick.AddListener(() => {
            if (!isAnimating) StartCoroutine(AnimateReroll());
        });
    }

    // --- GAME JUICE: Combat State Machine (Máy trạng thái chiến đấu) ---
    IEnumerator ExecuteCombatSequence()
    {
        if (enemyEntity == null || playerEntity == null) yield break;
        isAnimating = true; // Khóa UI

        // 1. Player tấn công
        int dmg = Mathf.Max(1, selectedDiceTotal);
        if (selectedDiceTotal == 0) dmg = 1;
        
        enemyEntity.TakeDamage(dmg); // Sẽ tự trigger HitFlash và CameraShake
        ResetDiceSelection();

        // Chờ hiệu ứng player đánh xong
        yield return new WaitForSeconds(0.8f);

        // 2. Enemy phản công nếu còn sống
        if (enemyEntity.IsAlive())
        {
            // Enemy lấy đà một nhịp
            if (enemyPortraitImage != null)
            {
                Vector3 pos = enemyPortraitImage.transform.localPosition;
                enemyPortraitImage.transform.localPosition = pos + new Vector3(20f, 0, 0); // Lùi về
                yield return new WaitForSeconds(0.2f);
                enemyPortraitImage.transform.localPosition = pos; // Phóng tới
            }

            playerEntity.TakeDamage(enemyBaseDamage);
            yield return new WaitForSeconds(0.6f);
        }

        isAnimating = false; // Mở lại UI
    }

    void ResetDiceSelection()
    {
        selectedDiceTotal = 0;
        selectedDiceIndices.Clear();
        foreach (var slot in diceSlots)
        {
            Image img = slot?.GetComponent<Image>();
            // Khôi phục màu gốc sẽ cần một cách tốt hơn, tạm thời cứ reset thủ công.
        }
    }

    // --- GAME JUICE: Xáo xúc xắc liên tục ---
    IEnumerator AnimateReroll()
    {
        if (rerollsLeft <= 0) yield break;
        rerollsLeft--;
        isAnimating = true;

        if (rerollButton != null && tmpType != null)
        {
            var txt = rerollButton.GetComponentInChildren(tmpType);
            if (txt != null) tmpType.GetProperty("text")?.SetValue(txt, $"RE-ROLL  ({rerollsLeft})");
        }

        float duration = 0.4f;
        float elapsed = 0f;
        
        int[] newVals = { Random.Range(1, 7), Random.Range(1, 5), Random.Range(3, 9) };
        
        // Quay số ngẫu nhiên liên tục trong 0.4 giây
        while (elapsed < duration)
        {
            for (int i = 0; i < diceSlots.Count && i < newVals.Length; i++)
            {
                if (tmpType == null) break;
                var valTxt = diceSlots[i].transform.Find("Val");
                if (valTxt != null)
                {
                    Component tmp = valTxt.GetComponent(tmpType);
                    int fakeVal = Random.Range(1, 10);
                    if (tmp != null) tmpType.GetProperty("text")?.SetValue(tmp, fakeVal.ToString());
                }
            }
            elapsed += 0.05f;
            yield return new WaitForSeconds(0.05f);
        }

        // Dừng lại ở kết quả cuối
        for (int i = 0; i < diceSlots.Count && i < newVals.Length; i++)
        {
            if (tmpType == null) break;
            var valTxt = diceSlots[i].transform.Find("Val");
            if (valTxt != null)
            {
                Component tmp = valTxt.GetComponent(tmpType);
                if (tmp != null) tmpType.GetProperty("text")?.SetValue(tmp, newVals[i].ToString());
            }
        }

        isAnimating = false;
    }

    IEnumerator PlayerDied()
    {
        isAnimating = true;
        yield return new WaitForSeconds(1.5f);
        if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
    }
}
