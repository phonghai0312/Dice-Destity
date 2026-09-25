using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("Data Source")]
    public BattleEntity targetEntity;
    
    [Header("UI References")]
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI shieldText;

    private void OnEnable()
    {
        if (targetEntity != null)
        {
            // Subscribe to events (Observer Pattern)
            targetEntity.OnHPChanged += UpdateHPUI;
            targetEntity.OnShieldChanged += UpdateShieldUI;
        }
    }

    private void OnDisable()
    {
        if (targetEntity != null)
        {
            // Unsubscribe to avoid memory leaks
            targetEntity.OnHPChanged -= UpdateHPUI;
            targetEntity.OnShieldChanged -= UpdateShieldUI;
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
            shieldText.text = currentShield > 0 ? currentShield.ToString() : "";
        }
    }
}
