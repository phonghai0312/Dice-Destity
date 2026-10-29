using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// CharacterSkillBarUI — Quản lý giao diện Thanh Năng Lượng (10 vạch Energy Pips),
/// Nút Kỹ năng Thường (Normal Skill) và Nút Kỹ năng Tối thượng (Ultimate Skill)
/// kèm hiệu ứng phát sáng khi đủ Năng lượng và Tooltip chi tiết khi hover chuột.
/// </summary>
public class CharacterSkillBarUI : MonoBehaviour
{
    [Header("References")]
    public SimpleCombatManager combatManager;
    public BattleEntity playerEntity;

    private TextMeshProUGUI energyHeaderText;
    private Button normalSkillBtn;
    private TextMeshProUGUI normalSkillText;
    private Outline normalSkillOutline;

    private Button ultimateSkillBtn;
    private TextMeshProUGUI ultimateSkillText;
    private Outline ultimateSkillOutline;

    private readonly List<Image> energyPipImages = new List<Image>();
    private bool isInitialized = false;

    public void Initialize(SimpleCombatManager manager, BattleEntity player, Transform parentCanvas)
    {
        combatManager = manager;
        playerEntity = player;

        if (!isInitialized && parentCanvas != null)
        {
            BuildSkillBarHierarchy(parentCanvas);
            isInitialized = true;
        }

        if (playerEntity != null)
        {
            playerEntity.OnEnergyChanged -= OnPlayerEnergyChanged;
            playerEntity.OnEnergyChanged += OnPlayerEnergyChanged;
            playerEntity.OnStatusChanged -= RefreshSkillBarState;
            playerEntity.OnStatusChanged += RefreshSkillBarState;
        }

        RefreshSkillBarState();
    }

    private void OnDestroy()
    {
        if (playerEntity != null)
        {
            playerEntity.OnEnergyChanged -= OnPlayerEnergyChanged;
            playerEntity.OnStatusChanged -= RefreshSkillBarState;
        }
    }

    private void Update()
    {
        if (!isInitialized || playerEntity == null || combatManager == null) return;

        // Hiệu ứng nhấp nháy viền sáng khi Kỹ năng Tối thượng hoặc Kỹ năng Thường sẵn sàng kích hoạt
        bool canAct = combatManager.currentPhase == SimpleCombatManager.CombatPhase.PlayerAction;
        int curEnergy = playerEntity.CurrentEnergy;
        int normCost = playerEntity.GetEffectiveSkillCost(combatManager.normalSkillBaseCost);
        int ultCost = playerEntity.GetEffectiveSkillCost(combatManager.ultimateSkillBaseCost);

        float pulse = 0.65f + Mathf.Sin(Time.time * 4.5f) * 0.35f;

        if (normalSkillOutline != null)
        {
            normalSkillOutline.effectColor = (canAct && curEnergy >= normCost)
                ? new Color(0.35f, 0.85f, 1f, pulse)
                : new Color(0.22f, 0.35f, 0.52f, 0.5f);
        }

        if (ultimateSkillOutline != null)
        {
            ultimateSkillOutline.effectColor = (canAct && curEnergy >= ultCost)
                ? new Color(1f, 0.62f, 0.15f, pulse)
                : new Color(0.45f, 0.22f, 0.18f, 0.5f);
        }
    }

    private void OnPlayerEnergyChanged(int current, int max)
    {
        RefreshSkillBarState();
    }

    private void BuildSkillBarHierarchy(Transform parentCanvas)
    {
        // Container đặt ở trung tâm sàn đấu phía trên thanh Tooltip
        GameObject rootObj = new GameObject("CharacterSkillBarRoot", typeof(RectTransform));
        rootObj.transform.SetParent(parentCanvas, false);
        RectTransform rootRt = rootObj.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 0.5f);
        rootRt.anchorMax = new Vector2(0.5f, 0.5f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.anchoredPosition = Vector2.zero;
        rootRt.sizeDelta = Vector2.zero;

        // 1. Thanh Năng Lượng (Energy Panel + 10 Pips)
        GameObject energyBox = new GameObject("PlayerEnergyPanel", typeof(RectTransform), typeof(Image), typeof(Outline));
        energyBox.transform.SetParent(rootObj.transform, false);
        RectTransform ebRt = energyBox.GetComponent<RectTransform>();
        ebRt.anchoredPosition = new Vector2(-450f, -246f);
        ebRt.sizeDelta = new Vector2(340f, 44f);

        Image ebBg = energyBox.GetComponent<Image>();
        ebBg.color = new Color(0.06f, 0.08f, 0.14f, 0.94f);
        ebBg.raycastTarget = false;

        Outline ebOl = energyBox.GetComponent<Outline>();
        ebOl.effectColor = new Color(0.25f, 0.78f, 1f, 0.85f);
        ebOl.effectDistance = new Vector2(1.4f, -1.4f);

        // Nhãn Năng lượng phía trên
        GameObject lblGo = new GameObject("PlayerEnergyText", typeof(RectTransform), typeof(TextMeshProUGUI));
        lblGo.transform.SetParent(energyBox.transform, false);
        RectTransform lblRt = lblGo.GetComponent<RectTransform>();
        lblRt.anchoredPosition = new Vector2(0f, 9f);
        lblRt.sizeDelta = new Vector2(320f, 20f);

        energyHeaderText = lblGo.GetComponent<TextMeshProUGUI>();
        energyHeaderText.fontSize = 14.5f;
        energyHeaderText.alignment = TextAlignmentOptions.Center;
        energyHeaderText.color = new Color(0.65f, 0.94f, 1f, 1f);
        energyHeaderText.raycastTarget = false;

        // 10 vạch Năng lượng (Energy Pips)
        GameObject pipsRow = new GameObject("EnergyPipsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        pipsRow.transform.SetParent(energyBox.transform, false);
        RectTransform pipsRt = pipsRow.GetComponent<RectTransform>();
        pipsRt.anchoredPosition = new Vector2(0f, -11f);
        pipsRt.sizeDelta = new Vector2(310f, 10f);

        HorizontalLayoutGroup hlg = pipsRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 5f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        energyPipImages.Clear();
        for (int i = 0; i < 10; i++)
        {
            GameObject pipGo = new GameObject($"EnergyPip_{i + 1}", typeof(RectTransform), typeof(Image));
            pipGo.transform.SetParent(pipsRow.transform, false);
            Image pipImg = pipGo.GetComponent<Image>();
            pipImg.color = new Color(0.16f, 0.22f, 0.32f, 0.85f);
            pipImg.raycastTarget = false;
            energyPipImages.Add(pipImg);
        }

        // 2. Nút Kỹ năng Thường (Normal Skill)
        normalSkillBtn = CreateSkillButtonWidget(
            rootObj.transform,
            "NormalSkillButton",
            new Vector2(-780f, 110f),
            new Vector2(230f, 66f),
            new Color(0.11f, 0.26f, 0.46f, 0.96f),
            new Color(0.38f, 0.78f, 1f, 0.9f),
            out normalSkillText,
            out normalSkillOutline);

        normalSkillBtn.onClick.AddListener(() =>
        {
            if (combatManager != null) combatManager.TryCastNormalSkill();
        });

        AddSkillHoverTooltip(normalSkillBtn.gameObject, isUltimate: false);

        // 3. Nút Kỹ năng Tối thượng (Ultimate Skill)
        ultimateSkillBtn = CreateSkillButtonWidget(
            rootObj.transform,
            "UltimateSkillButton",
            new Vector2(-780f, 30f),
            new Vector2(230f, 66f),
            new Color(0.46f, 0.14f, 0.12f, 0.96f),
            new Color(1f, 0.56f, 0.18f, 0.92f),
            out ultimateSkillText,
            out ultimateSkillOutline);

        ultimateSkillBtn.onClick.AddListener(() =>
        {
            if (combatManager != null) combatManager.TryCastUltimateSkill();
        });

        AddSkillHoverTooltip(ultimateSkillBtn.gameObject, isUltimate: true);
    }

    private Button CreateSkillButtonWidget(
        Transform parent,
        string name,
        Vector2 anchoredPos,
        Vector2 size,
        Color bgColor,
        Color outlineColor,
        out TextMeshProUGUI captionTmp,
        out Outline outlineComp)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        img.color = bgColor;

        outlineComp = go.GetComponent<Outline>();
        outlineComp.effectColor = outlineColor;
        outlineComp.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        GameObject txtGo = new GameObject("SkillCaption", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(go.transform, false);
        RectTransform txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(4f, 2f);
        txtRt.offsetMax = new Vector2(-4f, -2f);

        captionTmp = txtGo.GetComponent<TextMeshProUGUI>();
        captionTmp.fontSize = 13f;
        captionTmp.alignment = TextAlignmentOptions.Center;
        captionTmp.color = Color.white;
        captionTmp.raycastTarget = false;

        return btn;
    }

    private void AddSkillHoverTooltip(GameObject buttonObj, bool isUltimate)
    {
        EventTrigger trigger = buttonObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = buttonObj.AddComponent<EventTrigger>();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((_) =>
        {
            if (combatManager == null || playerEntity == null) return;
            if (isUltimate)
            {
                int cost = playerEntity.GetEffectiveSkillCost(combatManager.ultimateSkillBaseCost);
                int lvl = GameManager.Instance != null ? GameManager.Instance.ultimateSkillLevel : 1;
                combatManager.ShowCustomBannerTooltip(
                    $"<b>KỸ NĂNG TỐI THƯỢNG (Lv.{lvl})</b> [{cost} NL]: Gây <b>{combatManager.ultimateSkillDamage} Sát thương</b> lớn và áp <b>+{combatManager.ultimateSkillBurnStacks} tầng Thiêu đốt</b> lên đối thủ.");
            }
            else
            {
                int cost = playerEntity.GetEffectiveSkillCost(combatManager.normalSkillBaseCost);
                int lvl = GameManager.Instance != null ? GameManager.Instance.normalSkillLevel : 1;
                combatManager.ShowCustomBannerTooltip(
                    $"<b>KỸ NĂNG THƯỜNG (Lv.{lvl})</b> [{cost} NL]: Tiêu hao {cost} Năng lượng để gây ngay <b>{combatManager.normalSkillDamage} Sát thương</b> đơn vị lên đối thủ.");
            }
        });
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((_) =>
        {
            if (combatManager != null) combatManager.HideTooltip();
        });
        trigger.triggers.Add(exitEntry);
    }

    public void RefreshSkillBarState()
    {
        if (!isInitialized) return;

        int curEnergy = playerEntity != null ? playerEntity.CurrentEnergy : 0;
        int maxEnergy = playerEntity != null ? playerEntity.MaxEnergy : 10;
        int discount = playerEntity != null ? playerEntity.NextSkillCostReduction : 0;

        if (energyHeaderText != null)
        {
            string discountStr = discount > 0 ? $"  <color=#fde047>(-{discount} NL)</color>" : "";
            energyHeaderText.text = $"NĂNG LƯỢNG: <b>{curEnergy} / {maxEnergy}</b>{discountStr}";
        }

        for (int i = 0; i < energyPipImages.Count; i++)
        {
            Image pip = energyPipImages[i];
            if (pip == null) continue;
            if (i < curEnergy)
            {
                pip.color = i >= 6
                    ? new Color(1f, 0.65f, 0.18f, 0.98f)
                    : new Color(0.22f, 0.85f, 1f, 0.98f);
            }
            else
            {
                pip.color = new Color(0.14f, 0.18f, 0.26f, 0.78f);
            }
        }

        if (combatManager == null) return;

        int normalCost = playerEntity != null
            ? playerEntity.GetEffectiveSkillCost(combatManager.normalSkillBaseCost)
            : combatManager.normalSkillBaseCost;
        int ultCost = playerEntity != null
            ? playerEntity.GetEffectiveSkillCost(combatManager.ultimateSkillBaseCost)
            : combatManager.ultimateSkillBaseCost;

        int normLvl = GameManager.Instance != null ? GameManager.Instance.normalSkillLevel : 1;
        int ultLvl = GameManager.Instance != null ? GameManager.Instance.ultimateSkillLevel : 1;

        bool canAct = combatManager.CanPlayerCastCharacterSkills();

        if (normalSkillBtn != null)
        {
            normalSkillBtn.interactable = canAct && curEnergy >= normalCost;
        }
        if (normalSkillText != null)
        {
            normalSkillText.text = $"SKILL THƯỜNG Lv.{normLvl} [{normalCost} NL]\n<size=11>Gây {combatManager.normalSkillDamage} Sát thương</size>";
        }

        if (ultimateSkillBtn != null)
        {
            ultimateSkillBtn.interactable = canAct && curEnergy >= ultCost;
        }
        if (ultimateSkillText != null)
        {
            ultimateSkillText.text = $"TỐI THƯỢNG Lv.{ultLvl} [{ultCost} NL]\n<size=11>{combatManager.ultimateSkillDamage} ST + {combatManager.ultimateSkillBurnStacks} Cháy</size>";
        }
    }
}

