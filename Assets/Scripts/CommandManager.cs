using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Bản ghi một lệnh sử dụng xúc xắc trong pha PlayerAction để phục vụ hoàn tác (Undo - GDD 2.2 & 3.4).
/// </summary>
[Serializable]
public struct CombatCommandRecord
{
    public string description;
    public int primaryDiceSlotIndex;
    public int extraConsumedSlotIndex;
    public EntityStateSnapshot playerBefore;
    public EntityStateSnapshot enemyBefore;
}

/// <summary>
/// CommandManager — Quản lý Command Stack cho phép Undo các bước kéo thả/sử dụng xúc xắc trước khi bấm End Turn.
/// </summary>
public class CommandManager : MonoBehaviour
{
    private readonly Stack<string> commandStack = new Stack<string>();
    private readonly Stack<CombatCommandRecord> actionRecords = new Stack<CombatCommandRecord>();

    public event Action<int> OnStackCountChanged;

    public int UndoCount => actionRecords.Count;

    public void RecordDiceAction(CombatCommandRecord record)
    {
        actionRecords.Push(record);
        commandStack.Push(record.description);
        Debug.Log($"[CommandManager] Đã ghi lệnh: {record.description} (Undo khả dụng: {actionRecords.Count})");
        OnStackCountChanged?.Invoke(actionRecords.Count);
    }

    public bool TryPopLastDiceAction(out CombatCommandRecord record)
    {
        if (actionRecords.Count > 0)
        {
            record = actionRecords.Pop();
            if (commandStack.Count > 0) commandStack.Pop();
            Debug.Log($"[CommandManager] Hoàn tác (Undo): {record.description} (Còn lại: {actionRecords.Count})");
            OnStackCountChanged?.Invoke(actionRecords.Count);
            return true;
        }

        record = default;
        return false;
    }

    public void ExecuteCommand(string command)
    {
        commandStack.Push(command);
        Debug.Log($"[CommandManager] Executed: {command}. Stack count: {commandStack.Count}");
    }

    public void UndoLastCommand()
    {
        if (commandStack.Count > 0)
        {
            string cmd = commandStack.Pop();
            Debug.Log($"[CommandManager] Undo: {cmd}. Stack count: {commandStack.Count}");
        }
        else
        {
            Debug.Log("[CommandManager] Nothing to undo.");
        }
    }

    public void ClearHistory()
    {
        commandStack.Clear();
        actionRecords.Clear();
        OnStackCountChanged?.Invoke(0);
    }

    public void EndTurn()
    {
        Debug.Log("[CommandManager] Turn Ended. State transitions to EnemyExecution.");
        ClearHistory();
    }
}
