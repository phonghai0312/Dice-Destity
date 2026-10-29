using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// CharacterSkillBarUI - Quản lý giao diện Nút Kỹ năng Thường (Normal Skill) và Nút Kỹ năng Tối thượng (Ultimate Skill)
/// được đặt ngay bên dưới dòng chữ "KHAY XÚC XẮC CHIẾN THUẬT" trong DiceTray,
/// kèm hiệu ứng phát sáng viền khi đủ Năng lượng và Tooltip chi tiết khi hover chuột.
/// </summary>
public class CharacterSkillBarUI : MonoBehaviour
{
    [Header("References")]
    public SimpleCombatManager combatManager;
    public BattleEntity playerEntity;

    private Button normalSkillBtn;
    private TextMeshProUGUI normalSkillText;
    private Outline normalSkillOutline;

    private Button ultimateSkillBtn;
    private TextMeshProUGUI ultimateSkillText;
    private Outline ultimateSkillOutline;

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
                : new Color(0.22f, 0.35f, 0.52f, 0.45f);
        }

        if (ultimateSkillOutline != null)
        {
            ultimateSkillOutline.effectColor = (canAct && curEnergy >= ultCost)
                ? new Color(1f, 0.62f, 0.15f, pulse)
                : new Color(0.45f, 0.22f, 0.18f, 0.45f);
        }
    }

    private void OnPlayerEnergyChanged(int current, int max)
    {
        RefreshSkillBarState();
    }

    private void BuildSkillBarHierarchy(Transform parentCanvas)
    {
        // Gắn trực tiếp vào DiceTray dưới dòng chữ "KHAY XÚC XẮC CHIẾN THUẬT" theo layout thiết kế
        Transform diceTrayTr = parentCanvas.Find("DiceTray");
        Transform buttonsParent = diceTrayTr != null ? diceTrayTr : parentCanvas;

        Transform existingNorm = buttonsParent.Find("NormalSkillButton");
        if (existingNorm != null) Destroy(existingNorm.gameObject);

        Transform existingUlt = buttonsParent.Find("UltimateSkillButton");
        if (existingUlt != null) Destroy(existingUlt.gameObject);

        Vector2 normalPos;
        Vector2 ultPos;
        Vector2 buttonSize = new Vector2(195f, 136f);
        bool isInsideTray = diceTrayTr != null;

        if (isInsideTray)
        {
            // DiceTrayLabel ở anchorMin=(0, 1), anchoredPosition=(42, -22)
            // 2 ô kỹ năng nằm trực tiếp phía dưới dòng chữ Khay Xúc Xắc
            normalPos = new Vector2(42f, -56f);
            ultPos = new Vector2(247f, -56f);
        }
        else
        {
            normalPos = new Vector2(-785f, -442f);
            ultPos = new Vector2(-575f, -442f);
        }

        // 1. Nút Kỹ năng Thường (Normal Skill)
        normalSkillBtn = CreateSkillButtonWidget(
            buttonsParent,
            "NormalSkillButton",
            normalPos,
            buttonSize,
            new Color(0.08f, 0.15f, 0.25f, 0.96f),
            new Color(0.24f, 0.75f, 1f, 0.85f),
            isInsideTray,
            out normalSkillText,
            out normalSkillOutline);

        normalSkillBtn.onClick.AddListener(() =>
        {
            if (combatManager != null) combatManager.TryCastNormalSkill();
        });

        AddSkillHoverTooltip(normalSkillBtn.gameObject, isUltimate: false);

        // 2. Nút Kỹ năng Tối thượng (Ultimate Skill)
        ultimateSkillBtn = CreateSkillButtonWidget(
            buttonsParent,
            "UltimateSkillButton",
            ultPos,
            buttonSize,
            new Color(0.26f, 0.08f, 0.07f, 0.96f),
            new Color(1f, 0.55f, 0.18f, 0.85f),
            isInsideTray,
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
        bool useTopLeftAnchor,
        out TextMeshProUGUI captionTmp,
        out Outline outlineComp)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        if (useTopLeftAnchor)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
        }
        else
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
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
        txtRt.offsetMin = new Vector2(8f, 6f);
        txtRt.offsetMax = new Vector2(-8f, -6f);

        captionTmp = txtGo.GetComponent<TextMeshProUGUI>();
        captionTmp.fontSize = 13.5f;
        captionTmp.alignment = TextAlignmentOptions.Center;
        captionTmp.color = Color.white;
        captionTmp.lineSpacing = 4f;
        captionTmp.richText = true;
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
            string costStr = discount > 0 ? $"[{normalCost} NL <color=#fde047>(-{discount})</color>]" : $"[{normalCost} NL]";
            normalSkillText.text = $"<b>SKILL THƯỜNG Lv.{normLvl}</b>\n<color=#38bdf8><b>{costStr}</b></color>\n<size=11.5><color=#93c5fd>Gây {combatManager.normalSkillDamage} Sát thương</color></size>";
        }

        if (ultimateSkillBtn != null)
        {
            ultimateSkillBtn.interactable = canAct && curEnergy >= ultCost;
        }
        if (ultimateSkillText != null)
        {
            string costStr = discount > 0 ? $"[{ultCost} NL <color=#fde047>(-{discount})</color>]" : $"[{ultCost} NL]";
            ultimateSkillText.text = $"<b>TỐI THƯỢNG Lv.{ultLvl}</b>\n<color=#fb923c><b>{costStr}</b></color>\n<size=11.5><color=#fed7aa>{combatManager.ultimateSkillDamage} ST + {combatManager.ultimateSkillBurnStacks} Cháy</color></size>";
        }
    }
}
