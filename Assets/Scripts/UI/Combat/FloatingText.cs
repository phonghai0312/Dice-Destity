using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingText : MonoBehaviour
{
    private TextMeshProUGUI tmpro;
    private RectTransform rectTransform;
    public float floatSpeed = 100f;
    public float fadeDuration = 1f;

    public void Setup(int damage, bool isPierce)
    {
        tmpro = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
        if (tmpro == null) return;

        tmpro.text = "-" + damage;
        if (isPierce)
        {
            tmpro.color = Color.magenta;
            tmpro.text += " (Độc!)";
        }
        else
        {
            tmpro.color = new Color(0.94f, 0.27f, 0.27f); // Đỏ tươi
        }

        StartCoroutine(AnimateText());
    }

    public void SetupCustom(string message, Color textColor)
    {
        tmpro = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
        if (tmpro == null) return;

        tmpro.text = message;
        tmpro.color = textColor;

        StartCoroutine(AnimateText());
    }

    private IEnumerator AnimateText()
    {
        float timer = 0f;
        Color startColor = tmpro != null ? tmpro.color : Color.white;

        while (timer < fadeDuration)
        {
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition += Vector2.up * floatSpeed * Time.deltaTime;
            }
            if (tmpro != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                tmpro.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
