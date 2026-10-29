using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// CharacterSkillBarUI — Quản lý giao diện 2 Kỹ Năng Nhân Vật (Skill Thường & Skill Tối Thượng)
/// kèm icon minh họa hình ảnh tuyệt chiêu, đặt gọn gàng bên dưới dòng chữ "KHAY XÚC XẮC CHIẾN THUẬT"
/// trong lòng DiceTray.
/// </summary>
public class CharacterSkillBarUI : MonoBehaviour
{
    [Header("References")]
    public SimpleCombatManager combatManager;
    public BattleEntity playerEntity;

    [Header("Skill Icons")]
    public Sprite normalSkillIconSprite;
    public Sprite ultimateSkillIconSprite;

    private Button normalSkillBtn;
    private TextMeshProUGUI normalSkillTitleText;
    private TextMeshProUGUI normalSkillDescText;
    private Image normalSkillIconImg;
    private Outline normalSkillOutline;

    private Button ultimateSkillBtn;
    private TextMeshProUGUI ultimateSkillTitleText;
    private TextMeshProUGUI ultimateSkillDescText;
    private Image ultimateSkillIconImg;
    private Outline ultimateSkillOutline;

    private bool isInitialized = false;

    public void Initialize(SimpleCombatManager manager, BattleEntity player, Transform parentCanvas)
    {
        combatManager = manager;
        playerEntity = player;

        LoadSkillIconSprites();

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

    private void LoadSkillIconSprites()
    {
#if UNITY_EDITOR
        if (normalSkillIconSprite == null)
            normalSkillIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Skills/Skill_Normal_Slash.png");
        if (ultimateSkillIconSprite == null)
            ultimateSkillIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Skills/Skill_Ultimate_Cleave.png");
#endif
        if (normalSkillIconSprite == null)
            normalSkillIconSprite = LoadSpriteFromFile("Assets/Sprites/Skills/Skill_Normal_Slash.png");
        if (ultimateSkillIconSprite == null)
            ultimateSkillIconSprite = LoadSpriteFromFile("Assets/Sprites/Skills/Skill_Ultimate_Cleave.png");
    }

    private static Sprite LoadSpriteFromFile(string path)
    {
        if (!System.IO.File.Exists(path)) return null;
        try
        {
            byte[] bytes = System.IO.File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(bytes))
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }
        catch { }
        return null;
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
        // Dọn dẹp root cũ nếu có
        Transform oldRoot = parentCanvas.Find("CharacterSkillBarRoot");
        if (oldRoot != null) Destroy(oldRoot.gameObject);

        // Đặt 2 ô kỹ năng trực tiếp bên trong DiceTray dưới nhãn DiceTrayLabel
        Transform diceTrayTr = parentCanvas.Find("DiceTray");
        Transform containerParent = diceTrayTr != null ? diceTrayTr : parentCanvas;

        Transform existingGroup = containerParent.Find("SkillButtonsGroup");
        if (existingGroup != null) Destroy(existingGroup.gameObject);

        GameObject skillGroup = new GameObject("SkillButtonsGroup", typeof(RectTransform));
        skillGroup.transform.SetParent(containerParent, false);
        RectTransform groupRt = skillGroup.GetComponent<RectTransform>();
        groupRt.anchorMin = new Vector2(0f, 1f);
        groupRt.anchorMax = new Vector2(0f, 1f);
        groupRt.pivot = new Vector2(0f, 1f);
        groupRt.anchoredPosition = new Vector2(40f, -54f);
        groupRt.sizeDelta = new Vector2(400f, 142f);

        // 1. Nút Kỹ năng Thường (Normal Skill)
        normalSkillBtn = CreateSkillButtonWidget(
            skillGroup.transform,
            "NormalSkillButton",
            new Vector2(0f, 0f),
            new Vector2(195f, 142f),
            new Color(0.10f, 0.16f, 0.28f, 0.95f),
            new Color(0.35f, 0.78f, 1f, 0.9f),
            normalSkillIconSprite,
            out normalSkillTitleText,
            out normalSkillIconImg,
            out normalSkillDescText,
            out normalSkillOutline);

        normalSkillBtn.onClick.AddListener(() =>
        {
            if (combatManager != null) combatManager.TryCastNormalSkill();
        });

        AddSkillHoverTooltip(normalSkillBtn.gameObject, isUltimate: false);

        // 2. Nút Kỹ năng Tối thượng (Ultimate Skill)
        ultimateSkillBtn = CreateSkillButtonWidget(
            skillGroup.transform,
            "UltimateSkillButton",
            new Vector2(205f, 0f),
            new Vector2(195f, 142f),
            new Color(0.28f, 0.12f, 0.10f, 0.95f),
            new Color(1f, 0.58f, 0.18f, 0.92f),
            ultimateSkillIconSprite,
            out ultimateSkillTitleText,
            out ultimateSkillIconImg,
            out ultimateSkillDescText,
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
        Sprite iconSprite,
        out TextMeshProUGUI titleTmp,
        out Image iconImg,
        out TextMeshProUGUI descTmp,
        out Outline outlineComp)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        img.color = bgColor;

        outlineComp = go.GetComponent<Outline>();
        outlineComp.effectColor = outlineColor;
        outlineComp.effectDistance = new Vector2(1.6f, -1.6f);

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        // 1. Tiêu đề kỹ năng + Cost ở phía trên
        GameObject titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(go.transform, false);
        RectTransform titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -5f);
        titleRt.sizeDelta = new Vector2(-10f, 34f);

        titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
        titleTmp.fontSize = 12f;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        titleTmp.raycastTarget = false;

        // 2. Icon hình ảnh tuyệt chiêu ở giữa
        GameObject iconGo = new GameObject("SkillIcon", typeof(RectTransform), typeof(Image), typeof(Outline));
        iconGo.transform.SetParent(go.transform, false);
        RectTransform iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(0f, -2f);
        iconRt.sizeDelta = new Vector2(56f, 56f);

        iconImg = iconGo.GetComponent<Image>();
        iconImg.sprite = iconSprite;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        Outline iconOl = iconGo.GetComponent<Outline>();
        iconOl.effectColor = new Color(0.1f, 0.12f, 0.18f, 0.95f);
        iconOl.effectDistance = new Vector2(1.2f, -1.2f);

        // 3. Mô tả sát thương / hiệu ứng ở phía dưới
        GameObject descGo = new GameObject("DescText", typeof(RectTransform), typeof(TextMeshProUGUI));
        descGo.transform.SetParent(go.transform, false);
        RectTransform descRt = descGo.GetComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0f, 0f);
        descRt.anchorMax = new Vector2(1f, 0f);
        descRt.pivot = new Vector2(0.5f, 0f);
        descRt.anchoredPosition = new Vector2(0f, 5f);
        descRt.sizeDelta = new Vector2(-10f, 32f);

        descTmp = descGo.GetComponent<TextMeshProUGUI>();
        descTmp.fontSize = 11f;
        descTmp.alignment = TextAlignmentOptions.Center;
        descTmp.color = new Color(0.9f, 0.93f, 0.98f, 1f);
        descTmp.raycastTarget = false;

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
                    $"<b>KỸ NĂNG THƯỜNG (Lv.{lvl})</b> [{cost} NL]: Tiêu hao {cost} Năng lượng để gây ngay <b>{combatManager.normalSkillDamage} Sát thương</b> lên đối thủ.");
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
            bool canAffordNormal = canAct && curEnergy >= normalCost;
            normalSkillBtn.interactable = canAffordNormal;

            if (normalSkillTitleText != null)
            {
                string costTag = discount > 0
                    ? $"<color=#7dd3fc>[{normalCost} NL]</color> <color=#fde047>(-{discount})</color>"
                    : $"<color=#7dd3fc>[{normalCost} NL]</color>";
                normalSkillTitleText.text = $"<b>SKILL THƯỜNG Lv.{normLvl}</b>\n{costTag}";
            }

            if (normalSkillDescText != null)
            {
                normalSkillDescText.text = canAffordNormal
                    ? $"<color=#e2e8f0>Gây <b>{combatManager.normalSkillDamage} Sát thương</b></color>"
                    : $"<color=#94a3b8>(Chưa đủ {normalCost} NL)</color>";
            }

            if (normalSkillIconImg != null)
            {
                normalSkillIconImg.color = canAffordNormal ? Color.white : new Color(0.5f, 0.5f, 0.55f, 0.65f);
            }
        }

        if (ultimateSkillBtn != null)
        {
            bool canAffordUlt = canAct && curEnergy >= ultCost;
            ultimateSkillBtn.interactable = canAffordUlt;

            if (ultimateSkillTitleText != null)
            {
                string costTag = discount > 0
                    ? $"<color=#fca5a5>[{ultCost} NL]</color> <color=#fde047>(-{discount})</color>"
                    : $"<color=#fca5a5>[{ultCost} NL]</color>";
                ultimateSkillTitleText.text = $"<b>TỐI THƯỢNG Lv.{ultLvl}</b>\n{costTag}";
            }

            if (ultimateSkillDescText != null)
            {
                ultimateSkillDescText.text = canAffordUlt
                    ? $"<color=#fef08a><b>{combatManager.ultimateSkillDamage} ST</b> + <b>{combatManager.ultimateSkillBurnStacks} Cháy</b></color>"
                    : $"<color=#94a3b8>(Chưa đủ {ultCost} NL)</color>";
            }

            if (ultimateSkillIconImg != null)
            {
                ultimateSkillIconImg.color = canAffordUlt ? Color.white : new Color(0.5f, 0.5f, 0.55f, 0.65f);
            }
        }
    }
}
