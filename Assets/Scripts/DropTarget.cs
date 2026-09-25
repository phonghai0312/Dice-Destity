using UnityEngine;
using UnityEngine.EventSystems;

public class DropTarget : MonoBehaviour, IDropHandler
{
    public enum TargetType { Player, Enemy }
    public TargetType targetType;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            DraggableDice dice = eventData.pointerDrag.GetComponent<DraggableDice>();
            if (dice != null)
            {
                Debug.Log($"[DropTarget] Dice dropped on {targetType}!");
                // Process drop logic here (to be implemented in next phases)
                
                // For now, just destroy the dice to simulate it being used
                Destroy(eventData.pointerDrag);
            }
        }
    }
}
