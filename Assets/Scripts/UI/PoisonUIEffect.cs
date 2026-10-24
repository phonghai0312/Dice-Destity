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
            SetPoisoned(targetEntity.PoisonStacks > 0);
        }
    }

    public void SetPoisoned(bool isPoisoned)
    {
        if (hpBarImage != null)
        {
            if (!initializedColor)
            {
                originalColor = hpBarImage.color;
                initializedColor = true;
            }
            hpBarImage.color = isPoisoned ? new Color(0.6f, 0.1f, 0.85f) : originalColor;
        }
    }
}
