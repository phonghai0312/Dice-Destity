using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// DraggableDice — Hỗ trợ Kéo thả (Drag & Drop), Hover xem Tooltip, Highlight mục tiêu hợp lệ
/// và Click phải / Double-Click dùng nhanh xúc xắc (GDD 2.2).
/// </summary>
public class DraggableDice : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [HideInInspector] public int slotIndex = -1;
    [HideInInspector] public SimpleCombatManager combatManager;
    [HideInInspector] public bool wasDroppedOnTarget = false;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Canvas canvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (combatManager != null && !combatManager.CanInteractWithDiceSlot(slotIndex))
        {
            eventData.pointerDrag = null;
            return;
        }

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        wasDroppedOnTarget = false;
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        if (canvas != null)
        {
            transform.SetParent(canvas.transform, true);
            transform.SetAsLastSibling();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.85f;
            canvasGroup.blocksRaycasts = false;
        }

        if (combatManager != null)
        {
            combatManager.OnDiceDragStateChanged(slotIndex, true);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null) return;
        float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        rectTransform.anchoredPosition += eventData.delta / scale;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        if (combatManager != null)
        {
            combatManager.OnDiceDragStateChanged(slotIndex, false);
        }

        // Luôn trả xúc xắc về vị trí khay ban đầu để không làm vỡ bố cục UI và hỗ trợ Undo
        RestoreToSlotPosition();

        // Nếu người chơi kéo thả ra vùng màn hình tương ứng nhưng chưa trúng collider DropTarget,
        // kiểm tra vị trí thả chuột (nửa phải màn hình -> Quái, nửa trái -> Player)
        if (!wasDroppedOnTarget && combatManager != null)
        {
            Vector2 dropPos = eventData.position;
            float distMoved = Vector2.Distance(eventData.pressPosition, dropPos);
            if (distMoved > 65f)
            {
                DropTarget.TargetType inferredTarget = dropPos.x >= Screen.width * 0.45f
                    ? DropTarget.TargetType.Enemy
                    : DropTarget.TargetType.Player;
                combatManager.TryUseDiceFromDrag(slotIndex, inferredTarget);
            }
        }

        if (combatManager != null)
        {
            combatManager.RefreshDiceSlotVisuals();
        }
    }

    public void RestoreToSlotPosition()
    {
        if (originalParent != null && transform.parent != originalParent)
        {
            transform.SetParent(originalParent, true);
            transform.SetSiblingIndex(originalSiblingIndex);
        }
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (combatManager != null)
        {
            combatManager.ShowDiceTooltip(slotIndex);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (combatManager != null)
        {
            combatManager.HideTooltip();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (combatManager == null) return;

        // Click chuột phải hoặc Double-Click chuột trái -> kích hoạt dùng ngay kỹ năng của viên xúc xắc này!
        if (eventData.button == PointerEventData.InputButton.Right || eventData.clickCount >= 2)
        {
            combatManager.UseDiceSlotImmediately(slotIndex);
        }
    }
}
