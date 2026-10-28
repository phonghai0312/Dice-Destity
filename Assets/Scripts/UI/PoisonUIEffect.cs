using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PoisonUIEffect — Tự động đổi màu thanh máu sang tím độc khi thực thể đang dính Poison (GDD 3.1 & 3.3).
/// </summary>
public class PoisonUIEffect : MonoBehaviour
{
    public Image hpBarImage;
    public BattleEntity targetEntity;
    private Color originalColor = Color.white;
    private bool initializedColor;

    private void Awake()
    {
        if (hpBarImage != null && !initializedColor)
        {
            originalColor = hpBarImage.color;
            initializedColor = true;
        }
        if (targetEntity == null)
        {
            targetEntity = GetComponentInParent<BattleEntity>();
        }
    }

    private void OnEnable()
    {
        if (targetEntity != null)
        {
            targetEntity.OnStatusChanged += HandleStatusChanged;
            HandleStatusChanged();
        }
    }

    private void OnDisable()
    {
        if (targetEntity != null)
        {
            targetEntity.OnStatusChanged -= HandleStatusChanged;
        }
    }

    private void HandleStatusChanged()
    {
        if (targetEntity != null)
        {
            UpdateHPBarStatusColor(targetEntity.PoisonStacks > 0, targetEntity.BurnStacks > 0);
        }
    }

    public void SetPoisoned(bool isPoisoned)
    {
        bool isBurning = targetEntity != null && targetEntity.BurnStacks > 0;
        UpdateHPBarStatusColor(isPoisoned, isBurning);
    }

    public void UpdateHPBarStatusColor(bool isPoisoned, bool isBurning)
    {
        if (hpBarImage != null)
        {
            if (!initializedColor)
            {
                originalColor = hpBarImage.color;
                initializedColor = true;
            }

            if (isBurning && isPoisoned)
            {
                hpBarImage.color = new Color(0.88f, 0.25f, 0.55f); // Tím-Cam khi vừa Độc vừa Thiêu đốt
            }
            else if (isBurning)
            {
                hpBarImage.color = new Color(0.96f, 0.42f, 0.14f); // Đỏ-Cam rực khi dính Thiêu đốt
            }
            else if (isPoisoned)
            {
                hpBarImage.color = new Color(0.6f, 0.1f, 0.85f); // Tím độc khi dính Poison
            }
            else
            {
                hpBarImage.color = originalColor;
            }
        }
    }
}
