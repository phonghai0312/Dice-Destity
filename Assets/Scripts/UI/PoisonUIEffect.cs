using UnityEngine;
using UnityEngine.UI;

public class PoisonUIEffect : MonoBehaviour
{
    public Image hpBarImage;
    private Color originalColor;

    private void Start()
    {
        if (hpBarImage != null)
            originalColor = hpBarImage.color;
    }

    public void SetPoisoned(bool isPoisoned)
    {
        if (hpBarImage != null)
        {
            // Dark purple to indicate poison status
            hpBarImage.color = isPoisoned ? new Color(0.6f, 0f, 0.8f) : originalColor;
        }
    }
}
