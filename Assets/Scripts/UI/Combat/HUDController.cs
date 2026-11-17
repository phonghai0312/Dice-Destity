using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// HUDController — Giao tiếp với BattleEntity thông qua Observer Pattern (GDD mục 6).
/// Hiển thị Máu, Giáp và Túi hiệu ứng trạng thái (Poison, Stun, Weak, BonusAtk).
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Data Source")]
    public BattleEntity targetEntity;

    [Header("UI References")]
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI shieldText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI energyText;

    private void OnEnable()
    {
        if (targetEntity != null)
        {
            targetEntity.OnHPChanged += UpdateHPUI;
            targetEntity.OnShieldChanged += UpdateShieldUI;
            targetEntity.OnStatusChanged += UpdateStatusUI;
            targetEntity.OnEnergyChanged += UpdateEnergyUI;

            // Đồng bộ ngay khi bật
            UpdateHPUI(targetEntity.GetCurrentHP(), targetEntity.GetMaxHP());
            UpdateShieldUI(targetEntity.GetCurrentShield());
            UpdateStatusUI();
            UpdateEnergyUI(targetEntity.CurrentEnergy, targetEntity.MaxEnergy);
        }
    }

    private void OnDisable()
    {
        if (targetEntity != null)
        {
            targetEntity.OnHPChanged -= UpdateHPUI;
            targetEntity.OnShieldChanged -= UpdateShieldUI;
            targetEntity.OnStatusChanged -= UpdateStatusUI;
            targetEntity.OnEnergyChanged -= UpdateEnergyUI;
        }
    }

    private void UpdateHPUI(int currentHP, int maxHP)
    {
        if (hpText != null)
        {
            hpText.text = $"{currentHP} / {maxHP}";
        }
    }

    private void UpdateShieldUI(int currentShield)
    {
        if (shieldText != null)
        {
            shieldText.text = currentShield > 0 ? $"🛡 {currentShield}" : "";
        }
    }

    private void UpdateEnergyUI(int currentEnergy, int maxEnergy)
    {
        if (energyText != null)
        {
            int discount = targetEntity != null ? targetEntity.NextSkillCostReduction : 0;
            string discountTag = discount > 0 ? $" (-{discount}🔷)" : "";
            energyText.text = $"🔷 {currentEnergy}/{maxEnergy}{discountTag}";
        }
    }

    private void UpdateStatusUI()
    {
        if (targetEntity == null || statusText == null) return;

        List<string> tags = new List<string>();
        if (targetEntity.PoisonStacks > 0)
            tags.Add($"<color=#a855f7>☠ Độc:{targetEntity.PoisonStacks}</color>");
        if (targetEntity.BurnStacks > 0)
            tags.Add($"<color=#fb923c>🔥 Thiêu đốt:{targetEntity.BurnStacks}</color>");
        if (targetEntity.StunStacks > 0)
            tags.Add($"<color=#facc15>⚡ Choáng:{targetEntity.StunStacks}</color>");
        if (targetEntity.WeakStacks > 0)
            tags.Add($"<color=#94a3b8>⬇ Suy yếu:{targetEntity.WeakStacks}</color>");
        if (targetEntity.BonusAttack > 0)
            tags.Add($"<color=#f97316>⚔ +{targetEntity.BonusAttack} Công</color>");
        if (targetEntity.RetainHalfShieldNextTurn)
            tags.Add($"<color=#38bdf8>🏰 Giữ 50% Giáp</color>");

        statusText.text = string.Join("  ", tags);
    }
}
