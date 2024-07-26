using System.Collections.Generic;
using Dalamud.Utility;
using OpenerCreator.Actions;

namespace OpenerCreator.Helpers;

internal class LoadedActions
{
    private readonly HashSet<int> wrongActionsIndex = [];
    private List<int> actions = []; // int instead of uint until c# has tagged unions
    private int currentAction = -1;
    public string Name = ""; // needs to be public for ImGui refs

    internal void UpdateCurrentAction(int i)
    {
        if (i >= 0 && i < actions.Count)
            currentAction = i;
    }

    internal bool IsCurrentActionAt(int i)
    {
        return i == currentAction && isCurrentValid();
    }

    internal bool IsWrongActionAt(int i)
    {
        return wrongActionsIndex.Contains(i);
    }

    internal void AddWrongActionAt(int i)
    {
        wrongActionsIndex.Add(i);
    }

    internal void ClearWrongActions()
    {
        wrongActionsIndex.Clear();
        currentAction = -1;
    }

    internal int ActionsCount()
    {
        return actions.Count;
    }

    internal int GetActionAt(int i)
    {
        return actions[i];
    }

    internal void AddAction(int action)
    {
        actions.Add(action);
    }

    internal void RemoveActionAt(int i)
    {
        actions.RemoveAt(i);
    }

    internal void InsertActionAt(int i, int action)
    {
        actions.Insert(i, action);
    }

    internal List<int> GetActionsByRef()
    {
        return actions;
    }

    internal void ClearActions()
    {
        actions.Clear();
        currentAction = -1;
    }

    internal void AddActionsByRef(List<int> l)
    {
        actions = l;
    }

    internal bool HasName()
    {
        return !Name.IsNullOrEmpty();
    }

    public bool HasTrueNorth()
    {
        return actions.Contains((int)PvEActions.TrueNorthId);
    }

    private bool isCurrentValid()
    {
        return currentAction >= 0 && currentAction < actions.Count;
    }
}
