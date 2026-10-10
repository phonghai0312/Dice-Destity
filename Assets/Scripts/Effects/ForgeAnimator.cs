using UnityEngine;
using System.Collections;

/// <summary>
/// ForgeAnimator — Gắn vào các object trong Forge Scene để tạo hiệu ứng trôi nổi.
/// Cách dùng: AddComponent<ForgeAnimator>() lên bất kỳ UI object nào.
/// </summary>
public class ForgeAnimator : MonoBehaviour
{
    [Header("Float Settings")]
    public float floatAmplitude = 8f;    // Biên độ nổi lên xuống (pixel)
    public float floatSpeed = 1.2f;       // Tốc độ nổi
    public float floatOffset = 0f;        // Phase offset để các item không nổi cùng lúc

    [Header("Glow Pulse Settings")]
    public bool enableGlowPulse = false;  // Bật/tắt hiệu ứng sáng nhấp nháy
    public UnityEngine.UI.Image targetImage;
    public Color glowColor = new Color(0.72f, 0.53f, 0.05f, 1f);
    public Color normalColor = new Color(0.38f, 0.05f, 0.05f, 0.9f);
    public float pulseSpeed = 1.5f;

    [Header("Entry Anim")]
    public bool playEntryAnim = true;
    public float entryDelay = 0f;

    private RectTransform rt;
    private Vector2 startPos;
    private bool ready = false;

    void Start()
    {
        rt = transform as RectTransform;
        if (rt == null) return;
        startPos = rt.anchoredPosition;
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // Entry animation: trượt vào từ dưới + fade in
        if (playEntryAnim)
        {
            Vector2 offscreen = startPos + new Vector2(0, -60f);
            rt.anchoredPosition = offscreen;

            UnityEngine.UI.CanvasGroup cg = GetComponent<UnityEngine.UI.CanvasGroup>() 
                ?? gameObject.AddComponent<UnityEngine.UI.CanvasGroup>();
            cg.alpha = 0f;

            yield return new WaitForSeconds(entryDelay);

            float t = 0f;
            float dur = 0.45f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = t / dur;
                p = 1f - Mathf.Pow(1f - p, 3f); // Ease-out cubic
                rt.anchoredPosition = Vector2.Lerp(offscreen, startPos, p);
                cg.alpha = p;
                yield return null;
            }
            rt.anchoredPosition = startPos;
            cg.alpha = 1f;
        }

        ready = true;
    }

    void Update()
    {
        if (!ready || rt == null) return;

        // Float animation
        float y = Mathf.Sin((Time.time + floatOffset) * floatSpeed) * floatAmplitude;
        rt.anchoredPosition = new Vector2(startPos.x, startPos.y + y);

        // Glow pulse
        if (enableGlowPulse && targetImage != null)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            targetImage.color = Color.Lerp(normalColor, glowColor, t);
        }
    }

    public void Bounce()
    {
        StopAllCoroutines();
        StartCoroutine(DoBounce());
    }

    IEnumerator DoBounce()
    {
        float elapsed = 0f;
        float dur = 0.3f;
        Vector2 origin = startPos;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            float bounce = Mathf.Sin(t * Mathf.PI) * 20f;
            rt.anchoredPosition = new Vector2(origin.x, origin.y + bounce);
            yield return null;
        }
        rt.anchoredPosition = origin;
        ready = true;
    }
}
