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

    private void OnEnable()
    {
        if (targetEntity != null)
        {
            targetEntity.OnHPChanged += UpdateHPUI;
            targetEntity.OnShieldChanged += UpdateShieldUI;
            targetEntity.OnStatusChanged += UpdateStatusUI;

            // Đồng bộ ngay khi bật
            UpdateHPUI(targetEntity.GetCurrentHP(), targetEntity.GetMaxHP());
            UpdateShieldUI(targetEntity.GetCurrentShield());
            UpdateStatusUI();
        }
    }

    private void OnDisable()
    {
        if (targetEntity != null)
        {
            targetEntity.OnHPChanged -= UpdateHPUI;
            targetEntity.OnShieldChanged -= UpdateShieldUI;
            targetEntity.OnStatusChanged -= UpdateStatusUI;
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

    private void UpdateStatusUI()
    {
        if (targetEntity == null || statusText == null) return;

        List<string> tags = new List<string>();
        if (targetEntity.PoisonStacks > 0)
            tags.Add($"<color=#a855f7>☠ Độc:{targetEntity.PoisonStacks}</color>");
        if (targetEntity.StunStacks > 0)
            tags.Add($"<color=#facc15>⚡ Choáng:{targetEntity.StunStacks}</color>");
        if (targetEntity.WeakStacks > 0)
            tags.Add($"<color=#94a3b8>⬇ Suy yếu:{targetEntity.WeakStacks}</color>");
        if (targetEntity.BonusAttack > 0)
            tags.Add($"<color=#f97316>🔥 +{targetEntity.BonusAttack} Công</color>");
        if (targetEntity.RetainHalfShieldNextTurn)
            tags.Add($"<color=#38bdf8>🏰 Giữ 50% Giáp</color>");

        statusText.text = string.Join("  ", tags);
    }
}
