using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DiceVisual : MonoBehaviour
{
    private Image img;
    
    private void Awake()
    {
        img = GetComponent<Image>();
    }

    public void RollDice(Color finalColor, float duration = 0.5f)
    {
        StartCoroutine(AnimateRoll(finalColor, duration));
    }

    private IEnumerator AnimateRoll(Color finalColor, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            // Randomize color rapidly (simulate rolling faces)
            img.color = new Color(Random.value, Random.value, Random.value);
            yield return new WaitForSeconds(0.05f);
            elapsed += 0.05f;
        }
        
        img.color = finalColor;
        // Bounce effect to settle
        transform.localScale = Vector3.one * 1.2f;
        yield return new WaitForSeconds(0.1f);
        transform.localScale = Vector3.one;
    }
}
