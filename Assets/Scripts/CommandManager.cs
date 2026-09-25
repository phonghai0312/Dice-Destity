using UnityEngine;
using System.Collections.Generic;

public class CommandManager : MonoBehaviour
{
    // A simple stack to represent Undo actions for Phase 2
    private Stack<string> commandStack = new Stack<string>();

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

    public void EndTurn()
    {
        Debug.Log("[CommandManager] Turn Ended. State transitions to EnemyExecution.");
        commandStack.Clear();
    }
}
