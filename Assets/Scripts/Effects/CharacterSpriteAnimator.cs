using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// CharacterSpriteAnimator — Tạo hoạt ảnh "Nhún Nhún" (Squash & Stretch / Idle Bobbing) 
/// và các đòn đánh "Chém / Lướt" (Slash Attack) mượt mà cho Sprite 2D trong game Turn-Based.
/// Áp dụng tốt cho cả Hero và Quái vật (Orc, Minotaur, Goblin).
/// </summary>
public class CharacterSpriteAnimator : MonoBehaviour
{
    [Header("1. Cài Đặt Nhún Nhún (Idle Bobbing / Breathing)")]
    [Tooltip("Bật/tắt hiệu ứng nhún nhún khi đứng chờ lượt")]
    public bool enableIdleBobbing = true;
    [Tooltip("Tốc độ thở / nhún")]
    public float bobSpeed = 3.0f;
    [Tooltip("Biên độ nhấc người lên xuống (Y)")]
    public float bobHeight = 0.08f;
    [Tooltip("Độ co giãn cơ bắp (Squash & Stretch)")]
    public float squashStretchFactor = 0.04f;
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
    public Color hitFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
    public float hitFlashDuration = 0.12f;
    public float knockbackDistance = 0.25f;

    // Component References
    private SpriteRenderer spriteRenderer;
    private Vector3 originalLocalPos;
    private Vector3 originalLocalScale;
    private bool isExecutingAction = false;
    private Coroutine currentActionCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalLocalPos = spriteRenderer.transform.localPosition;
            originalLocalScale = spriteRenderer.transform.localScale;
        }
        else
        {
            originalLocalPos = transform.localPosition;
            originalLocalScale = transform.localScale;
        }

        // Tạo phase offset ngẫu nhiên nếu chưa gán
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
        // Khi không bận chém hoặc bị đánh, tự động chạy hoạt ảnh "nhún nhún"
        if (enableIdleBobbing && !isExecutingAction && spriteRenderer != null)
        {
            ApplyIdleBobbing();
        }
    }

    /// <summary>
    /// Diễn hoạt "nhún nhún": Kết hợp nâng hạ trục Y và co giãn tỉ lệ (Squash & Stretch)
    /// </summary>
    private void ApplyIdleBobbing()
    {
        float wave = Mathf.Sin((Time.time * bobSpeed) + timeOffset);

        // Nâng hạ vị trí Y
        Vector3 newPos = originalLocalPos;
        newPos.y += wave * bobHeight;
        spriteRenderer.transform.localPosition = newPos;

        // Co giãn trục Y (dãn khi nhấc lên, co lại khi nhún xuống)
        // và trục X biến thiên ngược lại để bảo toàn thể tích cơ thể
        Vector3 newScale = originalLocalScale;
        newScale.y += wave * (originalLocalScale.y * squashStretchFactor);
        newScale.x -= wave * (originalLocalScale.x * (squashStretchFactor * 0.5f));
        spriteRenderer.transform.localScale = newScale;
    }

    // ==========================================
    // ⚔️ ĐÒN CHÉM (SLASH ATTACK)
    // ==========================================

    /// <summary>
    /// Thực hiện đòn chém: Lấy đà -> Lao tới mục tiêu -> Bật vệt chém -> Thu đao lùi về
    /// </summary>
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
        Transform mover = transform;
        Vector3 startWorldPos = mover.position;
        Vector3 targetWorldPos = target != null ? target.position : (startWorldPos + transform.right * 2f);

        // Hướng chém (Hero quay phải thì hướng dương, Quái quay trái thì ngược lại)
        Vector3 attackDir = (targetWorldPos - startWorldPos).normalized;
        Vector3 lungeDestination = targetWorldPos - (attackDir * 0.8f); // Dừng trước mục tiêu 1 khoảng ngắn

        // BƯỚC 1: LẤY ĐÀ (Anticipation / Windup) — Lùi nhẹ người ra sau & nén người xuống
        float elapsed = 0f;
        Vector3 windupPos = startWorldPos - (attackDir * 0.35f);
        while (elapsed < windupTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / windupTime;
            mover.position = Vector3.Lerp(startWorldPos, windupPos, t);
            
            // Nén người (squash)
            if (spriteRenderer != null)
            {
                spriteRenderer.transform.localScale = new Vector3(
                    originalLocalScale.x * 1.1f,
                    originalLocalScale.y * 0.9f,
                    originalLocalScale.z
                );
            }
            yield return null;
        }

        // BƯỚC 2: PHÓNG TỚI CỰC NHANH (Strike / Lunge)
        elapsed = 0f;
        while (elapsed < strikeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / strikeTime;
            // Dùng hàm SmoothStep hoặc EaseOut để lao tới xé gió
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            mover.position = Vector3.Lerp(windupPos, lungeDestination, smoothT);

            // Giãn người dài ra khi lao tới (stretch)
            if (spriteRenderer != null)
            {
                spriteRenderer.transform.localScale = new Vector3(
                    originalLocalScale.x * 0.95f,
                    originalLocalScale.y * 1.15f,
                    originalLocalScale.z
                );
            }
            yield return null;
        }
        mover.position = lungeDestination;

        // BƯỚC 3: KÍCH HOẠT VỆT CHÉM (Slash VFX) & GÂY DAMAGE
        if (slashVFXObject != null)
        {
            slashVFXObject.SetActive(true);
        }

        // Gọi hàm gây dame cho mục tiêu (khiến mục tiêu giật lùi, chớp đỏ)
        if (onHit != null)
        {
            onHit.Invoke();
        }

        // Giữ thế chém một tích tắc ngắn để người chơi kịp thấy đòn đánh
        yield return new WaitForSeconds(0.08f);

        if (slashVFXObject != null)
        {
            slashVFXObject.SetActive(false);
        }

        // BƯỚC 4: THU VŨ KHÍ LÙI VỀ (Recovery)
        elapsed = 0f;
        while (elapsed < recoverTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / recoverTime;
            float easeT = 1f - Mathf.Pow(1f - t, 3f); // Ease-out cubic
            mover.position = Vector3.Lerp(lungeDestination, startWorldPos, easeT);

            // Phục hồi lại scale chuẩn
            if (spriteRenderer != null)
            {
                spriteRenderer.transform.localScale = Vector3.Lerp(
                    spriteRenderer.transform.localScale,
                    originalLocalScale,
                    t
                );
            }
            yield return null;
        }

        mover.position = startWorldPos;
        if (spriteRenderer != null)
        {
            spriteRenderer.transform.localPosition = originalLocalPos;
            spriteRenderer.transform.localScale = originalLocalScale;
        }
        isExecutingAction = false;
    }

    // ==========================================
    // 💥 CHỊU SÁT THƯƠNG (TAKE DAMAGE)
    // ==========================================

    /// <summary>
    /// Hiệu ứng trúng đòn: Giật lùi ra sau, rung lắc và chớp đỏ Sprite
    /// </summary>
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
        Transform mover = transform;
        Vector3 startPos = mover.position;
        Color normalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        // Giật lùi ra sau
        Vector3 recoilPos = startPos - (transform.right * knockbackDistance);
        if (spriteRenderer != null) spriteRenderer.color = hitFlashColor;

        float elapsed = 0f;
        float dur = hitFlashDuration;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            // Rung chấn nhẹ
            float shakeOffset = Mathf.Sin(elapsed * 50f) * 0.05f;
            mover.position = Vector3.Lerp(recoilPos, startPos, t) + new Vector3(0, shakeOffset, 0);
            yield return null;
        }

        mover.position = startPos;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
            spriteRenderer.transform.localPosition = originalLocalPos;
            spriteRenderer.transform.localScale = originalLocalScale;
        }
        isExecutingAction = false;
    }
}

