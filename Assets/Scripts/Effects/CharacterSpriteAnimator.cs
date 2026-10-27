using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CharacterSpriteAnimator — Hệ thống hoạt ảnh hành động chiến đấu cho Hiệp Sĩ & Quái Vật:
/// - Nhún nhún thở tự nhiên (Idle Bobbing / Squash & Stretch)
/// - Chém thường & Trọng kích (Slash / Heavy Cleave + Vệt kiếm VFX)
/// - Hút máu (Vampiric Slash + Hạt huyết khí bay về hồi phục)
/// - Gồng hồi máu & Thanh tẩy (Heal / Purify Power-up + Vòng hào quang năng lượng)
/// - Dựng khiên phòng thủ & Bất khuất (Guard Brace + Lá chắn quang học)
/// - Đâm độc (Venom Thrust + Vụ nổ độc tím)
/// - Giáng sấm gây choáng (Thunder Shock + Tia sét vàng)
/// - Gồng cuồng nộ (Enrage Roar cho Quái vật tích tụ sức mạnh)
/// - Trượt đòn (Miss / Blank Fumble) & Nhận sát thương (Knockback + Hit Flash)
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
    [Tooltip("Độ lệch pha ngẫu nhiên để các nhân vật không nhún cùng một lúc")]
    public float timeOffset = 0f;

    [Header("2. Cài Đặt Đòn Chém (Slash Attack Settings)")]
    [Tooltip("Đối tượng Sprite vệt chém (Slash VFX child object tùy chọn)")]
    public GameObject slashVFXObject;
    [Tooltip("Thời gian lấy đà ra sau (giây)")]
    public float windupTime = 0.11f;
    [Tooltip("Thời gian lao tới chém (giây)")]
    public float strikeTime = 0.13f;
    [Tooltip("Thời gian thu vũ khí lùi về vị trí cũ (giây)")]
    public float recoverTime = 0.22f;

    [Header("3. Hiệu Ứng Bị Chém / Chịu Sát Thương (Take Damage)")]
    public Color hitFlashColor = new Color(1f, 0.28f, 0.28f, 1f);
    public float hitFlashDuration = 0.22f;
    public float knockbackDistance = 0.42f;

    // Component References (Supports both SpriteRenderer and UI Image)
    private SpriteRenderer spriteRenderer;
    private Image uiImage;
    private Transform targetVisualTransform;
    private bool isUIElement = false;
    private Vector3 originalLocalPos;
    private Vector3 originalLocalScale;
    private Quaternion originalLocalRot;
    private Color baseVisualColor = Color.white;
    private bool isExecutingAction = false;
    private Coroutine currentActionCoroutine;
    private readonly List<GameObject> activeTempVFX = new List<GameObject>();

    private static Sprite cachedWhiteSprite;
    private static Sprite cachedCircleSprite;
    private static Sprite cachedRingSprite;
    private static Sprite cachedSlashSprite;
    private static Sprite cachedShieldSprite;

    public bool IsExecutingAction => isExecutingAction;

    private void Awake()
    {
        CacheVisualComponents();
        EnsureProceduralSprites();

        if (timeOffset == 0f)
        {
            timeOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        }

        if (slashVFXObject != null)
        {
            slashVFXObject.SetActive(false);
        }
    }

    public void CacheVisualComponents()
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
            baseVisualColor = spriteRenderer.color;
        }
        else if (uiImage != null)
        {
            targetVisualTransform = uiImage.transform;
            isUIElement = true;
            baseVisualColor = uiImage.color;
        }
        else
        {
            targetVisualTransform = transform;
            isUIElement = GetComponent<RectTransform>() != null;
            baseVisualColor = Color.white;
        }

        originalLocalPos = targetVisualTransform.localPosition;
        originalLocalScale = targetVisualTransform.localScale;
        originalLocalRot = targetVisualTransform.localRotation;
    }

    /// <summary>
    /// Cập nhật lại tọa độ gốc sau khi hệ thống UI thiết kế lại bố cục.
    /// </summary>
    public void RefreshOriginTransform()
    {
        if (targetVisualTransform == null) CacheVisualComponents();
        if (!isExecutingAction && targetVisualTransform != null)
        {
            originalLocalPos = targetVisualTransform.localPosition;
            originalLocalScale = targetVisualTransform.localScale;
            originalLocalRot = targetVisualTransform.localRotation;
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
        float effectiveBobHeight = isUIElement ? (bobHeight * 60f) : bobHeight;

        Vector3 newPos = originalLocalPos;
        newPos.y += wave * effectiveBobHeight;
        targetVisualTransform.localPosition = newPos;

        Vector3 newScale = originalLocalScale;
        newScale.y += wave * (originalLocalScale.y * squashStretchFactor);
        newScale.x -= wave * (originalLocalScale.x * (squashStretchFactor * 0.5f));
        targetVisualTransform.localScale = newScale;
        targetVisualTransform.localRotation = originalLocalRot;
    }

    private void StopCurrentActionAndReset()
    {
        if (currentActionCoroutine != null)
        {
            StopCoroutine(currentActionCoroutine);
            currentActionCoroutine = null;
        }

        CleanupTempVFX();

        if (targetVisualTransform != null)
        {
            targetVisualTransform.localPosition = originalLocalPos;
            targetVisualTransform.localScale = originalLocalScale;
            targetVisualTransform.localRotation = originalLocalRot;
        }
        SetVisualColor(baseVisualColor);
        isExecutingAction = false;
    }

    private void CleanupTempVFX()
    {
        for (int i = activeTempVFX.Count - 1; i >= 0; i--)
        {
            if (activeTempVFX[i] != null)
            {
                Destroy(activeTempVFX[i]);
            }
        }
        activeTempVFX.Clear();
    }

    private void OnDisable()
    {
        StopCurrentActionAndReset();
    }

    // =========================================================================
    // 1. HÀNH ĐỘNG CHÉM / TRỌNG KÍCH (STRIKE & HEAVY SLASH)
    // =========================================================================

    public void PlaySlashAttack(Transform targetTransform, Action onHitCallback = null)
    {
        PlaySlashAttack(targetTransform, false, onHitCallback);
    }

    public void PlaySlashAttack(Transform targetTransform, bool isHeavySlash, Action onHitCallback = null)
    {
        StopCurrentActionAndReset();
        currentActionCoroutine = StartCoroutine(SlashAttackRoutine(targetTransform, isHeavySlash, onHitCallback));
    }

    private IEnumerator SlashAttackRoutine(Transform target, bool isHeavy, Action onHit)
    {
        isExecutingAction = true;
        Transform mover = targetVisualTransform != null ? targetVisualTransform : transform;
        Vector3 startLocalPos = originalLocalPos;

        float dirSign = GetDirectionSignToTarget(mover, target);
        float stepMultiplier = isUIElement ? 85f : 1.2f;
        float distanceBoost = isHeavy ? 1.45f : 1.0f;

        Vector3 windupLocalPos = startLocalPos - new Vector3(dirSign * 0.45f * stepMultiplier * distanceBoost, -8f, 0f);
        Vector3 lungeLocalPos = startLocalPos + new Vector3(dirSign * 1.45f * stepMultiplier * distanceBoost, isHeavy ? 18f : 8f, 0f);

        // Pha 1: Lấy đà (Wind-up) — nghiêng người ra sau tụ lực
        float wTime = isHeavy ? windupTime * 1.35f : windupTime;
        float elapsed = 0f;
        if (isHeavy)
        {
            SetVisualColor(new Color(1f, 0.82f, 0.55f, 1f));
        }

        while (elapsed < wTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / wTime;
            mover.localPosition = Vector3.Lerp(startLocalPos, windupLocalPos, t);
            mover.localScale = new Vector3(
                originalLocalScale.x * Mathf.Lerp(1f, 1.12f, t),
                originalLocalScale.y * Mathf.Lerp(1f, 0.88f, t),
                originalLocalScale.z);
            mover.localRotation = Quaternion.Euler(0f, 0f, dirSign * Mathf.Lerp(0f, 10f, t));
            yield return null;
        }

        // Pha 2: Lao vút tới chém (Strike Lunge)
        float sTime = isHeavy ? strikeTime * 1.15f : strikeTime;
        elapsed = 0f;
        while (elapsed < sTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / sTime;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            mover.localPosition = Vector3.Lerp(windupLocalPos, lungeLocalPos, smoothT);
            mover.localScale = new Vector3(
                originalLocalScale.x * (isHeavy ? 1.18f : 1.08f),
                originalLocalScale.y * (isHeavy ? 1.12f : 1.06f),
                originalLocalScale.z);
            mover.localRotation = Quaternion.Euler(0f, 0f, -dirSign * Mathf.Lerp(-8f, isHeavy ? 18f : 13f, smoothT));
            yield return null;
        }
        mover.localPosition = lungeLocalPos;

        // Kích hoạt Vệt Kiếm Chém (Slash VFX) tại mục tiêu
        if (slashVFXObject != null) slashVFXObject.SetActive(true);
        if (target != null)
        {
            StartCoroutine(SpawnSlashArcVFX(
                target,
                isHeavy ? new Color(1f, 0.35f, 0.15f, 1f) : new Color(1f, 0.92f, 0.45f, 1f),
                isHeavy,
                dirSign));
        }

        onHit?.Invoke();

        yield return new WaitForSeconds(isHeavy ? 0.14f : 0.09f);
        if (slashVFXObject != null) slashVFXObject.SetActive(false);

        // Pha 3: Thu vũ khí nhảy lùi về vị trí ban đầu (Recover)
        SetVisualColor(baseVisualColor);
        elapsed = 0f;
        while (elapsed < recoverTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / recoverTime;
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            mover.localPosition = Vector3.Lerp(lungeLocalPos, startLocalPos, easeT);
            mover.localScale = Vector3.Lerp(mover.localScale, originalLocalScale, t);
            mover.localRotation = Quaternion.Lerp(mover.localRotation, originalLocalRot, t);
            yield return null;
        }

        mover.localPosition = originalLocalPos;
        mover.localScale = originalLocalScale;
        mover.localRotation = originalLocalRot;
        isExecutingAction = false;
    }

    // =========================================================================
    // 2. HÀNH ĐỘNG HÚT MÁU (VAMPIRIC STRIKE — CHÉM ĐỎ & HẤP THỤ SINH LỰC)
    // =========================================================================

    public void PlayVampiricAttack(Transform targetTransform, Action onHitCallback = null)
    {
        StopCurrentActionAndReset();
        currentActionCoroutine = StartCoroutine(VampiricAttackRoutine(targetTransform, onHitCallback));
    }

    private IEnumerator VampiricAttackRoutine(Transform target, Action onHit)
    {
        isExecutingAction = true;
        Transform mover = targetVisualTransform != null ? targetVisualTransform : transform;
        Vector3 startLocalPos = originalLocalPos;
        float dirSign = GetDirectionSignToTarget(mover, target);
        float stepMultiplier = isUIElement ? 85f : 1.2f;

        Vector3 windupPos = startLocalPos - new Vector3(dirSign * 0.38f * stepMultiplier, 0f, 0f);
        Vector3 lungePos = startLocalPos + new Vector3(dirSign * 1.35f * stepMultiplier, 12f, 0f);

        SetVisualColor(new Color(1f, 0.45f, 0.6f, 1f));

        float elapsed = 0f;
        while (elapsed < windupTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / windupTime;
            mover.localPosition = Vector3.Lerp(startLocalPos, windupPos, t);
            mover.localScale = new Vector3(originalLocalScale.x * 1.08f, originalLocalScale.y * 0.9f, originalLocalScale.z);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < strikeTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / strikeTime);
            mover.localPosition = Vector3.Lerp(windupPos, lungePos, t);
            mover.localRotation = Quaternion.Euler(0f, 0f, -dirSign * 14f * t);
            yield return null;
        }

        if (target != null)
        {
            StartCoroutine(SpawnSlashArcVFX(target, new Color(0.95f, 0.12f, 0.32f, 1f), true, dirSign));
        }

        onHit?.Invoke();
        yield return new WaitForSeconds(0.1f);

        // Thu người về kèm hiệu ứng gồng hấp thụ máu
        elapsed = 0f;
        while (elapsed < recoverTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / recoverTime;
            mover.localPosition = Vector3.Lerp(lungePos, startLocalPos, 1f - Mathf.Pow(1f - t, 3f));
            mover.localRotation = Quaternion.Lerp(mover.localRotation, originalLocalRot, t);
            yield return null;
        }

        // Gồng nhẹ hồi sinh lực (hào quang xanh lục-đỏ)
        StartCoroutine(SpawnHealOrBuffAuraVFX(mover, new Color(0.3f, 0.95f, 0.55f, 0.85f)));
        SetVisualColor(new Color(0.65f, 1f, 0.75f, 1f));
        mover.localScale = new Vector3(originalLocalScale.x * 0.95f, originalLocalScale.y * 1.12f, originalLocalScale.z);
        mover.localPosition = startLocalPos + new Vector3(0f, isUIElement ? 14f : 0.18f, 0f);
        yield return new WaitForSeconds(0.18f);

        SetVisualColor(baseVisualColor);
        mover.localPosition = originalLocalPos;
        mover.localScale = originalLocalScale;
        mover.localRotation = originalLocalRot;
        isExecutingAction = false;
    }

    // =========================================================================
    // 3. HÀNH ĐỘNG GỒNG HỒI MÁU / THANH TẨY (REGENERATE & PURIFY POWER-UP)
    // =========================================================================

    public void PlayHealPowerUp(bool isPurify = false, Action onPeakCallback = null)
    {
        StopCurrentActionAndReset();
        currentActionCoroutine = StartCoroutine(HealPowerUpRoutine(isPurify, onPeakCallback));
    }

    private IEnumerator HealPowerUpRoutine(bool isPurify, Action onPeak)
    {
        isExecutingAction = true;
        Transform mover = targetVisualTransform != null ? targetVisualTransform : transform;
        Vector3 startLocalPos = originalLocalPos;
        float stepMultiplier = isUIElement ? 1f : 0.015f;

        Color auraColor = isPurify
            ? new Color(0.45f, 0.95f, 1f, 0.9f)   // Xanh Lam Ánh Bạch Kim (Thanh tẩy)
            : new Color(0.25f, 0.95f, 0.52f, 0.9f); // Xanh Ngọc Lục Bảo (Hồi máu)

        Color tintColor = isPurify
            ? new Color(0.72f, 0.98f, 1f, 1f)
            : new Color(0.68f, 1f, 0.75f, 1f);

        // Pha 1: Hạ trọng tâm gồng tụ nội lực (Charge Crouch + Vibrate)
        float chargeTime = 0.18f;
        float elapsed = 0f;
        while (elapsed < chargeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / chargeTime;
            float vib = Mathf.Sin(elapsed * 70f) * 3.5f * stepMultiplier;
            mover.localPosition = startLocalPos + new Vector3(vib, -10f * t * stepMultiplier, 0f);
            mover.localScale = new Vector3(
                originalLocalScale.x * Mathf.Lerp(1f, 1.14f, t),
                originalLocalScale.y * Mathf.Lerp(1f, 0.86f, t),
                originalLocalScale.z);
            SetVisualColor(Color.Lerp(baseVisualColor, tintColor, t));
            yield return null;
        }

        // Pha 2: Bùng nổ năng lượng vươn người lên cao (Release Ascend + Aura Rings)
        StartCoroutine(SpawnHealOrBuffAuraVFX(mover, auraColor));
        onPeak?.Invoke();

        float riseTime = 0.24f;
        elapsed = 0f;
        Vector3 peakPos = startLocalPos + new Vector3(0f, 24f * stepMultiplier, 0f);
        while (elapsed < riseTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / riseTime);
            mover.localPosition = Vector3.Lerp(startLocalPos, peakPos, Mathf.Sin(t * Mathf.PI));
            mover.localScale = new Vector3(
                originalLocalScale.x * Mathf.Lerp(0.92f, 1.06f, t),
                originalLocalScale.y * Mathf.Lerp(1.16f, 1.02f, t),
                originalLocalScale.z);
            yield return null;
        }

        // Pha 3: Hạ xuống vững chãi
        SetVisualColor(baseVisualColor);
        mover.localPosition = originalLocalPos;
        mover.localScale = originalLocalScale;
        mover.localRotation = originalLocalRot;
        isExecutingAction = false;
    }

    // =========================================================================
    // 4. HÀNH ĐỘNG DỰNG KHIÊN / THẾ THỦ (BLOCK & IRON WALL BRACE)
    // =========================================================================

    public void PlayGuardBrace(bool isIronWall = false, Action onBraceCallback = null)
    {
        StopCurrentActionAndReset();
        currentActionCoroutine = StartCoroutine(GuardBraceRoutine(isIronWall, onBraceCallback));
    }

    private IEnumerator GuardBraceRoutine(bool isIronWall, Action onBrace)
    {
        isExecutingAction = true;
        Transform mover = targetVisualTransform != null ? targetVisualTransform : transform;
        Vector3 startLocalPos = originalLocalPos;
        float stepMultiplier = isUIElement ? 1f : 0.015f;

        // Xác định hướng nhìn vào giữa sân đấu
        float facingSign = (mover.position.x <= 0f) ? 1f : -1f;
        if (isUIElement && mover is RectTransform rt && rt.parent != null)
        {
            RectTransform parentRt = rt.parent as RectTransform;
            if (parentRt != null)
            {
                facingSign = (parentRt.anchoredPosition.x <= 0f) ? 1f : -1f;
            }
        }

        Color shieldColor = isIronWall
            ? new Color(1f, 0.84f, 0.32f, 0.92f)   // Vàng Kim Bất Khuất (Iron Wall)
            : new Color(0.32f, 0.82f, 1f, 0.92f);  // Xanh Băng Hộ Vệ (Block)

        Color bodyTint = isIronWall
            ? new Color(1f, 0.92f, 0.65f, 1f)
            : new Color(0.68f, 0.9f, 1f, 1f);

        // Pha 1: Bước dậm chân trụ vững & giương khiên chắn phía trước
        StartCoroutine(SpawnShieldBarrierVFX(mover, shieldColor, facingSign, isIronWall));
        onBrace?.Invoke();

        float braceTime = 0.16f;
        float elapsed = 0f;
        Vector3 bracePos = startLocalPos + new Vector3(facingSign * 18f * stepMultiplier, -6f * stepMultiplier, 0f);
        while (elapsed < braceTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / braceTime;
            mover.localPosition = Vector3.Lerp(startLocalPos, bracePos, t);
            mover.localScale = new Vector3(
                originalLocalScale.x * Mathf.Lerp(1f, isIronWall ? 1.18f : 1.12f, t),
                originalLocalScale.y * Mathf.Lerp(1f, 0.91f, t),
                originalLocalScale.z);
            SetVisualColor(Color.Lerp(baseVisualColor, bodyTint, t));
            yield return null;
        }

        // Giữ thế thủ vững vàng một nhịp
        yield return new WaitForSeconds(0.18f);

        // Pha 2: Trở về tư thế chiến đấu
        float returnTime = 0.16f;
        elapsed = 0f;
        while (elapsed < returnTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnTime;
            mover.localPosition = Vector3.Lerp(bracePos, startLocalPos, t);
            mover.localScale = Vector3.Lerp(mover.localScale, originalLocalScale, t);
            SetVisualColor(Color.Lerp(bodyTint, baseVisualColor, t));
            yield return null;
        }

        SetVisualColor(baseVisualColor);
        mover.localPosition = originalLocalPos;
        mover.localScale = originalLocalScale;
        mover.localRotation = originalLocalRot;
        isExecutingAction = false;
    }

    // =========================================================================
    // 5. HÀNH ĐỘNG ĐÂM ĐỘC / PHUN ĐỘC (POISON STING)
    // =========================================================================

    public void PlayPoisonStrike(Transform targetTransform, Action onHitCallback = null)
    {
        StopCurrentActionAndReset();
        currentActionCoroutine = StartCoroutine(PoisonStrikeRoutine(targetTransform, onHitCallback));
    }

    private IEnumerator PoisonStrikeRoutine(Transform target, Action onHit)
    {
        isExecutingAction = true;
        Transform mover = targetVisualTransform != null ? targetVisualTransform : transform;
        Vector3 startLocalPos = originalLocalPos;
        float dirSign = GetDirectionSignToTarget(mover, target);
        float stepMultiplier = isUIElement ? 80f : 1.15f;

        Vector3 thrustPos = startLocalPos + new Vector3(dirSign * 1.15f * stepMultiplier, 4f, 0f);
        SetVisualColor(new Color(0.75f, 0.45f, 1f, 1f));

        // Lao nhanh đâm mũi độc (Viper Thrust)
        float elapsed = 0f;
        float thrustTime = 0.12f;
        while (elapsed < thrustTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / thrustTime;
            mover.localPosition = Vector3.Lerp(startLocalPos, thrustPos, t);
            mover.localScale = new Vector3(originalLocalScale.x * 1.14f, originalLocalScale.y * 0.92f, originalLocalScale.z);
            mover.localRotation = Quaternion.Euler(0f, 0f, -dirSign * 15f * t);
            yield return null;
        }

        if (target != null)
        {
            StartCoroutine(SpawnPoisonBurstVFX(target));
        }

        onHit?.Invoke();
        yield return new WaitForSeconds(0.1f);

        // Rút nhanh về vị trí cũ
        elapsed = 0f;
        while (elapsed < recoverTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / recoverTime;
            mover.localPosition = Vector3.Lerp(thrustPos, startLocalPos, t);
            mover.localScale = Vector3.Lerp(mover.localScale, originalLocalScale, t);
            mover.localRotation = Quaternion.Lerp(mover.localRotation, originalLocalRot, t);
            yield return null;
        }

        SetVisualColor(baseVisualColor);
        mover.localPosition = originalLocalPos;
        mover.localScale = originalLocalScale;
        mover.localRotation = originalLocalRot;
        isExecutingAction = false;
    }

    // =========================================================================
    // 6. HÀNH ĐỘNG GIÁNG SẤM / GÂY CHOÁNG (SHOCK / STUN)
    // =========================================================================

    public void PlayShockStunStrike(Transform targetTransform, Action onHitCallback = null)
    {
        StopCurrentActionAndReset();
        currentActionCoroutine = StartCoroutine(ShockStunRoutine(targetTransform, onHitCallback));
    }

    private IEnumerator ShockStunRoutine(Transform target, Action onHit)
    {
        isExecutingAction = true;
        Transform mover = targetVisualTransform != null ? targetVisualTransform : transform;
        Vector3 startLocalPos = originalLocalPos;
        float dirSign = GetDirectionSignToTarget(mover, target);
        float stepMultiplier = isUIElement ? 1f : 0.015f;

        Color thunderTint = new Color(1f, 0.95f, 0.45f, 1f);
        SetVisualColor(thunderTint);

        // Pha 1: Nhấc người tụ điện vàng rực trên cao
        float chargeTime = 0.15f;
        float elapsed = 0f;
        Vector3 highPos = startLocalPos + new Vector3(-dirSign * 14f * stepMultiplier, 26f * stepMultiplier, 0f);
        while (elapsed < chargeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / chargeTime;
            mover.localPosition = Vector3.Lerp(startLocalPos, highPos, t);
            mover.localScale = new Vector3(originalLocalScale.x * 0.94f, originalLocalScale.y * 1.15f, originalLocalScale.z);
            yield return null;
        }

        // Pha 2: Giáng đòn sấm sét xuống mục tiêu
        Vector3 slamPos = startLocalPos + new Vector3(dirSign * 42f * stepMultiplier, -6f * stepMultiplier, 0f);
        elapsed = 0f;
        float slamTime = 0.1f;
        while (elapsed < slamTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slamTime;
            mover.localPosition = Vector3.Lerp(highPos, slamPos, t);
            mover.localScale = new Vector3(originalLocalScale.x * 1.12f, originalLocalScale.y * 0.9f, originalLocalScale.z);
            yield return null;
        }

        if (target != null)
        {
            StartCoroutine(SpawnThunderShockVFX(target));
        }

        onHit?.Invoke();
        yield return new WaitForSeconds(0.14f);

        // Pha 3: Thu về
        elapsed = 0f;
        while (elapsed < recoverTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / recoverTime;
            mover.localPosition = Vector3.Lerp(slamPos, startLocalPos, t);
            mover.localScale = Vector3.Lerp(mover.localScale, originalLocalScale, t);
            yield return null;
        }

        SetVisualColor(baseVisualColor);
        mover.localPosition = originalLocalPos;
        mover.localScale = originalLocalScale;
        mover.localRotation = originalLocalRot;
        isExecutingAction = false;
    }

    // =========================================================================
    // 7. HÀNH ĐỘNG GỒNG CUỒNG NỘ / TÍCH TỤ SỨC MẠNH (ENRAGE / BUFF CHARGE)
    // =========================================================================

    public void PlayEnrageCharge(Action onPeakCallback = null)
    {
        StopCurrentActionAndReset();
        currentActionCoroutine = StartCoroutine(EnrageChargeRoutine(onPeakCallback));
    }

    private IEnumerator EnrageChargeRoutine(Action onPeak)
    {
        isExecutingAction = true;
        Transform mover = targetVisualTransform != null ? targetVisualTransform : transform;
        Vector3 startLocalPos = originalLocalPos;
        float stepMultiplier = isUIElement ? 1f : 0.015f;

        Color furyColor = new Color(1f, 0.38f, 0.15f, 0.92f);
        StartCoroutine(SpawnHealOrBuffAuraVFX(mover, furyColor));

        float duration = 0.42f;
        float elapsed = 0f;
        bool triggeredPeak = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (!triggeredPeak && t >= 0.45f)
            {
                triggeredPeak = true;
                onPeak?.Invoke();
            }

            float shakeX = Mathf.Sin(elapsed * 85f) * 6f * stepMultiplier;
            float liftY = Mathf.Sin(t * Mathf.PI) * 16f * stepMultiplier;
            mover.localPosition = startLocalPos + new Vector3(shakeX, liftY, 0f);

            float swell = 1f + Mathf.Sin(t * Mathf.PI) * 0.22f;
            mover.localScale = new Vector3(
                originalLocalScale.x * swell,
                originalLocalScale.y * swell,
                originalLocalScale.z);

            SetVisualColor(Color.Lerp(baseVisualColor, new Color(1f, 0.45f, 0.25f, 1f), Mathf.Sin(t * Mathf.PI)));
            yield return null;
        }

        SetVisualColor(baseVisualColor);
        mover.localPosition = originalLocalPos;
        mover.localScale = originalLocalScale;
        mover.localRotation = originalLocalRot;
        isExecutingAction = false;
    }

    // =========================================================================
    // 8. HÀNH ĐỘNG TRƯỢT / HỤT ĐÒN (BLANK / MISS FUMBLE)
    // =========================================================================

    public void PlayMissFumble()
    {
        StopCurrentActionAndReset();
        currentActionCoroutine = StartCoroutine(MissFumbleRoutine());
    }

    private IEnumerator MissFumbleRoutine()
    {
        isExecutingAction = true;
        Transform mover = targetVisualTransform != null ? targetVisualTransform : transform;
        Vector3 startLocalPos = originalLocalPos;
        float stepMultiplier = isUIElement ? 1f : 0.015f;

        SetVisualColor(new Color(0.65f, 0.65f, 0.7f, 0.8f));

        float duration = 0.3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float sway = Mathf.Sin(t * Mathf.PI * 3f) * 14f * stepMultiplier;
            mover.localPosition = startLocalPos + new Vector3(sway, -6f * stepMultiplier, 0f);
            mover.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 3f) * 8f);
            yield return null;
        }

        SetVisualColor(baseVisualColor);
        mover.localPosition = originalLocalPos;
        mover.localScale = originalLocalScale;
        mover.localRotation = originalLocalRot;
        isExecutingAction = false;
    }

    // =========================================================================
    // 9. HÀNH ĐỘNG NHẬN SÁT THƯƠNG (TAKE DAMAGE KNOCKBACK & FLASH)
    // =========================================================================

    public void PlayTakeDamage()
    {
        StopCurrentActionAndReset();
        currentActionCoroutine = StartCoroutine(TakeDamageRoutine());
    }

    private IEnumerator TakeDamageRoutine()
    {
        isExecutingAction = true;
        Transform mover = targetVisualTransform != null ? targetVisualTransform : transform;

        float recoilDir = (mover.position.x <= 0f) ? -1f : 1f;
        if (isUIElement && mover is RectTransform rt && rt.parent != null)
        {
            RectTransform parentRt = rt.parent as RectTransform;
            if (parentRt != null)
            {
                recoilDir = (parentRt.anchoredPosition.x <= 0f) ? -1f : 1f;
            }
        }

        float stepMultiplier = isUIElement ? 65f : 1f;
        Vector3 recoilLocalPos = originalLocalPos + new Vector3(recoilDir * knockbackDistance * stepMultiplier, 6f, 0f);
        SetVisualColor(hitFlashColor);

        float elapsed = 0f;
        float dur = hitFlashDuration;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            float shakeOffset = Mathf.Sin(elapsed * 65f) * (0.12f * stepMultiplier) * (1f - t);
            mover.localPosition = Vector3.Lerp(recoilLocalPos, originalLocalPos, t) + new Vector3(shakeOffset, 0f, 0f);
            mover.localScale = new Vector3(
                originalLocalScale.x * Mathf.Lerp(0.88f, 1f, t),
                originalLocalScale.y * Mathf.Lerp(1.12f, 1f, t),
                originalLocalScale.z);
            mover.localRotation = Quaternion.Euler(0f, 0f, recoilDir * Mathf.Lerp(-12f, 0f, t));
            yield return null;
        }

        SetVisualColor(baseVisualColor);
        mover.localPosition = originalLocalPos;
        mover.localScale = originalLocalScale;
        mover.localRotation = originalLocalRot;
        isExecutingAction = false;
    }

    // =========================================================================
    // HỆ THỐNG PROCEDURAL VFX (TỰ ĐỘNG VẼ HIỆU ỨNG KIẾM CHÉM, KHIÊN, HÀO QUANG)
    // =========================================================================

    private float GetDirectionSignToTarget(Transform mover, Transform target)
    {
        if (target == null) return 1f;
        return (target.position.x >= mover.position.x) ? 1f : -1f;
    }

    private IEnumerator SpawnSlashArcVFX(Transform target, Color slashColor, bool isDoubleCross, float dirSign)
    {
        if (target == null) yield break;

        GameObject slashGo = CreateVFXOverlayObject(target, "SlashArcVFX", new Vector2(210f, 210f), cachedSlashSprite, slashColor);
        if (slashGo == null) yield break;

        RectTransform rt = slashGo.GetComponent<RectTransform>();
        Image img = slashGo.GetComponent<Image>();
        float baseAngle = dirSign >= 0f ? -35f : 35f;
        rt.localRotation = Quaternion.Euler(0f, 0f, baseAngle);

        GameObject crossGo = null;
        RectTransform crossRt = null;
        Image crossImg = null;
        if (isDoubleCross)
        {
            crossGo = CreateVFXOverlayObject(target, "HeavyCrossSlashVFX", new Vector2(240f, 240f), cachedSlashSprite, new Color(1f, 0.95f, 0.7f, 1f));
            if (crossGo != null)
            {
                crossRt = crossGo.GetComponent<RectTransform>();
                crossImg = crossGo.GetComponent<Image>();
                crossRt.localRotation = Quaternion.Euler(0f, 0f, -baseAngle);
            }
        }

        float dur = isDoubleCross ? 0.26f : 0.18f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            if (rt != null)
            {
                float s = Mathf.Lerp(0.45f, isDoubleCross ? 1.45f : 1.18f, t);
                rt.localScale = new Vector3(s, s, 1f);
            }
            if (img != null)
            {
                Color c = slashColor;
                c.a = Mathf.Lerp(1f, 0f, t * t);
                img.color = c;
            }
            if (crossRt != null && crossImg != null)
            {
                float s2 = Mathf.Lerp(0.35f, 1.55f, t);
                crossRt.localScale = new Vector3(s2, s2, 1f);
                Color c2 = crossImg.color;
                c2.a = Mathf.Lerp(1f, 0f, t);
                crossImg.color = c2;
            }
            yield return null;
        }

        RemoveAndDestroyVFX(slashGo);
        if (crossGo != null) RemoveAndDestroyVFX(crossGo);
    }

    private IEnumerator SpawnShieldBarrierVFX(Transform holder, Color shieldColor, float facingSign, bool isIronWall)
    {
        if (holder == null) yield break;

        GameObject shieldGo = CreateVFXOverlayObject(holder, "ShieldBarrierVFX", new Vector2(180f, 220f), cachedShieldSprite, shieldColor);
        if (shieldGo == null) yield break;

        RectTransform rt = shieldGo.GetComponent<RectTransform>();
        Image img = shieldGo.GetComponent<Image>();
        rt.anchoredPosition = new Vector2(facingSign * 55f, 5f);

        // Vòng xung kích bảo hộ bên ngoài
        GameObject ringGo = CreateVFXOverlayObject(holder, "ShieldRingVFX", new Vector2(210f, 210f), cachedRingSprite, shieldColor);
        RectTransform ringRt = ringGo != null ? ringGo.GetComponent<RectTransform>() : null;
        Image ringImg = ringGo != null ? ringGo.GetComponent<Image>() : null;

        float dur = isIronWall ? 0.42f : 0.34f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            if (rt != null)
            {
                float scalePop = t < 0.25f ? Mathf.Lerp(0.5f, 1.15f, t / 0.25f) : Mathf.Lerp(1.15f, 1.0f, (t - 0.25f) / 0.75f);
                rt.localScale = new Vector3(scalePop, scalePop, 1f);
            }
            if (img != null)
            {
                Color c = shieldColor;
                c.a = Mathf.Sin(t * Mathf.PI) * 0.9f;
                img.color = c;
            }
            if (ringRt != null && ringImg != null)
            {
                float rs = Mathf.Lerp(0.6f, 1.55f, t);
                ringRt.localScale = new Vector3(rs, rs, 1f);
                Color rc = shieldColor;
                rc.a = Mathf.Lerp(0.85f, 0f, t);
                ringImg.color = rc;
            }
            yield return null;
        }

        RemoveAndDestroyVFX(shieldGo);
        if (ringGo != null) RemoveAndDestroyVFX(ringGo);
    }

    private IEnumerator SpawnHealOrBuffAuraVFX(Transform holder, Color auraColor)
    {
        if (holder == null) yield break;

        GameObject ring1 = CreateVFXOverlayObject(holder, "AuraRing1", new Vector2(220f, 90f), cachedRingSprite, auraColor);
        GameObject ring2 = CreateVFXOverlayObject(holder, "AuraRing2", new Vector2(180f, 180f), cachedCircleSprite, new Color(auraColor.r, auraColor.g, auraColor.b, 0.35f));

        RectTransform rt1 = ring1 != null ? ring1.GetComponent<RectTransform>() : null;
        Image img1 = ring1 != null ? ring1.GetComponent<Image>() : null;
        RectTransform rt2 = ring2 != null ? ring2.GetComponent<RectTransform>() : null;
        Image img2 = ring2 != null ? ring2.GetComponent<Image>() : null;

        float dur = 0.42f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;

            if (rt1 != null && img1 != null)
            {
                rt1.anchoredPosition = new Vector2(0f, Mathf.Lerp(-95f, 95f, t));
                float s1 = Mathf.Lerp(0.75f, 1.35f, Mathf.Sin(t * Mathf.PI));
                rt1.localScale = new Vector3(s1, s1, 1f);
                Color c1 = auraColor;
                c1.a = Mathf.Lerp(0.95f, 0f, t);
                img1.color = c1;
            }

            if (rt2 != null && img2 != null)
            {
                float s2 = Mathf.Lerp(0.6f, 1.5f, t);
                rt2.localScale = new Vector3(s2, s2, 1f);
                Color c2 = auraColor;
                c2.a = Mathf.Lerp(0.45f, 0f, t);
                img2.color = c2;
            }

            yield return null;
        }

        if (ring1 != null) RemoveAndDestroyVFX(ring1);
        if (ring2 != null) RemoveAndDestroyVFX(ring2);
    }

    private IEnumerator SpawnPoisonBurstVFX(Transform target)
    {
        if (target == null) yield break;

        Color venomColor = new Color(0.68f, 0.25f, 0.95f, 0.9f);
        Color acidColor = new Color(0.45f, 0.95f, 0.35f, 0.85f);

        GameObject splash = CreateVFXOverlayObject(target, "VenomBurstVFX", new Vector2(190f, 190f), cachedRingSprite, venomColor);
        GameObject core = CreateVFXOverlayObject(target, "VenomCoreVFX", new Vector2(140f, 140f), cachedSlashSprite, acidColor);

        RectTransform rt1 = splash != null ? splash.GetComponent<RectTransform>() : null;
        Image img1 = splash != null ? splash.GetComponent<Image>() : null;
        RectTransform rt2 = core != null ? core.GetComponent<RectTransform>() : null;
        Image img2 = core != null ? core.GetComponent<Image>() : null;

        float dur = 0.32f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            if (rt1 != null && img1 != null)
            {
                float s = Mathf.Lerp(0.4f, 1.45f, t);
                rt1.localScale = new Vector3(s, s, 1f);
                Color c = venomColor;
                c.a = Mathf.Lerp(0.95f, 0f, t);
                img1.color = c;
            }
            if (rt2 != null && img2 != null)
            {
                rt2.localRotation = Quaternion.Euler(0f, 0f, t * 120f);
                float s2 = Mathf.Lerp(0.5f, 1.25f, t);
                rt2.localScale = new Vector3(s2, s2, 1f);
                Color c2 = acidColor;
                c2.a = Mathf.Lerp(0.9f, 0f, t);
                img2.color = c2;
            }
            yield return null;
        }

        if (splash != null) RemoveAndDestroyVFX(splash);
        if (core != null) RemoveAndDestroyVFX(core);
    }

    private IEnumerator SpawnThunderShockVFX(Transform target)
    {
        if (target == null) yield break;

        Color boltColor = new Color(1f, 0.94f, 0.28f, 1f);
        GameObject bolt = CreateVFXOverlayObject(target, "ThunderBoltVFX", new Vector2(90f, 280f), cachedSlashSprite, boltColor);
        GameObject ring = CreateVFXOverlayObject(target, "ShockRingVFX", new Vector2(210f, 210f), cachedRingSprite, new Color(0.55f, 0.9f, 1f, 0.95f));

        RectTransform boltRt = bolt != null ? bolt.GetComponent<RectTransform>() : null;
        Image boltImg = bolt != null ? bolt.GetComponent<Image>() : null;
        if (boltRt != null)
        {
            boltRt.localRotation = Quaternion.Euler(0f, 0f, 75f);
            boltRt.anchoredPosition = new Vector2(0f, 35f);
        }

        RectTransform ringRt = ring != null ? ring.GetComponent<RectTransform>() : null;
        Image ringImg = ring != null ? ring.GetComponent<Image>() : null;

        float dur = 0.28f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            if (boltRt != null && boltImg != null)
            {
                float flicker = Mathf.Sin(elapsed * 90f) > 0f ? 1f : 0.45f;
                Color c = boltColor;
                c.a = (1f - t) * flicker;
                boltImg.color = c;
            }
            if (ringRt != null && ringImg != null)
            {
                float s = Mathf.Lerp(0.4f, 1.6f, t);
                ringRt.localScale = new Vector3(s, s, 1f);
                Color rc = ringImg.color;
                rc.a = Mathf.Lerp(0.95f, 0f, t);
                ringImg.color = rc;
            }
            yield return null;
        }

        if (bolt != null) RemoveAndDestroyVFX(bolt);
        if (ring != null) RemoveAndDestroyVFX(ring);
    }

    private GameObject CreateVFXOverlayObject(Transform target, string name, Vector2 size, Sprite sprite, Color color)
    {
        Transform parent = target;
        RectTransform targetRt = target as RectTransform;
        if (targetRt == null && target != null)
        {
            targetRt = target.GetComponent<RectTransform>();
        }
        if (targetRt == null) return null;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.SetAsLastSibling();

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        Image img = go.AddComponent<Image>();
        img.sprite = sprite != null ? sprite : cachedWhiteSprite;
        img.color = color;
        img.raycastTarget = false;

        activeTempVFX.Add(go);
        return go;
    }

    private void RemoveAndDestroyVFX(GameObject vfxObj)
    {
        if (vfxObj == null) return;
        activeTempVFX.Remove(vfxObj);
        Destroy(vfxObj);
    }

    private static void EnsureProceduralSprites()
    {
        if (cachedWhiteSprite != null) return;

        // 1. White pixel sprite
        Texture2D whiteTex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                whiteTex.SetPixel(x, y, Color.white);
        whiteTex.Apply();
        cachedWhiteSprite = Sprite.Create(whiteTex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));

        // 2. Soft Glow Circle & Ring Sprites (64x64)
        int res = 64;
        Texture2D circleTex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        Texture2D ringTex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        Texture2D slashTex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        Texture2D shieldTex = new Texture2D(res, res, TextureFormat.RGBA32, false);

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float nx = (x / (float)(res - 1)) * 2f - 1f;
                float ny = (y / (float)(res - 1)) * 2f - 1f;
                float dist = Mathf.Sqrt(nx * nx + ny * ny);

                // Circle glow
                float cAlpha = Mathf.Clamp01(1f - dist);
                cAlpha = cAlpha * cAlpha;
                circleTex.SetPixel(x, y, new Color(1f, 1f, 1f, cAlpha));

                // Ring wave
                float ringAlpha = Mathf.Clamp01(1f - Mathf.Abs(dist - 0.72f) / 0.18f);
                ringTex.SetPixel(x, y, new Color(1f, 1f, 1f, ringAlpha * ringAlpha));

                // Crescent Slash Arc
                float arcDist = Mathf.Abs(ny - (-0.28f * nx * nx + 0.1f));
                float taper = Mathf.Clamp01(1f - Mathf.Abs(nx));
                float slashAlpha = Mathf.Clamp01(1f - arcDist / (0.16f * taper + 0.02f)) * taper;
                slashTex.SetPixel(x, y, new Color(1f, 1f, 1f, slashAlpha));

                // Heater Shield Crest shape
                float shieldWidth = ny > 0f ? 0.68f : Mathf.Lerp(0.05f, 0.68f, (ny + 0.9f) / 0.9f);
                bool insideShield = Mathf.Abs(nx) <= shieldWidth && ny >= -0.88f && ny <= 0.78f;
                float edgeDist = Mathf.Min(shieldWidth - Mathf.Abs(nx), Mathf.Min(0.78f - ny, ny + 0.88f));
                float shAlpha = insideShield ? (edgeDist < 0.12f ? 0.95f : 0.42f) : 0f;
                shieldTex.SetPixel(x, y, new Color(1f, 1f, 1f, shAlpha));
            }
        }

        circleTex.Apply();
        ringTex.Apply();
        slashTex.Apply();
        shieldTex.Apply();

        cachedCircleSprite = Sprite.Create(circleTex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
        cachedRingSprite = Sprite.Create(ringTex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
        cachedSlashSprite = Sprite.Create(slashTex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
        cachedShieldSprite = Sprite.Create(shieldTex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
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
