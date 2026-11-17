using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// DropTarget — Vùng nhận thao tác Kéo thả (Drag & Drop) xúc xắc lên Player hoặc Enemy (GDD 2.2).
/// </summary>
public class DropTarget : MonoBehaviour, IDropHandler
{
    public enum TargetType { Player, Enemy }
    public TargetType targetType;
    [HideInInspector] public SimpleCombatManager combatManager;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        DraggableDice dice = eventData.pointerDrag.GetComponent<DraggableDice>();
        if (dice == null) return;

        dice.wasDroppedOnTarget = true;
        dice.RestoreToSlotPosition();

        SimpleCombatManager mgr = combatManager != null ? combatManager : dice.combatManager;
        if (mgr != null)
        {
            mgr.TryUseDiceFromDrag(dice.slotIndex, targetType);
        }
    }
}
