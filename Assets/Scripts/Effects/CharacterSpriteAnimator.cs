using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CharacterSpriteAnimator — Tạo hoạt ảnh "Nhún Nhún" (Squash & Stretch / Idle Bobbing) 
/// và các đòn đánh "Chém / Lướt" (Slash Attack) mượt mà cho cả SpriteRenderer 2D lẫn UI Image (RectTransform).
/// Áp dụng tốt cho cả Hero và Quái vật (Orc, Minotaur, Goblin).
/// </summary>
public class CharacterSpriteAnimator : MonoBehaviour
{
    [Header("1. Cài Đặt Nhún Nhún (Idle Bobbing / Breathing)")]
    [Tooltip("Bật/tắt hiệu ứng nhún nhún khi đứng chờ lượt")]
    public bool enableIdleBobbing = true;
    [Tooltip("Tốc độ thở / nhún")]
    public float bobSpeed = 2.6f;
    [Tooltip("Biên độ nhấc người lên xuống (Y) — tự nhân theo UI nếu là RectTransform")]
    public float bobHeight = 0.08f;
    [Tooltip("Độ co giãn cơ bắp (Squash & Stretch)")]
    public float squashStretchFactor = 0.03f;
    [Tooltip("Độ lệch pha ngẫu nhiên để các quái vật không nhún cùng một lúc")]
    public float timeOffset = 0f;

    [Header("2. Cài Đặt Đòn Chém (Slash Attack Settings)")]
    [Tooltip("Đối tượng Sprite vệt chém (Slash VFX child object)")]
    public GameObject slashVFXObject;
    [Tooltip("Thời gian lấy đà ra sau (giây)")]
    public float windupTime = 0.1f;
    [Tooltip("Thời gian lao tới chém (giây)")]
    public float strikeTime = 0.12f;
    [Tooltip("Thời gian thu vũ khí lùi về vị trí cũ (giây)")]
    public float recoverTime = 0.2f;

    [Header("3. Hiệu Ứng Bị Chém / Chịu Sát Thương (Take Damage)")]
    public Color hitFlashColor = new Color(1f, 0.25f, 0.25f, 1f);
    public float hitFlashDuration = 0.15f;
    public float knockbackDistance = 0.25f;

    // Component References (Supports both SpriteRenderer and UI Image)
    private SpriteRenderer spriteRenderer;
    private Image uiImage;
    private Transform targetVisualTransform;
    private bool isUIElement = false;
    private Vector3 originalLocalPos;
    private Vector3 originalLocalScale;
    private bool isExecutingAction = false;
    private Coroutine currentActionCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        uiImage = GetComponent<Image>();
        if (uiImage == null)
        {
            uiImage = GetComponentInChildren<Image>();
        }

        if (spriteRenderer != null)
        {
            targetVisualTransform = spriteRenderer.transform;
            isUIElement = false;
        }
        else if (uiImage != null)
        {
            targetVisualTransform = uiImage.transform;
            isUIElement = true;
        }
        else
        {
            targetVisualTransform = transform;
            isUIElement = GetComponent<RectTransform>() != null;
        }

        originalLocalPos = targetVisualTransform.localPosition;
        originalLocalScale = targetVisualTransform.localScale;

        if (timeOffset == 0f)
        {
            timeOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        }

        if (slashVFXObject != null)
        {
            slashVFXObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (enableIdleBobbing && !isExecutingAction && targetVisualTransform != null)
        {
            ApplyIdleBobbing();
        }
    }

    private void ApplyIdleBobbing()
    {
        float wave = Mathf.Sin((Time.time * bobSpeed) + timeOffset);

        // Nếu là UI RectTransform, biên độ tính bằng pixel (khoảng 4.5px), còn World Space là 0.08 unit
        float effectiveBobHeight = isUIElement ? (bobHeight * 55f) : bobHeight;

        Vector3 newPos = originalLocalPos;
        newPos.y += wave * effectiveBobHeight;
        targetVisualTransform.localPosition = newPos;

        Vector3 newScale = originalLocalScale;
        newScale.y += wave * (originalLocalScale.y * squashStretchFactor);
        newScale.x -= wave * (originalLocalScale.x * (squashStretchFactor * 0.5f));
        targetVisualTransform.localScale = newScale;
    }

    public void PlaySlashAttack(Transform targetTransform, Action onHitCallback = null)
    {
        if (isExecutingAction && currentActionCoroutine != null)
        {
            StopCoroutine(currentActionCoroutine);
        }
        currentActionCoroutine = StartCoroutine(SlashAttackRoutine(targetTransform, onHitCallback));
    }

    private IEnumerator SlashAttackRoutine(Transform target, Action onHit)
    {
        isExecutingAction = true;
        Transform mover = targetVisualTransform != null ? targetVisualTransform : transform;
        Vector3 startLocalPos = originalLocalPos;

        float dirSign = 1f;
        if (target != null)
        {
            dirSign = (target.position.x >= mover.position.x) ? 1f : -1f;
        }

        float stepMultiplier = isUIElement ? 65f : 1f;
        Vector3 windupLocalPos = startLocalPos - new Vector3(dirSign * 0.35f * stepMultiplier, 0f, 0f);
        Vector3 lungeLocalPos = startLocalPos + new Vector3(dirSign * 1.15f * stepMultiplier, 0f, 0f);

        float elapsed = 0f;
        while (elapsed < windupTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / windupTime;
            mover.localPosition = Vector3.Lerp(startLocalPos, windupLocalPos, t);
            mover.localScale = new Vector3(
                originalLocalScale.x * 1.08f,
                originalLocalScale.y * 0.92f,
                originalLocalScale.z
            );
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < strikeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / strikeTime;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            mover.localPosition = Vector3.Lerp(windupLocalPos, lungeLocalPos, smoothT);
            mover.localScale = new Vector3(
                originalLocalScale.x * 0.94f,
                originalLocalScale.y * 1.12f,
                originalLocalScale.z
            );
            yield return null;
        }
        mover.localPosition = lungeLocalPos;

        if (slashVFXObject != null)
        {
            slashVFXObject.SetActive(true);
        }

        if (onHit != null)
        {
            onHit.Invoke();
        }

        yield return new WaitForSeconds(0.08f);

        if (slashVFXObject != null)
        {
            slashVFXObject.SetActive(false);
        }

        elapsed = 0f;
        while (elapsed < recoverTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / recoverTime;
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            mover.localPosition = Vector3.Lerp(lungeLocalPos, startLocalPos, easeT);
            mover.localScale = Vector3.Lerp(mover.localScale, originalLocalScale, t);
            yield return null;
        }

        mover.localPosition = originalLocalPos;
        mover.localScale = originalLocalScale;
        isExecutingAction = false;
    }

    public void PlayTakeDamage()
    {
        if (isExecutingAction && currentActionCoroutine != null)
        {
            StopCoroutine(currentActionCoroutine);
        }
        currentActionCoroutine = StartCoroutine(TakeDamageRoutine());
    }

    private IEnumerator TakeDamageRoutine()
    {
        isExecutingAction = true;
        Transform mover = targetVisualTransform != null ? targetVisualTransform : transform;
        Color normalColor = GetVisualColor();

        float stepMultiplier = isUIElement ? 55f : 1f;
        Vector3 recoilLocalPos = originalLocalPos - new Vector3(knockbackDistance * stepMultiplier, 0f, 0f);
        SetVisualColor(hitFlashColor);

        float elapsed = 0f;
        float dur = hitFlashDuration;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            float shakeOffset = Mathf.Sin(elapsed * 55f) * (0.06f * stepMultiplier);
            mover.localPosition = Vector3.Lerp(recoilLocalPos, originalLocalPos, t) + new Vector3(0f, shakeOffset, 0f);
            yield return null;
        }

        SetVisualColor(normalColor);
        mover.localPosition = originalLocalPos;
        mover.localScale = originalLocalScale;
        isExecutingAction = false;
    }

    private Color GetVisualColor()
    {
        if (spriteRenderer != null) return spriteRenderer.color;
        if (uiImage != null) return uiImage.color;
        return Color.white;
    }

    private void SetVisualColor(Color c)
    {
        if (spriteRenderer != null) spriteRenderer.color = c;
        if (uiImage != null) uiImage.color = c;
    }
}
