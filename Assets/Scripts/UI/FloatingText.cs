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
        
        tmpro.text = "-" + damage;
        if (isPierce) {
            tmpro.color = Color.magenta;
            tmpro.text += " (Pierce!)";
        } else {
            tmpro.color = Color.red;
        }

        StartCoroutine(AnimateText());
    }

    private IEnumerator AnimateText()
    {
        float timer = 0;
        Color startColor = tmpro.color;

        while (timer < fadeDuration)
        {
            rectTransform.anchoredPosition += Vector2.up * floatSpeed * Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            tmpro.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
